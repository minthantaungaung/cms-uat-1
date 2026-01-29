using aia_core.Entities;
using aia_core.Model.Mobile.Response;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Newtonsoft.Json;
using aia_core.Model.Mobile.Request;
using aia_core.Model.Mobile.Request.Servicing;
using Microsoft.AspNetCore.Hosting;
using DinkToPdf;
using DinkToPdf.Contracts;
using aia_core.Model.Mobile.Response.AiaILApiResponse;
using aia_core.Model.AiaCrm;
using aia_core.Model.Mobile.Response.Servicing;
using System.IO.Compression;

namespace aia_core.Repository.Mobile
{
    public interface IPolicyPaidUpRepository
    {
        Task<ResponseModel<ServicingResponseModel>> Submit(ServiceWithdrawRequest model);
    }
    public class PolicyPaidUpRepository : BaseRepository, IPolicyPaidUpRepository
    {
        #region "const"

        private readonly ICommonRepository commonRepository;
        private readonly IAiaILApiService aiaILApiService;
        private readonly IHostingEnvironment environment;
        private readonly IConverter converter;
        private readonly IAiaCmsApiService aiaCmsApiService;
        private readonly IAiaCrmApiService aiaCrmApiService;
        private readonly INotificationService notificationService;
        #endregion

        public PolicyPaidUpRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork,
            ICommonRepository commonRepository,
            IAiaILApiService aiaILApiService,
            IHostingEnvironment environment,
            IConverter converter,
            IAiaCrmApiService aiaCrmApiService,
            IAiaCmsApiService aiaCmsApiService,
            INotificationService notificationService)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.commonRepository = commonRepository;
            this.aiaILApiService = aiaILApiService;
            this.environment = environment;
            this.converter = converter;
            this.aiaCmsApiService = aiaCmsApiService;
            this.aiaCrmApiService = aiaCrmApiService;
            this.notificationService = notificationService;
            {

            }
        }

        public async Task<ResponseModel<ServicingResponseModel>> Submit(ServiceWithdrawRequest model)
        {
            try
            {
                MobileErrorLog("PolicyPaidUp FE Req =>", $"{JsonConvert.SerializeObject(model)}", "", httpContext?.HttpContext.Request.Path);

                Guid? memberID = commonRepository.GetMemberIDFromToken();
                AppConfig appConfig = unitOfWork.GetRepository<AppConfig>().Query().FirstOrDefault();
                string ServicingEmail = appConfig.ServicingEmail;


                var referenceNumber = Utils.ReferenceNumber(model.ClaimOtp.ReferenceNo);

                var claimOtp = unitOfWork.GetRepository<Entities.CommonOtp>()
                    .Query(x => x.OtpType == EnumOtpType.claim.ToString() && x.OtpTo == referenceNumber && x.MemberId == memberID)
                    .OrderByDescending(x => x.CreatedOn)
                    .FirstOrDefault();

                if (claimOtp == null && !model.IsSkipOtpValidation) return new ResponseModel<ServicingResponseModel> { Code = 401, Message = "OtpCode required." };

                var defaultDate = Utils.GetDefaultDate();
                if ((claimOtp?.OtpCode == model.ClaimOtp.OtpCode
                    && claimOtp?.OtpExpiry > defaultDate) || model.IsSkipOtpValidation)
                {
                    ServicePolicyPaidUp entity = new ServicePolicyPaidUp();
                    entity.ID = Guid.NewGuid();
                    entity.PolicyNumber = model.PolicyNo;
                    entity.Amount = model.Amount;
                    entity.Reason = model.Reason;
                    entity.SignatureImage = model.SignatureImage;
                    entity.CreatedOn = Utils.GetDefaultDate();
                    entity.MemberID = memberID;
                    entity.Status = EnumServicingStatus.Received.ToString();

                    if(model.BankInfo!=null)
                    {
                        entity.BankName = model.BankInfo.BankName;
                        entity.BankCode = model.BankInfo.BankCode;
                        entity.BankAccountName = model.BankInfo.BankAccountName;
                        entity.BankAccountNumber = model.BankInfo.BankAccountNumber;
                    }

                    unitOfWork.GetRepository<Entities.ServicePolicyPaidUp>().Add(entity);

                    #region "Service Main"
                    (string producttype, string productname) = GetProductInfo(model.PolicyNo);
                    (string? membertype, string? aiaMemberID, string? groupMemberId) = GetClientInfo(memberID);
                    Policy policy = unitOfWork.GetRepository<Policy>().Query(x => x.PolicyNo == model.PolicyNo).FirstOrDefault();
                    Client client = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == policy.PolicyHolderClientNo || x.ClientNo == policy.InsuredPersonClientNo).FirstOrDefault();
                    Entities.PolicyStatus _policyStatus = await unitOfWork.GetRepository<Entities.PolicyStatus>().Query(x => x.ShortDesc == policy.PolicyStatus).FirstOrDefaultAsync();
                    Member member = unitOfWork.GetRepository<Member>().Query(x => x.MemberId == memberID).FirstOrDefault();

                    ServiceMain serviceMain = new ServiceMain();
                    Guid guid = Guid.NewGuid();
                    serviceMain.ID = Guid.NewGuid();
                    serviceMain.MainID = entity.ID;
                    serviceMain.ServiceID = entity.ID;
                    serviceMain.ServiceType = EnumServiceType.PolicyPaidUp.ToString();
                    serviceMain.ServiceStatus = EnumServicingStatus.Received.ToString();
                    serviceMain.CreatedDate = Utils.GetDefaultDate(); serviceMain.OriginalCreatedDate = Utils.GetDefaultDate();
                    serviceMain.ProductType = producttype;
                    serviceMain.MemberType = membertype;
                    serviceMain.GroupMemberID = groupMemberId;
                    serviceMain.MemberID = aiaMemberID;
                    serviceMain.LoginMemberID = memberID;
                    serviceMain.PolicyNumber = policy?.PolicyNo;
                    serviceMain.PolicyStatus = policy?.PolicyStatus;
                    serviceMain.MobileNumber = member?.Mobile;
                    serviceMain.MemberName = member?.Name;
                    serviceMain.EstimatedCompletedDate = GetProgressAndContactHour(Utils.GetDefaultDate(),EnumProgressType.Service).CompletedDate;
                    unitOfWork.GetRepository<Entities.ServiceMain>().Add(serviceMain);
                    #endregion

                    unitOfWork.SaveChanges();

                    

                    CaseResponse crmResponse = await CreateCaseToCRM(client, entity, policy);

                    ILPolicyPaidUpSubmissionRequest iLPolicyPaidUpSubmissionRequest = new ILPolicyPaidUpSubmissionRequest();
                    iLPolicyPaidUpSubmissionRequest.policyNumber = model.PolicyNo;
                    iLPolicyPaidUpSubmissionRequest.requestId = entity.ID.ToString();
                    if(crmResponse.Code== "200")
                    {
                        iLPolicyPaidUpSubmissionRequest.incidentID = crmResponse.Data;  
                    }
                    else
                    {
                        iLPolicyPaidUpSubmissionRequest.incidentID = "";
                    }
                    
                    entity.ILRequest = JsonConvert.SerializeObject(iLPolicyPaidUpSubmissionRequest);
                    entity.ILRequestOn = Utils.GetDefaultDate();

                    //AIA IL
                    CommonRegisterResponse ilResponse = await aiaILApiService.PolicyPaidUpSubmission(iLPolicyPaidUpSubmissionRequest);

                    entity.ILResponse = JsonConvert.SerializeObject(ilResponse);
                    entity.ILResponseOn = Utils.GetDefaultDate();
                    unitOfWork.GetRepository<Entities.ServicePolicyPaidUp>().Update(entity);
                    unitOfWork.SaveChanges();

                    try
                    {
                        ServiceMain sm = unitOfWork.GetRepository<ServiceMain>().Query(x => x.ID == serviceMain.ID).FirstOrDefault();
                        sm.ILStatus = ilResponse.message.type == "SUCCESS" ? "success" : "fail";
                        sm.ILMessage = ilResponse.data.errorMessage;
                        unitOfWork.SaveChanges();
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine("ServiceMain IL Save Error : " + ex);
                    }

                    // if (ilResponse.message.type != "SUCCESS")
                    // {
                    //     return errorCodeProvider.GetResponseModelCustom<ServicingResponseModel>(ErrorCode.E500, ilResponse.data.errorMessage);
                    // }

                    //Task.Run(() => RunBackgroundCodeAsync(entity, client, policy, model));


                    try
                    {
                        //Notification
                        //MobileErrorLog("Notification Send HealthRenewal", "", "", httpContext?.HttpContext.Request.Path);
                        Console.WriteLine("Before SendServicingNoti");
                        _ = notificationService.SendServicingNoti(memberID ?? new Guid(), entity.ID, EnumServicingStatus.Received, EnumServiceType.PolicyPaidUp, model.PolicyNo);
                        Console.WriteLine("After SendServicingNoti");
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine($"SendServicingNoti Error | Ex message : {ex.Message} | Exception {ex}");
                        MobileErrorLog("Notification Error", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                    }

                    var pdfFileName = $"{entity.PolicyNumber}_AIA+_{entity.ID}_{DateTime.Now.ToString("yyyyMMddhhmmss")}.pdf";
                    Console.WriteLine($"Before GenerateServicingPdf");
                    byte[] pdfData = await GenerateServicingPdf(entity, pdfFileName, client.Name, entity.PolicyNumber,_policyStatus.LongDesc, client?.ClientNo);
                    Console.WriteLine($"After GenerateServicingPdf");
                    //AIA CMS
                    Console.WriteLine($"Before UpadeGeneratePdfToCMS");
                    await UpadeGeneratePdfToCMS(pdfData, entity.PolicyNumber, pdfFileName, entity);
                    Console.WriteLine($"After UpadeGeneratePdfToCMS");

                    // Task.Run(() => RunBackgroundCodeAsync(entity, client, policy, model, memberID, ServicingEmail, pdfFileName, pdfData));
                    _ = RunBackgroundCodeAsync(entity, client, policy, model, memberID, ServicingEmail, pdfFileName, pdfData, ilResponse);

                    return errorCodeProvider.GetResponseModel<ServicingResponseModel>(ErrorCode.E0,
                    new ServicingResponseModel()
                    {
                        servicingId = entity.ID.ToString()
                    });
                }
                else
                {
                    return new ResponseModel<ServicingResponseModel> { Code = 401, Message = "Invalid OtpCode or expired." };
                }

            }
            catch (System.Exception ex)
            {
                MobileErrorLog("PolicyPaidUp Ex => ", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<ServicingResponseModel>(ErrorCode.E500);
            }
        }

        private async Task RunBackgroundCodeAsync(ServicePolicyPaidUp entity, Client client, Policy policy, ServiceWithdrawRequest model, Guid? memberID, string ServicingEmail, string pdfFileName, byte[] pdfData,CommonRegisterResponse ilResponse)
        {
            Console.WriteLine("Inside RunBackgroundCodeAsync");
            //MobileErrorLog("RunBackgroundCodeAsync HealthRenewal", "", "", httpContext?.HttpContext.Request.Path);
            (List<byte[]> imageByteList, List<string> imageNameList) = await UploadDocToCMS(model, policy, entity);

            #region "Send Email"
            try
            {
                var attachments = new List<EmailAttachment>();
                var attachment = new EmailAttachment
                {
                    Data = pdfData,
                    FileName = pdfFileName,
                };

                attachments.Add(attachment);

                long totalSize = 0;
                foreach (var _fileByte in imageByteList)
                {
                    totalSize += _fileByte.Length;
                }
                MobileErrorLog("totalSize HealthRenewal", totalSize.ToString(), "", httpContext?.HttpContext.Request.Path);
                if (totalSize > 10 * 1024 * 1024) // Check if total size is greater than 10 MB
                {
                    byte[] zipBytes = CompressBytes(imageByteList, imageNameList);

                    var attachmentDoc = new EmailAttachment
                    {
                        Data = zipBytes,
                        FileName = $"{client.Name}_{entity.ID}",
                    };
                    attachments.Add(attachmentDoc);
                }
                else
                {
                    MobileErrorLog("imageByteList Count HealthRenewal", imageByteList.Count.ToString(), "", httpContext?.HttpContext.Request.Path);
                    for (int i = 0; i < imageByteList.Count; i++)
                    {
                        var attachmentDoc = new EmailAttachment
                        {
                            Data = imageByteList[i],
                            FileName = imageNameList[i],
                        };
                        attachments.Add(attachmentDoc);
                    }
                }
                MobileErrorLog("attachments Count HealthRenewal", attachments.Count.ToString(), "", httpContext?.HttpContext.Request.Path);


                string subject = $"{entity.PolicyNumber}/Policy Paid Up/{client.Name}";

                string BankInfo = $"<b>Bank Account Holder Name :</b> {model.BankInfo.BankAccountName}<br/><b>Bank Account No :</b> {model.BankInfo.BankAccountNumber}<br/><b>Bank Name :</b> {model.BankInfo.BankName}";

                var path = Path.Combine(
                this.environment.ContentRootPath, "email_templates/", "servicing_email_content.html");
                var emailContent = File.ReadAllText(path);
                emailContent = emailContent.Replace("{{EmailContent}}", subject);
                emailContent = emailContent.Replace("{{UniqueID}}", entity.ID.ToString());
                emailContent = emailContent.Replace("{{ILStatus}}", ilResponse?.message?.type);
                emailContent = emailContent.Replace("{{ILErrorMessage}}", ilResponse?.data?.errorMessage);

                emailContent = emailContent.Replace("{{BankInfo}}", BankInfo);

                //Utils.SendClaimEmail("kyawzaymoore@codigo.co", subject + " Test", emailContent, attachments, "aungwaiwaithin@codigo.co");

                Utils.SendServicingEmail(ServicingEmail, subject, emailContent
                            , attachments, "aungwaiwaithin@codigo.co", unitOfWork);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"RunBackgroundCodeAsync Error | Ex message : {ex.Message} | Exception {ex}");
                MobileErrorLog("Servicing Request Email Error", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

            }

            #endregion

        }

        public async Task<(List<byte[]>, List<string>)> UploadDocToCMS(ServiceWithdrawRequest model, Policy policy, ServicePolicyPaidUp entity)
        {
            try
            {

                List<byte[]> imageBytesList = new List<byte[]>();
                List<string> imageNameList = new List<string>();

                foreach (var doc in model.DocNameList)
                {
                    try
                    {
                        var fileName = doc;
                        var format = ".pdf";

                        #region #GetFileExt
                        // Use Path.GetExtension to get the file extension
                        string fileExtension = Path.GetExtension(fileName);

                        // Remove the leading dot (.) from the extension
                        if (!string.IsNullOrEmpty(fileExtension))
                        {
                            fileExtension = fileExtension.TrimStart('.');
                        }
                        #endregion

                        format = fileExtension;

                        var upload = new UploadBase64Request();
                        upload.docTypeId = "AIADOC1";

                        upload.PolicyNo = policy.PolicyNo.Substring(0, 10);

                        //upload.FileName = fileName; 

                        upload.fileName = doc; //TODO

                        upload.format = format;
                        upload.templateId = AppSettingsHelper.GetSetting("AiaCmsApi:TemplateId");


                        //string base64EncodedPDF = await azureStorage.GetBase64ByFileName(doc);
                        byte[] fileBytes = await azureStorage.GetByteByFileName(doc);
                        imageBytesList.Add(fileBytes);
                        imageNameList.Add(doc);

                        string base64EncodedPDF = Convert.ToBase64String(fileBytes);

                        // Construct the data URI
                        string dataURI = "";
                        //string dataURI = $"data:application/pdf;base64,{base64EncodedPDF}";
                        if (format == "jpeg" || format == "jpg")
                        {
                            dataURI = $"data:image/jpeg;base64,{base64EncodedPDF}";
                        }
                        else if (format == "png")
                        {
                            dataURI = $"data:image/png;base64,{base64EncodedPDF}";
                        }
                        else if (format == "pdf")
                        {
                            dataURI = $"data:application/pdf;base64,{base64EncodedPDF}";
                        }



                        // Output the data URI
                        Console.WriteLine(dataURI);

                        upload.file = dataURI;

                        ServicePolicyPaidUpDoc docData = new ServicePolicyPaidUpDoc();

                        docData.ServicePolicyPaidUpID = entity.ID;
                        
                        docData.CmsRequestOn = Utils.GetDefaultDate();
                        docData.DocName = doc;

                        var uploadResult = await aiaCmsApiService.UploadBase64(upload);

                        docData.UploadStatus = uploadResult?.msg;
                        docData.CmsResponseOn = Utils.GetDefaultDate();
                        docData.CmsResponse = uploadResult != null ? JsonConvert.SerializeObject(uploadResult) : null;


                        unitOfWork.GetRepository<Entities.ServicePolicyPaidUpDoc>().Add(docData);
                        unitOfWork.SaveChanges();

                        #region #Insert to ServiceMainDoc
                        try
                        {
                            unitOfWork.GetRepository<Entities.ServiceMainDoc>().Add(new ServiceMainDoc
                            {
                                Id = Guid.NewGuid(),
                                ServiceId = entity.ID,
                                MainId = entity.ID,
                                ServiceType = EnumServiceType.PolicyPaidUp.ToString(),
                                CmsRequestOn = Utils.GetDefaultDate(),
                                CmsResponseOn = Utils.GetDefaultDate(),
                                
                                CmsResponse = JsonConvert.SerializeObject(uploadResult),
                                FormId = GetServicingFormId(EnumServiceType.PolicyPaidUp),
                                DocName = doc,
                                DocType = format,
                                UploadStatus = uploadResult?.msg,
                            });

                            unitOfWork.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {

                        }
                        #endregion

                    }
                    catch (Exception ex)
                    {
                        MobileErrorLog($"ServicePolicyPaidUp => UploadBase64", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                    }

                }
                return (imageBytesList, imageNameList);
            }
            catch (System.Exception)
            {

                throw;
            }

        }

        public async Task<CaseResponse> CreateCaseToCRM(Client client, ServicePolicyPaidUp entity, Policy policy)
        {
            try
            {
                ServicePolicyPaidUp data = unitOfWork.GetRepository<ServicePolicyPaidUp>().Query(x=> x.ID == entity.ID).FirstOrDefault();

                CaseRequest crmModel = new CaseRequest();
                crmModel.CustomerInfo = new CustomerInfo();
                crmModel.CustomerInfo.ClientNumber = policy.PolicyNo.Contains("-")?(client.client_certificate!=null?client.client_certificate:client.ClientNo):client.ClientNo;
                crmModel.CustomerInfo.FirstName = client?.Name;
                crmModel.CustomerInfo.LastName = client?.Name;
                crmModel.CustomerInfo.Email = client?.Email;

                crmModel.PolicyInfo = new aia_core.Model.AiaCrm.PolicyInfo();
                if (policy != null)
                {
                    crmModel.PolicyInfo.PolicyNumber = policy?.PolicyNo;
                }

                crmModel.RequestInfo = new aia_core.Model.AiaCrm.Request();
                crmModel.RequestInfo.CaseCategory = "CC003";
                crmModel.RequestInfo.Channel = "100004";
                crmModel.RequestInfo.CaseType = "Member and Agent";
                crmModel.RequestInfo.RequestId = data.ID.ToString();
                crmModel.RequestInfo.BankName = data.BankCode;
                crmModel.RequestInfo.BankAccountName = data.BankAccountName;
                crmModel.RequestInfo.BankAccountNo = data.BankAccountNumber;


                data.CrmRequestOn = Utils.GetDefaultDate();

                MobileErrorLog("ServicePolicyPaidUp aiaCrmApiService => CreateCase", "Request"
                    , JsonConvert.SerializeObject(crmModel), "v1/claim/claim");

                CaseResponse crmResponse = await aiaCrmApiService.CreateCase(crmModel);


                MobileErrorLog("ServicePolicyPaidUp aiaCrmApiService => CreateCase", "Response"
                    , JsonConvert.SerializeObject(crmResponse), "v1/claim/claim");

                #region #UpdateClaimTran
                try
                {
                    data.CrmRequest = JsonConvert.SerializeObject(crmModel);
                    data.CrmResponse = JsonConvert.SerializeObject(crmResponse);
                    data.CrmResponseOn = Utils.GetDefaultDate();

                    unitOfWork.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    MobileErrorLog("update ServicePolicyPaidUp tran ex after Crm call", ex.Message
                        , JsonConvert.SerializeObject(ex), "v1/servicing/submit");
                }
                #endregion
                return crmResponse;
            }
            catch (System.Exception ex)
            {
                MobileErrorLog($"CreateCaseToCRM => UploadBase64", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return null;
            }

        }

        private async Task<byte[]> GenerateServicingPdf(ServicePolicyPaidUp data, string pdfFileName, string holderName, string policyNumber, string policyStatus, string clientNo)
        {
            var path = Path.Combine(
                    this.environment.ContentRootPath, "email_templates/", "servicing_request_two.html");

            var htmlData = File.ReadAllText(path);

            htmlData = htmlData.Replace("{{Title}}", "Request for Policy Paid Up");
            htmlData = htmlData.Replace("{{HolderName}}", holderName);
            htmlData = htmlData.Replace("{{ClientNo}}", clientNo);

            htmlData = htmlData.Replace("{{ChangeType}}", "Policy Paid Up");

            //Entities.PolicyStatus _policyStatus = await unitOfWork.GetRepository<Entities.PolicyStatus>().Query(x => x.ShortDesc == policyStatus).FirstOrDefaultAsync();

            htmlData = htmlData.Replace("{{PolicyNumber}}", policyNumber);
            htmlData = htmlData.Replace("{{PolicyStatus}}", policyStatus);
            htmlData = htmlData.Replace("{{SubmissionDate}}", data.CreatedOn.Value.ToString("dd MMM yyyy"));
            htmlData = htmlData.Replace("{{RequestDate}}", data.CreatedOn.Value.ToString("dd MMM yyyy"));
            htmlData = htmlData.Replace("{{RequestId}}", data.ID.ToString());

            htmlData = htmlData.Replace("{{ReasonOfRequest}}", data.Reason.ToString());
            htmlData = htmlData.Replace("{{Amount}}", data.Amount.ToString());
            htmlData = htmlData.Replace("{{FormID}}", "POSFRM1");


            string signatureBase64 = await azureStorage.GetBase64ByFileName(data.SignatureImage);
            string signatureMimeType = "image/jpg";
            string signatureDataUrl = $"data:{signatureMimeType};base64,{signatureBase64}";
            htmlData = htmlData.Replace("{{signDataUrl}}", signatureDataUrl ?? "");

            // MobileErrorLog("GenerateServicingPdf HealthRenewal html", ""
            //                 , htmlData, "v1/servicing/submit");

            try
            {
                var htmlToPdfDocument = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                                ColorMode = ColorMode.Color,
                                Orientation = Orientation.Portrait,
                                PaperSize = PaperKind.A4Extra,
                            },
                    Objects = {
                                    new ObjectSettings()
                                    {
                                        PagesCount = true,
                                        HtmlContent = htmlData  ,
                                        WebSettings = { DefaultEncoding = "utf-8" },
                                        HeaderSettings =
                                        { FontSize = 9, Right = "Page [page] of [toPage]", Line = true, Spacing = 2.812 }

                                    }

                                }
                };

                byte[] pdfData = this.converter.Convert(htmlToPdfDocument);

                IFormFile file = ConvertByteArrayToIFormFile(pdfData, pdfFileName);
                azureStorage.UploadAsync(pdfFileName, file);

                return pdfData;
            }
            catch (Exception ex)
            {
                MobileErrorLog("Dink2Pdf exception", ex.Message
                    , JsonConvert.SerializeObject(ex), "v1/claim/claim");
            }

            return null;
        }

        private IFormFile ConvertByteArrayToIFormFile(byte[] byteArray, string fileName)
        {
            // Create a MemoryStream from the byte array
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                // Create an instance of FormFile
                IFormFile formFile = new FormFile(stream, 0, byteArray.Length, "name", fileName);

                return formFile;
            }
        }

        private async Task<string> UpadeGeneratePdfToCMS(byte[] pdfData, string policyNo, string fileName, ServicePolicyPaidUp model)
        {
            var upload = new UploadBase64Request();

            string base64String = Convert.ToBase64String(pdfData);

            var dataUrl = "";
            if (!base64String.StartsWith("data:application/pdf;base64,"))
            {
                dataUrl = $"data:application/pdf;base64,{base64String}";
            }
            else
            {
                dataUrl = base64String;
            }



            upload.docTypeId = "POSFRM1";
            upload.PolicyNo = policyNo;
            upload.fileName = fileName;
            upload.format = ".pdf";
            upload.templateId = AppSettingsHelper.GetSetting("AiaCmsApi:TemplateId");

            
            model.CMS_RequestOn = Utils.GetDefaultDate();

            upload.file = dataUrl;

            var uploadResult = await aiaCmsApiService.UploadBase64(upload);

            model.CMS_Response = upload != null ? JsonConvert.SerializeObject(uploadResult) : "";
            model.CMS_ResponseOn = Utils.GetDefaultDate();
            unitOfWork.SaveChangesAsync();

            #region #Insert to ServiceMainDoc

            try
            {

                var pdfUpload = await azureStorage.UploadBase64Async(fileName, pdfData);

                await unitOfWork.GetRepository<Entities.ServiceMainDoc>().AddAsync(new ServiceMainDoc
                {
                    Id = Guid.NewGuid(),
                    MainId = model.ID,
                    ServiceId = model.ID,
                    ServiceType = EnumServiceType.PolicyPaidUp.ToString(),
                    CmsRequestOn = Utils.GetDefaultDate(),
                    CmsResponseOn = Utils.GetDefaultDate(),
                   
                    CmsResponse = JsonConvert.SerializeObject(uploadResult),
                    FormId = GetServicingFormId(EnumServiceType.PolicyPaidUp, true),
                    DocName = fileName,
                    DocType = "pdf",
                    UploadStatus = uploadResult?.msg,
                });

                await unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {

            }
            #endregion

            return "";
        }

        byte[] CompressBytes(List<byte[]> inputBytesList, List<string> imageNameList)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    for (int i = 0; i < inputBytesList.Count; i++)
                    {
                        ZipArchiveEntry entry = archive.CreateEntry(imageNameList[i]); // You can customize the entry names
                        using (Stream entryStream = entry.Open())
                        {
                            entryStream.Write(inputBytesList[i], 0, inputBytesList[i].Length);
                        }
                    }
                }

                return memoryStream.ToArray();
            }
        }
    }
}