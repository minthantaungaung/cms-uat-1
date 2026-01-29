using aia_core.Model.AiaCrm;
using aia_core.Model.Mobile.Request;
using aia_core.Model.Mobile.Response;
using aia_core.Repository.Mobile;
using aia_core.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Services
{
    public interface IAiaCrmApiService
    {
        Task<CaseResponse> CreateCase(CaseRequest model);
    }

    public class AiaCrmApiService : IAiaCrmApiService
    {
        protected readonly IUnitOfWork<Entities.Context> unitOfWork;

        protected readonly IConfiguration config;

        public AiaCrmApiService(IUnitOfWork<Entities.Context> unitOfWork, IConfiguration config)
        {
            this.unitOfWork = unitOfWork;
            this.config = config;
        }

        #region "create case"
        public async Task<CaseResponse> CreateCase(CaseRequest model)
        {
            try
            {
               


                //Ya, for all coast policies including claim, servicing, underwriting, others
                int indexOfDash = model.PolicyInfo.PolicyNumber.IndexOf('-');
                if (indexOfDash != -1)
                {
                    var originalPolicyNumber = model.PolicyInfo.PolicyNumber;
                    model.PolicyInfo.PolicyNumber = model.PolicyInfo.PolicyNumber.Substring(indexOfDash + 1);

                    model.PolicyInfo.MasterPolicyNumber = originalPolicyNumber.Substring(0, 10);
                }


                string accessToken = await GetAccessToken();
                string encryptRequestBody = Encrypt(Newtonsoft.Json.JsonConvert.SerializeObject(model));
                Console.WriteLine(encryptRequestBody);
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, config["AiaCrm:baseUrl"]);

                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Headers.Add("Ocp-Apim-Subscription-Key", config["AiaCrm:Ocp-Apim-Subscription-Key"]);

                var content = new StringContent(encryptRequestBody, null, "text/plain");
                request.Content = content;

                var response = await client.SendAsync(request);

                Console.WriteLine($"AiaCrmApiService => CreateCaseResponse => {response?.StatusCode} {response?.Content?.ReadAsStringAsync()?.Result}");

                if (response.StatusCode == HttpStatusCode.OK)
                {

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"JSON Response:\n{jsonResponse}");

                    // Deserialize the JSON and extract the "data" value
                    var responseData = JsonConvert.DeserializeObject<CaseCreateResponseData>(jsonResponse);
                    string decryptResponse = Decrypt(responseData.data);
                    var res = JsonConvert.DeserializeObject<CaseResponse>(decryptResponse);
                    return res;
                }
                else
                {
                    CaseResponse tempErrorResponse = new CaseResponse();
                    tempErrorResponse.Code = "500";
                    tempErrorResponse.Message = "Internal server error";
                    return tempErrorResponse;
                }

               

            }
            catch (Exception ex)
            {
                Console.WriteLine($"AiaCrmApiService CreateCase Error | Ex message : {ex.Message} | Exception {ex}");
                //Utils.MobileErrorLog("AiaCrmApiService => GetAccessToken", "Exception", JsonConvert.SerializeObject(ex), "", "", unitOfWork);
            }

            //return null; TLS

            #region #workAround
            CaseResponse commonErrResponse = new CaseResponse();
            commonErrResponse.Code = "500";
            commonErrResponse.Message = "Internal server error";
            return commonErrResponse;
            #endregion
        }
        #endregion

        #region "access token"
        private async Task<string> GetAccessToken()
        {
            try
            {
                Console.WriteLine("AiaCrmApiService GetAccessToken");
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, config["AiaCrm:loginBaseUrl"] + "token");

                var collection = new List<KeyValuePair<string, string>>();
                collection.Add(new("client_id", config["AiaCrm:client_id"]));
                collection.Add(new("client_secret", config["AiaCrm:client_secret"]));
                collection.Add(new("grant_type", config["AiaCrm:grant_type"]));
                collection.Add(new("scope", config["AiaCrm:scope"]));

                var content = new FormUrlEncodedContent(collection);
                request.Content = content;

                var response = await client.SendAsync(request);
                if(response.StatusCode == HttpStatusCode.OK)
                {

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"AiaCrmApiService GetAccessToken - {jsonResponse}");

                    var accessToken = Newtonsoft.Json.JsonConvert.DeserializeObject<AccessTokenResponse>(jsonResponse)?.access_token;

                    //return Decrypt("P1dW2cMBmrxsSr42KgVoB3yNPrxnMWm30keVeyd2IL15rVXR4yvXKD0RSzG5SLko7gvYBb8yrOuF1SCg2rVuu9Jziw5ECdIwUtRD4jNm5x4=");

                    return accessToken;
                }
                else
                {
                    return "";
                }

            }
            catch (Exception ex)
            {
                Utils.MobileErrorLog("AiaCrmApiService => GetAccessToken", "Exception", JsonConvert.SerializeObject(ex), "", "", unitOfWork);
            }

            return "";
        }
        #endregion

        #region "encryption"
        private string Encrypt(string value)
        {
            return Encrypt<RijndaelManaged>(value, config["AiaCrm:encKey"]);
        }

        private string Encrypt<T>(string value, string encKey)
                where T : SymmetricAlgorithm, new()
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(encKey);
            byte[] vectorBytes = Encoding.UTF8.GetBytes(config["AiaCrm:vectorBytes"]);
            byte[] saltBytes = Encoding.UTF8.GetBytes(config["AiaCrm:saltBytes"]);
            byte[] valueBytes = Encoding.UTF8.GetBytes(value);

            byte[] encrypted;
            using (T cipher = new T())
            {
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000, HashAlgorithmName.SHA256); // changes

                cipher.BlockSize = 128;

                // Use KeyVault
                cipher.KeySize = 256;
                cipher.Mode = CipherMode.CBC;
                cipher.Padding = PaddingMode.PKCS7;
                cipher.Key = key.GetBytes(32); // changes
                cipher.IV = vectorBytes;

                using (ICryptoTransform encryptor = cipher.CreateEncryptor())
                {
                    using (MemoryStream to = new MemoryStream())
                    {
                        using (CryptoStream writer = new CryptoStream(to, encryptor, CryptoStreamMode.Write))
                        {
                            writer.Write(valueBytes, 0, valueBytes.Length);
                            writer.FlushFinalBlock();
                            encrypted = to.ToArray();
                        }
                    }
                }

                cipher.Clear();
            }

            return Convert.ToBase64String(encrypted);
        }
        
        private string Decrypt(string value)
        {
            return Decrypt(value, config["AiaCrm:encKey"]);
        }

        private string Decrypt(string value, string encKey)
           
        {

            byte[] passwordBytes = Encoding.UTF8.GetBytes(encKey);
            byte[] vectorBytes = Encoding.UTF8.GetBytes(config["AiaCrm:vectorBytes"]);
            byte[] saltBytes = Encoding.UTF8.GetBytes(config["AiaCrm:saltBytes"]);
            byte[] valueBytes = Convert.FromBase64String(value);

            string decrypted = "";

            using (var cipher = Aes.Create())
            {
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000, HashAlgorithmName.SHA256); // changes



                cipher.BlockSize = 128;
                cipher.KeySize = 256;
                cipher.Mode = CipherMode.CBC;
                cipher.Padding = PaddingMode.PKCS7;
                cipher.Key = key.GetBytes(32); // changes
                cipher.IV = vectorBytes;

                using (ICryptoTransform decryptor = cipher.CreateDecryptor())
                {
                    using (MemoryStream from = new MemoryStream(valueBytes))
                    {
                        using (CryptoStream reader = new CryptoStream(from, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(reader, Encoding.UTF8))
                            {
                                decrypted = sr.ReadToEnd();
                            }
                        }
                    }
                }
                cipher.Clear();
            }

            return decrypted;
        }

        #endregion
        
        private class AccessTokenResponse
        {
            public string token_type { get; set; }
            public int expires_in { get; set; }
            public int ext_expires_in { get; set; }
            public string access_token { get; set; }
        }


    }
}