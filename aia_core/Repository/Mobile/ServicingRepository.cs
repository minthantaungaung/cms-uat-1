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
using aia_core.Model.Mobile.Servicing.Data.Response;
using aia_core.Model.Mobile.Response.Servicing;
using System.Collections.Generic;

namespace aia_core.Repository.Mobile
{
    public interface IServicingRepository
    {
        Task<ResponseModel<ServicingResponseModel>> Submit(ServicingRequestModel model);
        Task<ResponseModel<SubmitNoInsertNoAPICallsResponse>> SubmitNoInsertNoAPICalls(ServicingRequestModel model);

        Task<ResponseModel<SubmitNoInsertNoAPICallsResponse>> SubmitNoInsertNoAPICalls22(ServicingRequestModel model);

    }
    public class ServicingRepository : BaseRepository, IServicingRepository
    {
        #region "const"

        private readonly ICommonRepository commonRepository;
        private readonly IAiaILApiService aiaILApiService;
        private readonly IHostingEnvironment environment;
        private readonly IConverter converter;
        private readonly IAiaCmsApiService aiaCmsApiService;
        private readonly IAiaCrmApiService aiaCrmApiService;
        private readonly INotificationService notificationService;
        private readonly IServicingDataRepository servicingDataRepository;


        public ServicingRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork,
            ICommonRepository commonRepository,
            IAiaILApiService aiaILApiService,
            IHostingEnvironment environment,
            IConverter converter,
            IAiaCrmApiService aiaCrmApiService,
            IAiaCmsApiService aiaCmsApiService,
            INotificationService notificationService, 
            IServicingDataRepository servicingDataRepository)
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

            this.servicingDataRepository = servicingDataRepository;
        }
        #endregion

        public async Task<ResponseModel<ServicingResponseModel>> Submit(ServicingRequestModel model)
        {
            try
            {
                Guid MainID = Guid.NewGuid();
                Guid responseServicingID = new Guid();
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

                    #region # Improper Access Control Check For Client No 

                    if (model.ServicingType == EnumServicingType.PolicyHolderInformation)
                    {
                        var holderClientIdList = servicingDataRepository.GetPolicyHolderListForValidation();

                        Console.WriteLine($"GetPolicyHolderListForValidation => {JsonConvert.SerializeObject(holderClientIdList)}");
                        if (holderClientIdList == null || holderClientIdList.Code != 0 || holderClientIdList.Data?.Count == 0)
                        {
                            return new ResponseModel<ServicingResponseModel> { Code = 400, Message = "No Holder Client No Found" };
                        }

                        var validHolderCount = holderClientIdList?.Data?.Where(x => model.ClientNo.Contains(x.ClientNo)).Count();

                        if (model.ClientNo.Count > validHolderCount)
                        {
                            return new ResponseModel<ServicingResponseModel> { Code = 400, Message = "Invalid Holder Client No" };
                        }
                        

                        var hasPendingRecord = holderClientIdList?.Data?
                            .Any(x => model.ClientNo.Contains(x.ClientNo) && x.ServiceStatus == EnumServiceStatus.Received);

                        if (hasPendingRecord == true)
                        {
                            return new ResponseModel<ServicingResponseModel> { Code = 400, Message = "Holder Client No has pending record" };
                        }

                    }
                    else if (model.ServicingType == EnumServicingType.InsuredPersonInformation)
                    {
                        var insuredClientIdList = servicingDataRepository.GetInsuredPersonListForValidation();
                        Console.WriteLine($"GetInsuredPersonListForValidation => {JsonConvert.SerializeObject(insuredClientIdList)}");

                        if (insuredClientIdList == null || insuredClientIdList.Code != 0 || insuredClientIdList.Data?.Count == 0)
                        {
                            return new ResponseModel<ServicingResponseModel> { Code = 400, Message = "No Insured Client No Found" };
                        }

                        var validInsuredCount = insuredClientIdList?.Data?.Where(x => model.ClientNo.Contains(x.ClientNo)).Count();

                        if (model.ClientNo.Count > validInsuredCount)
                        {
                            return new ResponseModel<ServicingResponseModel> { Code = 400, Message = "Invalid Insured Client No" };
                        }

                        var hasPendingRecord = insuredClientIdList?.Data?
                            .Any(x => model.ClientNo.Contains(x.ClientNo) && x.ServiceStatus == EnumServiceStatus.Received);

                        if (hasPendingRecord == true)
                        {
                            return new ResponseModel<ServicingResponseModel> { Code = 400, Message = "Insured Client No has pending record" };
                        }
                    }                    

                    #endregion

                    model.MainId = MainID;
                    Console.WriteLine($"CommonHolderInsuredChangeService => {model.ServicingType} {model.MainId} " +
                        $"original model.ClientNo => {string.Join(", ", model.ClientNo)} model.IsAllProfileUpdate => {model.IsAllProfileUpdate}");

                    #region #IsAllProfileUpdate && Added related client no to model.ClientNo
                    if (model.IsAllProfileUpdate == true)
                    {
                        

                        var allClientNoList = GetAllClientNoListByClientNo(model.ClientNo.FirstOrDefault()); // get all client no list by NRC , 12/ABCDEF(N)111111
                                                                                                             // let say
                                                                                                             // 1111,
                                                                                                             // 2222,
                                                                                                             // 3333,
                                                                                                             // 4444,
                                                                                                             // 5555

                        Console.WriteLine($"CommonHolderInsuredChangeService => {model.ServicingType} {model.MainId} allClientNoList => {string.Join(", ", allClientNoList)}");

                        if (model.ServicingType == EnumServicingType.PolicyHolderInformation)
                        {
                            var policyListByHolder = unitOfWork.GetRepository<Entities.Policy>()
                                .Query(x => allClientNoList.Contains(x.PolicyHolderClientNo) && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                                .ToList();
                            // all policy list by those all client no as holder 
                            //p1, 1111, 3333
                            //p2, 1111, 3333
                            //p3, 1111, 3333
                            //p4, 3333, 4444

                            var insuredClientNoList = policyListByHolder
                                .Where(x => allClientNoList.Contains(x.InsuredPersonClientNo))
                                .Select(x => x.InsuredPersonClientNo)
                                .ToList()
                                .Distinct(); 
                            // get all insured client no list by matching same client no from all client no list by NRC 
                            // 3333
                            // 4444

                            Console.WriteLine($"CommonHolderInsuredChangeService => {model.ServicingType} {model.MainId} => insuredClientNoList => {string.Join(", ", insuredClientNoList)}");

                            var beneficiaryClientNoList = unitOfWork.GetRepository<Entities.Beneficiary>()
                                .Query(x => allClientNoList.Contains(x.BeneficiaryClientNo)
                                && policyListByHolder.Select(x => x.PolicyNo).Contains(x.PolicyNo))
                                .Select(x => x.BeneficiaryClientNo)
                                .ToList()
                                .Distinct(); // get all beneficiary client no list by matching same client no from all client no list by NRC 
                            // and matching policy no from all policy list
                            // 5555 as beneficiary client no
                            // p1, 1111, 3333, 5555

                            Console.WriteLine($"CommonHolderInsuredChangeService => {model.ServicingType} {model.MainId} => beneficiaryClientNoList => {string.Join(", ", beneficiaryClientNoList)}");

                            if (insuredClientNoList?.Any() == true)
                            {
                                model.ClientNo.AddRange(insuredClientNoList); //added 
                                                                              // 3333
                                                                              // 4444
                            }

                            if (beneficiaryClientNoList?.Any() == true)
                            {
                                model.ClientNo.AddRange(beneficiaryClientNoList); //added 
                                                                                  // 5555
                            }


                            model.ClientNo = model.ClientNo.Distinct().ToList(); //original client no is 1111 ,
                                                                                 //so list will be
                                                                                 //1111, as holder
                                                                                 //3333, as insured
                                                                                 //4444, as insured
                                                                                 //5555, as beneficiary

                            
                        }
                        else if (model.ServicingType == EnumServicingType.InsuredPersonInformation)
                        {
                            var policyListByInsured = unitOfWork.GetRepository<Entities.Policy>()
                                .Query(x => allClientNoList.Contains(x.InsuredPersonClientNo) && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                                .ToList();
                            // all policy list by those all client no as holder 
                            //p1, 1111, 3333
                            //p2, 1111, 3333
                            //p3, 1111, 3333
                            //p4, 3333, 4444
                            //p6, 6666, 3333 // holder is from other NRC
                            //p7, 7777, 3333 // holder is from other NRC

                            var holderClientNoList = policyListByInsured
                                .Where(x => allClientNoList.Contains(x.PolicyHolderClientNo))
                                .Select(x => x.PolicyHolderClientNo)
                                .ToList()
                                .Distinct();
                            // get all holder client no list by matching same client no from all client no list by NRC 
                            // 1111
                            // 3333

                            Console.WriteLine($"CommonHolderInsuredChangeService => {model.ServicingType} {model.MainId} => holderClientNoList => {string.Join(", ", holderClientNoList)}");

                            var policyListByHolder = policyListByInsured
                                .Where(x => holderClientNoList.Contains(x.PolicyHolderClientNo))
                                .ToList();

                            //p1, 1111, 3333
                            //p2, 1111, 3333
                            //p3, 1111, 3333
                            //p4, 3333, 4444

                            var beneficiaryClientNoList = unitOfWork.GetRepository<Entities.Beneficiary>()
                                .Query(x => allClientNoList.Contains(x.BeneficiaryClientNo)
                                && policyListByHolder.Select(x => x.PolicyNo).Contains(x.PolicyNo))
                                .Select(x => x.BeneficiaryClientNo)
                                .ToList()
                                .Distinct();

                            // and matching policy no from all policy list
                            // 5555 as beneficiary client no
                            // p1, 1111, 3333, 5555

                            Console.WriteLine($"CommonHolderInsuredChangeService => {model.ServicingType} {model.MainId} => beneficiaryClientNoList => {string.Join(", ", beneficiaryClientNoList)}");

                            if (holderClientNoList?.Any() == true)
                            {
                                model.ClientNo.AddRange(holderClientNoList);
                            }

                            if (beneficiaryClientNoList?.Any() == true)
                            {
                                model.ClientNo.AddRange(beneficiaryClientNoList);
                            }


                            model.ClientNo = model.ClientNo.Distinct().ToList();
                            //1111
                            //3333
                            //5555

                            
                        }
                    }

                    #endregion

                    Console.WriteLine($"CommonHolderInsuredChangeService => {model.ServicingType} {model.MainId} => updated model.ClientNo => {string.Join(", ", model.ClientNo)}");

                    foreach (var clientNo in model.ClientNo)
                    {
                        Client client = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == clientNo).FirstOrDefault();

                        int updateLevel = 0;
                        ILServicingChangeRequest ilUpdateData = new ILServicingChangeRequest();
                        ilUpdateData.client = new ILServicingClientInfo();

                        ServicingRequest data = new ServicingRequest();
                        data.ServicingID = Guid.NewGuid();
                        data.MainID = MainID;
                        responseServicingID = data.ServicingID;

                        data.ServicingType = model.ServicingType.ToString();
                        data.ClientNo = clientNo;

                        if (model.MaritalStatus != null)
                        {
                            data.MaritalStatus_Old = model.MaritalStatus.Old;
                            data.MaritalStatus_New = model.MaritalStatus.New;
                            ilUpdateData.client.maritalStatus = model.MaritalStatus.New;
                            updateLevel = 2;
                        }

                        if (model.FatherName != null)
                        {
                            data.FatherName_Old = model.FatherName.Old;
                            data.FatherName_New = model.FatherName.New;
                            ilUpdateData.client.fathersName = model.FatherName.New;
                        }

                        if (model.PhoneNumber != null)
                        {
                            data.PhoneNumber_Old = model.PhoneNumber.Old;
                            data.PhoneNumber_New = model.PhoneNumber.New;
                            ilUpdateData.client.phone = model.PhoneNumber.New;
                            updateLevel = 2;
                        }

                        if (model.EmailAddress != null)
                        {
                            data.EmailAddress_Old = model.EmailAddress.Old;
                            data.EmailAddress_New = model.EmailAddress.New;
                            ilUpdateData.client.email = model.EmailAddress.New;
                        }

                        if (model.Country != null)
                        {
                            data.Country_Old = model.Country.Old;
                            data.Country_New = model.Country.New;
                            ilUpdateData.client.country = model.Country.New;
                        }

                        if (model.Province != null)
                        {
                            Entities.Province oldProvince = unitOfWork.GetRepository<Entities.Province>().Query(x => x.province_code == model.Province.Old).FirstOrDefault();
                            Entities.Province newProvince = unitOfWork.GetRepository<Entities.Province>().Query(x => x.province_code == model.Province.New).FirstOrDefault();

                            data.Province_Old = oldProvince != null ? oldProvince.province_eng_name : model.Province.Old;
                            data.Province_New = newProvince != null ? newProvince.province_eng_name : model.Province.New;
                            ilUpdateData.client.address5 = newProvince != null ? newProvince.province_eng_name : model.Province.New;
                        }

                        if (model.Distinct != null)
                        {
                            Entities.District oldDistrict = unitOfWork.GetRepository<Entities.District>().Query(x => x.district_code == model.Distinct.Old).FirstOrDefault();
                            Entities.District newDistrict = unitOfWork.GetRepository<Entities.District>().Query(x => x.district_code == model.Distinct.New).FirstOrDefault();

                            data.Distinct_Old = oldDistrict != null ? oldDistrict.district_eng_name : model.Distinct.Old;
                            data.Distinct_New = newDistrict != null ? newDistrict.district_eng_name : model.Distinct.New;
                            ilUpdateData.client.address4 = newDistrict != null ? newDistrict.district_eng_name : model.Distinct.New;
                        }

                        if (model.Township != null)
                        {
                            Entities.Township oldTownship = unitOfWork.GetRepository<Entities.Township>().Query(x => x.township_code == model.Township.Old).FirstOrDefault();
                            Entities.Township newTownship = unitOfWork.GetRepository<Entities.Township>().Query(x => x.township_code == model.Township.New).FirstOrDefault();

                            data.Township_Old = oldTownship != null ? oldTownship.township_eng_name : model.Township.Old;
                            data.Township_New = newTownship != null ? newTownship.township_eng_name : model.Township.New;
                            ilUpdateData.client.address3 = newTownship != null ? newTownship.township_eng_name : model.Township.New;
                            ilUpdateData.townshipCode = model.Township.New;
                        }

                        if (model.Building != null)
                        {
                            data.Building_Old = model.Building.Old;
                            data.Building_New = model.Building.New;
                            ilUpdateData.client.address1 = model.Building.New;
                        }

                        if (model.Street != null)
                        {
                            data.Street_Old = model.Street.Old;
                            data.Street_New = model.Street.New;
                            ilUpdateData.client.address2 = model.Street.New;
                        }

                        data.SignatureImage = model.SignatureImage;

                        data.CreatedOn = Utils.GetDefaultDate();
                        data.MemberID = memberID;
                        data.Status = EnumServicingStatus.Received.ToString();



                        unitOfWork.GetRepository<Entities.ServicingRequest>().Add(data);

                        List<string> policyList = new List<string>();
                        if (data.ServicingType == EnumServicingType.PolicyHolderInformation.ToString())
                        {
                            policyList = GetActivePolicyNoListByHolder(clientNo);
                        }
                        else
                        {
                            policyList = GetActivePolicyNoListByInsured(clientNo);
                        }
                        string policyNumber = policyList.FirstOrDefault();

                        Policy policy = unitOfWork.GetRepository<Policy>().Query(x => x.PolicyNo == policyNumber).FirstOrDefault();
                        Member member = unitOfWork.GetRepository<Member>().Query(x => x.MemberId == memberID).FirstOrDefault();

                        #region "Service Main"
                        (string producttype, string productname) = GetProductInfo(policyNumber);
                        (string? membertype, string? aiaMemberID, string? groupMemberId) = GetClientInfo(memberID);

                        ServiceMain serviceMain = new ServiceMain();
                        serviceMain.ID = Guid.NewGuid();
                        serviceMain.MainID = MainID;
                        serviceMain.ServiceID = data.ServicingID;
                        serviceMain.ServiceType = model.ServicingType.ToString();
                        serviceMain.ServiceStatus = EnumServicingStatus.Received.ToString();
                        serviceMain.CreatedDate = Utils.GetDefaultDate(); serviceMain.OriginalCreatedDate = Utils.GetDefaultDate();
                        serviceMain.ProductType = producttype;
                        serviceMain.MemberType = membertype;
                        serviceMain.GroupMemberID = groupMemberId;
                        serviceMain.MemberID = clientNo;
                        serviceMain.LoginMemberID = memberID;
                        serviceMain.PolicyNumber = policy?.PolicyNo;
                        serviceMain.PolicyStatus = policy?.PolicyStatus;
                        serviceMain.MobileNumber = member?.Mobile;
                        serviceMain.MemberName = member?.Name;
                        serviceMain.EstimatedCompletedDate = GetProgressAndContactHour(Utils.GetDefaultDate(), EnumProgressType.Service).CompletedDate;
                        unitOfWork.GetRepository<Entities.ServiceMain>().Add(serviceMain);
                        #endregion

                        unitOfWork.SaveChanges();

                        ilUpdateData.client.updateClientLevel = updateLevel.ToString();
                        ilUpdateData.client.clientNumber = clientNo;
                        ilUpdateData.client.dob = client.Dob;
                        ilUpdateData.client.gender = client.Gender;
                        if (!String.IsNullOrEmpty(client.Nrc))
                        {
                            ilUpdateData.client.idtype = "N";
                            ilUpdateData.client.idnumber = client.Nrc;
                        }
                        else if (!String.IsNullOrEmpty(client.PassportNo))
                        {
                            ilUpdateData.client.idtype = "X";
                            ilUpdateData.client.idnumber = client.PassportNo;
                        }
                        else if (!String.IsNullOrEmpty(client.Other))
                        {
                            ilUpdateData.client.idtype = "N";
                            ilUpdateData.client.idnumber = client.Other;
                        }
                        ilUpdateData.client.name = client.Name;
                        ilUpdateData.client.occupation = client.Occupation;

                        if (model.ServicingType == EnumServicingType.PolicyHolderInformation)
                        {
                            ilUpdateData.requestType = "CPH";
                        }
                        else
                        {
                            ilUpdateData.requestType = "CPI";
                        }
                        ilUpdateData.policyNumber = policyNumber;

                        data.ILRequest = JsonConvert.SerializeObject(ilUpdateData);
                        data.ILRequestOn = Utils.GetDefaultDate();

                        Console.WriteLine("Servicing Submit Before IL Call");
                        //AIA IL
                        CommonRegisterResponse ilResponse = aiaILApiService.ClientUpdateServicingRequest(ilUpdateData, out string systemError, out string SerializeModel);
                        Console.WriteLine("Servicing Submit After IL Call");

                        data.ILResponse = systemError;
                        data.ILResponseOn = Utils.GetDefaultDate();
                        unitOfWork.GetRepository<Entities.ServicingRequest>().Update(data);
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
                        //     return errorCodeProvider.GetResponseModelCustom<string>(ErrorCode.E500, $"[IL] - {ilResponse.data.errorMessage}");
                        // }
                        // if (ilResponse.message.type == "SUCCESS")
                        // {


                        var pdfFileName = $"ServicingRequest-{data.ServicingType}-{data?.ClientNo}.pdf";
                        byte[] pdfData = await GenerateServicingPdf(data, pdfFileName, client.Name, policyNumber, policy?.PolicyStatus);

                        //AIA CMS
                        await UpadeGeneratePdfToCMS(pdfData, policyNumber, pdfFileName, data);

                        //AIA Crm
                        await CreateCaseToCRM(client, data, policy);

                        try
                        {
                            //Notification
                            MobileErrorLog("Notification Send", "", "", httpContext?.HttpContext.Request.Path);
                            if (model.ServicingType == EnumServicingType.PolicyHolderInformation)
                            {
                                await notificationService.SendServicingNoti(memberID ?? new Guid(), data.ServicingID, EnumServicingStatus.Received, EnumServiceType.PolicyHolderInformation);
                            }
                            else
                            {
                                await notificationService.SendServicingNoti(memberID ?? new Guid(), data.ServicingID, EnumServicingStatus.Received, EnumServiceType.InsuredPersonInformation);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            MobileErrorLog("Notification Error", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                        }


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



                            string subject = $"{policyNumber}/{data.ServicingType}/{client.Name}";

                            //Utils.SendClaimEmail("kyawzaymoore@codigo.co",subject + " Test","", attachments, "aungwaiwaithin@codigo.co");

                            var path = Path.Combine(
                            this.environment.ContentRootPath, "email_templates/", "servicing_email_content.html");
                            var emailContent = File.ReadAllText(path);
                            emailContent = emailContent.Replace("{{EmailContent}}", subject);
                            emailContent = emailContent.Replace("{{ILStatus}}", ilResponse.message.type);
                            emailContent = emailContent.Replace("{{ILErrorMessage}}", ilResponse.data.errorMessage);
                            emailContent = emailContent.Replace("{{UniqueID}}", data.ServicingID.ToString());

                            Utils.SendServicingEmail(ServicingEmail, subject, emailContent
                                        , attachments, "aungwaiwaithin@codigo.co", unitOfWork);
                        }
                        catch (System.Exception ex)
                        {
                            Console.WriteLine("Servicing Submit Email Error : " + ex);
                            MobileErrorLog("Servicing Request Email Error", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                        }

                        #endregion
                        //}

                    }
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
            catch (Exception ex)
            {
                //MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                Console.WriteLine("Servicing Submit Error : " + ex);
                return errorCodeProvider.GetResponseModel<ServicingResponseModel>(ErrorCode.E500);
            }
        }

        private async Task<byte[]> GenerateServicingPdf(ServicingRequest data, string pdfFileName, string holderName, string policyNumber, string policyStatus)
        {
            var path = Path.Combine(
                    this.environment.ContentRootPath, "email_templates/", "servicing_request.html");

            var htmlData = File.ReadAllText(path);

            htmlData = htmlData.Replace("{{Title}}", "Request for Alteration of Client Profile");
            htmlData = htmlData.Replace("{{HolderName}}", holderName);
            htmlData = htmlData.Replace("{{ClientNo}}", data.ClientNo);

            if (data.ServicingType == EnumServicingType.PolicyHolderInformation.ToString())
            {
                htmlData = htmlData.Replace("{{ChangeType}}", "Policy Holder Information");
            }
            else
            {
                htmlData = htmlData.Replace("{{ChangeType}}", "Insured Person Information");
            }

            Entities.PolicyStatus _policyStatus = await unitOfWork.GetRepository<Entities.PolicyStatus>().Query(x => x.ShortDesc == policyStatus).FirstOrDefaultAsync();

            htmlData = htmlData.Replace("{{PolicyNumber}}", policyNumber);
            htmlData = htmlData.Replace("{{PolicyStatus}}", _policyStatus.LongDesc);
            htmlData = htmlData.Replace("{{SubmissionDate}}", data.CreatedOn.Value.ToString("dd MMM yyyy"));
            htmlData = htmlData.Replace("{{RequestDate}}", data.CreatedOn.Value.ToString("dd MMM yyyy"));
            htmlData = htmlData.Replace("{{RequestId}}", data.ServicingID.ToString());
            htmlData = htmlData.Replace("{{FormID}}", "POSPPM1");




            string update_fields = "";
            string update_row = @"<tr class='fieldTable' style='height: 40px;'>
                                    <td class='fieldTable' style='width: 30%;'>{0}</td>
                                    <td class='fieldTable' style='width: 30%;'>{1}</td>
                                    <td class='fieldTable' style='width: 30%;'>{2}</td>
                                </tr>";
            //if (data.MaritalStatus_Old != data.MaritalStatus_New)
            //{
            update_fields += String.Format(update_row, "Marital Status", Utils.GetMaritalStatusDescription(data.MaritalStatus_Old), Utils.GetMaritalStatusDescription(data.MaritalStatus_New));
            //}

            //if (data.FatherName_Old != data.FatherName_New)
            //{
            update_fields += String.Format(update_row, "Father Name", data.FatherName_Old, data.FatherName_New);
            //}

            //if (data.PhoneNumber_Old != data.PhoneNumber_New)
            //{
            update_fields += String.Format(update_row, "Phone Number", data.PhoneNumber_Old, data.PhoneNumber_New);
            //}

            //if (data.EmailAddress_Old != data.EmailAddress_New)
            //{
            update_fields += String.Format(update_row, "Email Address", data.EmailAddress_Old, data.EmailAddress_New);
            //}

            //if (data.Country_Old != data.Country_New)
            //{
            Entities.Country oldCountry = await unitOfWork.GetRepository<Entities.Country>().Query(x => x.code == data.Country_Old).FirstOrDefaultAsync();
            Entities.Country newCountry = await unitOfWork.GetRepository<Entities.Country>().Query(x => x.code == data.Country_New).FirstOrDefaultAsync();
            update_fields += String.Format(update_row, "Country", oldCountry != null ? oldCountry.description : "", newCountry != null ? newCountry.description : "");
            //}

            //if (data.Province_Old != data.Province_New)
            //{
            update_fields += String.Format(update_row, "Province", data.Province_Old, data.Province_New);
            //}

            //if (data.Distinct_Old != data.Distinct_New)
            //{
            update_fields += String.Format(update_row, "Distinct", data.Distinct_Old, data.Distinct_New);
            //}

            //if (data.Township_Old != data.Township_New)
            //{
            update_fields += String.Format(update_row, "Township", data.Township_Old, data.Township_New);
            //}

            //if (data.Building_Old != data.Building_New)
            //{
            update_fields += String.Format(update_row, "Building", data.Building_Old, data.Building_New);
            //}

            //if (data.Street_Old != data.Street_New)
            //{
            update_fields += String.Format(update_row, "Street", data.Street_Old, data.Street_New);
            //}
            htmlData = htmlData.Replace("{{update_fields}}", update_fields);


            string signatureBase64 = await azureStorage.GetBase64ByFileName(data.SignatureImage);
            string signatureMimeType = "image/jpg";
            string signatureDataUrl = $"data:{signatureMimeType};base64,{signatureBase64}";
            htmlData = htmlData.Replace("{{signDataUrl}}", signatureDataUrl ?? "");

            MobileErrorLog("GenerateServicingPdf html", ""
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

                MobileErrorLog("Dink2Pdf Convert", "pdfData?.Length"
                    , $"{pdfData?.Length}", "v1/claim/claim");

                return pdfData;
            }
            catch (Exception ex)
            {
                MobileErrorLog("Dink2Pdf exception", ex.Message
                    , JsonConvert.SerializeObject(ex), "v1/claim/claim");
            }

            return null;
        }

        private async Task<string> UpadeGeneratePdfToCMS(byte[] pdfData, string policyNo, string fileName, ServicingRequest model)
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



            upload.docTypeId = "POSPPM1";
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
                var servicingType = EnumServiceType.PolicyHolderInformation;
                if (model.ServicingType == "InsuredPersonInformation")
                {
                    servicingType = EnumServiceType.InsuredPersonInformation;
                }
                unitOfWork.GetRepository<Entities.ServiceMainDoc>().Add(new ServiceMainDoc
                {
                    Id = Guid.NewGuid(),
                    MainId = model.ServicingID,
                    ServiceId = model.ServicingID,
                    ServiceType = model.ServicingType,
                    CmsRequestOn = Utils.GetDefaultDate(),
                    CmsResponseOn = Utils.GetDefaultDate(),
                    
                    CmsResponse = JsonConvert.SerializeObject(uploadResult),
                    FormId = GetServicingFormId(servicingType, true),
                    DocName = fileName,
                    DocType = "pdf",
                    UploadStatus = uploadResult?.msg,
                });

                unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {

            }
            #endregion

            return "";
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

        public async Task<string> CreateCaseToCRM(Client client, ServicingRequest data, Policy policy)
        {
            CaseRequest crmModel = new CaseRequest();
            crmModel.CustomerInfo = new CustomerInfo();
            crmModel.CustomerInfo.ClientNumber = policy.PolicyNo.Contains("-") ? (client.client_certificate != null ? client.client_certificate : client.ClientNo) : client.ClientNo;
            crmModel.CustomerInfo.FirstName = client?.Name;
            crmModel.CustomerInfo.LastName = client?.Name;
            crmModel.CustomerInfo.Email = client?.Email;

            crmModel.PolicyInfo = new aia_core.Model.AiaCrm.PolicyInfo();
            if (policy != null)
            {
                crmModel.PolicyInfo.PolicyNumber = policy?.PolicyNo;
            }

            crmModel.RequestInfo = new aia_core.Model.AiaCrm.Request();
            crmModel.RequestInfo.CaseCategory = "CC006";
            crmModel.RequestInfo.Channel = "100004";
            crmModel.RequestInfo.CaseType = "Member and Agent";
            crmModel.RequestInfo.RequestId = data.ServicingID.ToString();


            data.CrmRequestOn = Utils.GetDefaultDate();

            MobileErrorLog("Servicing aiaCrmApiService => CreateCase", "Request"
                , JsonConvert.SerializeObject(crmModel), "v1/claim/claim");

            var crmResponse = await aiaCrmApiService.CreateCase(crmModel);


            MobileErrorLog("Servicing aiaCrmApiService => CreateCase", "Response"
                , JsonConvert.SerializeObject(crmResponse), "v1/claim/claim");

            #region #UpdateClaimTran
            try
            {
                data.CrmRequest = JsonConvert.SerializeObject(crmModel);
                data.CrmResponse = JsonConvert.SerializeObject(crmResponse);
                data.CrmResponseOn = Utils.GetDefaultDate();

                unitOfWork.GetRepository<Entities.ServicingRequest>().Update(data);
                unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                MobileErrorLog("update servicing tran ex after Crm call", ex.Message
                    , JsonConvert.SerializeObject(ex), "v1/servicing/submit");
            }
            #endregion
            return "";
        }

        public async Task<ResponseModel<SubmitNoInsertNoAPICallsResponse>> SubmitNoInsertNoAPICalls(ServicingRequestModel model)
        {
            Console.WriteLine($"SubmitNoInsertNoAPICalls => ServicingRequestModel => {JsonConvert.SerializeObject(model)} ");

            if(model.ClaimOtp?.OtpCode != "C0d!go777$$131369")
            {
                return new ResponseModel<SubmitNoInsertNoAPICallsResponse> { Code = 401, Message = "Invalid OtpCode or expired." };
            }


            var response = new SubmitNoInsertNoAPICallsResponse()
            {
                ServiceRequests = new List<string>(),
                SQLQueryList = new List<SQLQueryList>()
            };

            try
            {
                Guid MainID = Guid.NewGuid();
                Guid responseServicingID = new Guid();
                Guid? memberID = commonRepository.GetMemberIDFromToken();
                AppConfig appConfig = unitOfWork.GetRepository<AppConfig>().Query().FirstOrDefault();
                string ServicingEmail = appConfig.ServicingEmail;


                //#region # Improper Access Control Check For Client No 

                //if (model.ServicingType == EnumServicingType.PolicyHolderInformation)
                //{
                //    var holderClientIdList = servicingDataRepository.GetPolicyHolderListForValidation();

                //    Console.WriteLine($"SubmitNoInsertNoAPICalls => {JsonConvert.SerializeObject(holderClientIdList)}");
                //    if (holderClientIdList == null || holderClientIdList.Code != 0 || holderClientIdList.Data?.Count == 0)
                //    {
                //        return new ResponseModel<SubmitNoInsertNoAPICallsResponse> { Code = 400, Message = "No Holder Client No Found" };
                //    }

                //    var validHolderCount = holderClientIdList?.Data?.Where(x => model.ClientNo.Contains(x.ClientNo)).Count();

                //    if (model.ClientNo.Count > validHolderCount)
                //    {
                //        return new ResponseModel<SubmitNoInsertNoAPICallsResponse> { Code = 400, Message = "Invalid Holder Client No" };
                //    }


                //    var hasPendingRecord = holderClientIdList?.Data?
                //        .Any(x => model.ClientNo.Contains(x.ClientNo) && x.ServiceStatus == EnumServiceStatus.Received);

                //    if (hasPendingRecord == true)
                //    {
                //        return new ResponseModel<SubmitNoInsertNoAPICallsResponse> { Code = 400, Message = "Holder Client No has pending record" };
                //    }

                //}
                //else if (model.ServicingType == EnumServicingType.InsuredPersonInformation)
                //{
                //    var insuredClientIdList = servicingDataRepository.GetInsuredPersonListForValidation();
                //    Console.WriteLine($"SubmitNoInsertNoAPICalls => {JsonConvert.SerializeObject(insuredClientIdList)}");

                //    if (insuredClientIdList == null || insuredClientIdList.Code != 0 || insuredClientIdList.Data?.Count == 0)
                //    {
                //        return new ResponseModel<SubmitNoInsertNoAPICallsResponse> { Code = 400, Message = "No Insured Client No Found" };
                //    }

                //    var validInsuredCount = insuredClientIdList?.Data?.Where(x => model.ClientNo.Contains(x.ClientNo)).Count();

                //    if (model.ClientNo.Count > validInsuredCount)
                //    {
                //        return new ResponseModel<SubmitNoInsertNoAPICallsResponse> { Code = 400, Message = "Invalid Insured Client No" };
                //    }

                //    var hasPendingRecord = insuredClientIdList?.Data?
                //        .Any(x => model.ClientNo.Contains(x.ClientNo) && x.ServiceStatus == EnumServiceStatus.Received);

                //    if (hasPendingRecord == true)
                //    {
                //        return new ResponseModel<SubmitNoInsertNoAPICallsResponse> { Code = 400, Message = "Insured Client No has pending record" };
                //    }
                //}

                //#endregion

                model.MainId = MainID;
                Console.WriteLine($"SubmitNoInsertNoAPICalls => {model.ServicingType} {model.MainId} " +
                    $"original model.ClientNo => {string.Join(", ", model.ClientNo)} model.IsAllProfileUpdate => {model.IsAllProfileUpdate}");

                #region #IsAllProfileUpdate && Added related client no to model.ClientNo
                if (model.IsAllProfileUpdate == true)
                {

                    var allClientNoList = new List<string>();
                    if (model.ClaimOtp.ReferenceNo == "Issued")
                    {
                        allClientNoList = SubmitNoInsertNoAPICalls_GetAllClientNoList(model.ClientNo.FirstOrDefault(), response); // get all client no list by NRC , 12/ABCDEF(N)111111

                    }
                    else if (model.ClaimOtp.ReferenceNo == "Fixed")
                    {
                        allClientNoList = SubmitNoInsertNoAPICalls_GetAllClientNoList2(model.ClientNo.FirstOrDefault(), response); // get all client no list by NRC , 12/ABCDEF(N)111111

                    }
                    // let say
                    // 1111,
                    // 2222,
                    // 3333,
                    // 4444,
                    // 5555

                    if (model.ServicingType == EnumServicingType.PolicyHolderInformation)
                    {
                        var policyListByHolder = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => allClientNoList.Contains(x.PolicyHolderClientNo) && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                            .ToList();
                        // all policy list by those all client no as holder 
                        //p1, 1111, 3333
                        //p2, 1111, 3333
                        //p3, 1111, 3333
                        //p4, 3333, 4444

                        var insuredClientNoList = policyListByHolder
                            .Where(x => allClientNoList.Contains(x.InsuredPersonClientNo))
                            .Select(x => x.InsuredPersonClientNo)
                            .ToList()
                            .Distinct();
                        // get all insured client no list by matching same client no from all client no list by NRC 
                        // 3333
                        // 4444

                        var beneficiaryClientNoList = unitOfWork.GetRepository<Entities.Beneficiary>()
                            .Query(x => allClientNoList.Contains(x.BeneficiaryClientNo)
                            && policyListByHolder.Select(x => x.PolicyNo).Contains(x.PolicyNo))
                            .Select(x => x.BeneficiaryClientNo)
                            .ToList()
                            .Distinct(); // get all beneficiary client no list by matching same client no from all client no list by NRC 
                                         // and matching policy no from all policy list
                                         // 5555 as beneficiary client no
                                         // p1, 1111, 3333, 5555

                        if (insuredClientNoList?.Any() == true)
                        {
                            model.ClientNo.AddRange(insuredClientNoList); //added 
                                                                          // 3333
                                                                          // 4444
                        }

                        if (beneficiaryClientNoList?.Any() == true)
                        {
                            model.ClientNo.AddRange(beneficiaryClientNoList); //added 
                                                                              // 5555
                        }


                        model.ClientNo = model.ClientNo.Distinct().ToList(); //original client no is 1111 ,
                                                                             //so list will be
                                                                             //1111, as holder
                                                                             //3333, as insured
                                                                             //4444, as insured
                                                                             //5555, as beneficiary

                    }
                    else if (model.ServicingType == EnumServicingType.InsuredPersonInformation)
                    {
                        var policyListByInsuredQuery = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => allClientNoList.Contains(x.InsuredPersonClientNo)
                            && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus));


                        var policyListByInsured = policyListByInsuredQuery.ToList();
                        // all policy list by those all client no as holder 
                        //p1, 1111, 3333
                        //p2, 1111, 3333
                        //p3, 1111, 3333
                        //p4, 3333, 4444
                        //p6, 6666, 3333 // holder is from other NRC
                        //p7, 7777, 3333 // holder is from other NRC

                        var policyListByInsuredQueryString = policyListByInsuredQuery.ToQueryString();
                        response.SQLQueryList.Add(new SQLQueryList
                        {
                            QueryName = "InsuredPolicies",
                            Query = policyListByInsuredQueryString,
                            ResultCount = policyListByInsured.Count,
                            Result = policyListByInsured.Select(x => x.PolicyNo).ToList(),
                        });

                        Console.WriteLine("SubmitNoInsertNoAPICalls_policyListByInsured SQL: " + policyListByInsuredQueryString);


                        var holderClientNoListQuery = policyListByInsured
                            .Where(x => allClientNoList.Contains(x.PolicyHolderClientNo))
                            .Select(x => x.PolicyHolderClientNo);

                        var holderClientNoList = holderClientNoListQuery
                            .ToList()
                            .Distinct();
                        // get all holder client no list by matching same client no from all client no list by NRC 
                        // 1111
                        // 3333

                        var holderClientNoListQueryString = holderClientNoListQuery.AsQueryable().ToQueryString();
                        response.SQLQueryList.Add(new SQLQueryList
                        {
                            QueryName = "HolderClientNoList",
                            Query = holderClientNoListQueryString,
                            ResultCount = policyListByInsured.Count,
                            Result = holderClientNoList.ToList(),

                        });

                        Console.WriteLine("SubmitNoInsertNoAPICalls_holderClientNoList SQL: " + holderClientNoListQueryString);


                        var policyListByHolderQuery = policyListByInsured
                            .Where(x => holderClientNoList.Contains(x.PolicyHolderClientNo));

                        var policyListByHolder = policyListByHolderQuery.ToList();

                        //p1, 1111, 3333
                        //p2, 1111, 3333
                        //p3, 1111, 3333
                        //p4, 3333, 4444

                        var policyListByHolderQueryString = policyListByHolderQuery.AsQueryable().ToQueryString();
                        //response.SQLQueryList.Add(new SQLQueryList
                        //{
                        //    QueryName = "policyListByHolder",
                        //    Query = policyListByHolderQueryString,
                        //    ResultCount = policyListByHolder.Count,
                        //    Result = policyListByHolder.Select(x => x.PolicyNo).ToList(),
                        //});

                        Console.WriteLine("SubmitNoInsertNoAPICalls_policyListByHolder SQL: " + policyListByHolderQueryString);


                        var beneficiaryClientNoListQuery = unitOfWork.GetRepository<Entities.Beneficiary>()
                            .Query(x => allClientNoList.Contains(x.BeneficiaryClientNo)
                            && policyListByHolder.Select(x => x.PolicyNo).Contains(x.PolicyNo))
                            .Select(x => x.BeneficiaryClientNo);

                        var beneficiaryClientNoList = beneficiaryClientNoListQuery
                            .ToList()
                            .Distinct();


                        var beneficiaryClientNoListQueryString = beneficiaryClientNoListQuery.AsQueryable().ToQueryString();
                        response.SQLQueryList.Add(new SQLQueryList
                        {
                            QueryName = "BeneficiaryClientNoList",
                            Query = beneficiaryClientNoListQueryString,
                            ResultCount = beneficiaryClientNoList.Count(),
                            Result = beneficiaryClientNoList.ToList(),
                        });
                        Console.WriteLine("SubmitNoInsertNoAPICalls_beneficiaryClientNoList SQL: " + beneficiaryClientNoListQueryString);

                        // and matching policy no from all policy list
                        // 5555 as beneficiary client no
                        // p1, 1111, 3333, 5555

                        if (holderClientNoList?.Any() == true)
                        {
                            model.ClientNo.AddRange(holderClientNoList);
                        }

                        if (beneficiaryClientNoList?.Any() == true)
                        {
                            model.ClientNo.AddRange(beneficiaryClientNoList);
                        }

                        model.ClientNo = model.ClientNo.Distinct().ToList();
                        //1111
                        //3333
                        //5555
                        
                        response.SQLQueryList.Add(new SQLQueryList
                        {
                            QueryName = "Distinct (HolderClientNoList && BeneficiaryClientNoList)",
                            Query = "LinqQuery",
                            ResultCount = model.ClientNo.Count(),
                            Result = model.ClientNo,
                        });
                        Console.WriteLine("SubmitNoInsertNoAPICalls_insuredClientIdList SQL: " + "LinqQuery");
                    }
                }

                #endregion

                Console.WriteLine($"SubmitNoInsertNoAPICalls => {model.ServicingType} {model.MainId} => model.ClientNo => {model.ClientNo.Count} {string.Join(Environment.NewLine, model.ClientNo)}");

                foreach (var clientNo in model.ClientNo)
                {
                    Client client = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == clientNo).FirstOrDefault();

                    int updateLevel = 0;
                    ILServicingChangeRequest ilUpdateData = new ILServicingChangeRequest();
                    ilUpdateData.client = new ILServicingClientInfo();

                    ServicingRequest data = new ServicingRequest();
                    data.ServicingID = Guid.NewGuid();
                    data.MainID = MainID;
                    responseServicingID = data.ServicingID;

                    data.ServicingType = model.ServicingType.ToString();
                    data.ClientNo = clientNo;

                    if (model.MaritalStatus != null)
                    {
                        data.MaritalStatus_Old = model.MaritalStatus.Old;
                        data.MaritalStatus_New = model.MaritalStatus.New;
                        ilUpdateData.client.maritalStatus = model.MaritalStatus.New;
                        updateLevel = 2;
                    }

                    if (model.FatherName != null)
                    {
                        data.FatherName_Old = model.FatherName.Old;
                        data.FatherName_New = model.FatherName.New;
                        ilUpdateData.client.fathersName = model.FatherName.New;
                    }

                    if (model.PhoneNumber != null)
                    {
                        data.PhoneNumber_Old = model.PhoneNumber.Old;
                        data.PhoneNumber_New = model.PhoneNumber.New;
                        ilUpdateData.client.phone = model.PhoneNumber.New;
                        updateLevel = 2;
                    }

                    if (model.EmailAddress != null)
                    {
                        data.EmailAddress_Old = model.EmailAddress.Old;
                        data.EmailAddress_New = model.EmailAddress.New;
                        ilUpdateData.client.email = model.EmailAddress.New;
                    }

                    if (model.Country != null)
                    {
                        data.Country_Old = model.Country.Old;
                        data.Country_New = model.Country.New;
                        ilUpdateData.client.country = model.Country.New;
                    }

                    if (model.Province != null)
                    {
                        Entities.Province oldProvince = unitOfWork.GetRepository<Entities.Province>().Query(x => x.province_code == model.Province.Old).FirstOrDefault();
                        Entities.Province newProvince = unitOfWork.GetRepository<Entities.Province>().Query(x => x.province_code == model.Province.New).FirstOrDefault();

                        data.Province_Old = oldProvince != null ? oldProvince.province_eng_name : model.Province.Old;
                        data.Province_New = newProvince != null ? newProvince.province_eng_name : model.Province.New;
                        ilUpdateData.client.address5 = newProvince != null ? newProvince.province_eng_name : model.Province.New;
                    }

                    if (model.Distinct != null)
                    {
                        Entities.District oldDistrict = unitOfWork.GetRepository<Entities.District>().Query(x => x.district_code == model.Distinct.Old).FirstOrDefault();
                        Entities.District newDistrict = unitOfWork.GetRepository<Entities.District>().Query(x => x.district_code == model.Distinct.New).FirstOrDefault();

                        data.Distinct_Old = oldDistrict != null ? oldDistrict.district_eng_name : model.Distinct.Old;
                        data.Distinct_New = newDistrict != null ? newDistrict.district_eng_name : model.Distinct.New;
                        ilUpdateData.client.address4 = newDistrict != null ? newDistrict.district_eng_name : model.Distinct.New;
                    }

                    if (model.Township != null)
                    {
                        Entities.Township oldTownship = unitOfWork.GetRepository<Entities.Township>().Query(x => x.township_code == model.Township.Old).FirstOrDefault();
                        Entities.Township newTownship = unitOfWork.GetRepository<Entities.Township>().Query(x => x.township_code == model.Township.New).FirstOrDefault();

                        data.Township_Old = oldTownship != null ? oldTownship.township_eng_name : model.Township.Old;
                        data.Township_New = newTownship != null ? newTownship.township_eng_name : model.Township.New;
                        ilUpdateData.client.address3 = newTownship != null ? newTownship.township_eng_name : model.Township.New;
                        ilUpdateData.townshipCode = model.Township.New;
                    }

                    if (model.Building != null)
                    {
                        data.Building_Old = model.Building.Old;
                        data.Building_New = model.Building.New;
                        ilUpdateData.client.address1 = model.Building.New;
                    }

                    if (model.Street != null)
                    {
                        data.Street_Old = model.Street.Old;
                        data.Street_New = model.Street.New;
                        ilUpdateData.client.address2 = model.Street.New;
                    }

                    data.SignatureImage = model.SignatureImage;

                    data.CreatedOn = Utils.GetDefaultDate();
                    data.MemberID = memberID;
                    data.Status = EnumServicingStatus.Received.ToString();


                    List<string> policyList = new List<string>();
                    if (data.ServicingType == EnumServicingType.PolicyHolderInformation.ToString())
                    {
                        policyList = GetActivePolicyNoListByHolder(clientNo);
                    }
                    else
                    {
                        policyList = GetActivePolicyNoListByInsured(clientNo);
                    }
                    string policyNumber = policyList.FirstOrDefault();

                    Policy policy = unitOfWork.GetRepository<Policy>().Query(x => x.PolicyNo == policyNumber).FirstOrDefault();

                    ilUpdateData.client.updateClientLevel = updateLevel.ToString();
                    ilUpdateData.client.clientNumber = clientNo;
                    ilUpdateData.client.dob = client.Dob;
                    ilUpdateData.client.gender = client.Gender;
                    if (!String.IsNullOrEmpty(client.Nrc))
                    {
                        ilUpdateData.client.idtype = "N";
                        ilUpdateData.client.idnumber = client.Nrc;
                    }
                    else if (!String.IsNullOrEmpty(client.PassportNo))
                    {
                        ilUpdateData.client.idtype = "X";
                        ilUpdateData.client.idnumber = client.PassportNo;
                    }
                    else if (!String.IsNullOrEmpty(client.Other))
                    {
                        ilUpdateData.client.idtype = "N";
                        ilUpdateData.client.idnumber = client.Other;
                    }
                    ilUpdateData.client.name = client.Name;
                    ilUpdateData.client.occupation = client.Occupation;

                    if (model.ServicingType == EnumServicingType.PolicyHolderInformation)
                    {
                        ilUpdateData.requestType = "CPH";
                    }
                    else
                    {
                        ilUpdateData.requestType = "CPI";
                    }
                    ilUpdateData.policyNumber = policyNumber;

                    data.ILRequest = JsonConvert.SerializeObject(ilUpdateData);
                    data.ILRequestOn = Utils.GetDefaultDate();

                    //

                    data.ILResponse = "";
                    data.ILResponseOn = Utils.GetDefaultDate();

                    var logText = $"{data.ServicingID}|{data.ServicingType}|{data.ClientNo}" +
                      $"|{data.MaritalStatus_Old}->{data.MaritalStatus_New}" +
                      $"|{data.FatherName_Old}->{data.FatherName_New}" +
                      $"|{data.PhoneNumber_Old}->{data.PhoneNumber_New}" +
                      $"|{data.EmailAddress_Old}->{data.EmailAddress_New}" +
                      $"|{data.Country_Old}->{data.Country_New}" +
                      $"|{data.Province_Old}->{data.Province_New}" +
                      $"|{data.Distinct_Old}->{data.Distinct_New}" +
                      $"|{data.Township_Old}->{data.Township_New}" +
                      $"|{data.Building_Old}->{data.Building_New}" +
                      $"|{data.Street_Old}->{data.Street_New}" +
                      $"|{data.SignatureImage}" +
                      $"|{data.CreatedOn}" +
                      $"|{data.UpdatedOn}" +
                      $"|{data.MemberID}" +
                      $"|{data.Status}" +
                      $"|{data.UpdateChannel}";

                    Console.WriteLine($"ServicingRequest => {logText}");

                    response.ServiceRequests.Add(logText);

                }
                response.ServiceRequestCount = response.ServiceRequests.Count;

                Console.WriteLine($"SubmitNoInsertNoAPICalls => ServicingResponseModel => {JsonConvert.SerializeObject(response)} ");

                return errorCodeProvider.GetResponseModel<SubmitNoInsertNoAPICallsResponse>(ErrorCode.E0, response);

            }
            catch (Exception ex)
            {
                //MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                Console.WriteLine("Servicing Submit Error : " + ex);
                return errorCodeProvider.GetResponseModel<SubmitNoInsertNoAPICallsResponse>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<SubmitNoInsertNoAPICallsResponse>> SubmitNoInsertNoAPICalls22(ServicingRequestModel model)
        {
            Console.WriteLine($"SubmitNoInsertNoAPICalls => ServicingRequestModel => {JsonConvert.SerializeObject(model)} ");

            if (model.ClaimOtp?.OtpCode != "C0d!go777$$131369")
            {
                return new ResponseModel<SubmitNoInsertNoAPICallsResponse> { Code = 401, Message = "Invalid OtpCode or expired." };
            }


            var response = new SubmitNoInsertNoAPICallsResponse()
            {
                ServiceRequests = new List<string>(),
                SQLQueryList = new List<SQLQueryList>()
            };

            try
            {
                Guid MainID = Guid.NewGuid();
                Guid responseServicingID = new Guid();
                Guid? memberID = commonRepository.GetMemberIDFromToken();
                AppConfig appConfig = unitOfWork.GetRepository<AppConfig>().Query().FirstOrDefault();
                string ServicingEmail = appConfig.ServicingEmail;


                

                model.MainId = MainID;
                Console.WriteLine($"SubmitNoInsertNoAPICalls => {model.ServicingType} {model.MainId} " +
                    $"original model.ClientNo => {string.Join(", ", model.ClientNo)} model.IsAllProfileUpdate => {model.IsAllProfileUpdate}");

                #region #IsAllProfileUpdate && Added related client no to model.ClientNo
                if (model.IsAllProfileUpdate == true)
                {

                    var allClientNoList = new List<string>();
                    if (model.ClaimOtp.ReferenceNo == "Issued")
                    {
                        allClientNoList = SubmitNoInsertNoAPICalls_GetAllClientNoList(model.ClientNo.FirstOrDefault(), response); // get all client no list by NRC , 12/ABCDEF(N)111111

                    }
                    else if (model.ClaimOtp.ReferenceNo == "Fixed")
                    {
                        allClientNoList = SubmitNoInsertNoAPICalls_GetAllClientNoList2(model.ClientNo.FirstOrDefault(), response); // get all client no list by NRC , 12/ABCDEF(N)111111

                    }
                    // let say
                    // 1111,
                    // 2222,
                    // 3333,
                    // 4444,
                    // 5555

                    if (model.ServicingType == EnumServicingType.PolicyHolderInformation)
                    {
                        var policyListByHolder = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => allClientNoList.Contains(x.PolicyHolderClientNo) && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                            .ToList();
                        // all policy list by those all client no as holder 
                        //p1, 1111, 3333
                        //p2, 1111, 3333
                        //p3, 1111, 3333
                        //p4, 3333, 4444

                        var insuredClientNoList = policyListByHolder
                            .Where(x => allClientNoList.Contains(x.InsuredPersonClientNo))
                            .Select(x => x.InsuredPersonClientNo)
                            .ToList()
                            .Distinct();
                        // get all insured client no list by matching same client no from all client no list by NRC 
                        // 3333
                        // 4444

                        var beneficiaryClientNoList = unitOfWork.GetRepository<Entities.Beneficiary>()
                            .Query(x => allClientNoList.Contains(x.BeneficiaryClientNo)
                            && policyListByHolder.Select(x => x.PolicyNo).Contains(x.PolicyNo))
                            .Select(x => x.BeneficiaryClientNo)
                            .ToList()
                            .Distinct(); // get all beneficiary client no list by matching same client no from all client no list by NRC 
                                         // and matching policy no from all policy list
                                         // 5555 as beneficiary client no
                                         // p1, 1111, 3333, 5555

                        if (insuredClientNoList?.Any() == true)
                        {
                            model.ClientNo.AddRange(insuredClientNoList); //added 
                                                                          // 3333
                                                                          // 4444
                        }

                        if (beneficiaryClientNoList?.Any() == true)
                        {
                            model.ClientNo.AddRange(beneficiaryClientNoList); //added 
                                                                              // 5555
                        }


                        model.ClientNo = model.ClientNo.Distinct().ToList(); //original client no is 1111 ,
                                                                             //so list will be
                                                                             //1111, as holder
                                                                             //3333, as insured
                                                                             //4444, as insured
                                                                             //5555, as beneficiary

                    }
                    else if (model.ServicingType == EnumServicingType.InsuredPersonInformation)
                    {
                        var policyListByInsuredQuery = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => allClientNoList.Contains(x.InsuredPersonClientNo)
                            && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus));


                        var policyListByInsured = policyListByInsuredQuery.ToList();
                        // all policy list by those all client no as holder 
                        //p1, 1111, 3333
                        //p2, 1111, 3333
                        //p3, 1111, 3333
                        //p4, 3333, 4444
                        //p6, 6666, 3333 // holder is from other NRC
                        //p7, 7777, 3333 // holder is from other NRC

                        var policyListByInsuredQueryString = policyListByInsuredQuery.ToQueryString();
                        response.SQLQueryList.Add(new SQLQueryList
                        {
                            QueryName = "InsuredPolicies",
                            Query = policyListByInsuredQueryString,
                            ResultCount = policyListByInsured.Count,
                            Result = policyListByInsured.Select(x => x.PolicyNo).ToList(),
                        });

                        Console.WriteLine("SubmitNoInsertNoAPICalls_policyListByInsured SQL: " + policyListByInsuredQueryString);


                        var holderClientNoListQuery = policyListByInsured
                            .Where(x => allClientNoList.Contains(x.PolicyHolderClientNo))
                            .Select(x => x.PolicyHolderClientNo);

                        var holderClientNoList = holderClientNoListQuery
                            .ToList()
                            .Distinct();
                        // get all holder client no list by matching same client no from all client no list by NRC 
                        // 1111
                        // 3333

                        var holderClientNoListQueryString = holderClientNoListQuery.AsQueryable().ToQueryString();
                        response.SQLQueryList.Add(new SQLQueryList
                        {
                            QueryName = "HolderClientNoList",
                            Query = holderClientNoListQueryString,
                            ResultCount = policyListByInsured.Count,
                            Result = holderClientNoList.ToList(),

                        });

                        Console.WriteLine("SubmitNoInsertNoAPICalls_holderClientNoList SQL: " + holderClientNoListQueryString);


                        var policyListByHolderQuery = policyListByInsured
                            .Where(x => holderClientNoList.Contains(x.PolicyHolderClientNo));

                        var policyListByHolder = policyListByHolderQuery.ToList();

                        //p1, 1111, 3333
                        //p2, 1111, 3333
                        //p3, 1111, 3333
                        //p4, 3333, 4444

                        var policyListByHolderQueryString = policyListByHolderQuery.AsQueryable().ToQueryString();
                        //response.SQLQueryList.Add(new SQLQueryList
                        //{
                        //    QueryName = "policyListByHolder",
                        //    Query = policyListByHolderQueryString,
                        //    ResultCount = policyListByHolder.Count,
                        //    Result = policyListByHolder.Select(x => x.PolicyNo).ToList(),
                        //});

                        Console.WriteLine("SubmitNoInsertNoAPICalls_policyListByHolder SQL: " + policyListByHolderQueryString);


                        var beneficiaryClientNoListQuery = unitOfWork.GetRepository<Entities.Beneficiary>()
                            .Query(x => allClientNoList.Contains(x.BeneficiaryClientNo)
                            && policyListByHolder.Select(x => x.PolicyNo).Contains(x.PolicyNo))
                            .Select(x => x.BeneficiaryClientNo);

                        var beneficiaryClientNoList = beneficiaryClientNoListQuery
                            .ToList()
                            .Distinct();


                        var beneficiaryClientNoListQueryString = beneficiaryClientNoListQuery.AsQueryable().ToQueryString();
                        response.SQLQueryList.Add(new SQLQueryList
                        {
                            QueryName = "BeneficiaryClientNoList",
                            Query = beneficiaryClientNoListQueryString,
                            ResultCount = beneficiaryClientNoList.Count(),
                            Result = beneficiaryClientNoList.ToList(),
                        });
                        Console.WriteLine("SubmitNoInsertNoAPICalls_beneficiaryClientNoList SQL: " + beneficiaryClientNoListQueryString);

                        // and matching policy no from all policy list
                        // 5555 as beneficiary client no
                        // p1, 1111, 3333, 5555

                        if (holderClientNoList?.Any() == true)
                        {
                            model.ClientNo.AddRange(holderClientNoList);
                        }

                        if (beneficiaryClientNoList?.Any() == true)
                        {
                            model.ClientNo.AddRange(beneficiaryClientNoList);
                        }

                        model.ClientNo = model.ClientNo.Distinct().ToList();
                        //1111
                        //3333
                        //5555

                        response.SQLQueryList.Add(new SQLQueryList
                        {
                            QueryName = "Distinct (HolderClientNoList && BeneficiaryClientNoList)",
                            Query = "LinqQuery",
                            ResultCount = model.ClientNo.Count(),
                            Result = model.ClientNo,
                        });
                        Console.WriteLine("SubmitNoInsertNoAPICalls_insuredClientIdList SQL: " + "LinqQuery");
                    }
                }

                #endregion

                Console.WriteLine($"SubmitNoInsertNoAPICalls => {model.ServicingType} {model.MainId} => model.ClientNo => {model.ClientNo.Count} {string.Join(Environment.NewLine, model.ClientNo)}");

                foreach (var clientNo in model.ClientNo)
                {
                    // --- Fetch client with try-catch and null check ---
                    Client client = null;
                    try
                    {
                        client = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == clientNo).FirstOrDefault();
                        if (client == null)
                        {
                            Console.WriteLine($"Client not found: {clientNo}");
                            continue; // Skip this client
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching client {clientNo}: {ex.Message}");
                        continue; // Skip this client
                    }

                    int updateLevel = 0;
                    ILServicingChangeRequest ilUpdateData = new ILServicingChangeRequest
                    {
                        client = new ILServicingClientInfo()
                    };

                    // --- Prepare servicing request ---
                    ServicingRequest data = new ServicingRequest
                    {
                        ServicingID = Guid.NewGuid(),
                        MainID = MainID,
                        ServicingType = model.ServicingType.ToString(),
                        ClientNo = clientNo,
                        SignatureImage = model.SignatureImage,
                        CreatedOn = Utils.GetDefaultDate(),
                        MemberID = memberID,
                        Status = EnumServicingStatus.Received.ToString()
                    };

                    responseServicingID = data.ServicingID;

                    // --- Update fields based on model ---
                    if (model.MaritalStatus != null)
                    {
                        data.MaritalStatus_Old = model.MaritalStatus.Old;
                        data.MaritalStatus_New = model.MaritalStatus.New;
                        ilUpdateData.client.maritalStatus = model.MaritalStatus.New;
                        updateLevel = 2;
                    }

                    if (model.FatherName != null)
                    {
                        data.FatherName_Old = model.FatherName.Old;
                        data.FatherName_New = model.FatherName.New;
                        ilUpdateData.client.fathersName = model.FatherName.New;
                    }

                    if (model.PhoneNumber != null)
                    {
                        data.PhoneNumber_Old = model.PhoneNumber.Old;
                        data.PhoneNumber_New = model.PhoneNumber.New;
                        ilUpdateData.client.phone = model.PhoneNumber.New;
                        updateLevel = 2;
                    }

                    if (model.EmailAddress != null)
                    {
                        data.EmailAddress_Old = model.EmailAddress.Old;
                        data.EmailAddress_New = model.EmailAddress.New;
                        ilUpdateData.client.email = model.EmailAddress.New;
                    }

                    if (model.Country != null)
                    {
                        data.Country_Old = model.Country.Old;
                        data.Country_New = model.Country.New;
                        ilUpdateData.client.country = model.Country.New;
                    }

                    if (model.Province != null)
                    {
                        Entities.Province oldProvince = unitOfWork.GetRepository<Entities.Province>()
                            .Query(x => x.province_code == model.Province.Old).FirstOrDefault();
                        Entities.Province newProvince = unitOfWork.GetRepository<Entities.Province>()
                            .Query(x => x.province_code == model.Province.New).FirstOrDefault();

                        data.Province_Old = oldProvince != null ? oldProvince.province_eng_name : model.Province.Old;
                        data.Province_New = newProvince != null ? newProvince.province_eng_name : model.Province.New;
                        ilUpdateData.client.address5 = newProvince != null ? newProvince.province_eng_name : model.Province.New;
                    }

                    if (model.Distinct != null)
                    {
                        Entities.District oldDistrict = unitOfWork.GetRepository<Entities.District>()
                            .Query(x => x.district_code == model.Distinct.Old).FirstOrDefault();
                        Entities.District newDistrict = unitOfWork.GetRepository<Entities.District>()
                            .Query(x => x.district_code == model.Distinct.New).FirstOrDefault();

                        data.Distinct_Old = oldDistrict != null ? oldDistrict.district_eng_name : model.Distinct.Old;
                        data.Distinct_New = newDistrict != null ? newDistrict.district_eng_name : model.Distinct.New;
                        ilUpdateData.client.address4 = newDistrict != null ? newDistrict.district_eng_name : model.Distinct.New;
                    }

                    if (model.Township != null)
                    {
                        Entities.Township oldTownship = unitOfWork.GetRepository<Entities.Township>()
                            .Query(x => x.township_code == model.Township.Old).FirstOrDefault();
                        Entities.Township newTownship = unitOfWork.GetRepository<Entities.Township>()
                            .Query(x => x.township_code == model.Township.New).FirstOrDefault();

                        data.Township_Old = oldTownship != null ? oldTownship.township_eng_name : model.Township.Old;
                        data.Township_New = newTownship != null ? newTownship.township_eng_name : model.Township.New;
                        ilUpdateData.client.address3 = newTownship != null ? newTownship.township_eng_name : model.Township.New;
                        ilUpdateData.townshipCode = model.Township.New;
                    }

                    if (model.Building != null)
                    {
                        data.Building_Old = model.Building.Old;
                        data.Building_New = model.Building.New;
                        ilUpdateData.client.address1 = model.Building.New;
                    }

                    if (model.Street != null)
                    {
                        data.Street_Old = model.Street.Old;
                        data.Street_New = model.Street.New;
                        ilUpdateData.client.address2 = model.Street.New;
                    }

                    // --- Get active policies ---
                    List<string> policyList = new List<string>();
                    if (data.ServicingType == EnumServicingType.PolicyHolderInformation.ToString())
                    {
                        policyList = GetActivePolicyNoListByHolder(clientNo);
                    }
                    else
                    {
                        policyList = GetActivePolicyNoListByInsured(clientNo);
                    }

                        string policyNumber = policyList.FirstOrDefault();
                    if (policyNumber == null)
                    {
                        Console.WriteLine($"No active policy found for client {clientNo}");
                        continue;
                    }

                    Policy policy = unitOfWork.GetRepository<Policy>().Query(x => x.PolicyNo == policyNumber).FirstOrDefault();

                    // --- Prepare IL update data ---
                    ilUpdateData.client.updateClientLevel = updateLevel.ToString();
                    ilUpdateData.client.clientNumber = clientNo;
                    ilUpdateData.client.dob = client.Dob;
                    ilUpdateData.client.gender = client.Gender;
                    ilUpdateData.client.name = client.Name;
                    ilUpdateData.client.occupation = client.Occupation;

                    if (!string.IsNullOrEmpty(client.Nrc))
                    {
                        ilUpdateData.client.idtype = "N";
                        ilUpdateData.client.idnumber = client.Nrc;
                    }
                    else if (!string.IsNullOrEmpty(client.PassportNo))
                    {
                        ilUpdateData.client.idtype = "X";
                        ilUpdateData.client.idnumber = client.PassportNo;
                    }
                    else if (!string.IsNullOrEmpty(client.Other))
                    {
                        ilUpdateData.client.idtype = "N";
                        ilUpdateData.client.idnumber = client.Other;
                    }

                    ilUpdateData.requestType = model.ServicingType == EnumServicingType.PolicyHolderInformation ? "CPH" : "CPI";
                    ilUpdateData.policyNumber = policyNumber;

                    // --- Serialize ILRequest safely ---
                    try
                    {
                        data.ILRequest = JsonConvert.SerializeObject(ilUpdateData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error serializing ILRequest for client {clientNo}: {ex.Message}");
                        data.ILRequest = "";
                    }

                    data.ILRequestOn = Utils.GetDefaultDate();
                    data.ILResponse = "";
                    data.ILResponseOn = Utils.GetDefaultDate();

                    // --- Log data for diagnostics ---
                    var logText = $"{data.ServicingID}|{data.ServicingType}|{data.ClientNo}" +
                      $"|{data.MaritalStatus_Old}->{data.MaritalStatus_New}" +
                      $"|{data.FatherName_Old}->{data.FatherName_New}" +
                      $"|{data.PhoneNumber_Old}->{data.PhoneNumber_New}" +
                      $"|{data.EmailAddress_Old}->{data.EmailAddress_New}" +
                      $"|{data.Country_Old}->{data.Country_New}" +
                      $"|{data.Province_Old}->{data.Province_New}" +
                      $"|{data.Distinct_Old}->{data.Distinct_New}" +
                      $"|{data.Township_Old}->{data.Township_New}" +
                      $"|{data.Building_Old}->{data.Building_New}" +
                      $"|{data.Street_Old}->{data.Street_New}" +
                      $"|{data.SignatureImage}" +
                      $"|{data.CreatedOn}" +
                      $"|{data.UpdatedOn}" +
                      $"|{data.MemberID}" +
                      $"|{data.Status}" +
                      $"|{data.UpdateChannel}";

                    Console.WriteLine($"ServicingRequest => {logText}");
                    response.ServiceRequests.Add(logText);
                }

                response.ServiceRequestCount = response.ServiceRequests.Count;

                Console.WriteLine($"SubmitNoInsertNoAPICalls => ServicingResponseModel => {JsonConvert.SerializeObject(response)} ");

                return errorCodeProvider.GetResponseModel<SubmitNoInsertNoAPICallsResponse>(ErrorCode.E0, response);

            }
            catch (Exception ex)
            {
                //MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                Console.WriteLine("Servicing Submit Error : " + ex);
                return errorCodeProvider.GetResponseModel<SubmitNoInsertNoAPICallsResponse>(ErrorCode.E500);
            }
        }
    }

    public class SubmitNoInsertNoAPICallsResponse
    {
        public List<SQLQueryList> SQLQueryList { get; set; }
        public int ServiceRequestCount { get; set; }
        public List<string> ServiceRequests { get; set; }
    }

    public class SQLQueryList
    {
        public string QueryName { get; set; }
        public string Query { get; set; }
        public int ResultCount
        {
            get; set;
        }
        public List<string>? Result { get; set; }
        
    }
}