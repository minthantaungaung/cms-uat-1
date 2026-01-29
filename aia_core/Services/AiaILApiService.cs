using aia_core.Model.Mobile.Request;
using aia_core.Model.Mobile.Request.Servicing;
using aia_core.Model.Mobile.Response;
using aia_core.Model.Mobile.Response.AiaILApiResponse;
using aia_core.Repository.Mobile;
using aia_core.UnitOfWork;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Services
{
    public interface IAiaILApiService
    {
        CommonRegisterResponse CommonRegister(CommonRegisterRequest model, EnumILClaimApi claimApi, out string systemError, out string SerializeModel);
        CommonRegisterResponse ClientUpdateServicingRequest(ILServicingChangeRequest model, out string systemError, out string SerializeModel);
        Task<CommonRegisterResponse> AdhocTopSubmission(ILAdhocTopupRequest model);
        Task<CommonRegisterResponse> SumAssuredSubmission(ILSumAssuredRequest model);
        Task<CommonRegisterResponse> PolicyPaidUpSubmission(ILPolicyPaidUpSubmissionRequest model);
        Task<CommonRegisterResponse> RefundPaymentSubmission(ILPaymentRefundRequest model);
        CommonRegisterResponse PaymentFrequencySubmission(ILPaymentFrequencyRequest model, out string systemError, out string SerializeModel);
        Task<CommonRegisterResponse> BeneficiariesSubmission(ILBeneficiariesRequest model);
    }

    public class AiaILApiService : IAiaILApiService
    {
        protected readonly IUnitOfWork<Entities.Context> unitOfWork;

        public AiaILApiService(IUnitOfWork<Entities.Context> unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public CommonRegisterResponse CommonRegister(CommonRegisterRequest model, EnumILClaimApi claimApi, out string systemError
            , out string SerializeModel)
        {
            systemError = "";
            SerializeModel = null;
            try
            {
                string cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmazeulap0001.aiaazure.biz.cer");
                //string cacertPath = "/app/wwwroot/cert/mmazeulap0001.aiaazure.biz.cer";


                Console.WriteLine($"CommonRegister {cacertPath}");


                //string newRootCertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Root-CA.cer");
                //X509Certificate2 newRootCertificate = new X509Certificate2(newRootCertPath);

                // Create and configure the HttpClientHandler
                var httpClientHandler = new HttpClientHandler();

                httpClientHandler.ClientCertificates.Add(new X509Certificate2(cacertPath));

                //httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                {
                    Console.WriteLine($"CommonRegister => ServerCertificateCustomValidationCallback {sslPolicyErrors}");
                    Console.WriteLine($"IL Certificate Subject: {cert.Subject}");
                    Console.WriteLine($"IL Issuer: {cert.Issuer}");
                    Console.WriteLine($"IL Thumbprint: {cert.Thumbprint}");

                    if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
                    {
                        // Check if the certificate chain has errors
                        foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                        {
                            Console.WriteLine($"CommonRegister => X509ChainStatus chainStatus {chainStatus.Status}");

                            if (chainStatus.Status == X509ChainStatusFlags.UntrustedRoot)
                            {
                                // Handle untrusted root certificate error
                                // Add the root certificate to the trusted store or update root certificate store
                                // Alternatively, return true to trust the certificate chain if it's acceptable in your scenario

                                return true;
                            }
                            // Handle other certificate chain errors as needed


                        }
                    }

                    return true; // always true, bypass
                };




                HttpClient httpClient = new HttpClient(httpClientHandler);

                //HttpClient client = new HttpClient();

                var url = AppSettingsHelper.GetSetting("AiaILApi:BaseUrl");
                var token = AppSettingsHelper.GetSetting("AiaILApi:Token");

                var apiUrl = "";

                if(claimApi == EnumILClaimApi.Health)
                {
                    apiUrl = AppSettingsHelper.GetSetting("AiaILApi:Health");
                }
                else if (claimApi == EnumILClaimApi.NonHealth)
                {
                    apiUrl = AppSettingsHelper.GetSetting("AiaILApi:NonHealth");
                }
                else if (claimApi == EnumILClaimApi.TPD)
                {
                    apiUrl = AppSettingsHelper.GetSetting("AiaILApi:TPD");
                }
                else if (claimApi == EnumILClaimApi.CI)
                {
                    apiUrl = AppSettingsHelper.GetSetting("AiaILApi:CI");
                }
                else if (claimApi == EnumILClaimApi.Death)
                {
                    apiUrl = AppSettingsHelper.GetSetting("AiaILApi:Death");
                }

                url = url + "/" + apiUrl;

                Utils.MobileErrorLog("AiaILApiService => CommonRegister", $"{claimApi.ToString()} Request", JsonConvert.SerializeObject(model), "", "", unitOfWork);

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                SerializeModel = JsonConvert.SerializeObject(model, settings);
                StringContent content = new StringContent(SerializeModel, Encoding.UTF8, "application/json");     

                // Add the token header
                httpClient.DefaultRequestHeaders.Add("Authorization", token);

                // Send the POST request
                HttpResponseMessage response = httpClient.PostAsync(url, content).Result;

                response.EnsureSuccessStatusCode();

                var responseContent = response.Content.ReadAsStringAsync().Result;

                Utils.MobileErrorLog("AiaILApiService => CommonRegister", $"{claimApi.ToString()} Response", responseContent, "", "", unitOfWork);

                systemError = responseContent;

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = JsonConvert.DeserializeObject<CommonRegisterResponse>(responseContent);
                    return result;
                }

                
                return null;

            }
            catch (Exception ex)
            {
                systemError = ex.Message;
                Utils.MobileErrorLog("AiaILApiService => CommonRegister", $"Exception {ex.Message}", JsonConvert.SerializeObject(ex), "", "", unitOfWork);
            }

            return null;
        }

        public CommonRegisterResponse ClientUpdateServicingRequest(ILServicingChangeRequest model, out string systemError, out string SerializeModel)
        {
            systemError = "";
            SerializeModel = null;
            try
            {
                string cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmazeulap0001.aiaazure.biz.cer");
                //string cacertPath = "/app/wwwroot/cert/mmazeulap0001.aiaazure.biz.cer";

                // Create and configure the HttpClientHandler
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ClientCertificates.Add(new X509Certificate2(cacertPath));
                ////httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;


                httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                {
                    Console.WriteLine($"ClientUpdateServicingRequest => ServerCertificateCustomValidationCallback {sslPolicyErrors}");
                    if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
                    {
                        // Check if the certificate chain has errors
                        foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                        {
                            Console.WriteLine($"ClientUpdateServicingRequest => X509ChainStatus chainStatus {chainStatus.Status}");

                            if (chainStatus.Status == X509ChainStatusFlags.UntrustedRoot)
                            {
                                // Handle untrusted root certificate error
                                // Add the root certificate to the trusted store or update root certificate store
                                // Alternatively, return true to trust the certificate chain if it's acceptable in your scenario


                                return true;
                            }
                            // Handle other certificate chain errors as needed


                        }
                    }
                    return true; // Reject the certificate chain
                };

                HttpClient httpClient = new HttpClient(httpClientHandler);
               

                var url = AppSettingsHelper.GetSetting("AiaILApi:BaseUrl");
                var token = AppSettingsHelper.GetSetting("AiaILApi:Token");

                var apiUrl = AppSettingsHelper.GetSetting("AiaILApi:ClientUpdate");

                url = url + "/" + apiUrl;

                Utils.MobileErrorLog("AiaILApiService => ClientUpdateServicingRequest", "Request", JsonConvert.SerializeObject(model), "", "", unitOfWork);

                SerializeModel = JsonConvert.SerializeObject(model);
                StringContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");     

                // Add the token header
                httpClient.DefaultRequestHeaders.Add("Authorization", token);

                // Send the POST request
                HttpResponseMessage response = httpClient.PostAsync(url, content).Result;

                response.EnsureSuccessStatusCode();

                var responseContent = response.Content.ReadAsStringAsync().Result;

                Utils.MobileErrorLog("AiaILApiService => ClientUpdateServicingRequest", "Response", responseContent, "", "", unitOfWork);

                systemError = responseContent;
                
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = JsonConvert.DeserializeObject<CommonRegisterResponse>(responseContent);
                    return result;
                }

                return null;
            }
            catch(Exception ex)
            {
                systemError = JsonConvert.SerializeObject(ex);
                Utils.MobileErrorLog("AiaILApiService => ClientUpdateServicingRequest", $"Exception {ex.Message}", JsonConvert.SerializeObject(ex), "", "", unitOfWork);
            }
            return null;
        }

        public async Task<CommonRegisterResponse> AdhocTopSubmission(ILAdhocTopupRequest model)
        {
            //systemError = "";
            //SerializeModel = null;
            try
            {
                string cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmazeulap0001.aiaazure.biz.cer");
                //string cacertPath = "/app/wwwroot/cert/mmazeulap0001.aiaazure.biz.cer";

                // Create and configure the HttpClientHandler
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ClientCertificates.Add(new X509Certificate2(cacertPath));
                httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                HttpClient httpClient = new HttpClient(httpClientHandler);

                //HttpClient client = new HttpClient();

                var url = AppSettingsHelper.GetSetting("AiaILApi:BaseUrl");
                var token = AppSettingsHelper.GetSetting("AiaILApi:Token");

                var apiUrl = AppSettingsHelper.GetSetting("AiaILApi:AdhocTopSubmission");

                url = url + "/" + apiUrl;

                Utils.MobileErrorLog("AiaILApiService => AdhocTopSubmission", "Request", JsonConvert.SerializeObject(model), "", "", unitOfWork);

                //SerializeModel = JsonConvert.SerializeObject(model);
                StringContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");     

                // Add the token header
                httpClient.DefaultRequestHeaders.Add("Authorization", token);

                // Send the POST request
                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                //response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                Utils.MobileErrorLog("AiaILApiService => ClientUpdateServicingRequest", "Response", responseContent, "", "", unitOfWork);

                //systemError = responseContent;
                
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = JsonConvert.DeserializeObject<CommonRegisterResponse>(responseContent);
                    return result;
                }

                return null;
            }
            catch(Exception ex)
            {
                //systemError = JsonConvert.SerializeObject(ex);
                Utils.MobileErrorLog("AiaILApiService => AdhocTopSubmission", $"Exception {ex.Message}", JsonConvert.SerializeObject(ex), "", "", unitOfWork);
            }
            return null;
        }

        public async Task<CommonRegisterResponse> SumAssuredSubmission(ILSumAssuredRequest model)
        {
            //systemError = "";
            //SerializeModel = null;
            try
            {
                string cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmazeulap0001.aiaazure.biz.cer");
                //string cacertPath = "/app/wwwroot/cert/mmazeulap0001.aiaazure.biz.cer";

                // Create and configure the HttpClientHandler
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ClientCertificates.Add(new X509Certificate2(cacertPath));
                httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                HttpClient httpClient = new HttpClient(httpClientHandler);

                //HttpClient client = new HttpClient();

                var url = AppSettingsHelper.GetSetting("AiaILApi:BaseUrl");
                var token = AppSettingsHelper.GetSetting("AiaILApi:Token");

                var apiUrl = AppSettingsHelper.GetSetting("AiaILApi:SumAssuredSubmission");

                url = url + "/" + apiUrl;

                Utils.MobileErrorLog("AiaILApiService => AdhocTopSubmission", "Request", JsonConvert.SerializeObject(model), "", "", unitOfWork);

                //SerializeModel = JsonConvert.SerializeObject(model);
                StringContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");     

                // Add the token header
                httpClient.DefaultRequestHeaders.Add("Authorization", token);

                // Send the POST request
                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                //response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                Utils.MobileErrorLog("AiaILApiService => SumAssuredSubmission", "Response", responseContent, "", "", unitOfWork);

                //systemError = responseContent;
                
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = JsonConvert.DeserializeObject<CommonRegisterResponse>(responseContent);
                    return result;
                }

                return null;
            }
            catch(Exception ex)
            {
                //systemError = JsonConvert.SerializeObject(ex);
                Utils.MobileErrorLog("AiaILApiService => SumAssuredSubmission", $"Exception {ex.Message}", JsonConvert.SerializeObject(ex), "", "", unitOfWork);
            }
            return null;
        }

        public async Task<CommonRegisterResponse> PolicyPaidUpSubmission(ILPolicyPaidUpSubmissionRequest model)
        {
            //systemError = "";
            //SerializeModel = null;
            try
            {
                string cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmazeulap0001.aiaazure.biz.cer");
                //string cacertPath = "/app/wwwroot/cert/mmazeulap0001.aiaazure.biz.cer";

                // Create and configure the HttpClientHandler
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ClientCertificates.Add(new X509Certificate2(cacertPath));
                httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                HttpClient httpClient = new HttpClient(httpClientHandler);

                //HttpClient client = new HttpClient();

                var url = AppSettingsHelper.GetSetting("AiaILApi:BaseUrl");
                var token = AppSettingsHelper.GetSetting("AiaILApi:Token");

                var apiUrl = AppSettingsHelper.GetSetting("AiaILApi:PolicyPaidUpSubmission");

                url = url + "/" + apiUrl;

                Utils.MobileErrorLog("AiaILApiService => AdhocTopSubmission", "Request", JsonConvert.SerializeObject(model), "", "", unitOfWork);

                //SerializeModel = JsonConvert.SerializeObject(model);
                StringContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");     

                // Add the token header
                httpClient.DefaultRequestHeaders.Add("Authorization", token);

                // Send the POST request
                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                //response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                Utils.MobileErrorLog("AiaILApiService => PolicyPaidUpSubmission", "Response", responseContent, "", "", unitOfWork);

                //systemError = responseContent;
                
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = JsonConvert.DeserializeObject<CommonRegisterResponse>(responseContent);
                    return result;
                }

                return null;
            }
            catch(Exception ex)
            {
                //systemError = JsonConvert.SerializeObject(ex);
                Utils.MobileErrorLog("AiaILApiService => PolicyPaidUpSubmission", $"Exception {ex.Message}", JsonConvert.SerializeObject(ex), "", "", unitOfWork);
            }
            return null;
        }

        public async Task<CommonRegisterResponse> RefundPaymentSubmission(ILPaymentRefundRequest model)
        {
            try
            {
                string cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmazeulap0001.aiaazure.biz.cer");
                //string cacertPath = "/app/wwwroot/cert/mmazeulap0001.aiaazure.biz.cer";

                // Create and configure the HttpClientHandler
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ClientCertificates.Add(new X509Certificate2(cacertPath));
                httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                HttpClient httpClient = new HttpClient(httpClientHandler);

                //HttpClient client = new HttpClient();

                var url = AppSettingsHelper.GetSetting("AiaILApi:BaseUrl");
                var token = AppSettingsHelper.GetSetting("AiaILApi:Token");

                var apiUrl = AppSettingsHelper.GetSetting("AiaILApi:RefundPaymentSubmission");

                url = url + "/" + apiUrl;

                Utils.MobileErrorLog("AiaILApiService => RefundPaymentSubmission", "Request", JsonConvert.SerializeObject(model), "", "", unitOfWork);

                //SerializeModel = JsonConvert.SerializeObject(model);
                StringContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");     

                // Add the token header
                httpClient.DefaultRequestHeaders.Add("Authorization", token);

                // Send the POST request
                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                //response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                Utils.MobileErrorLog("AiaILApiService => ClientUpdateServicingRequest", "Response", responseContent, "", "", unitOfWork);

                //systemError = responseContent;
                
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = JsonConvert.DeserializeObject<CommonRegisterResponse>(responseContent);
                    return result;
                }

                return null;
            }
            catch(Exception ex)
            {
                //systemError = JsonConvert.SerializeObject(ex);
                Utils.MobileErrorLog("AiaILApiService => RefundPaymentSubmission", $"Exception {ex.Message}", JsonConvert.SerializeObject(ex), "", "", unitOfWork);
            }
            return null;
        }

        public CommonRegisterResponse PaymentFrequencySubmission(ILPaymentFrequencyRequest model, out string systemError, out string SerializeModel)
        {
            systemError = "";
            SerializeModel = null;
            try
            {
                string cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmazeulap0001.aiaazure.biz.cer");
                //string cacertPath = "/app/wwwroot/cert/mmazeulap0001.aiaazure.biz.cer";

                // Create and configure the HttpClientHandler
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ClientCertificates.Add(new X509Certificate2(cacertPath));
                ////httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                {
                    Console.WriteLine($"PaymentFrequencySubmission => ServerCertificateCustomValidationCallback {sslPolicyErrors}");
                    if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
                    {
                        // Check if the certificate chain has errors
                        foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                        {
                            Console.WriteLine($"PaymentFrequencySubmission => X509ChainStatus chainStatus {chainStatus.Status}");

                            if (chainStatus.Status == X509ChainStatusFlags.UntrustedRoot)
                            {
                                // Handle untrusted root certificate error
                                // Add the root certificate to the trusted store or update root certificate store
                                // Alternatively, return true to trust the certificate chain if it's acceptable in your scenario

                                return true;
                            }
                            // Handle other certificate chain errors as needed

                           
                        }
                    }
                    return true; // Reject the certificate chain
                };

                HttpClient httpClient = new HttpClient(httpClientHandler);

                //HttpClient client = new HttpClient();

                var url = AppSettingsHelper.GetSetting("AiaILApi:BaseUrl");
                var token = AppSettingsHelper.GetSetting("AiaILApi:Token");

                var apiUrl = AppSettingsHelper.GetSetting("AiaILApi:PaymentFrequencySubmission");

                url = url + "/" + apiUrl;

                Utils.MobileErrorLog("AiaILApiService => PaymentFrequencySubmission", "Request", JsonConvert.SerializeObject(model), "", "", unitOfWork);

                SerializeModel = JsonConvert.SerializeObject(model);
                StringContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");     

                // Add the token header
                httpClient.DefaultRequestHeaders.Add("Authorization", token);

                // Send the POST request
                HttpResponseMessage response = httpClient.PostAsync(url, content).Result;

                response.EnsureSuccessStatusCode();

                var responseContent = response.Content.ReadAsStringAsync().Result;

                Utils.MobileErrorLog("AiaILApiService => ClientUpdateServicingRequest", "Response", responseContent, "", "", unitOfWork);

                systemError = responseContent;
                
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = JsonConvert.DeserializeObject<CommonRegisterResponse>(responseContent);
                    return result;
                }

                return null;
            }
            catch(Exception ex)
            {
                systemError = JsonConvert.SerializeObject(ex);
                Utils.MobileErrorLog("AiaILApiService => PaymentFrequencySubmission", $"Exception {ex.Message}", JsonConvert.SerializeObject(ex), "", "", unitOfWork);
            }
            return null;
        }

        public async Task<CommonRegisterResponse> BeneficiariesSubmission(ILBeneficiariesRequest model)
        {
            //systemError = "";
            //SerializeModel = null;
            try
            {
                string cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmazeulap0001.aiaazure.biz.cer");
                //string cacertPath = "/app/wwwroot/cert/mmazeulap0001.aiaazure.biz.cer";

                // Create and configure the HttpClientHandler
                var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ClientCertificates.Add(new X509Certificate2(cacertPath));
                httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                HttpClient httpClient = new HttpClient(httpClientHandler);

                //HttpClient client = new HttpClient();

                var url = AppSettingsHelper.GetSetting("AiaILApi:BaseUrl");
                var token = AppSettingsHelper.GetSetting("AiaILApi:Token");

                var apiUrl = AppSettingsHelper.GetSetting("AiaILApi:BeneficiariesSubmission");

                url = url + "/" + apiUrl;

                Utils.MobileErrorLog("AiaILApiService => BeneficiariesSubmission", "Request", JsonConvert.SerializeObject(model), "", "", unitOfWork);

                //SerializeModel = JsonConvert.SerializeObject(model);
                StringContent content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");     

                // Add the token header
                httpClient.DefaultRequestHeaders.Add("Authorization", token);

                // Send the POST request
                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                //response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                Utils.MobileErrorLog("AiaILApiService => BeneficiariesSubmission", "Response", responseContent, "", "", unitOfWork);

                //systemError = responseContent;
                
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = JsonConvert.DeserializeObject<CommonRegisterResponse>(responseContent);
                    return result;
                }

                return null;
            }
            catch(Exception ex)
            {
                //systemError = JsonConvert.SerializeObject(ex);
                Utils.MobileErrorLog("AiaILApiService => BeneficiariesSubmission", $"Exception {ex.Message}", JsonConvert.SerializeObject(ex), "", "", unitOfWork);
            }
            return null;
        }
    }
}
