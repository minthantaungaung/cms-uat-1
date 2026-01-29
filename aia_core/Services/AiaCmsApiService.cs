using aia_core.Entities;
using aia_core.Model.Mobile.Request;
using aia_core.Model.Mobile.Response;
using aia_core.Repository.Mobile;
using aia_core.UnitOfWork;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Services
{
    public interface IAiaCmsApiService
    {
        Task<GetDocumentListResponse> GetDocumentList(GetDocumentListRequest model);
        Task<UploadBase64Response> UploadBase64(UploadBase64Request model);
        Task<DownloadBase64Response> DownloadBase64(string documentId);
    }

    public class AiaCmsApiService : IAiaCmsApiService
    {
        protected readonly IUnitOfWork<Entities.Context> unitOfWork;

        public AiaCmsApiService(IUnitOfWork<Entities.Context> unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        public async Task<UploadBase64Response> UploadBase64(UploadBase64Request model)
        {
            try
            {
                model.PolicyNo = model.PolicyNo.Substring(0, 10);

                string cacertPath = "";
                if (AppSettingsHelper.GetSetting("Env").ToLower() == "production")
                {
                    cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmcms.aiaazure.biz.cer");
                }
                else if (AppSettingsHelper.GetSetting("Env").ToLower() == "uat")
                {
                    cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmcmsuat.aiaazure.biz.cer");
                }

                // Create and configure the HttpClientHandler
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ClientCertificates.Add(new X509Certificate2(cacertPath));
                httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                HttpClient httpClient = new HttpClient(httpClientHandler);

                var apiUrl = AppSettingsHelper.GetSetting("AiaCmsApi:UploadUrl");
                var token = AppSettingsHelper.GetSetting("AiaCmsApi:Token");
                //model.TemplateId = AppSettingsHelper.GetSetting("AiaCmsApi:TemplateId");                

                UploadBase64Request cloneRequest = new UploadBase64Request
                {
                    file = "[BASE64_DATA]",
                    PolicyNo = model.PolicyNo,
                    templateId = model.templateId,
                    docTypeId = model.docTypeId,
                    fileName = model.fileName,
                    format = model.format,
                    membershipId = model.membershipId,
                    claimId = model.claimId
                };
                StringContent cloneContent = new StringContent(JsonConvert.SerializeObject(cloneRequest), Encoding.UTF8, "application/json");
                string cloneContentString = await cloneContent.ReadAsStringAsync();


                StringContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                string jsonString = await content.ReadAsStringAsync();
                Console.WriteLine($"AiaCmsApiService => UploadBase64 => Request => ClaimId => {model.claimId} PolicyNo => {model.PolicyNo} FileName => {model.fileName} Json = > {cloneContentString}");
                

                // Add the token header
                httpClient.DefaultRequestHeaders.Add("token", token);

                // Send the POST request
                HttpResponseMessage response = httpClient.PostAsync(apiUrl, content).Result;

                
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"AiaCmsApiService => UploadBase64 => Response => ClaimId => {model.claimId} PolicyNo => {model.PolicyNo} FileName => {model.fileName} Json => {responseContent}");

                //if (response.StatusCode == System.Net.HttpStatusCode.OK)
                //{
                //    var result = JsonConvert.DeserializeObject<UploadBase64Response>(responseContent);

                //    return result;
                //}

                var result = JsonConvert.DeserializeObject<UploadBase64Response>(responseContent);

                return result;

            }
            catch (Exception ex)
            {
                Utils.MobileErrorLog("AiaCmsApiService => UploadBase64", $"Exception {ex.Message}", JsonConvert.SerializeObject(ex), "", "", unitOfWork);
            }

            return null;
        }

        public async Task<GetDocumentListResponse> GetDocumentList(GetDocumentListRequest model)
        {
            try
            {
                string cacertPath = "";
                if (AppSettingsHelper.GetSetting("Env").ToLower() == "production")
                {
                    cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmcms.aiaazure.biz.cer");
                }
                else if (AppSettingsHelper.GetSetting("Env").ToLower() == "uat")
                {
                    cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmcmsuat.aiaazure.biz.cer");
                }


                // Create and configure the HttpClientHandler
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ClientCertificates.Add(new X509Certificate2(cacertPath));
                httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                HttpClient httpClient = new HttpClient(httpClientHandler);

                var apiUrl = AppSettingsHelper.GetSetting("AiaCmsApi:DocumentListUrl");
                var token = AppSettingsHelper.GetSetting("AiaCmsApi:Token");
                model.templateId = AppSettingsHelper.GetSetting("AiaCmsApi:TemplateId");

                Utils.MobileErrorLog("AiaCmsApiService => GetDocumentList", "Request", JsonConvert.SerializeObject(model), "", "", unitOfWork);

                StringContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                // Add the token header
                httpClient.DefaultRequestHeaders.Add("token", token);

                // Send the POST request
                HttpResponseMessage response = httpClient.PostAsync(apiUrl, content).Result;


                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                Utils.MobileErrorLog("AiaCmsApiService => GetDocumentList", "Response", responseContent, "", "", unitOfWork);

                //if (response.StatusCode == System.Net.HttpStatusCode.OK)
                //{
                //    var result = JsonConvert.DeserializeObject<UploadBase64Response>(responseContent);

                //    return result;
                //}

                var result = JsonConvert.DeserializeObject<GetDocumentListResponse>(responseContent);

                return result;

            }
            catch (Exception ex)
            {
                Utils.MobileErrorLog("AiaCmsApiService => GetDocumentList", "Exception", JsonConvert.SerializeObject(ex), "", "", unitOfWork);
            }

            return null;
        }

        public async Task<DownloadBase64Response> DownloadBase64(string documentId)
        {
            try
            {

                string cacertPath = "";
                if (AppSettingsHelper.GetSetting("Env").ToLower() == "production")
                {
                    cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmcms.aiaazure.biz.cer");
                }
                else if (AppSettingsHelper.GetSetting("Env").ToLower() == "uat")
                {
                    cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmcmsuat.aiaazure.biz.cer");
                }

                // Create and configure the HttpClientHandler
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ClientCertificates.Add(new X509Certificate2(cacertPath));
                httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                HttpClient httpClient = new HttpClient(httpClientHandler);

                var apiUrl = AppSettingsHelper.GetSetting("AiaCmsApi:DownloadUrl");
                var token = AppSettingsHelper.GetSetting("AiaCmsApi:Token");

                

                Utils.MobileErrorLog("AiaCmsApiService => DownloadBase64", "Request", $"documentId => {documentId}", "", "", unitOfWork);
                

                // Add the token header
                httpClient.DefaultRequestHeaders.Add("token", token);


                var formData = new Dictionary<string, string>
                {
                    { "documentId", ""+documentId+"" },
                    { "docVersion", "1" }
                };

                string queryString = string.Join("&", formData.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                string fullUrl = $"{apiUrl}?{queryString}";


                // Send the POST request
                HttpResponseMessage response = httpClient.GetAsync(fullUrl).Result;



                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                Utils.MobileErrorLog("AiaCmsApiService => DownloadBase64", "Response", responseContent, "", "", unitOfWork);

                //if (response.StatusCode == System.Net.HttpStatusCode.OK)
                //{
                //    var result = JsonConvert.DeserializeObject<UploadBase64Response>(responseContent);

                //    return result;
                //}

                var result = JsonConvert.DeserializeObject<DownloadBase64Response>(responseContent);

                return result;

            }
            catch (Exception ex)
            {
                Utils.MobileErrorLog("AiaCmsApiService => DownloadBase64", $"Exception {ex.Message}", JsonConvert.SerializeObject(ex), "", "", unitOfWork);
            }

            return null;
        }
    }
}
