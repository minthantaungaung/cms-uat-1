using aia_core.UnitOfWork;
using Azure;
using CsvHelper;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace aia_core.Services
{
    #region #cls-user
    public class OtakUserProfile
    {
        public string? firstName { get; set; }
        public string? lastName { get; set; }
        public string? mobilePhone { get; set; }
        public string? phoneNumber { get; set; }
        public string? secondEmail { get; set; }
        public string? login { get; set; }
        public string? email { get; set; }
    }
    public class OktaUserCredential
    {
        public OktaUserCredentialProvider? provider { get; set; }
    }
    public class OktaUserCredentialProvider
    {
        public string? type { get; set; }
        public string? name { get; set; }
    }
    public class OktaUserResponse
    {
        public string? id { get; set; }
        public string? status { get; set; }
        public DateTime? created { get; set; }
        public DateTime? activated { get; set; }
        public DateTime? statusChanged { get; set; }
        public DateTime? lastLogin { get; set; }
        public DateTime? lastUpdated { get; set; }
        public DateTime? passwordChanged { get; set; }
        public object? type { get; set; }
        public OtakUserProfile? profile { get; set; }
        public OktaUserCredential? credentials { get; set; }
        public OktaUserProfileLink? _links { get; set; }
    }
    public class OktaUserProfileLink
    {
        public OktaUserProfileSelf? self { get; set; }
    }
    public class OktaUserProfileSelf
    {
        public string? href { get; set; }
    }
    #endregion

    #region #cls-token
    public class OktaTokenResponse
    {
        public string? token_type { get; set; }
        public long? expires_in { get; set; }
        public string? access_token { get; set; }
        public string? scope { get; set; }
        public string? refresh_token { get; set; }
        public string? id_token { get; set; }
    }
    #endregion

    #region #cls-general
    public class OktaErrorResponse
    {
        public string? errorId { get; set; }
        public string? errorCode { get; set; }
        public string? errorSummary { get; set; }
        public string? errorLink { get; set; }
        public OktaErrorCause[]? errorCauses { get; set; }
    }
    public class OktaErrorCause
    {
        public string? errorSummary { get; set; }
    }
    public class OktaFactorResponse
    {
        public string? id { get; set; }
        public string? factorType { get; set; }
        public string? provider { get; set; }
        public string? status { get; set; }
        public DateTime? created { get; set; }
        public DateTime? lastUpdated { get; set; }
        public OtakUserProfile? profile { get; set; }
    }
    #endregion

    public interface IOktaService
    {
        Task<ResponseModel<List<OktaFactorResponse>>> ListEnrollFactors(string oktaUserId);
        Task<ResponseModel<OktaFactorResponse>> EnrollNewSMS(string phoneNumber, string oktaUserId);
        Task<ResponseModel<OktaFactorResponse>> UnenrollSMS(string factorId, string oktaUserId);
        
        Task<ResponseModel<OktaFactorResponse>> IssueFactorChallenge(string factorId, string oktaUserId, int? tokenLifetimeSeconds = null);
        Task<ResponseModel<OktaFactorResponse>> VerifyFactorChallenge(string factorId, string passCode, string oktaUserId);

        Task<ResponseModel<OktaUserResponse>> RegisterUser(string username, Model.Mobile.Request.RegisterRequest model);
        Task<ResponseModel<OktaUserResponse>> RegisterUserMigration(string username, Model.Mobile.Request.RegisterRequest model);
        Task<ResponseModel<OktaUserResponse>> GetUser(string oktaUserId);
        Task<ResponseModel<OktaUserResponse>> UpdateUser(string oktaUserId, string email, string phone, string? firstName = null, string? lastName = null);
        Task<ResponseModel<object>> ChangePassword(string oktaUserId, string currentPassword, string newPassword);
        Task<ResponseModel<object>> ResetPassword(string oktaUserId, string newPassword);
        Task<ResponseModel<object>> DeactivateUser(string oktaUserId);
        Task<ResponseModel<object>> SuspendUser(string oktaUserId);
        Task<ResponseModel<object>> UnsuspendUser(string oktaUserId);
        Task<ResponseModel<object>> DeleteUser(string oktaUserId);
        Task<ResponseModel<object>> RefreshToken(Model.Mobile.Request.RefreshTokenRequest model);

        
    }
    public class OktaService: IOktaService
    {
        private readonly IConfiguration config;
        private readonly IErrorCodeProvider errorCodeProvider;
        private readonly IUnitOfWork<Entities.Context> unitOfWork;
        private readonly IHostingEnvironment env;
        private readonly string baseUrl = "";
        private readonly string clientID = "";
        private readonly string groupID = "";
        private readonly string privateKeyFile = "";
        private readonly IServiceProvider serviceProvider;
        private readonly SigningCredentials signingCredentials;


        public OktaService(IHostingEnvironment env,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork, IServiceProvider serviceProvider) 
        {
            this.env = env;
            this.config = config;
            this.errorCodeProvider = errorCodeProvider;
            this.unitOfWork = unitOfWork;
            baseUrl = config["Okta:BaseUrl"]; 
            clientID = config["Okta:ClientID"];
            groupID = config["Okta:GroupID"];
            privateKeyFile = config["Okta:PrivateKeyFile"];

            this.serviceProvider = serviceProvider;

            try
            {
                var keyPath = Path.GetFullPath(Path.Combine($"{env.ContentRootPath}", "ssl", $"{privateKeyFile}"));
                var privateKey = System.IO.File.ReadAllText(keyPath);
                var privateKeyBlocks = privateKey.Split("-", StringSplitOptions.RemoveEmptyEntries);
                var privateKeyBytes = Convert.FromBase64String(privateKeyBlocks[1]);

                RSA rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

                signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
            }
            catch(Exception ex) 
            { Console.WriteLine($"OktaService => SigningCredentials => Ex {JsonConvert.SerializeObject(ex)}"); }
            
        }

        /// <summary>
        /// generate sign jwt using private key
        /// </summary>
        /// <returns></returns>
        #region #sign-jwt
        private string SignJWT()
        {
            try
            {
                
                Console.WriteLine($"OktaService => SignJWT okta service ia m here");

                DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow.AddMinutes(5);
                long unixEpoch = dateTimeOffset.ToUnixTimeSeconds();

                Console.WriteLine($"SignJWT => DateTimeOffset.UtcNow.AddHours(1) => " +
                    $"unixEpoch {unixEpoch}, Equivalent => {dateTimeOffset.DateTime.ToString()}");

                var claims = new System.Security.Claims.Claim[]
                            {
                                new System.Security.Claims.Claim("aud", $"{config["okta:JwtSignUrl"]}"),
                                new System.Security.Claims.Claim("sub", clientID),
                                //new System.Security.Claims.Claim("jti", clientID),
                                new System.Security.Claims.Claim("exp", $"{unixEpoch}"),

                            };
                var token = new JwtSecurityToken(
                    issuer: clientID,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(30),
                    signingCredentials: signingCredentials
                );

                var tokenHandler = new JwtSecurityTokenHandler();
                string jwt = tokenHandler.WriteToken(token);

                

                return jwt;

            }
            catch (Exception ex)
            {
                
                Console.Error.WriteLine($"Okta.SignJWT.Exception => {ex?.Message} " +
                    $", JsonConvert.SerializeObject(ex) => {JsonConvert.SerializeObject(ex)}, InnerException => {ex?.InnerException?.Message}");
                throw ex;
            }
        }
        

        

        #endregion

        /// <summary>
        /// exchange service token to okta
        /// </summary>
        /// <returns></returns>
        #region #generate-token
        private async Task<Entities.OktaServiceToken> GenerateToken()
        {
            try
            {
                Console.WriteLine($"OktaService => GenerateToken okta service ia m here");
                    

                    

                    var entity = await unitOfWork.GetRepository<Entities.OktaServiceToken>().Query().FirstOrDefaultAsync();

                    using (HttpClient client = new HttpClient())
                    {
                        var clientAssertion = SignJWT();

                        Console.WriteLine($"client_assertion token => {clientAssertion}");

                    var content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "client_assertion", clientAssertion },
                        { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                        { "grant_type", "client_credentials" },
                        { "scope", "okta.users.read okta.users.manage" }
                    });

                        content.Headers.Clear();
                        content.Headers.Add("content-type", "application/x-www-form-urlencoded");
                        var response = await client.PostAsync($"{config["Okta:JwtSignUrl"]}", content);

                        string responseContent = await response.Content.ReadAsStringAsync();
                        //Utils.MobileErrorLog($"OktaService => GenerateToken", "okta service ia m here 123", "", "", "", unitOfWork);
                        //Utils.CmsErrorLog($"OktaService => GenerateToken", "okta service ia m here 123", "", "", "", unitOfWork);
                        Console.WriteLine($"OktaService => GenerateToken okta service ia m here 123");

                        if (response.IsSuccessStatusCode)
                        {

                            Console.WriteLine($"Okta.GenerateOktaJWTToken => {responseContent}");
                            var result = System.Text.Json.JsonSerializer.Deserialize<OktaTokenResponse>(responseContent);
                            if (entity == null)
                            {
                                entity = new Entities.OktaServiceToken
                                {
                                    Id = "x",
                                    TokenType = result.token_type,
                                    AccessToken = result.access_token,
                                    ExpiresIn = result.expires_in,
                                    Scope = result.scope,
                                    CreatedDate = Utils.GetDefaultDate(),
                                };
                                await unitOfWork.GetRepository<Entities.OktaServiceToken>().AddAsync(entity);
                            }
                            else
                            {
                                entity.TokenType = result.token_type;
                                entity.AccessToken = result.access_token;
                                entity.ExpiresIn = result.expires_in;
                                entity.Scope = result.scope;
                                entity.UpdatedDate = Utils.GetDefaultDate();
                            }
                            await unitOfWork.SaveChangesAsync();
                        }
                        else
                        {
                            //string responseContent = await response.Content.ReadAsStringAsync();
                            Console.Error.WriteLine($"Okta.GenerateOktaJWTToken => exchange service token fail: {responseContent} {response.StatusCode}");
                            throw new Exception($"exchange service token fail: {response.StatusCode}");
                        }
                    }
                    return entity;
                

                
            }
            catch (Exception ex)
            {
                //Utils.MobileErrorLog($"GenerateToken exception", ex?.Message, JsonConvert.SerializeObject(ex), "", "", unitOfWork);
                //Utils.CmsErrorLog($"GenerateToken exception", ex?.Message, JsonConvert.SerializeObject(ex), "", "", unitOfWork);

                Console.Error.WriteLine($"Okta.GenerateToken.Exception => {ex?.Message} {ex?.InnerException?.Message}");
                throw ex;
            }
        }
        #endregion

        private async Task<Entities.OktaServiceToken> GetToken()
        {
            try
            {
                Console.WriteLine($"GetToken()");
                var oktaToken = await unitOfWork.GetRepository<Entities.OktaServiceToken>().Query().FirstOrDefaultAsync();

                Console.WriteLine($"GetToken() => oktaToken => {JsonConvert.SerializeObject(oktaToken)}");
                if (oktaToken == null) oktaToken = await GenerateToken();

                Console.WriteLine($"GetToken() => GenerateToken() => oktaToken => {oktaToken}");
                return oktaToken;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// list user enrolled factors
        /// </summary>
        /// <param name="oktaUserId"></param>
        /// <returns></returns>
        #region #list-enrolled-factors
        public async Task<ResponseModel<List<OktaFactorResponse>>> ListEnrollFactors(string oktaUserId)
        {
            try 
            {
                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token?.TokenType ?? "Bearer", token?.AccessToken ?? "");

                    if (IsValidOktaUserId(oktaUserId))
                    {
                        var response = await client.GetAsync($"{baseUrl}/users/{oktaUserId}/factors");
                        string responseContent = await response.Content.ReadAsStringAsync();

                        Utils.MobileErrorLog($"OktaService => ListEnrollFactors {oktaUserId}", JsonConvert.SerializeObject(response.StatusCode), responseContent, "", "", unitOfWork);
                        Utils.CmsErrorLog($"OktaService => ListEnrollFactors {oktaUserId}", JsonConvert.SerializeObject(response.StatusCode), responseContent, "", "", unitOfWork);


                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return errorCodeProvider.GetResponseModel<List<OktaFactorResponse>>(ErrorCode.E0, System.Text.Json.JsonSerializer.Deserialize<List<OktaFactorResponse>>(responseContent));
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            token = await GenerateToken();
                            goto reloadOktaApi;
                        }
                        else
                        {

                            if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<List<OktaFactorResponse>>(ErrorCode.E400);

                            Console.WriteLine($"Okta.ListEnrollFactors.{response.StatusCode} => {responseContent}");
                            var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                            return new ResponseModel<List<OktaFactorResponse>>
                            {
                                Code = (int)ErrorCode.E400,
                                //Message = error.errorSummary //string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                                Message = error != null ? error.errorCauses.Count() > 0 ? error.errorCauses.FirstOrDefault().errorSummary : error.errorSummary : ""
                            };
                        }
                    }
                    else
                    {
                        return new ResponseModel<List<OktaFactorResponse>>
                        {
                            Code = (int)ErrorCode.E400
                        };
                    }
                }
            }
            catch(Exception ex) 
            {
                Utils.MobileErrorLog($"OktaService => ListEnrollFactors {oktaUserId}", ex.Message, JsonConvert.SerializeObject(ex), "", "", unitOfWork);

                Console.Error.WriteLine($"Okta.ListEnrollFactors.Exception => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<List<OktaFactorResponse>>(ErrorCode.E500);
            }
        }
        #endregion

        /// <summary>
        /// enroll new phone sms and request pass code
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="oktaUserId"></param>
        /// <returns></returns>
        #region #enroll-new-sms
        public async Task<ResponseModel<OktaFactorResponse>> EnrollNewSMS(string phoneNumber, string oktaUserId)
        {
            try
            {
                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    var payload = new
                    {
                        factorType = "sms",
                        provider = "OKTA",
                        profile = new
                        {
                            phoneNumber = phoneNumber
                        }
                    };
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
                    var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    if (IsValidOktaUserId(oktaUserId))
                    {
                        var response = await client.PostAsync($"{baseUrl}/users/{oktaUserId}/factors?activate=true&updatePhone=true", content);

                        string responseContent = await response.Content.ReadAsStringAsync();
                        Utils.MobileErrorLog($"OktaService => EnrollNewSMS {phoneNumber} | {oktaUserId}", JsonConvert.SerializeObject(response.StatusCode), responseContent, "", "", unitOfWork);
                        Utils.CmsErrorLog($"OktaService => EnrollNewSMS {phoneNumber} | {oktaUserId}", JsonConvert.SerializeObject(response.StatusCode), responseContent, "", "", unitOfWork);


                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var data = System.Text.Json.JsonSerializer.Deserialize<OktaFactorResponse>(responseContent);
                            return errorCodeProvider.GetResponseModel<OktaFactorResponse>(ErrorCode.E0, data);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            token = await GenerateToken();
                            goto reloadOktaApi;
                        }
                        else
                        {

                            if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<OktaFactorResponse>(ErrorCode.E400);

                            Console.WriteLine($"Okta.EnrollNewSMS.{response.StatusCode} => {responseContent}");
                            var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                            return new ResponseModel<OktaFactorResponse>
                            {
                                Code = (int)ErrorCode.E400,
                                //Message = error.errorSummary //string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                                Message = error != null ? error.errorCauses.Count() > 0 ? error.errorCauses.FirstOrDefault().errorSummary : error.errorSummary : ""
                            };
                        }
                    }
                    else
                    {
                        return new ResponseModel<OktaFactorResponse>
                        {
                            Code = (int)ErrorCode.E400
                        };
                    }
                }

                #region defer-restsharp
                //var options = new RestClientOptions("https://aia-mm-sit.okta.com/api/v1");
                //var request = new RestRequest($"users/{oktaUserId}/factors?activate=true&updatePhone=true", Method.Post);
                //request.AddHeader("Accept", "application/json");
                //request.AddHeader("Content-Type", "application/json");
                //request.AddHeader("Authorization", $"{token.TokenType} {token.AccessToken}");
                //var payload = new 
                //{
                //    factorType = "sms",
                //    provider = "OKTA",
                //    profile = new 
                //    {
                //        phoneNumber = phoneNumber
                //    }
                //};
                //request.AddJsonBody(System.Text.Json.JsonSerializer.Serialize(payload));

                //var client = new RestClient(options);
                //var result = await client.ExecuteAsync(request);
                //if (result.StatusCode == System.Net.HttpStatusCode.OK)
                //{
                //    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, result.Content);
                //}
                //else if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                //{
                //    token = await GenerateToken();
                //    goto reloadOktaApi;
                //}
                //else
                //{
                //    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);
                //}
                #endregion
            }
            catch (Exception ex)
            {
                Utils.MobileErrorLog($"OktaService => EnrollNewSMS {phoneNumber} | {oktaUserId}", ex.Message, JsonConvert.SerializeObject(ex), "", "", unitOfWork);
                Console.Error.WriteLine($"Okta.EnrollNewSMS.Exception => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<OktaFactorResponse>(ErrorCode.E500);
            }
        }
        #endregion

        /// <summary>
        /// unenroll email or sms factor
        /// </summary>
        /// <param name="factorId"></param>
        /// <param name="oktaUserId"></param>
        /// <returns></returns>
        #region #unenroll-existing-sms
        public async Task<ResponseModel<OktaFactorResponse>> UnenrollSMS(string factorId, string oktaUserId)
        {
            try
            {
                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token?.TokenType ?? "Bearer", token?.AccessToken ?? "");

                    if (IsValidOktaUserId(oktaUserId))
                    {
                        var response = await client.DeleteAsync($"{baseUrl}/users/{oktaUserId}/factors/{factorId}?removeRecoveryEnrollment=true");
                        string responseContent = await response.Content.ReadAsStringAsync();

                        Utils.MobileErrorLog($"OktaService => UnenrollSMS {factorId} | {oktaUserId}", JsonConvert.SerializeObject(response.StatusCode), responseContent, "", "", unitOfWork);


                        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                        {
                            return errorCodeProvider.GetResponseModel<OktaFactorResponse>(ErrorCode.E0);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            token = await GenerateToken();
                            goto reloadOktaApi;
                        }
                        else
                        {

                            if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<OktaFactorResponse>(ErrorCode.E400);

                            Console.WriteLine($"Okta.UnenrollSMS.{response.StatusCode} => {responseContent}");
                            var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                            return new ResponseModel<OktaFactorResponse>
                            {
                                Code = (int)ErrorCode.E400,
                                //Message = error.errorSummary //string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                                Message = error != null ? error.errorCauses.Count() > 0 ? error.errorCauses.FirstOrDefault().errorSummary : error.errorSummary : ""
                            };
                        }
                    }
                    else
                    {
                        return new ResponseModel<OktaFactorResponse>
                        {
                            Code = (int)ErrorCode.E400
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.MobileErrorLog($"OktaService => UnenrollSMS {factorId} | {oktaUserId}", ex.Message, JsonConvert.SerializeObject(ex), "", "", unitOfWork);
                Console.Error.WriteLine($"Okta.UnenrollSMS.Exception => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<OktaFactorResponse>(ErrorCode.E500);
            }
        }
        #endregion

        /// <summary>
        /// issue factor sms or email
        /// </summary>
        /// <param name="factorId"></param>
        /// <param name="oktaUserId"></param>
        /// <param name="tokenLifetimeSeconds"></param>
        /// <returns></returns>
        #region #issue-factor-challenge
        public async Task<ResponseModel<OktaFactorResponse>> IssueFactorChallenge(string factorId, string oktaUserId, int? tokenLifetimeSeconds = null)
        {
            try
            {
                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("my"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);

                    if (IsValidOktaUserId(oktaUserId) || IsValidOktaUserId(factorId))
                    {
                        var response = await client.PostAsync($"{baseUrl}/users/{oktaUserId}/factors/{factorId}/verify", null);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            return errorCodeProvider.GetResponseModel<OktaFactorResponse>(ErrorCode.E0, System.Text.Json.JsonSerializer.Deserialize<OktaFactorResponse>(responseContent));
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            token = await GenerateToken();
                            goto reloadOktaApi;
                        }
                        else
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<OktaFactorResponse>(ErrorCode.E400);

                            Console.WriteLine($"Okta.IssueFactorChallenge.{response.StatusCode} => {responseContent}");
                            var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                            return new ResponseModel<OktaFactorResponse>
                            {
                                Code = (int)ErrorCode.E400,
                                Message = string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                            };
                        }
                    }
                    else
                    {
                        return new ResponseModel<OktaFactorResponse>
                        {
                            Code = (int)ErrorCode.E400
                        };
                    }
                }

                #region #defer-restsharp
                //var options = new RestClientOptions("https://aia-mm-sit.okta.com/api/v1");
                //var request = new RestRequest($"users/{oktaUserId}/factors/{factorId}/verify", Method.Post);
                //request.AddHeader("Accept", "application/json");
                //request.AddHeader("Content-Type", "application/json");
                //request.AddHeader("Authorization", $"{token.TokenType} {token.AccessToken}");

                //var client = new RestClient(options);
                //var result = await client.ExecuteAsync(request);
                //if (result.StatusCode == System.Net.HttpStatusCode.OK)
                //{
                //    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, result.Content);
                //}
                //else if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                //{
                //    token = await GenerateToken();
                //    goto reloadOktaApi;
                //}
                //else
                //{
                //    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);
                //}
                #endregion
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Okta.IssueFactorChallenge.Exception => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<OktaFactorResponse>(ErrorCode.E500);
            }
        }
        #endregion

        /// <summary>
        /// verify factor sms or email
        /// </summary>
        /// <param name="factorId"></param>
        /// <param name="passCode"></param>
        /// <param name="oktaUserId"></param>
        /// <returns></returns>
        #region #verify-factor-challenge
        public async Task<ResponseModel<OktaFactorResponse>> VerifyFactorChallenge(string factorId, string passCode, string oktaUserId)
        {
            try
            {
                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    var payload = new
                    {
                        passCode = passCode
                    };

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
                    var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    if (IsValidOktaUserId(oktaUserId) || IsValidOktaUserId(factorId))
                    {

                        var response = await client.PostAsync($"{baseUrl}/users/{oktaUserId}/factors/{factorId}/verify", content);

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            return errorCodeProvider.GetResponseModel<OktaFactorResponse>(ErrorCode.E0, System.Text.Json.JsonSerializer.Deserialize<OktaFactorResponse>(responseContent));
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            token = await GenerateToken();
                            goto reloadOktaApi;
                        }
                        else
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<OktaFactorResponse>(ErrorCode.E400);

                            Console.WriteLine($"Okta.VerifyFactorChallenge.{response.StatusCode} => {responseContent}");
                            var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                            return new ResponseModel<OktaFactorResponse>
                            {
                                Code = (int)ErrorCode.E400,
                                Message = string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                            };
                        }
                    }
                    else
                    {
                        return new ResponseModel<OktaFactorResponse>
                        {
                            Code = (int)ErrorCode.E400
                        };
                    }
                }

                #region #defer-restsharp
                //var options = new RestClientOptions("https://aia-mm-sit.okta.com/api/v1");
                //var request = new RestRequest($"users/{oktaUserId}/factors/{factorId}/verify", Method.Post);
                //request.AddHeader("Accept", "application/json");
                //request.AddHeader("Content-Type", "application/json");
                //request.AddHeader("Authorization", $"{token.TokenType} {token.AccessToken}");
                //var payload = new
                //{
                //    passCode = passCode
                //};
                //request.AddJsonBody(System.Text.Json.JsonSerializer.Serialize(payload));

                //var client = new RestClient(options);
                //var result = await client.ExecuteAsync(request);
                //if (result.StatusCode == System.Net.HttpStatusCode.OK)
                //{
                //    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, result.Content);
                //}
                //else if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                //{
                //    token = await GenerateToken();
                //    goto reloadOktaApi;
                //}
                //else
                //{
                //    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);
                //}
                #endregion
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Okta.VerifyFactorChallenge.Exception => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<OktaFactorResponse>(ErrorCode.E500);
            }
        }
        #endregion


        /// <summary>
        /// create user account
        /// </summary>
        /// <param name="username"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        #region #register-user
        public async Task<ResponseModel<OktaUserResponse>> RegisterUserMigration(string username, Model.Mobile.Request.RegisterRequest model)
        {
            try
            {
                string algorithmValue = config["Okta:Algorithm"];
                // string workFactorValue = config["Okta:WorkFactor"];
                // string saltValue = config["Okta:Salt"];

                string[] parts = model.Password.Split('$');

                int workFactorValue = int.Parse(parts[2]);
                string saltValue = parts[3].Substring(0, 22);
                string value = parts[3].Substring(22);

                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    var payload = new
                    {
                        profile = new
                        {
                            firstName = model.FullName,
                            lastName = model.FullName,
                            email = model.Email,
                            mobilePhone = model.Phone,
                            login = username,
                            locale = "my_MM",
                        },
                        credentials = new
                        {
                            //password = new { value = model.ConfirmPassword },
                            password = new
                            {
                                hash = new
                                {
                                    algorithm = algorithmValue,
                                    workFactor = workFactorValue,
                                    salt = saltValue,
                                    value = value
                                }
                            },
                            provider = new { type = "OKTA", name = "OKTA" }
                        },
                        groupIds = new string[] { groupID }
                    };

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
                    var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"{baseUrl}/users", content);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        var data = System.Text.Json.JsonSerializer.Deserialize<OktaUserResponse>(responseContent);
                        
                        var enrollSms = await this.EnrollNewSMS(Utils.ReferenceNumber(model.Phone), data.id);
                        if(enrollSms?.Code == (long)ErrorCode.E0)
                            return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E0, data);

                        return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E400);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        token = await GenerateToken();
                        goto reloadOktaApi;
                    }
                    else
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E400);

                        Console.WriteLine($"Okta.RegisterUser.{response.StatusCode} => {responseContent}");
                        var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                        return new ResponseModel<OktaUserResponse>
                        {
                            Code = (int)ErrorCode.E400,
                            Message = string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                        };
                    }
                }
            }
            catch(Exception ex) 
            {
                Console.Error.WriteLine($"Okta.RegisterUser.Exception => {ex?.Message} | {ex}");
                return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<OktaUserResponse>> RegisterUser(string username, Model.Mobile.Request.RegisterRequest model)
        {
            try
            {
                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    var payload = new
                    {
                        profile = new
                        {
                            firstName = model.FullName,
                            lastName = model.FullName,
                            email = model.Email,
                            mobilePhone = model.Phone,
                            login = username,
                            locale = "my_MM",
                        },
                        credentials = new
                        {
                            password = new { value = model.ConfirmPassword },
                            provider = new { type = "OKTA", name = "OKTA" }
                        },
                        groupIds = new string[] { groupID }
                    };

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
                    var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    Console.WriteLine($"Okta.RegisterUser {username} Request => {JsonConvert.SerializeObject(payload)}");


                    var response = await client.PostAsync($"{baseUrl}/users", content);



                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();

                        Console.WriteLine($"Okta.RegisterUser {username} Response => {response.StatusCode} {responseContent}");

                        var data = System.Text.Json.JsonSerializer.Deserialize<OktaUserResponse>(responseContent);
                        
                        var enrollSms = await this.EnrollNewSMS(Utils.ReferenceNumber(model.Phone), data.id);
                        if(enrollSms?.Code == (long)ErrorCode.E0)
                            return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E0, data);

                        return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E400);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.WriteLine($"Okta.RegisterUser {username} Response => {response.StatusCode}");

                        token = await GenerateToken();
                        goto reloadOktaApi;
                    }
                    else
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();

                        Console.WriteLine($"Okta.RegisterUser {username} Response => {response.StatusCode} {responseContent}");

                        if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E400);

                        Console.WriteLine($"Okta.RegisterUser. {model.IdentificationValue} {model.IdentificationType} {response.StatusCode} => {responseContent}");
                        var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                        return new ResponseModel<OktaUserResponse>
                        {
                            Code = (int)ErrorCode.E400,
                            Message = string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                        };
                    }
                }
            }
            catch(Exception ex) 
            {
                Console.Error.WriteLine($"Okta.RegisterUser {username} Exception => {model.IdentificationValue} {model.IdentificationType} {ex?.Message} | {JsonConvert.SerializeObject(ex)}");
                return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E500);
            }
        }
        #endregion

        /// <summary>
        /// get user profile
        /// </summary>
        /// <param name="oktaUserId"></param>
        /// <returns></returns>
        #region #get-user
        public async Task<ResponseModel<OktaUserResponse>> GetUser(string oktaUserId)
        {
            try
            {
                var token = await GetToken();
                

            reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token?.TokenType ?? "Bearer", token?.AccessToken ?? "");

                    if (IsValidOktaUserId(oktaUserId))
                    {
                        var response = await client.GetAsync($"{baseUrl}/users/{oktaUserId}");
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E0, System.Text.Json.JsonSerializer.Deserialize<OktaUserResponse>(responseContent));
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            token = await GenerateToken();
                            goto reloadOktaApi;
                        }
                        else
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E400);

                            Console.WriteLine($"Okta.RegisterUser.{response.StatusCode} => {responseContent}");
                            var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                            return new ResponseModel<OktaUserResponse>
                            {
                                Code = (int)ErrorCode.E400,
                                Message = string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                            };
                        }
                    }
                    else
                    {
                        return new ResponseModel<OktaUserResponse>
                        {
                            Code = (int)ErrorCode.E400
                        };
                    }
                }
                return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E400);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Okta.GetUser.Exception => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E500);
            }
        }
        #endregion

        /// <summary>
        /// update user profile email, phone
        /// </summary>
        /// <param name="oktaUserId"></param>
        /// <param name="email"></param>
        /// <param name="phone"></param>
        /// <returns></returns>
        #region #update-email-phone
        public async Task<ResponseModel<OktaUserResponse>> UpdateUser(string oktaUserId, string email, string phone, string? firstName = null, string? lastName = null)
        {
            try 
            {
                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    var payload = new 
                    {
                        profile = new
                        {
                            email = email,
                            mobilePhone = phone,
                            firstName = firstName,
                            lastName = lastName
                        }
                    };

                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token?.TokenType ?? "Bearer", token?.AccessToken ?? "");
                    var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    if (IsValidOktaUserId(oktaUserId))
                    {
                        var response = await client.PostAsync($"{baseUrl}/users/{oktaUserId}", content);
                        string responseContent = await response.Content.ReadAsStringAsync();

                        Utils.MobileErrorLog($"OktaService => UpdateUser {email} | {phone}", JsonConvert.SerializeObject(response.StatusCode), responseContent, "", "", unitOfWork);
                        Utils.CmsErrorLog($"OktaService => UpdateUser {email} | {phone}", JsonConvert.SerializeObject(response.StatusCode), responseContent, "", "", unitOfWork);

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {

                            return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E0);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            Utils.MobileErrorLog($"OktaService => UpdateUser {email} | {phone}", "okta service ia m here ", responseContent, "", "", unitOfWork);
                            Utils.CmsErrorLog($"OktaService => UpdateUser {email} | {phone}", "okta service ia m here ", responseContent, "", "", unitOfWork);

                            token = await GenerateToken();
                            goto reloadOktaApi;
                        }
                        else
                        {

                            Utils.MobileErrorLog($"OktaService => UpdateUser {email} | {phone}", "okta service ia m here 123 ", responseContent, "", "", unitOfWork);
                            Utils.CmsErrorLog($"OktaService => UpdateUser {email} | {phone}", "okta service ia m here 123", responseContent, "", "", unitOfWork);


                            if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E400);

                            Console.WriteLine($"Okta.RegisterUser.{response.StatusCode} => {responseContent}");
                            var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                            return new ResponseModel<OktaUserResponse>
                            {
                                Code = (int)ErrorCode.E400,
                                //Message = error.errorSummary //string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                                Message = error != null ? error.errorCauses.Count() > 0 ? error.errorCauses.FirstOrDefault().errorSummary : error.errorSummary : ""
                            };
                        }
                    }
                    else
                    {
                        return new ResponseModel<OktaUserResponse>
                        {
                            Code = (int)ErrorCode.E400
                        };
                    }
                }
                return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E400);
            }
            catch(Exception ex)
            {
                Utils.MobileErrorLog($"OktaService => UpdateUser {email} | {phone}", ex.Message, JsonConvert.SerializeObject(ex), "", "", unitOfWork);
                Console.Error.WriteLine($"Okta.UpdateUser.Exception => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<OktaUserResponse>(ErrorCode.E500);
            }
        }
        #endregion

        /// <summary>
        /// change user password
        /// </summary>
        /// <param name="oktaUserId"></param>
        /// <param name="currentPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        #region #change-password
        public async Task<ResponseModel<object>> ChangePassword(string oktaUserId, string currentPassword, string newPassword)
        {
            try
            {
                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    var payload = new
                    {
                        oldPassword = new
                        {
                            value = currentPassword
                        },
                        newPassword = new
                        {
                            value = newPassword
                        },
                        revokeSessions = true
                    };
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
                    var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    if (IsValidOktaUserId(oktaUserId))
                    {
                        Console.WriteLine($"Okta.ChangePassword.Request Url => {baseUrl}/users/{oktaUserId}/credentials/change_password?strict=true" +
                            $"Json =>  {JsonConvert.SerializeObject(payload)}");

                        var response = await client.PostAsync($"{baseUrl}/users/{oktaUserId}/credentials/change_password?strict=true", content);

                        

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();

                            Console.WriteLine($"Okta.ChangePassword.Response => {response.StatusCode} => {responseContent}");
                            return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            token = await GenerateToken();
                            goto reloadOktaApi;
                        }
                        else
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);

                            Console.WriteLine($"Okta.ChangePassword.{response.StatusCode} => {responseContent}");
                            var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                            return new ResponseModel<object>
                            {
                                Code = (int)ErrorCode.E400,
                                Message = string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                            };
                        }
                    }
                    else
                    {
                        return new ResponseModel<object>
                        {
                            Code = (int)ErrorCode.E400
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Okta.ChangePassword.Exception => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion

        /// <summary>
        /// reset user password
        /// </summary>
        /// <param name="oktaUserId"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        #region #reset-password
        public async Task<ResponseModel<object>> ResetPassword(string oktaUserId, string newPassword)
        {
            try
            {
                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    var payload = new
                    {
                        credentials = new
                        {
                            password = new 
                            {
                                value = newPassword
                            }
                        }
                    };
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
                    var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    if (IsValidOktaUserId(oktaUserId))
                    {

                        #region #RequestLog
                        
                        var resetUrl = $"{baseUrl}/users/{oktaUserId}?strict=true";
                        var resetRequestJson = JsonConvert.SerializeObject(payload);
                        Console.WriteLine($"Okta.ResetPassword Request => {oktaUserId} {resetUrl} {resetRequestJson} {Utils.GetDefaultDate()}");
                        
                        #endregion


                        var response = await client.PostAsync($"{baseUrl}/users/{oktaUserId}?strict=true", content);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {

                            

                            string responseContent = await response.Content.ReadAsStringAsync();

                            Console.WriteLine($"Okta.ResetPassword Response => Success => oktaUserId => {oktaUserId} {Utils.GetDefaultDate()}" +
                                $"response.StatusCode => {response.StatusCode} responseContent => {responseContent}");

                            return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            token = await GenerateToken();
                            goto reloadOktaApi;
                        }
                        else
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);

                            Console.WriteLine($"Okta.ResetPassword Response {oktaUserId} {response.StatusCode} => {responseContent}");
                            var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                            return new ResponseModel<object>
                            {
                                Code = (int)ErrorCode.E400,
                                Message = string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                            };
                        }
                    }
                    else
                    {
                        return new ResponseModel<object>
                        {
                            Code = (int)ErrorCode.E400
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Okta.ResetPassword.Exception => {oktaUserId} {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion

        /// <summary>
        /// deactivate user
        /// </summary>
        /// <param name="oktaUserId"></param>
        /// <returns></returns>
        #region #deactivate-user
        public async Task<ResponseModel<object>> DeactivateUser(string oktaUserId)
        {
            try
            {
                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token?.TokenType ?? "Bearer", token?.AccessToken ?? "");

                    if (IsValidOktaUserId(oktaUserId))
                    {

                        var response = await client.PostAsync($"{baseUrl}/users/{oktaUserId}/lifecycle/deactivate?sendEmail=false", null);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            token = await GenerateToken();
                            goto reloadOktaApi;
                        }
                        else
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);

                            Console.WriteLine($"Okta.DeactivateUser.{response.StatusCode} => {responseContent}");
                            var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                            return new ResponseModel<object>
                            {
                                Code = (int)ErrorCode.E400,
                                Message = string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? "fail"
                            };
                        }
                    }
                    else
                    {
                        return new ResponseModel<object>
                        {
                            Code = (int)ErrorCode.E400
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Okta.DeactivateUser.Exception => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion

        /// <summary>
        /// suspended user
        /// </summary>
        /// <param name="oktaUserId"></param>
        /// <returns></returns>
        #region #suspendUser-user
        public async Task<ResponseModel<object>> SuspendUser(string oktaUserId)
        {
            try
            {
                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token?.TokenType ?? "Bearer", token?.AccessToken ?? "");

                    if (IsValidOktaUserId(oktaUserId))
                    {
                        var response = await client.PostAsync($"{baseUrl}/users/{oktaUserId}/lifecycle/suspend", null);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            token = await GenerateToken();
                            goto reloadOktaApi;
                        }
                        else
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);

                            Console.WriteLine($"Okta.SuspendUser.{response.StatusCode} => {responseContent}");
                            var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                            return new ResponseModel<object>
                            {
                                Code = (int)ErrorCode.E400,
                                Message = string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                            };
                        }
                    }
                    else
                    {
                        return new ResponseModel<object>
                        {
                            Code = (int)ErrorCode.E400
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Okta.SuspendUser.Exception => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion

        /// <summary>
        /// unsuspended user
        /// </summary>
        /// <param name="oktaUserId"></param>
        /// <returns></returns>
        #region #unsuspendUser-user
        public async Task<ResponseModel<object>> UnsuspendUser(string oktaUserId)
        {
            try
            {
                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token?.TokenType ?? "Bearer", token?.AccessToken ?? "");

                    if (IsValidOktaUserId(oktaUserId))
                    {
                        var response = await client.PostAsync($"{baseUrl}/users/{oktaUserId}/lifecycle/unsuspend", null);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            token = await GenerateToken();
                            goto reloadOktaApi;
                        }
                        else
                        {

                            string responseContent = await response.Content.ReadAsStringAsync();

                            Utils.MobileErrorLog($"OktaService => UnsuspendUser {oktaUserId}", JsonConvert.SerializeObject(response.StatusCode), responseContent, "", "", unitOfWork);
                            Utils.CmsErrorLog($"OktaService => UnsuspendUser {oktaUserId}", JsonConvert.SerializeObject(response.StatusCode), responseContent, "", "", unitOfWork);


                            if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);

                            Console.WriteLine($"Okta.UnsuspendUser.{response.StatusCode} => {responseContent}");
                            var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                            return new ResponseModel<object>
                            {
                                Code = (int)ErrorCode.E400,
                                Message = string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                            };
                        }
                    }
                    else
                    {
                        return new ResponseModel<object>
                        {
                            Code = (int)ErrorCode.E400
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.MobileErrorLog($"OktaService => UnsuspendUser {oktaUserId}", JsonConvert.SerializeObject(ex), ex.Message, "", "", unitOfWork);
                Utils.CmsErrorLog($"OktaService => UnsuspendUser {oktaUserId}", JsonConvert.SerializeObject(ex), ex.Message, "", "", unitOfWork);


                Console.Error.WriteLine($"Okta.UnsuspendUser.Exception => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion

        /// <summary>
        /// delete user
        /// </summary>
        /// <param name="oktaUserId"></param>
        /// <returns></returns>
        #region #delete-user
        public async Task<ResponseModel<object>> DeleteUser(string oktaUserId)
        {
            try
            {
                var token = await GetToken();

                reloadOktaApi:
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token?.TokenType ?? "Bearer", token?.AccessToken ?? "");

                    if (IsValidOktaUserId(oktaUserId))
                    {
                        Console.WriteLine($"Okta.DeleteUser {oktaUserId} Request");

                        var response = await client.DeleteAsync($"{baseUrl}/users/{oktaUserId}");
                        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                        {
                            return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            token = await GenerateToken();
                            goto reloadOktaApi;
                        }
                        else
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();

                            Console.WriteLine($"Okta.DeleteUser {oktaUserId} Response => {responseContent}");

                            if (string.IsNullOrEmpty(responseContent)) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);

                            Console.WriteLine($"Okta.DeleteUser.{response.StatusCode} => {responseContent}");
                            var error = System.Text.Json.JsonSerializer.Deserialize<OktaErrorResponse>(responseContent);
                            return new ResponseModel<object>
                            {
                                Code = (int)ErrorCode.E400,
                                Message = string.Join('|', error?.errorCauses?.Select(s => s.errorSummary)?.ToArray()) ?? error?.errorSummary
                            };
                        }
                    }
                    else
                    {
                        return new ResponseModel<object>
                        {
                            Code = (int)ErrorCode.E400
                        };
                    }
                }
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"Okta.DeleteUser {oktaUserId} Exception => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion

        #region #refresh-token
        public async Task<ResponseModel<object>> RefreshToken(Model.Mobile.Request.RefreshTokenRequest model)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "grant_type", "refresh_token" },
                        { "client_id", $"{model.ClientId}" },
                        { "redirect_uri", $"{model.RedirectUri}" },
                        { "scope", "offline_access openid" },
                        { "refresh_token", $"{model.RefreshToken}"}
                    });
                    
                   
                    content.Headers.Clear();
                    content.Headers.Add("content-type", "application/x-www-form-urlencoded");

                    Utils.MobileErrorLog($"RefreshToken Request Log OktaService: {JsonConvert.SerializeObject(content)}",null,null, "RefreshToken Request",null,unitOfWork);
                    

                    var response = await client.PostAsync($"{config["Okta:RefreshTokenUrl"]}", content);

                    Utils.MobileErrorLog($"RefreshToken Response Log OktaService: {JsonConvert.SerializeObject(response)}",null,null, "RefreshToken Response",null,unitOfWork);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Okta.RefreshToken => {responseContent}");
                        Utils.MobileErrorLog($"RefreshToken Response Log responseContent Success OktaService: {JsonConvert.SerializeObject(responseContent)}",null,null, "RefreshToken Response",null,unitOfWork);

                        var oktaToken = System.Text.Json.JsonSerializer.Deserialize<OktaTokenResponse>(responseContent);
                        return new ResponseModel<object> 
                        {
                            Code = (int)ErrorCode.E0,
                            Message = "Success",
                            Data = new 
                            {
                                token_type = oktaToken.token_type,
                                expires_in = oktaToken.expires_in,
                                access_token = oktaToken.access_token,
                                id_token = oktaToken.id_token,
                                refresh_token = oktaToken.refresh_token,
                                scope = oktaToken.scope
                            }
                        };
                    }
                    else
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.Error.WriteLine($"Okta.RefreshToken => {responseContent} {response.StatusCode}");
                        Utils.MobileErrorLog($"RefreshToken Response Log responseContent Not Success OktaService: {JsonConvert.SerializeObject(responseContent)}",null,null, "RefreshToken Response",null,unitOfWork);
                        return new ResponseModel<object> { Code = (int)response.StatusCode, Message = "The refresh token is invalid or expired." };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Okta.RefreshToken.Exception => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion
    
        private bool IsValidOktaUserId(string userId)
        {
            string userIdPattern = "^[a-zA-Z0-9]+$";

            if (Regex.IsMatch(userId, userIdPattern))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        
    }
}
