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
using Microsoft.Extensions.DependencyInjection;
using aia_core.Model.Cms.Response;
using DocumentFormat.OpenXml.Vml.Office;

namespace aia_core.Repository.Mobile
{
    public interface IBeneficiariesRepository
    {
        Task<ResponseModel<ServicingResponseModel>> Submit(ServiceBeneficiariesRequest model);
    }
    public class BeneficiariesRepository : BaseRepository, IBeneficiariesRepository
    {
        #region "const"

        private readonly ICommonRepository commonRepository;
        private readonly IAiaILApiService aiaILApiService;
        private readonly IHostingEnvironment environment;
        private readonly IConverter converter;
        private readonly IAiaCmsApiService aiaCmsApiService;
        private readonly IAiaCrmApiService aiaCrmApiService;
        private readonly INotificationService notificationService;
        private readonly IServiceProvider serviceProvider;
        private readonly IServiceScopeFactory serviceFactory;
        #endregion

        public BeneficiariesRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork,
            ICommonRepository commonRepository,
            IAiaILApiService aiaILApiService,
            IHostingEnvironment environment,
            IConverter converter,
            IAiaCrmApiService aiaCrmApiService,
            IAiaCmsApiService aiaCmsApiService,
            INotificationService notificationService,
            IServiceProvider serviceProvider,
            IServiceScopeFactory serviceFactory)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.commonRepository = commonRepository;
            this.aiaILApiService = aiaILApiService;
            this.environment = environment;
            this.converter = converter;
            this.aiaCmsApiService = aiaCmsApiService;
            this.aiaCrmApiService = aiaCrmApiService;
            this.notificationService = notificationService;
            this.serviceProvider = serviceProvider;
            this.serviceFactory = serviceFactory;
            {

            }
        }

        public async Task<ResponseModel<ServicingResponseModel>> Submit(ServiceBeneficiariesRequest model)
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
                    Guid MainID = Guid.NewGuid();
                    Guid responseServicingID = Guid.NewGuid();
                    foreach (var item in model.ServiceBeneficiaryShare)
                    {
                        ServiceBeneficiary entity = new ServiceBeneficiary();
                        entity.ID = Guid.NewGuid();
                        entity.MainID = MainID;
                        entity.PolicyNumber = item.PolicyNo;
                        entity.SignatureImage = model.SignatureImage;
                        entity.CreatedOn = Utils.GetDefaultDate();
                        entity.MemberID = memberID;
                        entity.Status = EnumServicingStatus.Received.ToString();

                        unitOfWork.GetRepository<Entities.ServiceBeneficiary>().Add(entity);

                        responseServicingID = entity.ID;

                        #region "Service Main"
                        (string producttype, string productname) = GetProductInfo(item.PolicyNo);
                        (string? membertype, string? aiaMemberID, string? groupMemberId) = GetClientInfo(memberID);
                        Policy policy = unitOfWork.GetRepository<Policy>().Query(x => x.PolicyNo == item.PolicyNo).FirstOrDefault();
                        Client client = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == policy.PolicyHolderClientNo || x.ClientNo == policy.InsuredPersonClientNo).FirstOrDefault();
                        Entities.PolicyStatus _policyStatus = await unitOfWork.GetRepository<Entities.PolicyStatus>().Query(x => x.ShortDesc == policy.PolicyStatus).FirstOrDefaultAsync();
                        Member member = unitOfWork.GetRepository<Member>().Query(x => x.MemberId == memberID).FirstOrDefault();

                        ServiceMain serviceMain = new ServiceMain();
                        Guid guid = Guid.NewGuid();
                        serviceMain.ID = Guid.NewGuid();
                        serviceMain.MainID = MainID;
                        serviceMain.ServiceID = entity.ID;
                        serviceMain.ServiceType = EnumServiceType.BeneficiaryInformation.ToString();
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
                        serviceMain.FERequest = JsonConvert.SerializeObject(model);
                        unitOfWork.GetRepository<Entities.ServiceMain>().Add(serviceMain);
                        #endregion

                        List<Beneficiary> originalBeneficiaryData = unitOfWork.GetRepository<Beneficiary>().Query(x => x.PolicyNo == item.PolicyNo).ToList();
                        List<string> originalBeneficiares = originalBeneficiaryData.Select(s => s.BeneficiaryClientNo).ToList();
                        List<string> requestBeneficiares = item.ServiceBeneficiary.Where(x => x.IsNewBeneficiary == false).Select(s => s.ClientNo).ToList();

                        List<string> removedBeneficiares = originalBeneficiares.Except(requestBeneficiares).ToList();
                        List<string> updatedBeneficiares = originalBeneficiares.Intersect(requestBeneficiares).ToList();
                        //List<ServiceBeneficiaryModel> addedBeneficiares = item.ServiceBeneficiary.Where(x=>x.IsNewBeneficiary==true).ToList();
                        List<string> addedBeneficiares = requestBeneficiares.Except(originalBeneficiares).ToList();

                        //Removed Beneficiares Share
                        foreach (var _remove in removedBeneficiares)
                        {

                            var oldBeneficiary = unitOfWork.GetRepository<Entities.Beneficiary>()
                                .Query(x => x.BeneficiaryClientNo == _remove && x.PolicyNo == item.PolicyNo)                                
                                .FirstOrDefault(); //TLS

                            var _relationship = unitOfWork.GetRepository<Entities.Relationship>()
                                .Query(x => x.Code == oldBeneficiary.Relationship)
                                .FirstOrDefault(); //TLS

                            var rsName = string.IsNullOrEmpty(_relationship?.Name) ? oldBeneficiary?.Relationship : _relationship?.Name;

                            ServiceBeneficiaryShareInfo shareInfo = new ServiceBeneficiaryShareInfo();
                            shareInfo.ID = Guid.NewGuid();
                            shareInfo.ServiceBeneficiaryID = entity.ID;
                            shareInfo.ClientNo = _remove;
                            shareInfo.Type = EnumBeneficiaryShareInfoType.Remove.ToString();
                            shareInfo.CreatedOn = Utils.GetDefaultDate();
                            shareInfo.OldPercentage = oldBeneficiary?.Percentage; //TLS
                            shareInfo.NewPercentage = oldBeneficiary?.Percentage; //TLS
                            shareInfo.NewRelationShipCode = rsName;
                            shareInfo.OldRelationShipCode = rsName;

                            unitOfWork.GetRepository<Entities.ServiceBeneficiaryShareInfo>().Add(shareInfo);
                        }

                        //Updated Beneficiares Share
                        foreach (var _update in updatedBeneficiares)
                        {
                            Beneficiary oldData = originalBeneficiaryData.Where(x => x.BeneficiaryClientNo == _update).FirstOrDefault();
                            ServiceBeneficiaryModel updateData = item.ServiceBeneficiary.Where(x => x.ClientNo == _update).FirstOrDefault();

                            ServiceBeneficiaryShareInfo shareInfo = new ServiceBeneficiaryShareInfo();
                            shareInfo.ID = Guid.NewGuid();
                            shareInfo.ServiceBeneficiaryID = entity.ID;
                            shareInfo.ClientNo = _update;
                            shareInfo.Type = EnumBeneficiaryShareInfoType.Update.ToString();
                            shareInfo.OldRelationShipCode = oldData.Relationship;
                            shareInfo.NewRelationShipCode = updateData.RelationShipCode;
                            shareInfo.OldPercentage = oldData.Percentage;
                            shareInfo.NewPercentage = updateData.Percentage;

                            shareInfo.CreatedOn = Utils.GetDefaultDate();
                            unitOfWork.GetRepository<Entities.ServiceBeneficiaryShareInfo>().Add(shareInfo);
                        }

                        //Added Beneficiares Share
                        foreach (var _add in addedBeneficiares)
                        {
                            ServiceBeneficiaryModel newAddInfo = item.ServiceBeneficiary.Where(x => x.ClientNo == _add).FirstOrDefault();

                            ServiceBeneficiaryShareInfo shareInfo = new ServiceBeneficiaryShareInfo();
                            shareInfo.ID = Guid.NewGuid();
                            shareInfo.ClientNo = _add;
                            shareInfo.ServiceBeneficiaryID = entity.ID;
                            shareInfo.Type = EnumBeneficiaryShareInfoType.New.ToString();
                            shareInfo.NewRelationShipCode = newAddInfo.RelationShipCode;
                            shareInfo.NewPercentage = newAddInfo.Percentage;

                            shareInfo.CreatedOn = Utils.GetDefaultDate();
                            unitOfWork.GetRepository<Entities.ServiceBeneficiaryShareInfo>().Add(shareInfo);
                        }

                        //Added New Beneficiares Share
                        List<ServiceBeneficiaryModel> newBeneficiary = item.ServiceBeneficiary.Where(x => x.IsNewBeneficiary == true).ToList();
                        foreach (var _add in newBeneficiary)
                        {
                            ServiceBeneficiaryShareInfo shareInfo = new ServiceBeneficiaryShareInfo();
                            shareInfo.ID = Guid.NewGuid();
                            shareInfo.ServiceBeneficiaryID = entity.ID;
                            shareInfo.Type = EnumBeneficiaryShareInfoType.New.ToString();
                            shareInfo.NewRelationShipCode = _add.RelationShipCode;
                            shareInfo.NewPercentage = _add.Percentage;
                            shareInfo.IdValue = _add.IdValue;

                            shareInfo.CreatedOn = Utils.GetDefaultDate();
                            unitOfWork.GetRepository<Entities.ServiceBeneficiaryShareInfo>().Add(shareInfo);

                            //Save New Beneficiares Person Info
                            NewBeneficiariesModel newPersonInfo = model.NewBeneficiaries.Where(x => x.IdValue == _add.IdValue).FirstOrDefault();
                            ServiceBeneficiaryPersonalInfo personInfo = new ServiceBeneficiaryPersonalInfo();
                            personInfo.ID = Guid.NewGuid();
                            personInfo.ServiceBeneficiaryID = entity.ID;
                            personInfo.ServiceBeneficiaryShareID = shareInfo.ID;
                            personInfo.IsNewBeneficiary = true;
                            personInfo.Name = newPersonInfo.Name;
                            personInfo.Gender = newPersonInfo.Gender;
                            personInfo.Dob = newPersonInfo.DateOfBirth;
                            personInfo.OldMobileNumber = newPersonInfo.MobileNo;
                            personInfo.IdType = newPersonInfo.IdType;
                            personInfo.IdValue = newPersonInfo.IdValue;
                            personInfo.NewMobileNumber = newPersonInfo.MobileNo;
                            personInfo.MobileNumber = newPersonInfo.MobileNo;
                            personInfo.CreatedOn = Utils.GetDefaultDate();
                            personInfo.IdFrontImageName = newPersonInfo.IdFrontImageId;
                            personInfo.IdBackImageName = newPersonInfo.IdBackImageId;

                            unitOfWork.GetRepository<Entities.ServiceBeneficiaryPersonalInfo>().Add(personInfo);
                        }

                        foreach (var updatePerson in model.ExistingBeneficiaries)
                        {
                            ServiceBeneficiaryPersonalInfo personInfo = new ServiceBeneficiaryPersonalInfo();
                            personInfo.ID = Guid.NewGuid();
                            personInfo.ServiceBeneficiaryID = entity.ID;
                            personInfo.IsNewBeneficiary = false;
                            personInfo.ClientNo = updatePerson.ClientNo;
                            personInfo.Name = updatePerson.Name;
                            personInfo.OldMobileNumber = updatePerson.OldMobileNumber;
                            personInfo.NewMobileNumber = updatePerson.NewMobileNumber;
                            personInfo.MobileNumber = updatePerson.NewMobileNumber;

                            personInfo.CreatedOn = Utils.GetDefaultDate();

                            unitOfWork.GetRepository<Entities.ServiceBeneficiaryPersonalInfo>().Add(personInfo);
                        }

                    }
                    unitOfWork.SaveChanges();

                    _ = RunBackgroudILSubmit(MainID, model, memberID);

                    return errorCodeProvider.GetResponseModel<ServicingResponseModel>(ErrorCode.E0,
                    new ServicingResponseModel()
                    {
                        servicingId = responseServicingID.ToString()
                    });
                }
                else
                {
                    return new ResponseModel<ServicingResponseModel> { Code = 401, Message = "Invalid OtpCode or expired." };
                }

            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"BeneficiariesRepository Submit Error | Ex message : {ex.Message} | Exception {ex}");
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<ServicingResponseModel>(ErrorCode.E500);
            }
        }

        private async Task RunBackgroudILSubmit(Guid MainID, ServiceBeneficiariesRequest model, Guid? memberID)
        {
            try
            {
                Console.WriteLine("Inside RunBackgroudILSubmit");
                List<ServiceBeneficiary> _beneficiaryList = new List<ServiceBeneficiary>();
                using (var scope = this.serviceFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork<aia_core.Entities.Context>>();
                    AppConfig appConfig = unitOfWork.GetRepository<AppConfig>().Query().FirstOrDefault();
                    string ServicingEmail = appConfig.ServicingEmail;

                    List<ServiceBeneficiary> beneficiaryList = unitOfWork.GetRepository<ServiceBeneficiary>().Query(x => x.MainID == MainID).ToList();
                    _beneficiaryList = beneficiaryList;
                    foreach (var item in beneficiaryList)
                    {
                        #region "IL"
                        List<ServiceBeneficiaryShareInfo> serviceBeneficiaryShareList = unitOfWork.GetRepository<ServiceBeneficiaryShareInfo>().Query(x => x.ServiceBeneficiaryID == item.ID
                        && x.Type != EnumBeneficiaryShareInfoType.Remove.ToString()).ToList();

                        ILBeneficiariesRequest request = new ILBeneficiariesRequest();
                        request.requestType = "CBR";


                        #region #DeletedBeneficiary
                        List<string> removedClientNoList = unitOfWork.GetRepository<ServiceBeneficiaryShareInfo>()
                            .Query(x => x.ServiceBeneficiaryID == item.ID && x.Type == EnumBeneficiaryShareInfoType.Remove.ToString())
                            .Select(x => x.ClientNo)
                            .ToList();

                        if (removedClientNoList?.Any() == true)
                        {
                            var removedClientNoAsString = string.Join(",", removedClientNoList);
                            var removedNote = $"STP: Beneficiary removed: [{removedClientNoAsString}]. Letter printed in [{item.PolicyNumber}]";

                            request.noteList = new List<BeneficiariesNoteListModel>
                            {
                                new BeneficiariesNoteListModel
                                {
                                    noteType = "MYX",
                                    notes = new List<object> { removedNote },
                                }
                            };
                        }

                        
                        #endregion

                        request.policy = new BeneficiariesPolicyModel();
                        request.policy.policyNumber = item.PolicyNumber;

                        request.policy.beneficiaries = new List<ILBeneficiaryModel>();

                        foreach (var i in serviceBeneficiaryShareList)
                        {
                            if (!String.IsNullOrEmpty(i.ClientNo))
                            {
                                Client client = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == i.ClientNo).FirstOrDefault();

                                string idtype = ""; string idvalue = "";
                                if (!String.IsNullOrEmpty(client.Nrc))
                                {
                                    idtype = "N";
                                    idvalue = client.Nrc;
                                }
                                else if (!String.IsNullOrEmpty(client.PassportNo))
                                {
                                    idtype = "X";
                                    idvalue = client.PassportNo;
                                }
                                else if (!String.IsNullOrEmpty(client.Other))
                                {
                                    idtype = "O";
                                    idvalue = client.Other;
                                }

                                ServiceBeneficiaryPersonalInfo updateInfoData = unitOfWork.GetRepository<ServiceBeneficiaryPersonalInfo>().Query(x => x.ClientNo == i.ClientNo).FirstOrDefault();

                                request.policy.beneficiaries.Add(
                                    new ILBeneficiaryModel()
                                    {
                                        action = i.Type == EnumBeneficiaryShareInfoType.New.ToString() ? "add" : "update",
                                        clientNumber = i.ClientNo,
                                        dob = client.Dob,
                                        gender = client.Gender,
                                        idnumber = idvalue,
                                        idtype = idtype,
                                        name = client.Name,
                                        percentage = i.NewPercentage.ToString(),
                                        phone = updateInfoData != null ? updateInfoData.NewMobileNumber : client.PhoneNo,
                                        relation = i.NewRelationShipCode,
                                        updateClientLevel = "2"
                                    });
                            }
                            else
                            {
                                var _new = model.NewBeneficiaries.Where(x => x.IdValue == i.IdValue).FirstOrDefault();

                                string idtype = "";
                                if (_new.IdType.ToLower() == "nrc")
                                {
                                    idtype = "N";
                                }
                                else if (_new.IdType.ToLower() == "passport")
                                {
                                    idtype = "X";
                                }
                                else if (_new.IdType.ToLower() == "other" || _new.IdType.ToLower() == "others")
                                {
                                    idtype = "O";
                                }

                                request.policy.beneficiaries.Add(
                                    new ILBeneficiaryModel()
                                    {
                                        action = i.Type == EnumBeneficiaryShareInfoType.New.ToString() ? "add" : "update",
                                        clientNumber = i.ClientNo,
                                        dob = _new.DateOfBirth,
                                        gender = _new.Gender,
                                        idnumber = _new.IdValue,
                                        idtype = idtype,
                                        name = _new.Name,
                                        percentage = i.NewPercentage.ToString(),
                                        phone = _new.MobileNo,
                                        relation = i.NewRelationShipCode,
                                        updateClientLevel = "2"
                                    });
                            }
                        }


                        item.ILRequest = JsonConvert.SerializeObject(request);
                        item.ILRequestOn = Utils.GetDefaultDate();

                        //AIA IL
                        CommonRegisterResponse ilResponse = await aiaILApiService.BeneficiariesSubmission(request);

                        item.ILResponse = JsonConvert.SerializeObject(ilResponse);
                        item.ILResponseOn = Utils.GetDefaultDate();

                        unitOfWork.GetRepository<Entities.ServiceBeneficiary>().Update(item);
                        unitOfWork.SaveChanges();

                        try
                        {
                            ServiceMain sm = unitOfWork.GetRepository<ServiceMain>().Query(x => x.ServiceID == item.ID).FirstOrDefault();
                            sm.ILStatus = ilResponse.message.type;
                            sm.ILMessage = ilResponse.data.errorMessage;
                            unitOfWork.GetRepository<Entities.ServiceMain>().Update(sm);
                        }
                        catch (System.Exception ex)
                        {
                            Console.WriteLine("ServiceMain IL Save Error : " + ex);
                        }
                        #endregion

                        #region "Crm"
                        Policy policy = unitOfWork.GetRepository<Policy>().Query(x => x.PolicyNo == item.PolicyNumber).FirstOrDefault();
                        Client crmClient = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == policy.PolicyHolderClientNo || x.ClientNo == policy.InsuredPersonClientNo).FirstOrDefault();

                        CaseRequest crmModel = new CaseRequest();
                        crmModel.CustomerInfo = new CustomerInfo();
                        crmModel.CustomerInfo.ClientNumber = crmClient.ClientNo;
                        crmModel.CustomerInfo.FirstName = crmClient?.Name;
                        crmModel.CustomerInfo.LastName = crmClient?.Name;
                        crmModel.CustomerInfo.Email = crmClient?.Email;

                        crmModel.PolicyInfo = new aia_core.Model.AiaCrm.PolicyInfo();
                        if (policy != null)
                        {
                            crmModel.PolicyInfo.PolicyNumber = policy?.PolicyNo;
                        }

                        crmModel.RequestInfo = new aia_core.Model.AiaCrm.Request();
                        crmModel.RequestInfo.CaseCategory = "CC009";
                        crmModel.RequestInfo.Channel = "100004";
                        crmModel.RequestInfo.CaseType = "Member and Agent";
                        crmModel.RequestInfo.RequestId = item.ID.ToString();
                        item.CrmRequestOn = Utils.GetDefaultDate();

                        var crmResponse = await aiaCrmApiService.CreateCase(crmModel);
                        Console.WriteLine("crmResponse Response");

                        item.CrmRequest = JsonConvert.SerializeObject(crmModel);
                        item.CrmResponse = JsonConvert.SerializeObject(crmResponse);
                        item.CrmResponseOn = Utils.GetDefaultDate();
                        Console.WriteLine("Before ServiceBeneficiary Update");
                        unitOfWork.GetRepository<Entities.ServiceBeneficiary>().Update(item);
                        unitOfWork.SaveChanges();
                        #endregion

                        #region "PDF"
                        //(string producttype, string productname) = GetProductInfo(item.PolicyNumber);
                        var temppolicy = unitOfWork.GetRepository<Entities.Policy>().Query(x => x.PolicyNo == item.PolicyNumber).FirstOrDefault();
                        var product = unitOfWork.GetRepository<Entities.Product>().Query(x => x.ProductTypeShort == temppolicy.ProductType && x.IsActive == true && x.IsDelete == false)
                            .Select(x => new { x.ProductTypeShort, x.TitleEn })
                            .FirstOrDefault();
                        string producttype = product?.ProductTypeShort;
                        string productname = product?.TitleEn;
                        
                        //Policy policy = unitOfWork.GetRepository<Policy>().Query(x => x.PolicyNo == item.PolicyNumber).FirstOrDefault();
                        Client _client = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == policy.PolicyHolderClientNo || x.ClientNo == policy.InsuredPersonClientNo).FirstOrDefault();
                        Entities.PolicyStatus _policyStatus = await unitOfWork.GetRepository<Entities.PolicyStatus>().Query(x => x.ShortDesc == policy.PolicyStatus).FirstOrDefaultAsync();
                        Member member = unitOfWork.GetRepository<Member>().Query(x => x.MemberId == memberID).FirstOrDefault();

                        var pdfFileName = $"{item.PolicyNumber}_AIA+_{item.ID}_{DateTime.Now.ToString("yyyyMMddhhmmss")}.pdf";

                        byte[] pdfData = await GenerateServicingPdf(item, pdfFileName, _client.Name, item.PolicyNumber, _policyStatus.LongDesc, _client?.ClientNo);



                        #region #Insert to ServiceMainDoc

                        var pdfUpload = azureStorage.UploadBase64Async(pdfFileName, pdfData);

                        var uploadId = Guid.NewGuid();

                        unitOfWork.GetRepository<Entities.ServiceMainDoc>().AddAsync(new ServiceMainDoc
                        {
                            Id = uploadId,
                            MainId = MainID,
                            ServiceId = item.ID,
                            ServiceType = EnumServiceType.BeneficiaryInformation.ToString(),
                            CmsRequestOn = Utils.GetDefaultDate(),
                            CmsResponseOn = Utils.GetDefaultDate(),
                            CmsReqeust = "",
                            CmsResponse = "",
                            FormId = GetServicingFormId(EnumServiceType.BeneficiaryInformation, true),
                            DocName = pdfFileName,
                            DocType = "pdf",
                            UploadStatus = "Success",
                        });

                        unitOfWork.SaveChangesAsync();

                        #endregion

                        await UpadeGeneratePdfToCMS(pdfData, item.PolicyNumber, pdfFileName, item, uploadId);

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


                            string subject = $"{item.PolicyNumber}/Beneficiaries/{_client.Name}";

                            var path = Path.Combine(
                            this.environment.ContentRootPath, "email_templates/", "servicing_email_content.html");
                            var emailContent = File.ReadAllText(path);
                            emailContent = emailContent.Replace("{{EmailContent}}", subject);
                            emailContent = emailContent.Replace("{{UniqueID}}", item.ID.ToString());

                            emailContent = emailContent.Replace("{{ILStatus}}", ilResponse.message.type);
                            emailContent = emailContent.Replace("{{ILErrorMessage}}", ilResponse.data.errorMessage);

                            emailContent = emailContent.Replace("{{BankInfo}}", "");

                            //Utils.SendClaimEmail("kyawzaymoore@codigo.co", subject + " Test", emailContent, attachments, "aungwaiwaithin@codigo.co");

                            Utils.SendServicingEmail(ServicingEmail, subject, emailContent
                                        , attachments, "aungwaiwaithin@codigo.co", unitOfWork);
                        }
                        catch (System.Exception ex)
                        {
                            Console.WriteLine($"RunBackgroundCodeAsync Error | Ex message : {ex.Message} | Exception {ex}");
                            MobileErrorLog("Servicing PaymentFrequency Email Error", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                        }

                        #endregion
                        #endregion

                    }
                    List<Guid> guids = beneficiaryList.Select(s => s.ID).ToList();
                    List<ServiceBeneficiaryPersonalInfo> personalInfos = unitOfWork.GetRepository<ServiceBeneficiaryPersonalInfo>().Query(x => guids.Contains(x.ServiceBeneficiaryID.Value) && x.IsNewBeneficiary == true).ToList();

                    foreach (var item in personalInfos)
                    {
                        ServiceBeneficiary serviceBeneficiary = beneficiaryList.Where(x => x.ID == item.ServiceBeneficiaryID).FirstOrDefault();

                        #region "Front"
                        if (!String.IsNullOrEmpty(item.IdFrontImageName))
                        {
                            var fileNameFront = item.IdFrontImageName;
                            var format = ".pdf";

                            #region #GetFileExt
                            // Use Path.GetExtension to get the file extension
                            string fileExtension = Path.GetExtension(fileNameFront);

                            // Remove the leading dot (.) from the extension
                            if (!string.IsNullOrEmpty(fileExtension))
                            {
                                fileExtension = fileExtension.TrimStart('.');
                            }
                            #endregion

                            format = fileExtension;

                            var uploadFront = new UploadBase64Request();
                            uploadFront.docTypeId = "BFID1";
                            uploadFront.PolicyNo = serviceBeneficiary.PolicyNumber.Substring(0, 10);
                            uploadFront.fileName = fileNameFront;
                            uploadFront.format = format;
                            uploadFront.templateId = AppSettingsHelper.GetSetting("AiaCmsApi:TemplateId");
                            byte[] fileBytes = await azureStorage.GetByteByFileName(fileNameFront);

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
                            uploadFront.file = dataURI;

                            
                            item.Front_CMS_RequestOn = Utils.GetDefaultDate();

                            var uploadResultFront = await aiaCmsApiService.UploadBase64(uploadFront);

                            item.Front_CMS_ResponseOn = Utils.GetDefaultDate();
                            item.Front_CMS_Response = uploadResultFront != null ? JsonConvert.SerializeObject(uploadResultFront) : null;



                            #region #Insert to ServiceMainDoc
                            try
                            {
                                unitOfWork.GetRepository<Entities.ServiceMainDoc>().Add(new ServiceMainDoc
                                {
                                    Id = Guid.NewGuid(),
                                    ServiceId = item.ServiceBeneficiaryID,
                                    MainId = item.ServiceBeneficiaryID,
                                    ServiceType = EnumServiceType.BeneficiaryInformation.ToString(),
                                    CmsRequestOn = Utils.GetDefaultDate(),
                                    CmsResponseOn = Utils.GetDefaultDate(),
                                    
                                    CmsResponse = JsonConvert.SerializeObject(uploadResultFront),
                                    FormId = GetServicingFormId(EnumServiceType.BeneficiaryInformation, false, true),
                                    DocName = fileNameFront,
                                    DocType = format,
                                    UploadStatus = uploadResultFront?.msg,
                                });

                                unitOfWork.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {

                            }
                            #endregion
                        }
                        #endregion

                        #region "Back"
                        if (!String.IsNullOrEmpty(item.IdBackImageName))
                        {
                            var fileNameBack = item.IdBackImageName;
                            var format = ".pdf";

                            #region #GetFileExt
                            // Use Path.GetExtension to get the file extension
                            var fileExtension = Path.GetExtension(fileNameBack);

                            // Remove the leading dot (.) from the extension
                            if (!string.IsNullOrEmpty(fileExtension))
                            {
                                fileExtension = fileExtension.TrimStart('.');
                            }
                            #endregion

                            format = fileExtension;

                            var uploadBack = new UploadBase64Request();
                            uploadBack.docTypeId = "BFID2";
                            uploadBack.PolicyNo = serviceBeneficiary.PolicyNumber.Substring(0, 10);
                            uploadBack.fileName = fileNameBack;
                            uploadBack.format = format;
                            uploadBack.templateId = AppSettingsHelper.GetSetting("AiaCmsApi:TemplateId");
                            byte[] fileBytes = await azureStorage.GetByteByFileName(fileNameBack);

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
                            uploadBack.file = dataURI;

                            
                            item.Back_CMS_RequestOn = Utils.GetDefaultDate();

                            var uploadResultBack = await aiaCmsApiService.UploadBase64(uploadBack);

                            item.Back_CMS_ResponseOn = Utils.GetDefaultDate();
                            item.Back_CMS_Response = uploadResultBack != null ? JsonConvert.SerializeObject(uploadResultBack) : null;


                            #region #Insert to ServiceMainDoc
                            try
                            {
                                unitOfWork.GetRepository<Entities.ServiceMainDoc>().Add(new ServiceMainDoc
                                {
                                    Id = Guid.NewGuid(),
                                    ServiceId = item.ServiceBeneficiaryID,
                                    MainId = item.ServiceBeneficiaryID,
                                    ServiceType = EnumServiceType.BeneficiaryInformation.ToString(),
                                    CmsRequestOn = Utils.GetDefaultDate(),
                                    CmsResponseOn = Utils.GetDefaultDate(),
                                    
                                    CmsResponse = JsonConvert.SerializeObject(uploadResultBack),
                                    FormId = GetServicingFormId(EnumServiceType.BeneficiaryInformation, false, false, true),
                                    DocName = fileNameBack,
                                    DocType = format,
                                    UploadStatus = uploadResultBack?.msg,
                                });

                                unitOfWork.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {

                            }
                            #endregion
                        }
                        #endregion

                        unitOfWork.GetRepository<Entities.ServiceBeneficiaryPersonalInfo>().Update(item);
                        unitOfWork.SaveChanges();
                    }




                }

                //(string? membertype, string? aiaMemberID, string? groupMemberId) = GetClientInfo(memberID);
                foreach (var item in _beneficiaryList)
                {
                    try
                    {
                        //Notification
                        //MobileErrorLog("Notification Send HealthRenewal", "", "", httpContext?.HttpContext.Request.Path);
                        Console.WriteLine("Before SendServicingNoti");
                        notificationService.SendServicingNoti(memberID ?? new Guid(), item.ID, EnumServicingStatus.Received, EnumServiceType.BeneficiaryInformation, item.PolicyNumber);
                        Console.WriteLine("After SendServicingNoti");
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine($"SendServicingNoti Error | Ex message : {ex.Message} | Exception {ex}");
                        MobileErrorLog("Notification Error", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                    }

                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"RunBackgroudILSubmit Error | Ex message : {ex.Message} | Exception {ex}");
            }

        }

        private async Task RunBackgroundCodeAsync(ServiceHealthRenewal entity, Client client, Policy policy, ServiceCommonPartOne model, Guid? memberID, string ServicingEmail, string pdfFileName, byte[] pdfData)
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


                string subject = $"{entity.PolicyNumber}/HealthRenewal/{client.Name}";



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
                Console.WriteLine($"RunBackgroundCodeAsync Error | Ex message : {ex.Message} | Exception {ex}");
                MobileErrorLog("Servicing Request Email Error", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

            }

            #endregion

        }

        public async Task<(List<byte[]>, List<string>)> UploadDocToCMS(ServiceCommonPartOne model, Policy policy, ServiceHealthRenewal entity)
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

                        ServiceHealthRenewalDoc docData = new ServiceHealthRenewalDoc();

                        docData.ServiceHealthRenewalID = entity.ID;
                        
                        docData.CmsRequestOn = Utils.GetDefaultDate();
                        docData.DocName = doc;

                        var uploadResult = await aiaCmsApiService.UploadBase64(upload);

                        docData.UploadStatus = uploadResult?.msg;
                        docData.CmsResponseOn = Utils.GetDefaultDate();
                        docData.CmsResponse = uploadResult != null ? JsonConvert.SerializeObject(uploadResult) : null;


                        unitOfWork.GetRepository<Entities.ServiceHealthRenewalDoc>().Add(docData);
                        unitOfWork.SaveChanges();

                    }
                    catch (Exception ex)
                    {
                        MobileErrorLog($"ServiceHealthRenewal => UploadBase64", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                    }

                }
                return (imageBytesList, imageNameList);
            }
            catch (System.Exception)
            {

                throw;
            }

        }

        public async Task<string> CreateCaseToCRM(Client client, ServiceHealthRenewal entity, Policy policy)
        {
            try
            {
                ServiceHealthRenewal data = unitOfWork.GetRepository<ServiceHealthRenewal>().Query(x => x.ID == entity.ID).FirstOrDefault();

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
                crmModel.RequestInfo.CaseCategory = "CC009";
                crmModel.RequestInfo.Channel = "100004";
                crmModel.RequestInfo.CaseType = "Member and Agent";
                crmModel.RequestInfo.RequestId = data.ID.ToString();


                data.CrmRequestOn = Utils.GetDefaultDate();

                MobileErrorLog("LapseReinstatement aiaCrmApiService => CreateCase", "Request"
                    , JsonConvert.SerializeObject(crmModel), "v1/claim/claim");

                var crmResponse = await aiaCrmApiService.CreateCase(crmModel);


                MobileErrorLog("LapseReinstatement aiaCrmApiService => CreateCase", "Response"
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
                    MobileErrorLog("update LapseReinstatement tran ex after Crm call", ex.Message
                        , JsonConvert.SerializeObject(ex), "v1/servicing/submit");
                }
                #endregion
                return "";
            }
            catch (System.Exception ex)
            {
                //MobileErrorLog($"CreateCaseToCRM => UploadBase64", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return "";
            }

        }

        private async Task<byte[]> GenerateServicingPdf(ServiceBeneficiary data, string pdfFileName, string holderName, string policyNumber, string policyStatus, string clientNo)
        {
            using (var scope = this.serviceFactory.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork<aia_core.Entities.Context>>();
                var path = Path.Combine(
                        this.environment.ContentRootPath, "email_templates/", "beneficiary_request.html");

                var htmlData = File.ReadAllText(path);

                htmlData = htmlData.Replace("{{Title}}", "Request for Alteration of Beneficiary");
                htmlData = htmlData.Replace("{{HolderName}}", holderName);
                htmlData = htmlData.Replace("{{ClientNo}}", clientNo);

                htmlData = htmlData.Replace("{{ChangeType}}", "Beneficiary Information");

                //Entities.PolicyStatus _policyStatus = await unitOfWork.GetRepository<Entities.PolicyStatus>().Query(x => x.ShortDesc == policyStatus).FirstOrDefaultAsync();

                htmlData = htmlData.Replace("{{PolicyNumber}}", policyNumber);
                htmlData = htmlData.Replace("{{PolicyStatus}}", policyStatus);
                htmlData = htmlData.Replace("{{SubmissionDate}}", data.CreatedOn.Value.ToString("dd MMM yyyy"));
                htmlData = htmlData.Replace("{{RequestDate}}", data.CreatedOn.Value.ToString("dd MMM yyyy"));
                htmlData = htmlData.Replace("{{RequestId}}", data.ID.ToString());

                htmlData = htmlData.Replace("{{FormID}}", "POSBFM1");


                string signatureBase64 = await azureStorage.GetBase64ByFileName(data.SignatureImage);
                string signatureMimeType = "image/jpg";
                string signatureDataUrl = $"data:{signatureMimeType};base64,{signatureBase64}";
                htmlData = htmlData.Replace("{{signDataUrl}}", signatureDataUrl ?? "");

                List<ServiceBeneficiaryShareInfo> serviceBeneficiaryShareList = unitOfWork.GetRepository<ServiceBeneficiaryShareInfo>().Query(x => x.ServiceBeneficiaryID == data.ID
                            /*&& x.Type != EnumBeneficiaryShareInfoType.Remove.ToString()*/ ).ToList();

                List<ServiceBeneficiaryPersonalInfo> serviceBeneficiaryPersonalList = unitOfWork.GetRepository<ServiceBeneficiaryPersonalInfo>()
                .Query(x => x.ServiceBeneficiaryID == data.ID).ToList();

                var tablepath = Path.Combine(
                        this.environment.ContentRootPath, "email_templates/", "beneficiary_table.html");

                var tableHtmlData = File.ReadAllText(tablepath);
                string tableData = "";

                int i = 1;
                foreach (var item in serviceBeneficiaryShareList)
                {
                    string _htmlData = tableHtmlData;
                    if (!String.IsNullOrEmpty(item.ClientNo))
                    {
                        Client client = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == item.ClientNo).FirstOrDefault();
                        var _personInfo = serviceBeneficiaryPersonalList.Where(x=>x.ClientNo == item.ClientNo).FirstOrDefault();

                        _htmlData = _htmlData.Replace("{{i}}", i.ToString());

                        _htmlData = _htmlData.Replace("{{FullName_Old}}", client.Name);
                        _htmlData = _htmlData.Replace("{{DOB_Old}}", client.Dob.ToString("yyyy-MM-dd"));
                        _htmlData = _htmlData.Replace("{{Gender_Old}}", client.Gender);

                        _htmlData = _htmlData.Replace("{{FullName}}", client.Name);
                        _htmlData = _htmlData.Replace("{{DOB}}", client.Dob.ToString("yyyy-MM-dd"));
                        _htmlData = _htmlData.Replace("{{Gender}}", client.Gender);

                        string idvalue = "";
                        if (!String.IsNullOrEmpty(client.Nrc))
                        {
                            idvalue = client.Nrc;
                        }
                        else if (!String.IsNullOrEmpty(client.PassportNo))
                        {
                            idvalue = client.PassportNo;
                        }
                        else if (!String.IsNullOrEmpty(client.Other))
                        {
                            idvalue = client.Other;
                        }
                        _htmlData = _htmlData.Replace("{{Passport/NRC_Old}}", idvalue);
                        _htmlData = _htmlData.Replace("{{Passport/NRC}}", idvalue);
                        _htmlData = _htmlData.Replace("{{PhoneNumber_Old}}", _personInfo!=null?_personInfo.OldMobileNumber:"");
                        _htmlData = _htmlData.Replace("{{PhoneNumber_New}}", _personInfo!=null?_personInfo.NewMobileNumber:"");


                        Relationship oldRelationship = unitOfWork.GetRepository<Relationship>()
                                                        .Query(x => x.Code == item.OldRelationShipCode).FirstOrDefault();

                        Relationship newRelationship = unitOfWork.GetRepository<Relationship>()
                                                        .Query(x => x.Code == item.NewRelationShipCode).FirstOrDefault();
                        
                        _htmlData = _htmlData.Replace("{{RelationShip_Old}}", oldRelationship!=null?oldRelationship.Name:"");
                        _htmlData = _htmlData.Replace("{{RelationShip_New}}", newRelationship!=null?newRelationship.Name:"");

                        _htmlData = _htmlData.Replace("{{Percentage_Old}}", item.OldPercentage!=null?item.OldPercentage.ToString():"");
                        

                        

                        _htmlData = _htmlData.Replace("{{DeleteStatus}}", item.Type);

                        if (item.Type.ToLower() == "remove")
                        {
                            _htmlData = _htmlData.Replace("{{Percentage_New}}", "");
                        }
                        else
                        {
                            _htmlData = _htmlData.Replace("{{Percentage_New}}", item.NewPercentage != null ? item.NewPercentage.ToString() : "");
                        }

                    }
                    else
                    {
                        var _personInfo = serviceBeneficiaryPersonalList.Where(x=>x.IdValue == item.IdValue).FirstOrDefault();

                        _htmlData = _htmlData.Replace("{{i}}", i.ToString());

                        _htmlData = _htmlData.Replace("{{FullName_Old}}", "");
                        _htmlData = _htmlData.Replace("{{DOB_Old}}", "");
                        _htmlData = _htmlData.Replace("{{Gender_Old}}", "");
                        _htmlData = _htmlData.Replace("{{Passport/NRC_Old}}", "");

                        _htmlData = _htmlData.Replace("{{FullName}}", _personInfo.Name);
                        _htmlData = _htmlData.Replace("{{DOB}}", _personInfo.Dob.Value.ToString("yyyy-MM-dd"));
                        _htmlData = _htmlData.Replace("{{Gender}}", _personInfo.Gender);
                        _htmlData = _htmlData.Replace("{{Passport/NRC}}", _personInfo.IdValue);

                        _htmlData = _htmlData.Replace("{{PhoneNumber_Old}}", "");
                        _htmlData = _htmlData.Replace("{{PhoneNumber_New}}", _personInfo.NewMobileNumber);

                        Relationship oldRelationship = unitOfWork.GetRepository<Relationship>()
                                                        .Query(x => x.Code == item.OldRelationShipCode).FirstOrDefault();

                        Relationship newRelationship = unitOfWork.GetRepository<Relationship>()
                                                        .Query(x => x.Code == item.NewRelationShipCode).FirstOrDefault();
                        
                        _htmlData = _htmlData.Replace("{{RelationShip_Old}}", oldRelationship!=null?oldRelationship.Name:"");
                        _htmlData = _htmlData.Replace("{{RelationShip_New}}", newRelationship!=null?newRelationship.Name:"");

                        _htmlData = _htmlData.Replace("{{Percentage_Old}}", item.OldPercentage!=null?item.OldPercentage.ToString():"");
                        _htmlData = _htmlData.Replace("{{Percentage_New}}", item.NewPercentage!=null?item.NewPercentage.ToString():"");

                        _htmlData = _htmlData.Replace("{{DeleteStatus}}", "");
                    }


                    tableData += _htmlData;
                    i++;
                }

                htmlData = htmlData.Replace("{{update_fields}}", tableData);

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

        private async Task<string> UpadeGeneratePdfToCMS(byte[] pdfData, string policyNo, string fileName, ServiceBeneficiary model, Guid? uploadId = null)
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



            upload.docTypeId = "POSBFM1";
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

            //#region #Insert to ServiceMainDoc

            //if (uploadId != null && uploadResult != null)
            //{
            //    var pdf = unitOfWork.GetRepository<Entities.ServiceMainDoc>()
            //        .Query(x => x.Id == uploadId)
            //        .FirstOrDefault();

            //    if(pdf != null)
            //    {
            //        pdf.CmsReqeust = JsonConvert.SerializeObject(upload);
            //        pdf.CmsResponse = JsonConvert.SerializeObject(uploadResult);
            //        pdf.CmsRequestOn = Utils.GetDefaultDate();
            //        pdf.CmsResponseOn = Utils.GetDefaultDate();
            //        pdf.UploadStatus = uploadResult?.msg;

            //        unitOfWork.SaveChanges();
            //    }
                
            //}
            

            //#endregion

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