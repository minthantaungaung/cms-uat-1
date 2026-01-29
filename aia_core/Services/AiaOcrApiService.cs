using aia_core.Entities;
using aia_core.Model.ClaimProcess;
using aia_core.Model.Mobile.Request;
using aia_core.Model.Mobile.Response.AiaILApiResponse;
using aia_core.Model.Mobile.Response.OcrApi;
using aia_core.UnitOfWork;
using Azure;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Services
{
    public class OCRFileModel
    {
        public string FileName { get; set; }
        public byte[] FileContent { get; set; }
        public string FileType { get; set; }
    }
    public interface IAiaOcrApiService
    {
        Task SendDocs(Guid claimId, string hospitalName, List<OCRFileModel> ocrFileList);

        Task<AiaValidateDocApiResponseModel> ValidateDoc(IFormFile doc, string masterClientNo);

    }
    public class AiaOcrApiService : IAiaOcrApiService
    {
        private readonly IUnitOfWork<Context> unitOfWork;
        private readonly ILogService logService;
        public AiaOcrApiService(IUnitOfWork<Context> _unitOfWork, ILogService logService)
        {
            this.unitOfWork = _unitOfWork;
            this.logService = logService;
        }
        public async Task SendDocs(Guid claimId, string hospitalName, List<OCRFileModel> ocrFileList)
        {

            try
            {
                string cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmazeulap0001.aiaazure.biz.cer");

                Console.WriteLine($"OCR => Certificate path: {cacertPath}");

                using (var client = new HttpClient(new HttpClientHandler
                {
                    ClientCertificates = { new X509Certificate2(cacertPath) },
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        Console.WriteLine($"OCR => ServerCertificateCustomValidationCallback: {sslPolicyErrors}");

                        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
                        {
                            // Check if the certificate chain has errors
                            foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                            {
                                Console.WriteLine($"OCR => X509ChainStatus chainStatus {chainStatus.Status}");

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
                    }
                }))
                {
                    var apiEndpointUrl = AppSettingsHelper.GetSetting("AiaProcessClaim:endpoint");
                    var apiKey = AppSettingsHelper.GetSetting("AiaProcessClaim:apikey");

                    if(AppSettingsHelper.GetSetting("AiaProcessClaim:IsProduction") == "true")
                    {
                        apiKey = AppSettingsHelper.GetSetting("AiaProcessClaim:ProductionKey");
                    }

                    using (var content = new MultipartFormDataContent())
                    {

                        var _claimIdStr = claimId.ToString();
                        content.Add(new StringContent(_claimIdStr), "claimID");
                        content.Add(new StringContent(hospitalName), "hospitalName");

                        Console.WriteLine($"OCR => request parameters => {claimId} {apiEndpointUrl} {apiKey} {_claimIdStr} {hospitalName}");

                        // Add files from the dictionary to the content
                        foreach (var ocrFile in ocrFileList)
                        {
                            var fileName = ocrFile.FileName;
                            var stream = ocrFile.FileContent;                           

                            var fileContent = new ByteArrayContent(stream);

                            string mimeType = GetMimeType(fileName);
                            fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(mimeType);

                            // Add the file content to the MultipartFormDataContent
                            if(ocrFile.FileType == DefaultConstants.CLAIM_MEDICAL_BILL_DOCTYPEID)
                            {
                                content.Add(fileContent, "bill_files", fileName);
                            }
                            else if (ocrFile.FileType == DefaultConstants.CLAIM_MEDICAL_RECORD_DOCTYPEID)
                            {
                                content.Add(fileContent, "record_files", fileName);
                            }

                            Console.WriteLine($"OCR => fileContent.Headers.ContentType: {fileName} {ocrFile.FileType} {stream.Length} {mimeType}");
                        }


                        client.DefaultRequestHeaders.Add("api-key", apiKey);
                        var response = await client.PostAsync(apiEndpointUrl, content);

                        response.EnsureSuccessStatusCode();

                        var responseString = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"OCR => response parameters => {claimId} => {response.StatusCode} {responseString}");

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            //var claimProcessResponseModel = JsonConvert.DeserializeObject<List<ClaimProcessResponseModel>>(responseString);



                            //if (claimProcessResponseModel?.Any() == true)
                            //{
                            //    claimProcessResponseModel.ForEach(item =>
                            //    {
                            //        var apiLog = new ClaimDocumentsMedicaBillApiLog
                            //        {
                            //            Id = Guid.NewGuid(),
                            //            claimId = item.results.claimId,
                            //            admissionDate = item.results.admissionDate,
                            //            billingDate = item.results.billingDate,
                            //            billType = string.Join(",", item.results.billType),
                            //            dischargeDate = item.results.dischargeDate,
                            //            doctorName = item.results.doctorName,
                            //            patientName = item.results.patientName,
                            //            hospitalName = item.results.hospitalName,
                            //            netAmount = item.results.netAmount,
                            //            response = responseString,
                            //            ReceivedAt = Utils.GetDefaultDate(),
                            //            status = response.StatusCode.ToString()
                            //        };

                            //        logService.InsertOcrResponse(apiLog);
                            //    });
                            //}

                            ////JArray outerArray;

                            ////try
                            ////{
                            ////    outerArray = JsonConvert.DeserializeObject<JArray>(responseString);

                            ////    // Loop through groups
                            ////    foreach (var group in outerArray)
                            ////    {
                            ////        foreach (var item in group)
                            ////        {
                            ////            var results = item["results"];

                            ////            if (results == null)
                            ////                continue;

                            ////            // Decide which model to use based on presence of a key
                            ////            if (results["admissionDate"] != null)
                            ////            {
                            ////                var claim1 = results.ToObject<ClaimResultType1>();
                            ////                Console.WriteLine($"Type1: {claim1.claimId} | {claim1.patientName}");

                            ////                var apiLog = new ClaimDocumentsMedicaBillApiLog
                            ////                {
                            ////                    Id = Guid.NewGuid(),
                            ////                    claimId = claim1.claimId,
                            ////                    admissionDate = claim1.admissionDate,
                            ////                    billingDate = claim1.billingDate,
                            ////                    billType = string.Join(",", claim1.billType),
                            ////                    dischargeDate = claim1.dischargeDate,
                            ////                    doctorName = claim1.doctorName,
                            ////                    patientName = claim1.patientName,
                            ////                    hospitalName = claim1.hospitalName,
                            ////                    netAmount = claim1.netAmount,
                            ////                    response = responseString,
                            ////                    ReceivedAt = Utils.GetDefaultDate(),
                            ////                    status = response.StatusCode.ToString()
                            ////                };

                            ////                logService.InsertOcrResponse(apiLog);

                            ////            }
                            ////            else if (results["admissionDiagnosis"] != null)
                            ////            {
                            ////                var claim2 = results.ToObject<ClaimResultType2>();
                            ////                Console.WriteLine($"Type2: {claim2.claimId} | {claim2.patientName}");
                            ////            }
                            ////        }
                            ////    }
                            ////}
                            ////catch (Exception ex)
                            ////{
                            ////    Console.WriteLine($"Failed to parse OCR response: {ex.Message}");
                            ////    return;
                            ////}
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"OCR => exception => {claimId} => {JsonConvert.SerializeObject(ex)}");

            }
        }


        public string GetMimeType(string fileName)
        {
            var fileExtension = Path.GetExtension(fileName).ToLower();
            switch (fileExtension)
            {
                case ".pdf":
                    return "application/pdf";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".txt":
                    return "text/plain";
                default:
                    return "application/octet-stream"; // Default for unknown file types
            }
        }

        public async Task<AiaValidateDocApiResponseModel> ValidateDoc(IFormFile doc, string masterClientNo)
        {
            var responseModel = new AiaValidateDocApiResponseModel();

            try
            {
                string cacertPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mmazeulap0001.aiaazure.biz.cer");

                Console.WriteLine($"OCR Validate Doc => Certificate path: {cacertPath}");

                using (var client = new HttpClient(new HttpClientHandler
                {
                    ClientCertificates = { new X509Certificate2(cacertPath) },
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        Console.WriteLine($"OCR Validate Doc => ServerCertificateCustomValidationCallback: {sslPolicyErrors}");

                        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
                        {
                            // Check if the certificate chain has errors
                            foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                            {
                                Console.WriteLine($"OCR Validate Doc => X509ChainStatus chainStatus {chainStatus.Status}");

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
                    }
                }))
                {
                    var apiEndpointUrl = AppSettingsHelper.GetSetting("AiaValidateDocApi:endpoint");
                    var apiKey = AppSettingsHelper.GetSetting("AiaValidateDocApi:apikey");

                    if (AppSettingsHelper.GetSetting("AiaValidateDocApi:IsProduction") == "true")
                    {
                        apiKey = AppSettingsHelper.GetSetting("AiaValidateDocApi:ProductionKey");
                    }

                    using (var content = new MultipartFormDataContent())
                    {

                        using (var memoryStream = new MemoryStream())
                        {
                            // Copy the file stream into the memory stream
                            await doc.CopyToAsync(memoryStream);

                            // Step 2: Create ByteArrayContent using the memory stream's content
                            var fileContent = new ByteArrayContent(memoryStream.ToArray());

                            // Step 3: Optionally, set the MIME type if needed
                            string mimeType = GetMimeType(doc.FileName);
                            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

                            // Add the file content to the MultipartFormDataContent
                            content.Add(fileContent, "file", doc.FileName);

                            // Add the clientID as a string parameter
                            var clientIDContent = new StringContent(masterClientNo);
                            content.Add(clientIDContent, "clientID");

                            Console.WriteLine($"OCR Validate Doc => fileContent.Headers.ContentType: {mimeType}");
                            Console.WriteLine($"OCR Validate Doc => request parameters => {doc.Name} {masterClientNo}");

                            client.DefaultRequestHeaders.Add("api-key", apiKey);
                            var response = await client.PostAsync(apiEndpointUrl, content);

                            var responseString = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"OCR Validate Doc => response parameters => {doc.FileName} => {response.StatusCode} {responseString}");

                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                responseModel = JsonConvert.DeserializeObject<AiaValidateDocApiResponseModel>(responseString);

                                
                            }
                        }


                        
                    }
                }

                
            }
            catch (Exception ex)
            {

                Console.WriteLine($"OCR Validate Doc => exception => {doc.Name} => {JsonConvert.SerializeObject(ex)}");

            }

            return responseModel;
        }
    }
}
