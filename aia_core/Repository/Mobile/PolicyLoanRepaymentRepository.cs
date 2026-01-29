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
    public interface IPolicyLoanRepaymentRepository
    {
        Task<ResponseModel<ServicingResponseModel>> Submit(ServiceCommonPartOne model);
    }
    public class PolicyLoanRepaymentRepository : BaseRepository, IPolicyLoanRepaymentRepository
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

        public PolicyLoanRepaymentRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
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

        public async Task<ResponseModel<ServicingResponseModel>> Submit(ServiceCommonPartOne model)
        {
            try
            {
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
                    ServicePolicyLoanRepayment entity = new ServicePolicyLoanRepayment();
                    entity.ID = Guid.NewGuid();
                    entity.PolicyNumber = model.PolicyNo;
                    entity.Amount = model.Amount;
                    entity.Reason = model.Reason;
                    entity.SignatureImage = model.SignatureImage;
                    entity.CreatedOn = Utils.GetDefaultDate();
                    entity.MemberID = memberID;
                    entity.Status = EnumServicingStatus.Received.ToString();

                    unitOfWork.GetRepository<Entities.ServicePolicyLoanRepayment>().Add(entity);

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
                    serviceMain.ServiceType = EnumServiceType.PolicyLoanRepayment.ToString();
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

                    
                    

                    //Task.Run(() => RunBackgroundCodeAsync(entity, client, policy, model));
                    await CreateCaseToCRM(client, entity, policy);
                    //await UploadDocToCMS(model, policy,entity);

                    try
                    {
                        //Notification
                        //MobileErrorLog("Notification Send HealthRenewal", "", "", httpContext?.HttpContext.Request.Path);
                        _ = notificationService.SendServicingNoti(memberID ?? new Guid(), entity.ID, EnumServicingStatus.Received, EnumServiceType.PolicyLoanRepayment, model.PolicyNo);
                    }
                    catch (System.Exception ex)
                    {
                        MobileErrorLog("Notification Error", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                    }

                    var pdfFileName = $"{entity.PolicyNumber}_AIA+_{entity.ID}_{DateTime.Now.ToString("yyyyMMddhhmmss")}.pdf";
                    byte[] pdfData = await GenerateServicingPdf(entity, pdfFileName, client.Name, entity.PolicyNumber, _policyStatus.LongDesc, client?.ClientNo);
                    //AIA CMS
                    _ = UpadeGeneratePdfToCMS(pdfData, entity.PolicyNumber, pdfFileName, entity);

                    // Task.Run(() => RunBackgroundCodeAsync(entity, client, policy, model, memberID, ServicingEmail, pdfFileName, pdfData));
                    _ = RunBackgroundCodeAsync(entity, client, policy, model, memberID, ServicingEmail, pdfFileName, pdfData);

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
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<ServicingResponseModel>(ErrorCode.E500);
            }
        }

        private async Task RunBackgroundCodeAsync(ServicePolicyLoanRepayment entity, Client client, Policy policy, ServiceCommonPartOne model, Guid? memberID, string ServicingEmail, string pdfFileName, byte[] pdfData)
        {
            MobileErrorLog("RunBackgroundCodeAsync ServicePolicyLoanRepayment", "", "", httpContext?.HttpContext.Request.Path);
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
                MobileErrorLog("attachments Count ServicePolicyLoanRepayment", attachments.Count.ToString(), "", httpContext?.HttpContext.Request.Path);


                string subject = $"{entity.PolicyNumber}/Policy Loan Repayment/{client.Name}";



                var path = Path.Combine(
                this.environment.ContentRootPath, "email_templates/", "servicing_email_content_two.html");
                var emailContent = File.ReadAllText(path);
                emailContent = emailContent.Replace("{{EmailContent}}", subject);
                emailContent = emailContent.Replace("{{UniqueID}}", entity.ID.ToString());

                emailContent = emailContent.Replace("{{BankInfo}}", "");

                //Utils.SendClaimEmail("kyawzaymoore@codigo.co", subject + " Test", emailContent, attachments, "aungwaiwaithin@codigo.co");

                Utils.SendServicingEmail(ServicingEmail, subject, emailContent
                            , attachments, "aungwaiwaithin@codigo.co", unitOfWork);
            }
            catch (System.Exception ex)
            {
                MobileErrorLog("Servicing Request Email Error", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

            }

            #endregion

        }

        public async Task<(List<byte[]>, List<string>)> UploadDocToCMS(ServiceCommonPartOne model, Policy policy, ServicePolicyLoanRepayment entity)
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

                        ServicePolicyLoanRepaymentDoc docData = new ServicePolicyLoanRepaymentDoc();

                        docData.ServicePolicyLoanRepaymentID = entity.ID;
                        
                        docData.CmsRequestOn = Utils.GetDefaultDate();
                        docData.DocName = doc;

                        var uploadResult = await aiaCmsApiService.UploadBase64(upload);

                        docData.UploadStatus = uploadResult?.msg;
                        docData.CmsResponseOn = Utils.GetDefaultDate();
                        docData.CmsResponse = uploadResult != null ? JsonConvert.SerializeObject(uploadResult) : null;


                        unitOfWork.GetRepository<Entities.ServicePolicyLoanRepaymentDoc>().Add(docData);
                        unitOfWork.SaveChanges();


                        #region #Insert to ServiceMainDoc
                        try
                        {
                            unitOfWork.GetRepository<Entities.ServiceMainDoc>().Add(new ServiceMainDoc
                            {
                                Id = Guid.NewGuid(),
                                ServiceId = entity.ID,
                                MainId = entity.ID,
                                ServiceType = EnumServiceType.PolicyLoanRepayment.ToString(),
                                CmsRequestOn = Utils.GetDefaultDate(),
                                CmsResponseOn = Utils.GetDefaultDate(),
                                
                                CmsResponse = JsonConvert.SerializeObject(uploadResult),
                                FormId = GetServicingFormId(EnumServiceType.PolicyLoanRepayment),
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
                        MobileErrorLog($"ServicePolicyLoanRepayment => UploadBase64", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                    }

                }
                return (imageBytesList, imageNameList);
            }
            catch (System.Exception)
            {

                throw;
            }

        }

        public async Task<string> CreateCaseToCRM(Client client, ServicePolicyLoanRepayment entity, Policy policy)
        {
            try
            {
                ServicePolicyLoanRepayment data = unitOfWork.GetRepository<ServicePolicyLoanRepayment>().Query(x=> x.ID == entity.ID).FirstOrDefault();

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
                crmModel.RequestInfo.CaseCategory = "CC011";
                crmModel.RequestInfo.Channel = "100004";
                crmModel.RequestInfo.CaseType = "Member and Agent";
                crmModel.RequestInfo.RequestId = data.ID.ToString();


                data.CrmRequestOn = Utils.GetDefaultDate();

                MobileErrorLog("PolicyLoanRepayment aiaCrmApiService => CreateCase", "Request"
                    , JsonConvert.SerializeObject(crmModel), "v1/claim/claim");

                var crmResponse = await aiaCrmApiService.CreateCase(crmModel);


                MobileErrorLog("PolicyLoanRepayment aiaCrmApiService => CreateCase", "Response"
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
                    MobileErrorLog("update PolicyLoanRepayment tran ex after Crm call", ex.Message
                        , JsonConvert.SerializeObject(ex), "v1/servicing/submit");
                }
                #endregion
                return "";
            }
            catch (System.Exception ex)
            {
                MobileErrorLog($"CreateCaseToCRM => UploadBase64", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return "";
            }

        }

        private async Task<byte[]> GenerateServicingPdf(ServicePolicyLoanRepayment data, string pdfFileName, string holderName, string policyNumber, string policyStatus, string clientNo)
        {
            var path = Path.Combine(
                    this.environment.ContentRootPath, "email_templates/", "servicing_request_two.html");

            var htmlData = File.ReadAllText(path);

            htmlData = htmlData.Replace("{{Title}}", "Request for Policy Loan Repayment");
            htmlData = htmlData.Replace("{{HolderName}}", holderName);
            htmlData = htmlData.Replace("{{ClientNo}}", clientNo);

            htmlData = htmlData.Replace("{{ChangeType}}", "Policy Loan Repayment");

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

            MobileErrorLog("GenerateServicingPdf HealthRenewal html", ""
                            , htmlData, "v1/servicing/submit");

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

        private async Task<string> UpadeGeneratePdfToCMS(byte[] pdfData, string policyNo, string fileName, ServicePolicyLoanRepayment model)
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
            await unitOfWork.SaveChangesAsync();


            #region #Insert to ServiceMainDoc
            try
            {
                var azureUpload = await azureStorage.UploadBase64Async(fileName, pdfData);
                Console.WriteLine($"PolicyLoanRepayment => azureStorage.UploadBase64Async => {JsonConvert.SerializeObject(azureUpload)}");


                await unitOfWork.GetRepository<Entities.ServiceMainDoc>().AddAsync(new ServiceMainDoc
                {
                    Id = Guid.NewGuid(),
                    MainId = model.ID,
                    ServiceId = model.ID,
                    ServiceType = EnumServiceType.PolicyLoanRepayment.ToString(),
                    CmsRequestOn = Utils.GetDefaultDate(),
                    CmsResponseOn = Utils.GetDefaultDate(),
                    
                    CmsResponse = JsonConvert.SerializeObject(uploadResult),
                    FormId = GetServicingFormId(EnumServiceType.PolicyLoanRepayment, true),
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