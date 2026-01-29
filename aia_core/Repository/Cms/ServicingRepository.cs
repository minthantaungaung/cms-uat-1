using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Request.Common;
using aia_core.Model.Cms.Request.Servicing;
using aia_core.Model.Cms.Response;
using aia_core.Model.Cms.Response.Common;
using aia_core.Model.Cms.Response.Servicing;
using aia_core.Model.Cms.Response.ServicingDetail;
using aia_core.Model.Mobile.Response;
using aia_core.Model.Mobile.Response.AiaILApiResponse;
using aia_core.Model.Mobile.Servicing.Data.Response;
using aia_core.RecurringJobs;
using aia_core.Repository.Mobile;
using aia_core.Services;
using aia_core.UnitOfWork;
using Azure;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Google.Apis.Requests.BatchRequest;

namespace aia_core.Repository.Cms
{
    public interface IServicingRepository
    {
        ResponseModel<PagedList<ServicingListResponse>> List(ServicingListRequest model);
        ResponseModel<ServiceDetailResponse> Get(Guid serviceId);

        ResponseModel<string> UpdateServiceStatus(ServiceStatusUpdateRequest model);

        ResponseModel<PagedList<ServiceImagingLogResponse>> ImagingLog(ServiceImagingLogRequest model);

        ResponseModel<PagedList<ServiceImagingLogResponse>> ImagingLogOld(ServiceImagingLogRequest model);

        ResponseModel<PagedList<ServiceFailedLogResponse>> FailedLog(ServiceFailedLogRequest model);

        ResponseModel<FailedLogDetailResponse> GetFailedLogDetail(Guid id);

        ResponseModel<List<ServicingListResponse>> Export(DateTime? fromDate, DateTime? toDate);

        ResponseModel<PagedList<ServicingListResponse>> Export(ServicingListRequest model);
    }
    public class ServicingRepository : BaseRepository, IServicingRepository
    {
        private readonly INotificationService notificationService;

        public ServicingRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Context> unitOfWork, IRecurringJobRunner recurringJobRunner, INotificationService notificationService)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.notificationService = notificationService;
        }

        ResponseModel<List<ServicingListResponse>> IServicingRepository.Export(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var list = unitOfWork.GetRepository<Entities.ServiceMain>()
                    .Query(x => x.CreatedDate.Value.Date >= fromDate.Value.Date && x.CreatedDate.Value.Date <= toDate.Value.Date)
                    .OrderByDescending(x => x.CreatedDate)
                    .Select(x => new ServicingListResponse
                    {
                        MainId = x.MainID,
                        ServiceId = x.ServiceID,
                        CreatedDate = x.CreatedDate,
                        AppMemberId = x.LoginMemberID,
                        MemberId = x.MemberID,
                        GroupMemberId = x.GroupMemberID,
                        MemberPhone = x.MobileNumber,
                        ServiceType = x.ServiceType,
                        ServiceStatus = x.ServiceStatus,
                        PolicyNumber = x.PolicyNumber,
                        PolicyStatus = x.PolicyStatus,
                        SubmissionDate = x.CreatedDate,
                        MemberType = x.MemberType,
                        StatusUpdatedBy = x.UpdatedBy != null ? x.UpdatedBy.ToString() : "",
                        IsPending = x.IsPending,
                        MemberName = x.MemberName,
                        StatusUpdatedDate = x.UpdatedOn,
                        UpdateChannel = x.UpdateChannel,
                    }
                    )
                    .ToList();                

                list?.ForEach(item =>
                {
                    if (item.IsPending != null && item.IsPending == true && item.ServiceStatus == EnumServiceStatus.Received.ToString())
                    {
                        item.ServiceStatus = "Pending";
                    }

                    if (item.CreatedDate != null)
                    {
                        item.RemainingTime = GetProgressAndContactHour(item.CreatedDate.Value)?.Hours ?? "";
                    }

                    var servicetype = unitOfWork.GetRepository<Entities.ServiceType>().Query(x => x.ServiceTypeEnum == item.ServiceType).FirstOrDefault();
                    item.ServiceName = servicetype?.ServiceTypeNameEn;


                    var policy = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.PolicyNo == item.PolicyNumber)
                        .FirstOrDefault();

                    if (policy != null)
                    {
                        var policyStatus = unitOfWork.GetRepository<Entities.PolicyStatus>()
                        .Query(x => x.ShortDesc == policy.PolicyStatus)
                        .FirstOrDefault();

                        item.PolicyStatus = policyStatus?.LongDesc;
                    }

                    if (!string.IsNullOrEmpty(item.StatusUpdatedBy))
                    {
                        var updatedBy = unitOfWork.GetRepository<Entities.Staff>()
                        .Query(x => x.Id == new Guid(item.StatusUpdatedBy) && x.IsActive == true)
                        .Select(x => x.Name)
                        .FirstOrDefault();


                        item.StatusUpdatedBy = updatedBy;
                    }
                    else
                    {
                        item.StatusUpdatedBy = item.UpdateChannel;

                        if (item.UpdateChannel == "Job")
                        {
                            item.StatusUpdatedBy = "IL";
                        }
                    }

                });


                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Servicing,
                        objectAction: EnumObjectAction.Export);

                return errorCodeProvider.GetResponseModel<List<ServicingListResponse>>(ErrorCode.E0, list);

            }
            catch (Exception ex)
            {
                CmsErrorLog("Export", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<ServicingListResponse>>(ErrorCode.E500);
            }
        }

        ResponseModel<PagedList<ServiceFailedLogResponse>> IServicingRepository.FailedLog(ServiceFailedLogRequest model)
        {
            try
            {

                var query = unitOfWork.GetRepository<Entities.ServicePaymentFrequencyValidateMessage>()
                    .Query();

                if (!string.IsNullOrEmpty(model.PolicyNo))
                {
                    query = query.Where(x => x.PolicyNumber.Contains(model.PolicyNo));
                }
                if (!string.IsNullOrEmpty(model.MemberId))
                {
                    query = query.Where(x => x.ClientNo.Contains(model.MemberId));
                }
                if (!string.IsNullOrEmpty(model.PhoneNo))
                {
                    query = query.Where(x => x.MobileNumber.Contains(model.PhoneNo));
                }

                var count = query.Count();

                var response = query
                    .OrderByDescending(x => x.Date)
                    .Skip(((model.Page ?? 0) - 1) * (model.Size ?? 0)).Take(model.Size ?? 0)
                    .Select(x => new ServiceFailedLogResponse
                    {
                        TranDate = x.Date,
                        ServiceType = "Payment Frequency",
                        PolicyNumber = x.PolicyNumber,
                        MemberId = x.ClientNo,
                        PhoneNo = x.MobileNumber,
                        Old = x.Old,
                        New = x.New,
                        ErrorMessage = x.Message,
                        
                        Id = x.Id,
                    })
                    .ToList();
                

                var data = new PagedList<ServiceFailedLogResponse>(
                    source: response,
                    totalCount: count,
                    pageNumber: model.Page ?? 0,
                    pageSize: model.Size ?? 0);

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Servicing,
                        objectAction: EnumObjectAction.View);

                return errorCodeProvider.GetResponseModel<PagedList<ServiceFailedLogResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<ServiceFailedLogResponse>>(ErrorCode.E500);
            }
        }

        ResponseModel<ServiceDetailResponse> IServicingRepository.Get(Guid serviceId)
        {
            try
            {
                var serviceMain = unitOfWork.GetRepository<Entities.ServiceMain>().Query(x => x.ServiceID == serviceId).FirstOrDefault();

                if (serviceMain != null)
                {
                    var response = new ServiceDetailResponse();

                    #region #Common
                    var serviceType = unitOfWork.GetRepository<Entities.ServiceType>()
                                    .Query(x => x.ServiceTypeEnum == serviceMain.ServiceType)
                                    .FirstOrDefault();

                    response.serviceType = serviceMain.ServiceType;
                    response.serviceName = serviceType?.ServiceTypeNameEn;
                    response.memberId = serviceMain.LoginMemberID?.ToString();
                    response.serviceStatus = serviceMain.ServiceStatus;

                    if (serviceMain.IsPending != null && serviceMain.IsPending == true 
                        && serviceMain.ServiceStatus == EnumServiceStatus.Received.ToString())
                    {
                        response.serviceStatus = "Pending";
                    }



                    

                    response.serviceStatusList = new List<string> { "Received", "Approved", "NotApproved", "Paid", "Pending" };

                    response.internalRemark = serviceMain.InternalRemark;

                    var member = unitOfWork.GetRepository<Entities.Member>().Query(x => x.MemberId == serviceMain.LoginMemberID).FirstOrDefault();
                    response.memberName = member?.Name;

                    response.policyNumber = serviceMain.PolicyNumber;

                    var product = unitOfWork.GetRepository<Entities.Product>()
                        .Query(x => x.ProductTypeShort == serviceMain.ProductType && x.IsActive == true && x.IsDelete == false)
                        .FirstOrDefault();

                    response.productType = product?.TitleEn;

                    response.policyHolderClientNo = serviceMain.MemberID;


                    var policy = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.PolicyNo == serviceMain.PolicyNumber)
                        .FirstOrDefault();

                    

                    
                    response.paidToDate = policy?.PaidToDate;
                    response.policyIssueDate = policy?.PolicyIssueDate;

                    if (policy != null)
                    {
                        var policyStatus = unitOfWork.GetRepository<Entities.PolicyStatus>()
                        .Query(x => x.ShortDesc == policy.PolicyStatus)
                        .FirstOrDefault();

                        response.policyStatus = policyStatus?.LongDesc;
                    }

                    var client = unitOfWork.GetRepository<Entities.Client>()
                            .Query(x => x.ClientNo == serviceMain.MemberID)
                            .FirstOrDefault();

                    response.policyHolderName = client?.Name;

                    var serviceProgress = GetProgressAndContactHour(serviceMain.CreatedDate.Value, EnumProgressType.Service);

                    response.remainingTime = serviceProgress?.Hours;                   

                    #endregion

                    response.requestSummary = new RequestSummary();

                    if (serviceMain.CreatedDate != null)
                        response.requestSummary.submissionDate = serviceMain.CreatedDate.Value.ToString(DefaultConstants.DateTimeFormat);

                    if (serviceMain.ServiceType == EnumServiceType.InsuredPersonInformation.ToString()
                        || serviceMain.ServiceType == EnumServiceType.PolicyHolderInformation.ToString())
                    {
                        var servicingDetail = unitOfWork.GetRepository<Entities.ServicingRequest>()
                           .Query(x => x.ServicingID == serviceId)
                           .FirstOrDefault();

                        
                        

                        if (servicingDetail != null)
                        {
                            if (!string.IsNullOrEmpty(servicingDetail.SignatureImage))
                                response.signatureImage = GetFileFullUrl(servicingDetail.SignatureImage);

                            if (!string.IsNullOrEmpty(servicingDetail.ILResponse))
                            {
                                try
                                {
                                    var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(servicingDetail.ILResponse);
                                    response.ilStatus = ilResponse?.data?.status;
                                    response.ilErrorMessage = ilResponse?.data?.errorMessage;
                                }
                                catch {
                                    response.ilStatus = "IL error";
                                    response.ilErrorMessage = servicingDetail.ILResponse;
                                }
                                
                            }


                            var oldCountryName = unitOfWork.GetRepository<Entities.Country>()
                                 .Query(x => x.code == servicingDetail.Country_Old)
                                 .Select(x => x.description)
                                 .FirstOrDefault();

                            var newCountryName = unitOfWork.GetRepository<Entities.Country>()
                                .Query(x => x.code == servicingDetail.Country_New)
                                .Select(x => x.description)
                                .FirstOrDefault();

                            response.requestSummary.general = new List<General>
                            {
                                new General
                                {
                                    label = "Married Status",
                                    oldInformation = Utils.GetMaritalStatus(servicingDetail.MaritalStatus_Old),
                                    newInformation = Utils.GetMaritalStatus(servicingDetail.MaritalStatus_New),
                                },
                                new General
                                {
                                    label = "Father name",
                                    oldInformation = servicingDetail.FatherName_Old,
                                    newInformation = servicingDetail.FatherName_New,
                                },
                                new General
                                {
                                    label = "Phone number",
                                    oldInformation = servicingDetail.PhoneNumber_Old,
                                    newInformation = servicingDetail.PhoneNumber_New,
                                },
                                new General
                                {
                                    label = "Country",
                                    oldInformation = oldCountryName ?? servicingDetail.Country_Old,
                                    newInformation = newCountryName ?? servicingDetail.Country_New,
                                },
                                new General
                                {
                                     label = "Province",
                                    oldInformation = servicingDetail.Province_Old,
                                    newInformation = servicingDetail.Province_New,
                                },
                                new General
                                {
                                     label = "Distinct",
                                    oldInformation = servicingDetail.Distinct_Old,
                                    newInformation = servicingDetail.Distinct_New,
                                },
                                new General
                                {
                                     label = "Township",
                                    oldInformation = servicingDetail.Township_Old,
                                    newInformation = servicingDetail.Township_New,
                                },
                                new General
                                {
                                     label = "Street",
                                    oldInformation = servicingDetail.Street_Old,
                                    newInformation = servicingDetail.Street_New,
                                },
                                new General
                                {
                                     label = "Building",
                                    oldInformation = servicingDetail.Building_Old,
                                    newInformation = servicingDetail.Building_New,
                                },
                            };
                        }
                    }
                    if (serviceMain.ServiceType == EnumServiceType.BeneficiaryInformation.ToString())
                    {
                        var servicingDetail = unitOfWork.GetRepository<Entities.ServiceBeneficiary>()
                           .Query(x => x.ID == serviceId)
                           .FirstOrDefault();

                        if (servicingDetail != null)
                        {
                            if (!string.IsNullOrEmpty(servicingDetail.SignatureImage))
                                response.signatureImage = GetFileFullUrl(servicingDetail.SignatureImage);

                            if (!string.IsNullOrEmpty(servicingDetail.ILResponse))
                            {
                                


                                try
                                {
                                    var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(servicingDetail.ILResponse);
                                    response.ilStatus = ilResponse?.data?.status;
                                    response.ilErrorMessage = ilResponse?.data?.errorMessage;
                                }
                                catch
                                {
                                    response.ilStatus = "IL error";
                                    response.ilErrorMessage = servicingDetail.ILResponse;
                                }
                            }

                            response.requestSummary.beneficiary = new Model.Cms.Response.ServicingDetail.Beneficiary();

                            var newBeneficiaryList = unitOfWork.GetRepository<Entities.ServiceBeneficiaryPersonalInfo>()
                           .Query(x => x.ServiceBeneficiaryID == serviceId && x.IsNewBeneficiary == true)
                           .ToList();

                            if (newBeneficiaryList != null)
                            {

                                response.requestSummary.beneficiary.newBeneficiaries = new List<NewBeneficiary>();

                                foreach (var beneficiary in newBeneficiaryList)
                                {
                                    response.attachments = new List<Attachment>();

                                    var gender = "";
                                    if (!string.IsNullOrEmpty(beneficiary.Gender))
                                    {
                                        gender = Utils.GetGender(beneficiary.Gender);
                                    }                                        

                                    var dob = "";
                                    if (beneficiary.Dob != null)
                                    {
                                        dob = beneficiary.Dob.Value.ToString(DefaultConstants.DateFormat);
                                    }

                                    var idFrontImage = "";
                                    if (!string.IsNullOrEmpty(beneficiary.IdFrontImageName))
                                    {
                                        idFrontImage = GetFileFullUrl(beneficiary.IdFrontImageName);

                                        response.attachments.Add(new Attachment
                                        {
                                            fileName = beneficiary.IdFrontImageName,
                                            fileUrl = idFrontImage,
                                            errorMessage = "Success",
                                        });
                                    }

                                    var idBackImage = "";
                                    if (!string.IsNullOrEmpty(beneficiary.IdBackImageName))
                                    {
                                        idBackImage = GetFileFullUrl(beneficiary.IdBackImageName);

                                        response.attachments.Add(new Attachment
                                        {
                                            fileName = beneficiary.IdBackImageName,
                                            fileUrl = idBackImage,
                                            errorMessage = "Success",
                                        });
                                    }

                                    response.requestSummary.beneficiary.newBeneficiaries.Add(new NewBeneficiary
                                    { 
                                        name = beneficiary.Name,
                                        gender = gender,
                                        mobileNo = beneficiary.MobileNumber,
                                        dob = dob,
                                        idType = beneficiary.IdType,
                                        idValue = beneficiary.IdValue,
                                        idBackImage = idBackImage,
                                        idFrontImage = idFrontImage,
                                    });
                                }
                            }


                            var oldBeneficiaryList = unitOfWork.GetRepository<Entities.ServiceBeneficiaryPersonalInfo>()
                          .Query(x => x.ServiceBeneficiaryID == serviceId && x.IsNewBeneficiary == false)
                          .ToList();

                            if (oldBeneficiaryList != null)
                            {
                                response.requestSummary.beneficiary.profileUpdate = new List<ProfileUpdate>();

                                foreach (var beneficiary in oldBeneficiaryList)
                                {
                                    var _oldClient = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == beneficiary.ClientNo)
                                        .FirstOrDefault();
                                    string _idValue = "";
                                    string _idType = "";
                                    if(!String.IsNullOrEmpty(_oldClient.Nrc))
                                    {
                                        _idValue = _oldClient.Nrc;
                                        _idType = "NRC";
                                    }
                                    else if(!String.IsNullOrEmpty(_oldClient.PassportNo))
                                    {
                                        _idValue = _oldClient.PassportNo;
                                        _idType = "Passport";
                                    }
                                    else if(!String.IsNullOrEmpty(_oldClient.Other))
                                    {
                                        _idValue = _oldClient.Other;
                                        _idType = "Other";
                                    }

                                    response.requestSummary.beneficiary.profileUpdate.Add(new ProfileUpdate
                                    { 
                                        label = "Mobile No",
                                        oldInformation = beneficiary.OldMobileNumber,
                                        newInformation = beneficiary.NewMobileNumber,
                                        idValue = _idValue,
                                        idType = _idType,
                                        Name = beneficiary.Name,
                                    });
                                }
                            }

                            var sharedList = unitOfWork.GetRepository<Entities.ServiceBeneficiaryShareInfo>()
                           .Query(x => x.ServiceBeneficiaryID == serviceId)
                           .ToList();

                            if (sharedList != null)
                            {
                                response.requestSummary.beneficiary.beneficiaryShare = new List<BeneficiaryShare>();

                                foreach(var share in sharedList)
                                {
                                    string name = "";
                                    if(String.IsNullOrEmpty(share.ClientNo))
                                    {
                                        name = newBeneficiaryList.Where(x=> x.IdValue == share.IdValue).Select(x=> x.Name).FirstOrDefault();
                                    }
                                    else
                                    {
                                        name = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == share.ClientNo)
                                        .Select(x =>  x.Name)
                                        .FirstOrDefault();
                                    }


                                    var oldRelationship = unitOfWork.GetRepository<Entities.Relationship>().Query(x => x.Code == share.OldRelationShipCode)
                                        .Select(x => x.Name)
                                        .FirstOrDefault();

                                    var newRelationShip = unitOfWork.GetRepository<Entities.Relationship>().Query(x => x.Code == share.NewRelationShipCode)
                                        .Select(x => x.Name)
                                        .FirstOrDefault();

                                    var oldPercentage = 0;
                                    if (share.OldPercentage != null)
                                    {
                                        oldPercentage = Convert.ToInt32(share.OldPercentage);
                                    }

                                    var newPercentage = 0;
                                    if (share.NewPercentage != null)
                                    {
                                        newPercentage = Convert.ToInt32(share.NewPercentage);
                                    }
                                    response.requestSummary.beneficiary.beneficiaryShare.Add(new BeneficiaryShare
                                    { 
                                        name = name,
                                        oldRelationship = oldRelationship ?? share.OldRelationShipCode,
                                        newRelationship = newRelationShip ?? share.NewRelationShipCode,
                                        oldPercentage = $"{oldPercentage.ToString()}%",
                                        newPercentage = $"{newPercentage.ToString()}%",
                                        beneficiaryType = share.Type,
                                    }
                                    );
                                }
                            }
                        }
                    }
                    else if (serviceMain.ServiceType == EnumServiceType.LapseReinstatement.ToString()
                        || serviceMain.ServiceType == EnumServiceType.HealthRenewal.ToString()
                        || serviceMain.ServiceType == EnumServiceType.PolicyLoanRepayment.ToString()
                        || serviceMain.ServiceType == EnumServiceType.AcpLoanRepayment.ToString()
                        || serviceMain.ServiceType == EnumServiceType.AdHocTopup.ToString()
                        || serviceMain.ServiceType == EnumServiceType.SumAssuredChange.ToString()
                        )
                    {
                        if (serviceMain.ServiceType == EnumServiceType.LapseReinstatement.ToString())
                        {
                            var serviceDetail = unitOfWork.GetRepository<Entities.ServiceLapseReinstatement>()
                                    .Query(x => x.ID == serviceId)
                                    .FirstOrDefault();

                            if (serviceDetail != null)
                            {
                                
                                response.requestSummary.amount = $"{serviceDetail.Amount:N0} MMK";
                                response.requestSummary.reasonOfRequest = serviceDetail.Reason;

                                if (!string.IsNullOrEmpty(serviceDetail.SignatureImage))
                                    response.signatureImage = GetFileFullUrl(serviceDetail.SignatureImage);

                                if (!string.IsNullOrEmpty(serviceDetail.ILResponse))
                                {
                                    
                                    try
                                    {
                                        var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(serviceDetail.ILResponse);
                                        response.ilStatus = ilResponse?.data?.status;
                                        response.ilErrorMessage = ilResponse?.data?.errorMessage;
                                    }
                                    catch
                                    {
                                        response.ilStatus = "IL error";
                                        response.ilErrorMessage = serviceDetail.ILResponse;
                                    }
                                }

                                var docList = unitOfWork.GetRepository<Entities.ServiceLapseReinstatementDoc>()
                                    .Query(x => x.ServiceLapseReinstatementID == serviceId)
                                    .ToList();

                                if (docList != null)
                                {
                                    response.attachments = new List<Attachment>();

                                    docList?.ForEach(doc =>
                                    {
                                        response.attachments.Add(new Attachment
                                        {
                                            fileName = doc.DocName,
                                            fileUrl = GetFileFullUrl(doc.DocName),
                                            errorMessage = doc.UploadStatus,
                                        });
                                    }
                                    );

                                }

                            }
                        }
                        else if (serviceMain.ServiceType == EnumServiceType.HealthRenewal.ToString())
                        {
                            var serviceDetail = unitOfWork.GetRepository<Entities.ServiceHealthRenewal>()
                                    .Query(x => x.ID == serviceId)
                                    .FirstOrDefault();

                            if (serviceDetail != null)
                            {
                                response.requestSummary.amount = $"{serviceDetail.Amount:N0} MMK";
                                response.requestSummary.reasonOfRequest = serviceDetail.Reason;

                                if (!string.IsNullOrEmpty(serviceDetail.SignatureImage))
                                    response.signatureImage = GetFileFullUrl(serviceDetail.SignatureImage);

                                if (!string.IsNullOrEmpty(serviceDetail.ILResponse))
                                {
                                    
                                    try
                                    {
                                        var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(serviceDetail.ILResponse);
                                        response.ilStatus = ilResponse?.data?.status;
                                        response.ilErrorMessage = ilResponse?.data?.errorMessage;
                                    }
                                    catch
                                    {
                                        response.ilStatus = "IL error";
                                        response.ilErrorMessage = serviceDetail.ILResponse;
                                    }
                                }

                                var docList = unitOfWork.GetRepository<Entities.ServiceHealthRenewalDoc>()
                                    .Query(x => x.ServiceHealthRenewalID == serviceId)
                                    .ToList();

                                if (docList != null)
                                {
                                    response.attachments = new List<Attachment>();

                                    docList?.ForEach(doc =>
                                    {
                                        response.attachments.Add(new Attachment
                                        {
                                            fileName = doc.DocName,
                                            fileUrl = GetFileFullUrl(doc.DocName),
                                            errorMessage = doc.UploadStatus,
                                        });
                                    }
                                    );

                                }

                            }
                        }
                        else if (serviceMain.ServiceType == EnumServiceType.PolicyLoanRepayment.ToString())
                        {
                            var serviceDetail = unitOfWork.GetRepository<Entities.ServicePolicyLoanRepayment>()
                                    .Query(x => x.ID == serviceId)
                                    .FirstOrDefault();

                            if (serviceDetail != null)
                            {
                                response.requestSummary.amount = $"{serviceDetail.Amount:N0} MMK";
                                response.requestSummary.reasonOfRequest = serviceDetail.Reason;

                                if (!string.IsNullOrEmpty(serviceDetail.SignatureImage))
                                    response.signatureImage = GetFileFullUrl(serviceDetail.SignatureImage);

                                if (!string.IsNullOrEmpty(serviceDetail.ILResponse))
                                {
                                    
                                    try
                                    {
                                        var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(serviceDetail.ILResponse);
                                        response.ilStatus = ilResponse?.data?.status;
                                        response.ilErrorMessage = ilResponse?.data?.errorMessage;
                                    }
                                    catch
                                    {
                                        response.ilStatus = "IL error";
                                        response.ilErrorMessage = serviceDetail.ILResponse;
                                    }
                                }

                                var docList = unitOfWork.GetRepository<Entities.ServicePolicyLoanRepaymentDoc>()
                                    .Query(x => x.ServicePolicyLoanRepaymentID == serviceId)
                                    .ToList();

                                if (docList != null)
                                {
                                    response.attachments = new List<Attachment>();

                                    docList?.ForEach(doc =>
                                    {
                                        response.attachments.Add(new Attachment
                                        {
                                            fileName = doc.DocName,
                                            fileUrl = GetFileFullUrl(doc.DocName),
                                            errorMessage = doc.UploadStatus,
                                        });
                                    }
                                    );

                                }

                            }
                        }
                        else if (serviceMain.ServiceType == EnumServiceType.AcpLoanRepayment.ToString())
                        {
                            var serviceDetail = unitOfWork.GetRepository<Entities.ServiceACPLoanRepayment>()
                                    .Query(x => x.ID == serviceId)
                                    .FirstOrDefault();

                            if (serviceDetail != null)
                            {
                                response.requestSummary.amount = $"{serviceDetail.Amount:N0} MMK";
                                response.requestSummary.reasonOfRequest = serviceDetail.Reason;

                                if (!string.IsNullOrEmpty(serviceDetail.SignatureImage))
                                    response.signatureImage = GetFileFullUrl(serviceDetail.SignatureImage);

                                if (!string.IsNullOrEmpty(serviceDetail.ILResponse))
                                {
                                    
                                    try
                                    {
                                        var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(serviceDetail.ILResponse);
                                        response.ilStatus = ilResponse?.data?.status;
                                        response.ilErrorMessage = ilResponse?.data?.errorMessage;
                                    }
                                    catch
                                    {
                                        response.ilStatus = "IL error";
                                        response.ilErrorMessage = serviceDetail.ILResponse;
                                    }
                                }

                                var docList = unitOfWork.GetRepository<Entities.ServiceACPLoanRepaymentDoc>()
                                    .Query(x => x.ServiceACPLoanRepaymentID == serviceId)
                                    .ToList();

                                if (docList != null)
                                {
                                    response.attachments = new List<Attachment>();

                                    docList?.ForEach(doc =>
                                    {
                                        response.attachments.Add(new Attachment
                                        {
                                            fileName = doc.DocName,
                                            fileUrl = GetFileFullUrl(doc.DocName),
                                            errorMessage = doc.UploadStatus,
                                        });
                                    }
                                    );

                                }

                            }
                        }
                        else if (serviceMain.ServiceType == EnumServiceType.AdHocTopup.ToString())
                        {
                            var serviceDetail = unitOfWork.GetRepository<Entities.ServiceAdhocTopup>()
                                    .Query(x => x.ID == serviceId)
                                    .FirstOrDefault();

                            if (serviceDetail != null)
                            {
                                response.requestSummary.amount = $"{serviceDetail.Amount:N0} MMK";
                                response.requestSummary.reasonOfRequest = serviceDetail.Reason;

                                if (!string.IsNullOrEmpty(serviceDetail.SignatureImage))
                                    response.signatureImage = GetFileFullUrl(serviceDetail.SignatureImage);

                                if (!string.IsNullOrEmpty(serviceDetail.ILResponse))
                                {
                                    
                                    try
                                    {
                                        var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(serviceDetail.ILResponse);
                                        response.ilStatus = ilResponse?.data?.status;
                                        response.ilErrorMessage = ilResponse?.data?.errorMessage;
                                    }
                                    catch
                                    {
                                        response.ilStatus = "IL error";
                                        response.ilErrorMessage = serviceDetail.ILResponse;
                                    }
                                }

                                var docList = unitOfWork.GetRepository<Entities.ServiceAdhocTopupDoc>()
                                    .Query(x => x.ServiceAdhocTopupID == serviceId)
                                    .ToList();

                                if (docList != null)
                                {
                                    response.attachments = new List<Attachment>();

                                    docList?.ForEach(doc =>
                                    {
                                        response.attachments.Add(new Attachment
                                        {
                                            fileName = doc.DocName,
                                            fileUrl = GetFileFullUrl(doc.DocName),
                                            errorMessage = doc.UploadStatus,
                                        });
                                    }
                                    );

                                }

                            }
                        }
                        else if (serviceMain.ServiceType == EnumServiceType.SumAssuredChange.ToString())
                        {
                            var serviceDetail = unitOfWork.GetRepository<Entities.ServiceSumAssuredChange>()
                                    .Query(x => x.ID == serviceId)
                                    .FirstOrDefault();

                            if (serviceDetail != null)
                            {
                                response.requestSummary.amount = $"{serviceDetail.Amount:N0} MMK";
                                response.requestSummary.reasonOfRequest = serviceDetail.Reason;

                                if (!string.IsNullOrEmpty(serviceDetail.SignatureImage))
                                    response.signatureImage = GetFileFullUrl(serviceDetail.SignatureImage);

                                if (!string.IsNullOrEmpty(serviceDetail.ILResponse))
                                {
                                    
                                    try
                                    {
                                        var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(serviceDetail.ILResponse);
                                        response.ilStatus = ilResponse?.data?.status;
                                        response.ilErrorMessage = ilResponse?.data?.errorMessage;
                                    }
                                    catch
                                    {
                                        response.ilStatus = "IL error";
                                        response.ilErrorMessage = serviceDetail.ILResponse;
                                    }
                                }

                                var docList = unitOfWork.GetRepository<Entities.ServiceSumAssuredChangeDoc>()
                                    .Query(x => x.ServiceSumAssuredChangeID == serviceId)
                                    .ToList();

                                if (docList != null)
                                {
                                    response.attachments = new List<Attachment>();

                                    docList?.ForEach(doc =>
                                    {
                                        response.attachments.Add(new Attachment
                                        {
                                            fileName = doc.DocName,
                                            fileUrl = GetFileFullUrl(doc.DocName),
                                            errorMessage = doc.UploadStatus,
                                        });
                                    }
                                    );

                                }

                            }
                        }
                    }
                    else if (serviceMain.ServiceType == EnumServiceType.PartialWithdraw.ToString()
                        || serviceMain.ServiceType == EnumServiceType.PolicyLoan.ToString()
                        || serviceMain.ServiceType == EnumServiceType.PolicySurrender.ToString()
                        || serviceMain.ServiceType == EnumServiceType.PolicyPaidUp.ToString()
                        || serviceMain.ServiceType == EnumServiceType.RefundOfPayment.ToString())
                    {
                        if (serviceMain.ServiceType == EnumServiceType.PartialWithdraw.ToString())
                        {
                            var serviceDetail = unitOfWork.GetRepository<Entities.ServicePartialWithdraw>()
                                    .Query(x => x.ID == serviceId)
                                    .FirstOrDefault();

                            if (serviceDetail != null)
                            {
                                response.requestSummary.amount = $"{serviceDetail.Amount:N0} MMK";
                                response.requestSummary.reasonOfRequest = serviceDetail.Reason;

                                response.bank = new Model.Cms.Response.ServicingDetail.Bank
                                {
                                    bankCode = serviceDetail.BankCode,
                                    bankName = serviceDetail.BankName,
                                    bankAccountName = serviceDetail.BankAccountName,
                                    bankAccountNumber = serviceDetail.BankAccountNumber,
                                };

                                if (!string.IsNullOrEmpty(serviceDetail.SignatureImage))
                                    response.signatureImage = GetFileFullUrl(serviceDetail.SignatureImage);

                                if (!string.IsNullOrEmpty(serviceDetail.ILResponse))
                                {
                                    
                                    try
                                    {
                                        var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(serviceDetail.ILResponse);
                                        response.ilStatus = ilResponse?.data?.status;
                                        response.ilErrorMessage = ilResponse?.data?.errorMessage;
                                    }
                                    catch
                                    {
                                        response.ilStatus = "IL error";
                                        response.ilErrorMessage = serviceDetail.ILResponse;
                                    }
                                }

                                var docList = unitOfWork.GetRepository<Entities.ServicePartialWithdrawDoc>()
                                    .Query(x => x.ServicePartialWithdrawID == serviceId)
                                    .ToList();

                                if (docList != null)
                                {
                                    response.attachments = new List<Attachment>();

                                    docList?.ForEach(doc =>
                                    {
                                        response.attachments.Add(new Attachment
                                        {
                                            fileName = doc.DocName,
                                            fileUrl = GetFileFullUrl(doc.DocName),
                                            errorMessage = doc.UploadStatus,
                                        });
                                    }
                                    );

                                }

                            }
                        }
                        else if (serviceMain.ServiceType == EnumServiceType.PolicyLoan.ToString())
                        {
                            var serviceDetail = unitOfWork.GetRepository<Entities.ServicePolicyLoan>()
                                    .Query(x => x.ID == serviceId)
                                    .FirstOrDefault();

                            if (serviceDetail != null)
                            {
                                response.requestSummary.amount = $"{serviceDetail.Amount:N0} MMK";
                                response.requestSummary.reasonOfRequest = serviceDetail.Reason;

                                response.bank = new Model.Cms.Response.ServicingDetail.Bank
                                {
                                    bankCode = serviceDetail.BankCode,
                                    bankName = serviceDetail.BankName,
                                    bankAccountName = serviceDetail.BankAccountName,
                                    bankAccountNumber = serviceDetail.BankAccountNumber,
                                };

                                if (!string.IsNullOrEmpty(serviceDetail.SignatureImage))
                                    response.signatureImage = GetFileFullUrl(serviceDetail.SignatureImage);

                                if (!string.IsNullOrEmpty(serviceDetail.ILResponse))
                                {
                                    try
                                    {
                                        var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(serviceDetail.ILResponse);
                                        response.ilStatus = ilResponse?.data?.status;
                                        response.ilErrorMessage = ilResponse?.data?.errorMessage;
                                    }
                                    catch
                                    {
                                        response.ilStatus = "IL error";
                                        response.ilErrorMessage = serviceDetail.ILResponse;
                                    }
                                }

                                var docList = unitOfWork.GetRepository<Entities.ServicePolicyLoanDoc>()
                                    .Query(x => x.ServicePolicyLoanID == serviceId)
                                    .ToList();

                                if (docList != null)
                                {
                                    response.attachments = new List<Attachment>();

                                    docList?.ForEach(doc =>
                                    {
                                        response.attachments.Add(new Attachment
                                        {
                                            fileName = doc.DocName,
                                            fileUrl = GetFileFullUrl(doc.DocName),
                                            errorMessage = doc.UploadStatus,
                                        });
                                    }
                                    );

                                }

                            }
                        }
                        else if (serviceMain.ServiceType == EnumServiceType.PolicySurrender.ToString())
                        {
                            var serviceDetail = unitOfWork.GetRepository<Entities.ServicePolicySurrender>()
                                    .Query(x => x.ID == serviceId)
                                    .FirstOrDefault();

                            if (serviceDetail != null)
                            {
                                response.requestSummary.amount = $"{serviceDetail.Amount:N0} MMK";
                                response.requestSummary.reasonOfRequest = serviceDetail.Reason;

                                response.bank = new Model.Cms.Response.ServicingDetail.Bank
                                {
                                    bankCode = serviceDetail.BankCode,
                                    bankName = serviceDetail.BankName,
                                    bankAccountName = serviceDetail.BankAccountName,
                                    bankAccountNumber = serviceDetail.BankAccountNumber,
                                };

                                if (!string.IsNullOrEmpty(serviceDetail.SignatureImage))
                                    response.signatureImage = GetFileFullUrl(serviceDetail.SignatureImage);

                                if (!string.IsNullOrEmpty(serviceDetail.ILResponse))
                                {
                                    try
                                    {
                                        var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(serviceDetail.ILResponse);
                                        response.ilStatus = ilResponse?.data?.status;
                                        response.ilErrorMessage = ilResponse?.data?.errorMessage;
                                    }
                                    catch
                                    {
                                        response.ilStatus = "IL error";
                                        response.ilErrorMessage = serviceDetail.ILResponse;
                                    }
                                }

                                var docList = unitOfWork.GetRepository<Entities.ServicePolicySurrenderDoc>()
                                    .Query(x => x.ServicePolicySurrenderID == serviceId)
                                    .ToList();

                                if (docList != null)
                                {
                                    response.attachments = new List<Attachment>();

                                    docList?.ForEach(doc =>
                                    {
                                        response.attachments.Add(new Attachment
                                        {
                                            fileName = doc.DocName,
                                            fileUrl = GetFileFullUrl(doc.DocName),
                                            errorMessage = doc.UploadStatus,
                                        });
                                    }
                                    );

                                }

                            }
                        }
                        else if (serviceMain.ServiceType == EnumServiceType.PolicyPaidUp.ToString())
                        {
                            var serviceDetail = unitOfWork.GetRepository<Entities.ServicePolicyPaidUp>()
                                    .Query(x => x.ID == serviceId)
                                    .FirstOrDefault();


                            if (serviceDetail != null)
                            {
                                response.requestSummary.amount = $"{serviceDetail.Amount:N0} MMK";
                                response.requestSummary.reasonOfRequest = serviceDetail.Reason;

                                response.bank = new Model.Cms.Response.ServicingDetail.Bank
                                {
                                    bankCode = serviceDetail.BankCode,
                                    bankName = serviceDetail.BankName,
                                    bankAccountName = serviceDetail.BankAccountName,
                                    bankAccountNumber = serviceDetail.BankAccountNumber,
                                };

                                if (!string.IsNullOrEmpty(serviceDetail.SignatureImage))
                                    response.signatureImage = GetFileFullUrl(serviceDetail.SignatureImage);

                                if (!string.IsNullOrEmpty(serviceDetail.ILResponse))
                                {
                                    try
                                    {
                                        var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(serviceDetail.ILResponse);
                                        response.ilStatus = ilResponse?.data?.status;
                                        response.ilErrorMessage = ilResponse?.data?.errorMessage;
                                    }
                                    catch
                                    {
                                        response.ilStatus = "IL error";
                                        response.ilErrorMessage = serviceDetail.ILResponse;
                                    }
                                }

                                var docList = unitOfWork.GetRepository<Entities.ServicePolicyPaidUpDoc>()
                                    .Query(x => x.ServicePolicyPaidUpID == serviceId)
                                    .ToList();

                                if (docList != null)
                                {
                                    response.attachments = new List<Attachment>();

                                    docList?.ForEach(doc =>
                                    {
                                        response.attachments.Add(new Attachment
                                        {
                                            fileName = doc.DocName,
                                            fileUrl = GetFileFullUrl(doc.DocName),
                                            errorMessage = doc.UploadStatus,
                                        });
                                    }
                                    );

                                }

                            }
                        }
                        else if (serviceMain.ServiceType == EnumServiceType.RefundOfPayment.ToString())
                        {
                            var serviceDetail = unitOfWork.GetRepository<Entities.ServiceRefundOfPayment>()
                                    .Query(x => x.ID == serviceId)
                                    .FirstOrDefault();

                            if (serviceDetail != null)
                            {
                                response.requestSummary.amount = $"{serviceDetail.Amount:N0} MMK";
                                response.requestSummary.reasonOfRequest = serviceDetail.Reason;

                                response.bank = new Model.Cms.Response.ServicingDetail.Bank
                                {
                                    bankCode = serviceDetail.BankCode,
                                    bankName = serviceDetail.BankName,
                                    bankAccountName = serviceDetail.BankAccountName,
                                    bankAccountNumber = serviceDetail.BankAccountNumber,
                                };

                                if (!string.IsNullOrEmpty(serviceDetail.SignatureImage))
                                    response.signatureImage = GetFileFullUrl(serviceDetail.SignatureImage);

                                if (!string.IsNullOrEmpty(serviceDetail.ILResponse))
                                {
                                    try
                                    {
                                        var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(serviceDetail.ILResponse);
                                        //response.ilStatus = ilResponse?.data?.status;
                                        //response.ilErrorMessage = ilResponse?.data?.errorMessage;

                                        response.ilStatus = ilResponse?.message?.code;
                                        response.ilErrorMessage = ilResponse?.message?.text;
                                    }
                                    catch
                                    {
                                        response.ilStatus = "IL error";
                                        response.ilErrorMessage = serviceDetail.ILResponse;
                                    }
                                }

                                var docList = unitOfWork.GetRepository<Entities.ServiceRefundOfPaymentDoc>()
                                    .Query(x => x.ServiceRefundOfPaymentID == serviceId)
                                    .ToList();

                                if (docList != null)
                                {
                                    response.attachments = new List<Attachment>();

                                    docList?.ForEach(doc =>
                                    {
                                        response.attachments.Add(new Attachment
                                        {
                                            fileName = doc.DocName,
                                            fileUrl = GetFileFullUrl(doc.DocName),
                                            errorMessage = doc.UploadStatus,
                                        });
                                    }
                                    );

                                }

                            }

                            
                        }
                    }
                    else if (serviceMain.ServiceType == EnumServiceType.PaymentFrequency.ToString())
                    {
                        //TODO attachment

                        var serviceDetail = unitOfWork.GetRepository<Entities.ServicePaymentFrequency>()
                                    .Query(x => x.ID == serviceId)
                                    .FirstOrDefault();

                        

                        if (serviceDetail != null)
                        {
                            if (!string.IsNullOrEmpty(serviceDetail.ILResponse))
                            {
                                try
                                {
                                    var ilResponse = JsonConvert.DeserializeObject<CommonRegisterResponse>(serviceDetail.ILResponse);
                                    response.ilStatus = ilResponse?.data?.status;
                                    response.ilErrorMessage = ilResponse?.data?.errorMessage;
                                }
                                catch
                                {
                                    response.ilStatus = "IL error";
                                    response.ilErrorMessage = serviceDetail.ILResponse;
                                }
                            }                            

                            response.requestSummary.general = new List<General>
                            {
                                new General
                                {
                                    label = "Frequency",
                                    oldInformation = Utils.GetPaymentFrequency(serviceDetail.FrequencyType_Old),
                                    newInformation = Utils.GetPaymentFrequency(serviceDetail.FrequencyType_New),
                                },
                                new General
                                {
                                    label = "Installment",
                                    oldInformation = $"{serviceDetail.Amount_Old:N0} MMK",
                                    newInformation = $"{serviceDetail.Amount_New:N0} MMK",
                                },
                            };

                            if (!string.IsNullOrEmpty(serviceDetail.SignatureImage))
                                response.signatureImage = GetFileFullUrl(serviceDetail.SignatureImage);
                        }
                    }

                    #region #Imaging Log Error
                    var serviceMainDocList = unitOfWork.GetRepository<Entities.ServiceMainDoc>()
                        .Query(x => x.ServiceId == serviceId)
                        .OrderByDescending(x => x.CmsRequestOn)
                        .ThenBy(x => x.DocType)
                        .ToList();

                    response.attachments = new List<Attachment>();
                    serviceMainDocList?.ForEach(doc => 
                    {
                        response.attachments.Add(new Attachment
                        {
                            FormId = doc.FormId,
                            fileName = doc?.DocName,
                            fileUrl = GetFileFullUrl(doc?.DocName),
                            errorMessage = doc?.UploadStatus,
                        });
                    });

                    #endregion

                    CmsAuditLog(
                            objectGroup: EnumObjectGroup.Servicing,
                            objectAction: EnumObjectAction.View);

                        return errorCodeProvider.GetResponseModel<ServiceDetailResponse>(ErrorCode.E0, response);
                    }
                    else
                    {

                    CmsAuditLog(
                            objectGroup: EnumObjectGroup.Servicing,
                            objectAction: EnumObjectAction.View);
                    return errorCodeProvider.GetResponseModel<ServiceDetailResponse>(ErrorCode.E400);
                    }


                

            }
            catch (Exception ex)
            {
                CmsErrorLog("ServiceDetail Ex =>", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<ServiceDetailResponse>(ErrorCode.E400);
            }
        }

        ResponseModel<FailedLogDetailResponse> IServicingRepository.GetFailedLogDetail(Guid id)
        {
            try
            {
                var response = new FailedLogDetailResponse();

                var validateMessage = unitOfWork.GetRepository<Entities.ServicePaymentFrequencyValidateMessage>()
                        .Query(x => x.Id == id).FirstOrDefault();

                if(validateMessage != null)
                {
                    var policy = unitOfWork.GetRepository<Entities.Policy>().Query(x => x.PolicyNo == validateMessage.PolicyNumber).FirstOrDefault();

                    if (policy != null)
                    {
                        var product = unitOfWork.GetRepository<Entities.Product>()
                            .Query(x => x.ProductTypeShort == policy.ProductType && x.IsDelete == false && x.IsActive == true).FirstOrDefault();
                        var holder = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == policy.PolicyHolderClientNo).FirstOrDefault();
                        var insured = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == policy.InsuredPersonClientNo).FirstOrDefault();
                        var policyStatus = unitOfWork.GetRepository<Entities.PolicyStatus>().Query(x => x.ShortDesc == policy.PolicyStatus).FirstOrDefault();


                        response.PolicyNumber = validateMessage.PolicyNumber;
                        response.ProductType = product?.TitleEn;
                        response.PolicyStatus = policyStatus?.LongDesc;
                        response.Components = policy.Components;
                        response.PolicyHolderClientNo = policy.PolicyHolderClientNo;
                        response.PolicyHolderClientName = holder?.Name;
                        response.InsuredPersonClientNo = policy.InsuredPersonClientNo;
                        response.InsuredPersonClientName = insured?.Name;
                        response.ServiceType = "Payment Frequency";
                        response.PolicyIssuedDate = policy.PolicyIssueDate;
                        response.MemberSinceDate = policy.PolicyIssueDate;
                        response.OriginalCommenceDate = policy.OriginalCommencementDate;
                        response.Date = validateMessage.Date;
                        response.Id = id;
                        response.Olddata = validateMessage.Old;
                        response.Newdata = validateMessage.New;
                        response.ErrorMessage = validateMessage.Message;
                        response.PhoneNumber = validateMessage.MobileNumber;


                        return errorCodeProvider.GetResponseModel<FailedLogDetailResponse>(ErrorCode.E0, response);

                    }
                }
                


                return errorCodeProvider.GetResponseModel<FailedLogDetailResponse>(ErrorCode.E400);
            }
            catch (Exception ex)
            {
                CmsErrorLog("GetFailedLogDetail", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<FailedLogDetailResponse>(ErrorCode.E500);
            }
        }

        ResponseModel<PagedList<ServiceImagingLogResponse>> IServicingRepository.ImagingLogOld(ServiceImagingLogRequest model)
        {
            try
            {

                var query = unitOfWork.GetRepository<Entities.ServiceMain>()
                    .Query();

                if (model.ServiceType != null)
                {
                    query = query.Where(x => x.ServiceType == model.ServiceType.ToString());
                }

                if (model.FromDate != null && model.ToDate != null)
                {
                    query = query.Where(x => x.CreatedDate >= model.FromDate && x.CreatedDate <= model.ToDate);
                }

                if (!string.IsNullOrEmpty(model.PolicyNo))
                {
                    query = query.Where(x => x.PolicyNumber.Contains(model.PolicyNo));
                }
                if (!string.IsNullOrEmpty(model.MainServiceId))
                {
                    query = query.Where(x => x.MainID.ToString() == model.MainServiceId);
                }
                if (!string.IsNullOrEmpty(model.ServiceId))
                {
                    query = query.Where(x => x.ServiceID.ToString() == model.ServiceId);
                }

                var count = query.Count();

                var response = query
                    .OrderByDescending(x => x.CreatedDate)
                    .Skip(((model.Page ?? 0) - 1) * (model.Size ?? 0)).Take(model.Size ?? 0)
                    .Select(x => new ServiceImagingLogResponse
                    {
                        TranDate = x.CreatedDate,
                        ServiceId = x.ServiceID,
                        MainServiceId = x.MainID,
                        ServiceTypeEnum = x.ServiceType,
                        PolicyNumber = x.PolicyNumber,
                        MemberId = x.MemberID,
                        PhoneNumber = x.MobileNumber,
                    })
                    .ToList();


                foreach (var item in response)
                {
                    var serviceName = unitOfWork.GetRepository<Entities.ServiceType>()
                    .Query(x => x.ServiceTypeEnum == item.ServiceTypeEnum)
                    .Select(x => x.ServiceTypeNameEn)
                    .FirstOrDefault();

                    item.ServiceType = serviceName;



                    (string? docName, string? cmsResponse, string? code) docInfo = ("", "", "");

                    if (item.ServiceTypeEnum == EnumServiceType.PartialWithdraw.ToString())
                    {
                        var doc = unitOfWork.GetRepository<Entities.ServicePartialWithdrawDoc>()
                        .Query(x => x.ServicePartialWithdrawID == item.ServiceId)
                        .Select(x => new { x.DocName, x.CmsResponse, x.UploadStatus })
                        .FirstOrDefault();

                        if (doc != null)
                        {
                            docInfo = (doc.DocName, doc.CmsResponse, doc.UploadStatus);
                        }
                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.PolicyLoan.ToString())
                    {
                        var doc = unitOfWork.GetRepository<Entities.ServicePolicyLoanDoc>()
                        .Query(x => x.ServicePolicyLoanID == item.ServiceId)
                         .Select(x => new { x.DocName, x.CmsResponse, x.UploadStatus })
                        .FirstOrDefault();

                        if (doc != null)
                        {
                            docInfo = (doc.DocName, doc.CmsResponse, doc.UploadStatus);
                        }
                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.PolicySurrender.ToString())
                    {
                        var doc = unitOfWork.GetRepository<Entities.ServicePolicySurrenderDoc>()
                        .Query(x => x.ServicePolicySurrenderID == item.ServiceId)
                         .Select(x => new { x.DocName, x.CmsResponse, x.UploadStatus })
                        .FirstOrDefault();

                        if (doc != null)
                        {
                            docInfo = (doc.DocName, doc.CmsResponse, doc.UploadStatus);
                        }
                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.PolicyPaidUp.ToString())
                    {
                        var doc = unitOfWork.GetRepository<Entities.ServicePolicyPaidUpDoc>()
                        .Query(x => x.ServicePolicyPaidUpID == item.ServiceId)
                         .Select(x => new { x.DocName, x.CmsResponse, x.UploadStatus })
                        .FirstOrDefault();

                        if (doc != null)
                        {
                            docInfo = (doc.DocName, doc.CmsResponse, doc.UploadStatus);
                        }
                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.RefundOfPayment.ToString())
                    {
                        var doc = unitOfWork.GetRepository<Entities.ServiceRefundOfPaymentDoc>()
                        .Query(x => x.ServiceRefundOfPaymentID == item.ServiceId)
                         .Select(x => new { x.DocName, x.CmsResponse, x.UploadStatus })
                        .FirstOrDefault();

                        if (doc != null)
                        {
                            docInfo = (doc.DocName, doc.CmsResponse, doc.UploadStatus);
                        }
                    }


                    else if (item.ServiceTypeEnum == EnumServiceType.AdHocTopup.ToString())
                    {
                        var doc = unitOfWork.GetRepository<Entities.ServiceAdhocTopupDoc>()
                        .Query(x => x.ServiceAdhocTopupID == item.ServiceId)
                        .Select(x => new { x.DocName, x.CmsResponse, x.UploadStatus })
                        .FirstOrDefault();

                        if (doc != null)
                        {
                            docInfo = (doc.DocName, doc.CmsResponse, doc.UploadStatus);
                        }
                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.AcpLoanRepayment.ToString())
                    {
                        var doc = unitOfWork.GetRepository<Entities.ServiceACPLoanRepaymentDoc>()
                        .Query(x => x.ServiceACPLoanRepaymentID == item.ServiceId)
                         .Select(x => new { x.DocName, x.CmsResponse, x.UploadStatus })
                        .FirstOrDefault();

                        if (doc != null)
                        {
                            docInfo = (doc.DocName, doc.CmsResponse, doc.UploadStatus);
                        }
                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.PolicyLoanRepayment.ToString())
                    {
                        var doc = unitOfWork.GetRepository<Entities.ServicePolicyLoanRepaymentDoc>()
                        .Query(x => x.ServicePolicyLoanRepaymentID == item.ServiceId)
                        .Select(x => new { x.DocName, x.CmsResponse, x.UploadStatus })
                        .FirstOrDefault();

                        if (doc != null)
                        {
                            docInfo = (doc.DocName, doc.CmsResponse, doc.UploadStatus);
                        }
                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.HealthRenewal.ToString())
                    {
                        var doc = unitOfWork.GetRepository<Entities.ServiceHealthRenewalDoc>()
                        .Query(x => x.ServiceHealthRenewalID == item.ServiceId)
                         .Select(x => new { x.DocName, x.CmsResponse, x.UploadStatus })
                        .FirstOrDefault();

                        if (doc != null)
                        {
                            docInfo = (doc.DocName, doc.CmsResponse, doc.UploadStatus);
                        }
                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.LapseReinstatement.ToString())
                    {
                        var doc = unitOfWork.GetRepository<Entities.ServiceLapseReinstatementDoc>()
                        .Query(x => x.ServiceLapseReinstatementID == item.ServiceId)
                         .Select(x => new { x.DocName, x.CmsResponse, x.UploadStatus })
                        .FirstOrDefault();

                        if (doc != null)
                        {
                            docInfo = (doc.DocName, doc.CmsResponse, doc.UploadStatus);
                        }
                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.SumAssuredChange.ToString())
                    {
                        var doc = unitOfWork.GetRepository<Entities.ServiceSumAssuredChangeDoc>()
                        .Query(x => x.ServiceSumAssuredChangeID == item.ServiceId)
                        .Select(x => new { x.DocName, x.CmsResponse, x.UploadStatus })
                        .FirstOrDefault();

                        if (doc != null)
                        {
                            docInfo = (doc.DocName, doc.CmsResponse, doc.UploadStatus);
                        }
                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.PaymentFrequency.ToString())
                    {

                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.BeneficiaryInformation.ToString())
                    {


                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.PolicyHolderInformation.ToString()
                        || item.ServiceTypeEnum == EnumServiceType.InsuredPersonInformation.ToString())
                    {

                    }

                    item.DocName = docInfo.docName;
                    item.Code = docInfo.code;
                    item.Message = docInfo.cmsResponse;


                    if (item.ServiceTypeEnum == EnumServiceType.LapseReinstatement.ToString()
                        || item.ServiceTypeEnum == EnumServiceType.HealthRenewal.ToString()
                        || item.ServiceTypeEnum == EnumServiceType.PolicyLoanRepayment.ToString()
                        || item.ServiceTypeEnum == EnumServiceType.AcpLoanRepayment.ToString()
                        || item.ServiceTypeEnum == EnumServiceType.AdHocTopup.ToString()
                        || item.ServiceTypeEnum == EnumServiceType.PartialWithdraw.ToString()
                        || item.ServiceTypeEnum == EnumServiceType.PolicyLoan.ToString()
                        || item.ServiceTypeEnum == EnumServiceType.PolicyPaidUp.ToString()
                        || item.ServiceTypeEnum == EnumServiceType.PolicySurrender.ToString()
                        || item.ServiceTypeEnum == EnumServiceType.SumAssuredChange.ToString()
                        || item.ServiceTypeEnum == EnumServiceType.RefundOfPayment.ToString()
                        )
                    {
                        if (!string.IsNullOrEmpty(item.DocName) && item.DocName.Contains(".pdf"))
                        {
                            item.FormID = "POSFRM1";
                        }
                        else if (!string.IsNullOrEmpty(item.DocName) && !item.DocName.Contains(".pdf"))
                        {
                            item.FormID = "AIADOC1";
                        }
                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.PolicyHolderInformation.ToString()
                        || item.ServiceTypeEnum == EnumServiceType.InsuredPersonInformation.ToString())
                    {

                        //Not saving Pdf file name

                        //if (!string.IsNullOrEmpty(item.DocName) && item.DocName.Contains(".pdf"))
                        //{
                        //    item.FormID = "POSPPM1";
                        //}

                        item.FormID = "POSPPM1";
                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.PaymentFrequency.ToString())
                    {
                        //Not saving Pdf file name

                        //if (!string.IsNullOrEmpty(item.DocName) && item.DocName.Contains(".pdf"))
                        //{
                        //    item.FormID = "POSBLM1";
                        //}

                        item.FormID = "POSBLM1";
                    }
                    else if (item.ServiceTypeEnum == EnumServiceType.BeneficiaryInformation.ToString())
                    {
                        //Not saving Pdf file name
                        if (!string.IsNullOrEmpty(item.DocName) && item.DocName.Contains(".pdf"))
                        {
                            item.FormID = "POSBFM1";
                        }
                        else if (!string.IsNullOrEmpty(item.DocName) && !item.DocName.Contains(".pdf"))
                        {
                            var doc = unitOfWork.GetRepository<Entities.ServiceBeneficiaryPersonalInfo>()
                            .Query(x => x.ServiceBeneficiaryID == item.ServiceId)
                            .FirstOrDefault();

                            if (doc != null)
                            {
                                item.DocName = $"IdFrontImageName => {doc.IdFrontImageName}, IdBackImageName => {doc.IdBackImageName}";
                                item.Code = "";
                                item.Message = "";
                                item.FormID = "BFID1, BFID2";
                            }
                        }
                    }

                }

                var result = new PagedList<ServiceImagingLogResponse>(
                   source: response,
                   totalCount: count,
                   pageNumber: model.Page ?? 0,
                   pageSize: model.Size ?? 0);

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Servicing,
                        objectAction: EnumObjectAction.View);

                return errorCodeProvider.GetResponseModel<PagedList<ServiceImagingLogResponse>>(ErrorCode.E0, result);
            }
            catch (Exception ex)
            {
                CmsErrorLog("ImagingLog", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<PagedList<ServiceImagingLogResponse>>(ErrorCode.E500);
            }
        }


        ResponseModel<PagedList<ServiceImagingLogResponse>> IServicingRepository.ImagingLog(ServiceImagingLogRequest model)
        {
            try
            {

                var queryStrings = PrepareListQuery(model);

                var count = unitOfWork.GetRepository<GetCountByRawQuery>()
                    .FromSqlRaw(queryStrings.CountQuery, null, CommandType.Text)
                    .FirstOrDefault();

                var list = unitOfWork.GetRepository<ServiceImagingLogResponse>()
                    .FromSqlRaw(queryStrings.ListQuery, null, CommandType.Text)
                    .ToList();

                var result = new PagedList<ServiceImagingLogResponse>(
                   source: list,
                   totalCount: count.SelectCount,
                   pageNumber: model.Page ?? 0,
                   pageSize: model.Size ?? 0);

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Servicing,
                        objectAction: EnumObjectAction.View);

                return errorCodeProvider.GetResponseModel<PagedList<ServiceImagingLogResponse>>(ErrorCode.E0, result);
            }
            catch (Exception ex)
            {
                CmsErrorLog("ImagingLog", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<PagedList<ServiceImagingLogResponse>>(ErrorCode.E500);
            }
        }

        private aia_core.Repository.QueryStrings PrepareListQuery(ServiceImagingLogRequest model)
        {
            #region #CountQuery
            var countQuery = @"SELECT COUNT(ServiceMainDoc.ServiceId) AS SelectCount ";
            var asQuery = @"";
            #endregion

            #region #DataQuery
            var dataQuery = @"SELECT
                                ServiceMainDoc.MainId AS MainServiceId,
                                ServiceMainDoc.ServiceId AS ServiceId,
                                ServiceMainDoc.ServiceType AS ServiceTypeEnum,
                                ServiceType.ServiceTypeNameEn AS ServiceType,
                                ServiceMainDoc.CmsRequestOn AS TranDate,
                                ServiceMainDoc.DocType AS DocType,
                                ServiceMainDoc.NrcDocType AS NrcDocType,
                                ServiceMainDoc.DocName AS DocName,
                                ServiceMain.MemberID AS MemberId,
                                ServiceMain.PolicyNumber AS PolicyNumber,
                                ServiceMain.MobileNumber AS PhoneNumber,
                                ServiceMainDoc.UploadStatus AS Code, 
                                ServiceMainDoc.CmsResponse AS Message,
                                ServiceMainDoc.FormId AS FormID ";
            #endregion

            #region #FromQuery
            var fromQuery = @"FROM ServiceMainDoc
                                LEFT JOIN ServiceMain ON ServiceMain.ServiceID = ServiceMainDoc.ServiceId
                                LEFT JOIN ServiceType ON ServiceType.ServiceTypeEnum = ServiceMainDoc.ServiceType ";
            #endregion

            #region #GroupQuery

            var groupQuery = @"";


            var groupQueryForCount = @"";
            #endregion

            #region #OrderQuery
            var orderQuery = @"Order by ServiceMainDoc.CmsRequestOn DESC ";
            var orderQueryForCount = @" ";
            #endregion

            #region #FilterQuery

            var filterQuery = @"where 1 = 1 ";

            if (!string.IsNullOrEmpty(model.MainServiceId))
            {
                filterQuery += $@"AND ServiceMainDoc.MainId LIKE '%{model.MainServiceId}%' ";
            }
            if (!string.IsNullOrEmpty(model.ServiceId))
            {
                filterQuery += $@"AND ServiceMainDoc.ServiceId LIKE '%{model.ServiceId}%' ";
            }
            if (!string.IsNullOrEmpty(model.PolicyNo))
            {
                filterQuery += $@"AND ServiceMain.PolicyNumber LIKE '%{model.PolicyNo}%' ";
            }
            if (model.ServiceType != null)
            {
                var serviceTypeString = string.Join(",", model.ServiceType.Select(x => $"'{x.ToString()}'").ToList());
                filterQuery += $@"AND ServiceMainDoc.ServiceType IN ({serviceTypeString}) ";
            }
            if (!string.IsNullOrEmpty(model.FormID))
            {
                filterQuery += $@"AND ServiceMainDoc.FormId LIKE '%{model.FormID}%' ";
            }
            if (model.FromDate != null && model.ToDate != null)
            {
                filterQuery += $@"AND CONVERT(DATE, ServiceMainDoc.CmsRequestOn) >= '{model.FromDate.Value.ToString("yyyy-MM-dd")}' 
                    AND CONVERT(DATE, ServiceMainDoc.CmsRequestOn) <= '{model.ToDate.Value.ToString("yyyy-MM-dd")}' ";
            }
            #endregion

            #region #OffsetQuery

            #endregion


            var offsetQuery = $"OFFSET {(model.Page - 1) * model.Size} ROWS FETCH NEXT {model.Size} ROWS ONLY";


            countQuery = $"{countQuery}{fromQuery}{filterQuery}{groupQueryForCount}{asQuery}";
            var listQuery = $"{dataQuery}{fromQuery}{filterQuery}{groupQuery}{orderQuery}{offsetQuery}";

            var QueryString = new aia_core.Repository.QueryStrings { CountQuery = countQuery, ListQuery = listQuery };

            return QueryString;
        }

        ResponseModel<PagedList<ServicingListResponse>> IServicingRepository.List(ServicingListRequest model)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.ServiceMain>().Query();



                if (!string.IsNullOrEmpty(model.MemberName))
                {
                    query = query.Where(x => x.MemberName.Contains(model.MemberName));
                }

                if (!string.IsNullOrEmpty(model.MemberId))
                {
                    query = query.Where(x => x.MemberID.Contains(model.MemberId) || x.GroupMemberID.Contains(model.MemberId));
                }

                if (!string.IsNullOrEmpty(model.MemberPhone))
                {
                    query = query.Where(x => x.MobileNumber.Contains(model.MemberPhone));
                }

                if (!string.IsNullOrEmpty(model.PolicyNumber))
                {
                    query = query.Where(x => x.PolicyNumber.Contains(model.PolicyNumber));
                }

                if (!string.IsNullOrEmpty(model.PolicyStatus))
                {
                    query = query.Where(x => x.PolicyStatus == model.PolicyStatus);
                }

                if (!string.IsNullOrEmpty(model.MainId))
                {
                    query = query.Where(x => x.MainID.ToString() == model.MainId);
                }

                if (!string.IsNullOrEmpty(model.ServiceId))
                {
                    query = query.Where(x => x.ServiceID.ToString() == model.ServiceId);
                }

                if (model.ServiceType != null && model.ServiceType.Any())
                {
                    query = query.Where(x => model.ServiceType.Contains(x.ServiceType));
                }

                if (!string.IsNullOrEmpty(model.ServiceStatus))
                {
                    query = query.Where(x => x.ServiceStatus == model.ServiceStatus);
                }

                if (model.FromDate != null && model.ToDate != null)
                {
                    query = query.Where(x => x.CreatedDate.Value.Date >= model.FromDate.Value.Date && x.CreatedDate.Value.Date <= model.ToDate.Value.Date);
                }

                var count = query.Count();

                query = query.OrderByDescending(x => x.CreatedDate);


                var list = query.Skip((model.Page - 1) * model.Size).Take(model.Size).ToList()
                    .Select(x => new ServicingListResponse
                    { 
                        MainId = x.MainID,
                        ServiceId = x.ServiceID,
                        CreatedDate = x.CreatedDate,
                        AppMemberId = x.LoginMemberID,
                        MemberId = x.MemberID,
                        GroupMemberId = x.GroupMemberID,
                        MemberPhone = x.MobileNumber,                        
                        ServiceType = x.ServiceType,
                        ServiceStatus = x.ServiceStatus,
                        PolicyNumber = x.PolicyNumber,
                        PolicyStatus = x.PolicyStatus,
                        SubmissionDate = x.CreatedDate,
                        MemberType = x.MemberType,
                        StatusUpdatedBy = x.UpdatedBy?.ToString(), //TODO
                        IsPending = x.IsPending,
                        MemberName = x.MemberName,
                        StatusUpdatedDate = x.UpdatedOn,
                        UpdateChannel = x.UpdateChannel,
                    }
                    )
                    .ToList();

                list?.ForEach(item =>
                {
                    if ((item.IsPending != null && item.IsPending == true) && item.ServiceStatus == EnumServiceStatus.Received.ToString())
                    {
                        item.ServiceStatus = "Pending";
                    }

                    if(item.CreatedDate != null)
                    {
                        item.RemainingTime = GetProgressAndContactHour(item.CreatedDate.Value)?.Hours ?? "";
                    }
                    

                    var servicetype = unitOfWork.GetRepository<Entities.ServiceType>().Query(x => x.ServiceTypeEnum == item.ServiceType).FirstOrDefault();
                    item.ServiceName = servicetype?.ServiceTypeNameEn;
                   

                    var policy = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.PolicyNo == item.PolicyNumber)
                        .FirstOrDefault();                    

                    if (policy != null)
                    {
                        var policyStatus = unitOfWork.GetRepository<Entities.PolicyStatus>()
                        .Query(x => x.ShortDesc == policy.PolicyStatus)
                        .FirstOrDefault();

                        item.PolicyStatus = policyStatus?.LongDesc;
                    }

                    if (!string.IsNullOrEmpty(item.StatusUpdatedBy))
                    {
                        var updatedBy = unitOfWork.GetRepository<Entities.Staff>()
                        .Query(x => x.Id == new Guid(item.StatusUpdatedBy) && x.IsActive == true)
                        .Select(x => x.Name)
                        .FirstOrDefault();                        

                        item.StatusUpdatedBy = updatedBy;

                    }
                    else
                    {
                        item.StatusUpdatedBy = item.UpdateChannel;

                        if(item.UpdateChannel == "Job")
                        {
                            item.StatusUpdatedBy = "IL";
                        }
                    }
                    
                    
                });


                var result = new PagedList<ServicingListResponse>(
                   source: list,
                   totalCount: count,
                   pageNumber: model.Page,
                   pageSize: model.Size);

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Servicing,
                        objectAction: EnumObjectAction.View);

                return errorCodeProvider.GetResponseModel<PagedList<ServicingListResponse>>(ErrorCode.E0, result);

            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<ServicingListResponse>>(ErrorCode.E400);
            }
        }

        ResponseModel<string> IServicingRepository.UpdateServiceStatus(ServiceStatusUpdateRequest model)
        {
            try
            {

                var serviceMain = unitOfWork.GetRepository<Entities.ServiceMain>().Query(x => x.ServiceID == model.ServiceId).FirstOrDefault();

                if (serviceMain != null)
                {
                    if (model.Status != "Pending")
                    {

                        #region #UpdateStatustoEachServiceTable 
                        //29-07-2024 Monday Hotfix, resending noti IL update records to DB even after made status changed by CMS already Case!!

                        if (model.ServiceType == EnumServiceType.PolicyHolderInformation ||
                            model.ServiceType == EnumServiceType.InsuredPersonInformation)
                        {
                            ServicingRequest serviceChild = unitOfWork.GetRepository<ServicingRequest>().Query(x => x.ServicingID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdateChannel = "CMS";
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }
                        else if (model.ServiceType == EnumServiceType.BeneficiaryInformation)
                        {
                            ServiceBeneficiary serviceChild = unitOfWork.GetRepository<ServiceBeneficiary>().Query(x => x.ID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }
                        else if (model.ServiceType == EnumServiceType.LapseReinstatement)
                        {
                            ServiceLapseReinstatement serviceChild = unitOfWork.GetRepository<ServiceLapseReinstatement>().Query(x => x.ID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }
                        else if (model.ServiceType == EnumServiceType.HealthRenewal)
                        {
                            ServiceHealthRenewal serviceChild = unitOfWork.GetRepository<ServiceHealthRenewal>().Query(x => x.ID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }
                        else if (model.ServiceType == EnumServiceType.PolicyLoanRepayment)
                        {
                            ServicePolicyLoanRepayment serviceChild = unitOfWork.GetRepository<ServicePolicyLoanRepayment>().Query(x => x.ID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }
                        else if (model.ServiceType == EnumServiceType.AcpLoanRepayment)
                        {
                            ServiceACPLoanRepayment serviceChild = unitOfWork.GetRepository<ServiceACPLoanRepayment>().Query(x => x.ID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }
                        else if (model.ServiceType == EnumServiceType.AdHocTopup)
                        {
                            ServiceAdhocTopup serviceChild = unitOfWork.GetRepository<ServiceAdhocTopup>().Query(x => x.ID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }
                        else if (model.ServiceType == EnumServiceType.PartialWithdraw)
                        {
                            ServicePartialWithdraw serviceChild = unitOfWork.GetRepository<ServicePartialWithdraw>().Query(x => x.ID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }
                        else if (model.ServiceType == EnumServiceType.PolicyLoanRepayment)
                        {
                            ServicePolicyLoan serviceChild = unitOfWork.GetRepository<ServicePolicyLoan>().Query(x => x.ID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }
                        else if (model.ServiceType == EnumServiceType.PolicyPaidUp)
                        {
                            ServicePolicyPaidUp serviceChild = unitOfWork.GetRepository<ServicePolicyPaidUp>().Query(x => x.ID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }
                        else if (model.ServiceType == EnumServiceType.PolicySurrender)
                        {
                            ServicePolicySurrender serviceChild = unitOfWork.GetRepository<ServicePolicySurrender>().Query(x => x.ID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }
                        else if (model.ServiceType == EnumServiceType.PaymentFrequency)
                        {
                            ServicePaymentFrequency serviceChild = unitOfWork.GetRepository<ServicePaymentFrequency>().Query(x => x.ID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }
                        else if (model.ServiceType == EnumServiceType.SumAssuredChange)
                        {
                            ServiceSumAssuredChange serviceChild = unitOfWork.GetRepository<ServiceSumAssuredChange>().Query(x => x.ID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }
                        else if (model.ServiceType == EnumServiceType.RefundOfPayment)
                        {
                            ServiceRefundOfPayment serviceChild = unitOfWork.GetRepository<ServiceRefundOfPayment>().Query(x => x.ID == serviceMain.ServiceID).FirstOrDefault();
                            serviceChild.Status = model.Status;
                            serviceChild.UpdatedOn = Utils.GetDefaultDate();
                        }

                        unitOfWork.SaveChanges();

                        #endregion

                        serviceMain.IsPending = false;
                        serviceMain.ServiceStatus = model.Status;
                        serviceMain.InternalRemark = model.InternalRemark;
                        serviceMain.UpdatedOn = Utils.GetDefaultDate();
                        serviceMain.UpdatedBy = new Guid(GetCmsUser().ID);
                        serviceMain.UpdateChannel = "CMS";
                        unitOfWork.SaveChanges();


                        #region #Noti

                        var appMemberIdList = unitOfWork.GetRepository<Entities.MemberClient>()
                                        .Query(x => x.MemberId == serviceMain.LoginMemberID)
                                        .Select(x => x.MemberId)
                                        .ToList();

                        appMemberIdList = appMemberIdList?.Distinct().ToList();

                        if (appMemberIdList != null && appMemberIdList.Any())
                        {
                            foreach (var appMemberId in appMemberIdList)
                            {
                                try
                                {
                                    EnumServicingStatus? serviceStatus = null;

                                    serviceStatus = (EnumServicingStatus)Enum.Parse(typeof(EnumServicingStatus), model.Status);


                                    var notiMsg = notificationService.SendServicingNoti(appMemberId.Value, model.ServiceId.Value, serviceStatus.Value, model.ServiceType.Value, serviceMain.PolicyNumber);




                                }
                                catch (Exception ex)
                                {
                                    MobileErrorLog($"UpdateServiceStatus => Exc {model.ServiceId}", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                                }


                            }
                        }

                        var serviceStatusUpdate = unitOfWork.GetRepository<Entities.ServiceStatusUpdate>()
                            .Query(x => x.ServiceID == model.ServiceId)
                            .OrderByDescending(x => x.CreatedDate)
                            .FirstOrDefault();

                        if(serviceStatusUpdate != null)
                        {
                            serviceStatusUpdate.IsDone = true;
                            unitOfWork.SaveChanges();
                        }

                        #endregion

                    }
                    else
                    {
                        serviceMain.IsPending = true;
                        serviceMain.UpdatedOn = Utils.GetDefaultDate();
                        serviceMain.UpdatedBy = new Guid(GetCmsUser().ID);

                        
                        serviceMain.UpdateChannel = "CMS";

                        unitOfWork.SaveChanges();
                    }

                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0, "successfully updated");
                }
                else
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);
                }

            }
            catch (Exception ex)
            {
                CmsErrorLog("UpdateServiceStatus", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
        }

        ResponseModel<PagedList<ServicingListResponse>> IServicingRepository.Export(ServicingListRequest model)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.ServiceMain>().Query();



                if (!string.IsNullOrEmpty(model.MemberName))
                {
                    query = query.Where(x => x.MemberName.Contains(model.MemberName));
                }

                if (!string.IsNullOrEmpty(model.MemberId))
                {
                    query = query.Where(x => x.MemberID.Contains(model.MemberId) || x.GroupMemberID.Contains(model.MemberId));
                }

                if (!string.IsNullOrEmpty(model.MemberPhone))
                {
                    query = query.Where(x => x.MobileNumber.Contains(model.MemberPhone));
                }

                if (!string.IsNullOrEmpty(model.PolicyNumber))
                {
                    query = query.Where(x => x.PolicyNumber.Contains(model.PolicyNumber));
                }

                if (!string.IsNullOrEmpty(model.PolicyStatus))
                {
                    query = query.Where(x => x.PolicyStatus == model.PolicyStatus);
                }

                if (!string.IsNullOrEmpty(model.MainId))
                {
                    query = query.Where(x => x.MainID.ToString() == model.MainId);
                }

                if (!string.IsNullOrEmpty(model.ServiceId))
                {
                    query = query.Where(x => x.ServiceID.ToString() == model.ServiceId);
                }

                if (model.ServiceType != null && model.ServiceType.Any())
                {
                    query = query.Where(x => model.ServiceType.Contains(x.ServiceType));
                }

                if (!string.IsNullOrEmpty(model.ServiceStatus))
                {
                    query = query.Where(x => x.ServiceStatus == model.ServiceStatus);
                }

                if (model.FromDate != null && model.ToDate != null)
                {
                    query = query.Where(x => x.CreatedDate.Value.Date >= model.FromDate.Value.Date && x.CreatedDate.Value.Date <= model.ToDate.Value.Date);
                }

                var count = query.Count();

                query = query.OrderByDescending(x => x.CreatedDate);


                var list = query
                    .ToList()
                    .Select(x => new ServicingListResponse
                    {
                        MainId = x.MainID,
                        ServiceId = x.ServiceID,
                        CreatedDate = x.CreatedDate,
                        AppMemberId = x.LoginMemberID,
                        MemberId = x.MemberID,
                        GroupMemberId = x.GroupMemberID,
                        MemberPhone = x.MobileNumber,
                        ServiceType = x.ServiceType,
                        ServiceStatus = x.ServiceStatus,
                        PolicyNumber = x.PolicyNumber,
                        PolicyStatus = x.PolicyStatus,
                        SubmissionDate = x.CreatedDate,
                        MemberType = x.MemberType,
                        StatusUpdatedBy = x.UpdatedBy?.ToString(), //TODO
                        IsPending = x.IsPending,
                        MemberName = x.MemberName,
                        StatusUpdatedDate = x.UpdatedOn,
                        UpdateChannel = x.UpdateChannel,
                    }
                    )
                    .ToList();

                list?.ForEach(item =>
                {
                    if ((item.IsPending != null && item.IsPending == true) && item.ServiceStatus == EnumServiceStatus.Received.ToString())
                    {
                        item.ServiceStatus = "Pending";
                    }

                    //if (item.CreatedDate != null)
                    //{
                    //    item.RemainingTime = GetProgressAndContactHour(item.CreatedDate.Value)?.Hours ?? "";
                    //}


                    var servicetype = unitOfWork.GetRepository<Entities.ServiceType>().Query(x => x.ServiceTypeEnum == item.ServiceType).FirstOrDefault();
                    item.ServiceName = servicetype?.ServiceTypeNameEn;


                    var policy = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.PolicyNo == item.PolicyNumber)
                        .FirstOrDefault();

                    if (policy != null)
                    {
                        var policyStatus = unitOfWork.GetRepository<Entities.PolicyStatus>()
                        .Query(x => x.ShortDesc == policy.PolicyStatus)
                        .FirstOrDefault();

                        item.PolicyStatus = policyStatus?.LongDesc;
                    }

                    if (!string.IsNullOrEmpty(item.StatusUpdatedBy))
                    {
                        var updatedBy = unitOfWork.GetRepository<Entities.Staff>()
                        .Query(x => x.Id == new Guid(item.StatusUpdatedBy) && x.IsActive == true)
                        .Select(x => x.Name)
                        .FirstOrDefault();

                        item.StatusUpdatedBy = updatedBy;

                    }
                    else
                    {
                        item.StatusUpdatedBy = item.UpdateChannel;

                        if (item.UpdateChannel == "Job")
                        {
                            item.StatusUpdatedBy = "IL";
                        }
                    }


                });


                var result = new PagedList<ServicingListResponse>(
                   source: list,
                   totalCount: count,
                   pageNumber: model.Page,
                   pageSize: model.Size);

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Servicing,
                        objectAction: EnumObjectAction.Export);

                return errorCodeProvider.GetResponseModel<PagedList<ServicingListResponse>>(ErrorCode.E0, result);

            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<ServicingListResponse>>(ErrorCode.E400);
            }
        }
    }
}
