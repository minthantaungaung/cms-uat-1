using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.Model.Mobile.Response;
using aia_core.RecurringJobs;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Transactions;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Spreadsheet;
using Google.Apis.Discovery;
using DocumentFormat.OpenXml.Office2013.Word;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml.Vml.Office;
using System.Net.NetworkInformation;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.IdentityModel.Tokens;
using DocumentFormat.OpenXml.Drawing;
using System.Data;
using DocumentFormat.OpenXml.Bibliography;
using aia_core.Model.Mobile.Request;
using FastMember;
using Google.Api.Gax.ResourceNames;
using System.ComponentModel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;
using Irony.Parsing;

namespace aia_core.Repository.Cms
{
    public interface IClaimRepository
    {
        ResponseModel<PagedList<ClaimResponse>> List(aia_core.Model.Cms.Request.ClaimRequest model);
        ResponseModel<ClaimDetailResponse> Get(Guid? claimId);
        ResponseModel<PagedList<FailedLogResponse>> FailedLog(aia_core.Model.Cms.Request.FailedLogRequest model);
        ResponseModel<PagedList<ImagingLogResponse>> ImagingLog(aia_core.Model.Cms.Request.ImagingLogRequest model);

        ResponseModel<ImageLogDetail> ImagingLogDetail(string claimId, string uploadId);

        ResponseModel<List<ClaimStatusResp>> GetClaimStatus();

        ResponseModel<string> UpdateClaimStatus(ClaimStatusUpdateRequest model);

        ResponseModel<PagedList<CrmFailedLogResponse>> CrmFailedLog(aia_core.Model.Cms.Request.FailedLogRequest model);
        ResponseModel<List<ClaimResponse>> Export(DateTime? fromDate, DateTime? toDate);

        ResponseModel<PagedList<ClaimValidateMessageResponse>> GetClaimValidateMessageList(ClaimValidateMessageRequest model);


        ResponseModel<object> GetProductList();

        ResponseModel<List<ClaimResponse>> Export(aia_core.Model.Cms.Request.ClaimRequest model);
    }

    public class ClaimRepository: BaseRepository, IClaimRepository
    {
        private readonly INotificationService notificationService;

        public ClaimRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Context> unitOfWork, IRecurringJobRunner recurringJobRunner, INotificationService notificationService)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.notificationService = notificationService;
        }

        ResponseModel<PagedList<FailedLogResponse>> IClaimRepository.FailedLog(FailedLogRequest model)
        {
            try
            {

                var query = unitOfWork.GetRepository<Entities.ClaimTran>()
                    .Query()
                    //.Query(x => x.Ilstatus.ToLower() != "success")
                    .Include(x => x.Client)
                    .OrderByDescending(x => x.TransactionDate)
                    .AsQueryable()
                    ;

                #region #JoinClaim

                #endregion

                if (!string.IsNullOrEmpty(model.ClaimId))
                {
                    query = query.Where(x => x.ClaimId.ToString().Contains(model.ClaimId));
                }

                if (!string.IsNullOrEmpty(model.MainClaimId))
                {
                    query = query.Where(x => x.MainId.ToString().Contains(model.MainClaimId));
                }

                if (!string.IsNullOrEmpty(model.PolicyNo))
                {
                    query = query.Where(x => x.PolicyNo.Contains(model.PolicyNo));
                }

                if (model.ClaimType != null && model.ClaimType.Any())
                {
                    var claimTypeStr = new List<string>();
                    foreach (var claimType in model.ClaimType)
                    {
                        claimTypeStr.Add(claimType.ToString());
                    }
                    query = query.Where(x => claimTypeStr.Contains(x.ClaimFormType));
                }

                if (!string.IsNullOrEmpty(model.PhoneNo))
                { 
                    query = query.Where(x => x.MemberPhone.Contains(model.PhoneNo));
                }

                int totalCount = 0;
                totalCount = query.Count();

                var source = query.Skip(((model.Page ?? 0) - 1) * (model.Size ?? 0)).Take(model.Size ?? 0)
                    .Select(x => new FailedLogResponse(x))
                    .ToList();

                source.ForEach(tran => {
                   tran.ProductType = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == tran.PolicyNo)
                    .Select(x => x.ProductType)
                    .FirstOrDefault();
                });

                var data = new PagedList<FailedLogResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: model.Page ?? 0,
                    pageSize: model.Size ?? 0);

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Claim,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<FailedLogResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<FailedLogResponse>>(ErrorCode.E400);
            }
        }

        ResponseModel<ClaimDetailResponse> IClaimRepository.Get(Guid? claimId)
        {
            try
            {
                var data = new ClaimDetailResponse();

                var claimTran = unitOfWork.GetRepository<Entities.ClaimTran>()
                    .Query(x => x.ClaimId == claimId)
                    .FirstOrDefault();

                if (claimTran == null) return errorCodeProvider.GetResponseModel<ClaimDetailResponse>(ErrorCode.E400);

                var policy = unitOfWork.GetRepository<Entities.Policy>().Query(x => x.PolicyNo == claimTran.PolicyNo)
                    .Select(x => new { x.PolicyHolderClientNo, x.InsuredPersonClientNo, x.ProductType })
                    .FirstOrDefault();

                var owner = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == policy.PolicyHolderClientNo).FirstOrDefault();
                var insured = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == policy.InsuredPersonClientNo).FirstOrDefault();
                var product = unitOfWork.GetRepository<Entities.Product>()
                    .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
                var claimStatusUpdate = unitOfWork.GetRepository<Entities.ClaimsStatusUpdate>()
                    .Query(x => x.ClaimId == claimTran.ClaimId.ToString())
                    .OrderByDescending(x => x.CreatedDate)
                    .FirstOrDefault();


                var claiment = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == claimTran.HolderClientNo).FirstOrDefault();

                var claimBenefits = unitOfWork.GetRepository<Entities.ClaimBenefit>()
                    .Query(x => x.ClaimId == claimTran.ClaimId)
                    .ToList();

                var  claimBenefitsLst = new List<ClaimBenefit>();

                foreach (var claimBenefit in claimBenefits)
                {
                    claimBenefitsLst.Add(new ClaimBenefit
                    {
                        BenefitCode = claimBenefit.BenefitCode,
                    BenefitAmount = claimBenefit.BenefitAmount,
                    BenefitName =   claimBenefit.BenefitName,
                    BenefitFromDate = claimBenefit.BenefitFromDate,
                    BenefitToDate = claimBenefit.BenefitToDate,
                    TotalCalculatedAmount = claimBenefit.TotalCalculatedAmount ?? 0,
                    }
                    );
                }

                #region #ClaimHeader

                var claimHeader = new ClaimHeader();
                claimHeader.PolicyNo = claimTran.PolicyNo;
                claimHeader.ClaimType = claimTran.ClaimType;
                claimHeader.ClaimStatus = claimTran.ClaimStatus;
                claimHeader.ClaimStatusCode = claimTran.ClaimStatusCode;
                claimHeader.ClaimBy = claiment?.Name;
                claimHeader.ILRemark = "DummyILRemark";

                #region #TMA complaint on 24-05-2024
                var aiaClaimTblRecord  = unitOfWork.GetRepository<Entities.Claim>().Query(x => x.ClaimId == claimId.ToString()).FirstOrDefault();
                if (aiaClaimTblRecord != null && aiaClaimTblRecord.Status == "PN")
                {
                    claimHeader.ClaimStatus = "Pending";
                    claimHeader.ClaimStatusCode = aiaClaimTblRecord.Status;
                }
                #endregion

                //BT	Settled
                //EX	Ex-gratia
                //DC	Declined
                //RJ	Rejected

                var codeList = new string[] { "Settled", "Ex-gratia", "Declined", "Rejected" };

                if(codeList.Contains(claimTran.ClaimStatus))
                {
                    #region #Get ClaimStatus from AIA Claim Table First, If not exist claim, Select from ClaimTran, Ko MZ request
                    var aiaClaimTbl = unitOfWork.GetRepository<Entities.Claim>()
                        .Query(x => x.ClaimId == claimId.ToString())
                        .FirstOrDefault();

                    if (aiaClaimTbl != null)
                    {
                        var aiaClaimStatusTbl = unitOfWork.GetRepository<Entities.ClaimStatus>()
                            .Query(x => x.ShortDesc == aiaClaimTbl.Status)
                            .FirstOrDefault();

                        if (aiaClaimStatusTbl != null)
                        {
                            claimHeader.ClaimStatus = aiaClaimStatusTbl.LongDesc;
                            claimHeader.ClaimStatusCode = aiaClaimTbl.Status;
                        }
                    }
                    #endregion
                }





                //var ILRemark = unitOfWork.GetRepository<Entities.ClaimsStatusUpdate>().Query(x => x.ClaimId == claimId.ToString())
                //    .OrderByDescending(x => x.CreatedDate)
                //    .Select(x => x.RemarkFromIL)
                //    .FirstOrDefault();

                //claimHeader.ILRemark = ILRemark;

                #endregion

                #region #ClaimCommon
                var claimCommon = new ClaimCommon()
                { 
                
                };
                #endregion


                #region #FollowupData

                var followups = unitOfWork.GetRepository<Entities.ClaimFollowup>().Query(x => x.ClaimId == claimId)
                    .ToList();

                FollowupData? followupData = null;

                if (followups != null && followups.Any())
                {
                    followupData = new FollowupData
                    {
                        RequiredInfo = followups.OrderByDescending(x => x.CmsRequestOn).First().RequiredInfo,
                    };

                    followupData.AttachedFiles = new List<string>();
                    followupData.AttachedFiles2 = new List<Doc2>();
                    foreach (var followup in followups)
                    {
                        followupData.AttachedFiles.Add(GetFileFullUrl(EnumFileType.Bank, followup.DocName));
                        followupData.AttachedFiles2.Add(new Doc2 { Url = GetFileFullUrl(EnumFileType.Bank, followup.DocName), Name = followup.DocName2 });
                        
                    }
                }
                #endregion


                var eligibleAmount = unitOfWork.GetRepository<Entities.ClaimsStatusUpdate>()
                    .Query(x => x.ClaimId == claimId.ToString() && x.PayableAmountFromIL != null)
                    .OrderByDescending(x => x.CreatedDate)
                    .Select(x => x.PayableAmountFromIL)
                    .FirstOrDefault();

                #region #ClaimRequestDetail

                var ILRemark = unitOfWork.GetRepository<Entities.ClaimsStatusUpdate>().Query(x => x.ClaimId == claimId.ToString())
                    .OrderByDescending(x => x.CreatedDate)
                    .Select(x => x.RemarkFromIL)
                    .FirstOrDefault();

                #region #HideDiagnosisNameForSomeClaimType
                List<string> noDiagnosisList = new List<string>()
                {
                    EnumBenefitFormType.DentalCare.ToString(),
                    EnumBenefitFormType.MaternityCare.ToString(),
                    EnumBenefitFormType.PhysicalCheckup.ToString(),
                    EnumBenefitFormType.Vaccination.ToString(),
                    EnumBenefitFormType.VisionCare.ToString(),
                    EnumBenefitFormType.AcceleratedCancerBenefit.ToString(),
                };

                var _diagnosisName = "";
                if (claimTran.ClaimFormType == EnumBenefitFormType.CriticalIllnessBenefit.ToString() && claimTran.DiagnosisId != null)
                {
                    var criticalIllnessName = unitOfWork.GetRepository<Entities.CriticalIllness>()
                        .Query(x => x.ID == claimTran.DiagnosisId && x.IsDelete == false)
                        .Select(x => x.Name)
                        .FirstOrDefault();

                    _diagnosisName = criticalIllnessName;
                }
                else if (noDiagnosisList.Contains(claimTran.ClaimFormType) == false)
                {
                    _diagnosisName = !string.IsNullOrEmpty(claimTran.DiagnosisNameEn) ? claimTran.DiagnosisNameEn : claimTran.CausedByNameEn;
                }
                #endregion

                var claimDetail = new ClaimRequestDetail()
                {
                    #region #Bank
                    BankAcc = claimTran.BankAccountNumber,
                    BankAccName = claimTran.BankAccountName,
                    BankName = claimTran.BankNameEn,
                    BankCode = claimTran.BankCode,
                    EligibleAmount = eligibleAmount,
                    #endregion

                    ClaimType = claimTran.ClaimType,
                    PolicyNo = claimTran.PolicyNo,
                    ILErrMessage = claimTran.IlerrorMessage,
                    ILStatus = claimTran.Ilstatus,
                    ProductType = product?.TitleEn,
                    ClaimStatus = claimTran.ClaimStatus,
                    ClaimStatusCode = claimTran.ClaimStatusCode,
                    RemainingHour = GetProgressAndContactHour(claimTran.TransactionDate.Value)?.Hours ?? "",
                    Reason = claimStatusUpdate?.Reason,

                    ILRemark = ILRemark,

                #region #IncurredDetail
                IncurredDetail = new IncurredDetail()
                    {
                        Doctor = claimTran.DoctorName,
                        Hospital = claimTran.HospitalNameEn,
                        Location = claimTran.LocationNameEn,
                        Summary = claimTran.IncidentSummary,

                    },
                    #endregion

                    #region #ClaimSummary
                    ClaimSummary = new ClaimSummary()
                    {
                        ProductCode = policy?.ProductType,

                        ClaimFormType = (EnumBenefitFormType?)Enum.Parse(typeof(EnumBenefitFormType), claimTran.ClaimFormType),

                        Diagnosis = _diagnosisName,

                        CausedByType = claimTran.CausedByType,
                        CausedById = claimTran.CausedById,
                        CausedByDate = claimTran.CausedByDate,
                        CausedByCode = claimTran.CausedByCode,
                        CausedByNameEn = claimTran.CausedByNameEn,
                        CausedByNameMm = claimTran.CausedByNameMm,

                        HospitalVisits = claimTran.TreatmentCount,
                        InsuredPerson = insured?.Name,


                        TreatmentFromDate = claimTran.TreatmentFromDate,
                        TreatmentToDate = claimTran.TreatmentToDate,
                        IncurredAmount = claimTran.IncurredAmount,

                       
                        

                        ClaimBenefits = claimBenefitsLst,

                        #region #Common


                        ClaimForPolicyNo = claimTran.ClaimForPolicyNo,
                        ClaimantName = claimTran.ClaimantName,
                        ClaimantAddress = claimTran.ClaimantAddress,
                        ClaimantDob = claimTran.ClaimantDob,
                        ClaimantEmail = claimTran.ClaimantEmail,
                        ClaimantGender = claimTran.ClaimantGender,
                        ClaimantIdenType = claimTran.ClaimantIdenType,
                        ClaimantIdenValue = claimTran.ClaimantIdenValue,
                        ClaimantPhone = claimTran.ClaimantPhone,
                        ClaimantRelationship = claimTran.ClaimantRelationship,
                        ClaimantRelationshipMm = claimTran.ClaimantRelationshipMm,

                        
                        
                        #endregion
                    }

                    #endregion

                };

                


                if (!string.IsNullOrEmpty(claimTran.TreatmentDates))
                {

                    var treatmentDatesList = claimTran.TreatmentDates.Trim().Split(",");
                    claimDetail.ClaimSummary.TreatmentDates = treatmentDatesList.ToArray();

                    //////#region #ConvertToDatetime
                    //////try
                    //////{
                    //////    var dateStringList = claimTran.TreatmentDates.Trim().Split(",");
                    //////    var dateList = new List<DateTime>();
                    //////    foreach (var dateString in dateStringList) 
                    //////    {
                    //////        // Define the expected format
                    //////        string format = "yyyy-MM-ddTHH:mm:ss";

                    //////        // Use DateTime.ParseExact to convert the string to DateTime
                    //////        if (DateTime.TryParseExact(dateString, format, null, System.Globalization.DateTimeStyles.None, out DateTime result))
                    //////        {
                    //////            // Output the result with the desired format
                    //////            string resultString = result.ToString(format);
                    //////            Console.WriteLine($"Original String: {dateString}");
                    //////            Console.WriteLine($"Parsed DateTime: {resultString}");

                    //////            dateList.Add(result);
                    //////        }
                    //////        else
                    //////        {
                    //////            Console.WriteLine("Failed to parse the date string.");
                    //////        }
                    //////    }

                    //////    claimDetail.ClaimSummary.TreatmentDates = dateList.OrderBy(x => x).ToArray();
                    //////}
                    //////catch
                    //////{ }
                    //////#endregion


                }
                

                #region #Claim Docs
                var claimDocs = new List<ClaimDocs>();
                var docs = unitOfWork.GetRepository<Entities.ClaimDocument>()
                    .Query(x => x.ClaimId == claimTran.ClaimId && x.UploadStatus == "success"
                    ).ToList();

                foreach (var doc in docs)
                {
                    claimDocs.Add(new ClaimDocs {
                    TypeName = doc.DocTypeName,
                    DocName2 = doc.DocName2,
                    Doc = GetFileFullUrl(EnumFileType.Bank, doc.DocName),                    
                    });
                }

                var claimDocsGrp = claimDocs.GroupBy(x => x.TypeName).ToList();

                var claimDocList = new List<ClaimDocs>();
                foreach (var doc in claimDocsGrp)
                {
                    claimDocList.Add(new ClaimDocs {
                        TypeName = doc.First().TypeName,
                        Docs = doc.Select(x => x.Doc).ToArray(),

                        Docs2 = doc.Select(x => new Doc2 { Name = x.DocName2, Url = x.Doc }).ToList(),
                    });
                }

                claimDocList = claimDocList.OrderBy(x => x.TypeName).ToList();
                claimDetail.ClaimDocs = claimDocList.ToArray();
                claimDetail.ClaimDocPolicyHolderName = claiment?.Name;
                #endregion

                #region #Error Docs
                var errorDocs = new List<ImagingLogError>();
                var errDocs = unitOfWork.GetRepository<Entities.ClaimDocument>()
                    .Query(x => x.ClaimId == claimTran.ClaimId
                    ).ToList();

                foreach (var errDoc in errDocs)
                {
                    errorDocs.Add(new ImagingLogError
                    {
                        DocName = errDoc.DocName2,
                        message = errDoc.UploadStatus,
                    });
                }


                errorDocs = errorDocs.OrderBy(x => x.DocName).ToList();
                claimDetail.ImagingLogErrors = errorDocs.ToArray();
                #endregion

                #endregion

                #region #Owner
                var OwnerOccu = unitOfWork.GetRepository<Entities.Occupation>()
                    .Query(x => x.Code == owner.Occupation)
                    .FirstOrDefault()?.Description;

                var ownerNrc = string.IsNullOrEmpty(owner?.Nrc)
                            ? (string.IsNullOrEmpty(owner?.PassportNo)
                            ? (owner?.Other)
                            : owner?.PassportNo)
                            : owner?.Nrc;

                var ownerDetail = new PolicyOwnerDetail()
                {
                    ClientNo = owner?.ClientNo,
                    Name = owner?.Name,
                    Dob = owner?.Dob.ToString(),
                    Email = owner?.Email,
                    Phone = owner?.PhoneNo,
                    IdValue = ownerNrc,
                    Father = owner?.FatherName,
                    Married = owner?.MaritalStatus,
                    Gender = Utils.GetGender(owner?.Gender),
                    Occupation = OwnerOccu,
                    Address =
                                        owner?.Address1
                                        + ", " + owner?.Address2
                                        + ", " + owner?.Address3
                                        + ", " + owner?.Address4
                                        + ", " + owner?.Address5
                                        ,
                    
                };

                #endregion

                #region #Insured
                var insuredOccu = unitOfWork.GetRepository<Entities.Occupation>()
                    .Query(x => x.Code == insured.Occupation)
                    .FirstOrDefault()?.Description;

                var insuredNrc = string.IsNullOrEmpty(insured?.Nrc)
                            ? (string.IsNullOrEmpty(insured?.PassportNo)
                            ? (insured?.Other)
                            : insured?.PassportNo)
                            : insured?.Nrc;

                var insuredDetail = new InsuredPersonDetail()
                {
                    ClientNo = insured?.ClientNo,
                    Name = insured?.Name,
                    Dob = insured?.Dob.ToString(),
                    Email = insured?.Email,
                    Phone = insured?.PhoneNo,
                    IdValue = insuredNrc,
                    Father = insured?.FatherName,
                    Married = insured?.MaritalStatus,
                    Gender = Utils.GetGender(insured?.Gender),
                    Occupation = insuredOccu,
                    Address =
                                        insured?.Address1
                                        + ", " + insured?.Address2
                                        + ", " + insured?.Address3
                                        + ", " + insured?.Address4
                                        + ", " + insured?.Address5
                                        ,
                };

                data.InsuredPersonDetail = insuredDetail;
                data.ClaimRequestDetail = claimDetail;
                data.PolicyOwnerDetail = ownerDetail;
                data.ClaimHeader = claimHeader;
                data.ClaimCommon = claimCommon;
                data.HasFollowupData = followupData != null ? true : false;
                data.FollowupData = followupData;
                #endregion

                try
                {
                    #region #MedicalBillClaimProcess
                    var claimDocumentsMedicaBillApiLogs = unitOfWork.GetRepository<ClaimDocumentsMedicaBillApiLog>().Query(x => x.claimId == claimId.ToString()).ToList();

                    if (claimDocumentsMedicaBillApiLogs?.Any() == true)
                    {
                        data.MedicalBillClaimProcess = new List<MedicalBillClaimProcessResponse>();
                        claimDocumentsMedicaBillApiLogs?.ForEach(item =>
                        {
                            data.MedicalBillClaimProcess.Add(new MedicalBillClaimProcessResponse
                            {
                                Id = item.Id,
                                claimId = new Guid(item.claimId),
                                admissionDate = item.admissionDate,
                                billingDate = item.billingDate,
                                billType = item.billType,
                                dischargeDate = item.dischargeDate,
                                hospitalName = item.hospitalName,
                                doctorName = item.doctorName,
                                patientName = item.patientName,
                                netAmount = item.netAmount,
                                SentAt = item.SentAt,
                                ReceivedAt = item.ReceivedAt,
                                response = item.response,
                                fileName = item.fileName,
                                status = item.status,
                            });
                        });
                    }

                    #endregion
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Claim => Get => MedicalBillClaimProcess => Ex {claimId} => {ex.Message} => {JsonConvert.SerializeObject(ex)}");

                }


                return errorCodeProvider.GetResponseModel<ClaimDetailResponse>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Claim => Get => Ex {claimId} => {ex.Message} => {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel<ClaimDetailResponse>(ErrorCode.E400);
            }
        }

        ResponseModel<PagedList<ImagingLogResponse>> IClaimRepository.ImagingLog(ImagingLogRequest model)
        {
            try
            {
                model.QueryType = EnumQueryType.List;

                var queryStrings = PrepareImagingListQuery(model);


                var count = unitOfWork.GetRepository<ClaimCount>()
                    .FromSqlRaw(queryStrings.CountQuery, null, CommandType.Text)
                    .FirstOrDefault();

                var list = unitOfWork.GetRepository<ImagingLogResponse>()
                    .FromSqlRaw(queryStrings.ListQuery, null, CommandType.Text)
                    .ToList();

                var result = new PagedList<ImagingLogResponse>(
                   source: list,
                   totalCount: count?.SelectCount ?? 0,
                   pageNumber: model.Page ?? 0,
                   pageSize: model.Size ?? 0);

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Claim,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<ImagingLogResponse>>(ErrorCode.E0, result);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<ImagingLogResponse>>(ErrorCode.E400);
            }
        }

        ResponseModel<PagedList<ClaimResponse>> IClaimRepository.List(Model.Cms.Request.ClaimRequest model)
        {
            try
            {
                model.QueryType = EnumQueryType.List;
                var queryStrings = PrepareListQuery(model);

                var count = unitOfWork.GetRepository<ClaimCount>()
                    .FromSqlRaw(queryStrings?.CountQuery, null, CommandType.Text)
                    .FirstOrDefault();

                var list = unitOfWork.GetRepository<ClaimResponse>()
                    .FromSqlRaw(queryStrings?.ListQuery, null, CommandType.Text)                    
                    .ToList();

                //CmsErrorLog("IClaimRepository => CountQuery", $"{queryStrings?.CountQuery}", "", httpContext?.HttpContext.Request.Path);
                //CmsErrorLog("IClaimRepository => ListQuery", $"{queryStrings?.ListQuery}", "", httpContext?.HttpContext.Request.Path);


                List<string> noDiagnosisList = new List<string>()
                {
                    EnumBenefitFormType.DentalCare.ToString(),
                    EnumBenefitFormType.MaternityCare.ToString(),
                    EnumBenefitFormType.PhysicalCheckup.ToString(),
                    EnumBenefitFormType.Vaccination.ToString(),
                    EnumBenefitFormType.VisionCare.ToString(),
                    EnumBenefitFormType.AcceleratedCancerBenefit.ToString(),
                };

                foreach ( var claim in list )
                {
                    claim.RemainingHour = GetProgressAndContactHour(claim.TranDate.Value)?.Hours?? "";

                    if (claim.ClaimFormType == EnumBenefitFormType.CriticalIllnessBenefit.ToString() && claim.DiagnosisId != null)
                    {
                        var criticalIllnessName = unitOfWork.GetRepository<Entities.CriticalIllness>()
                            .Query(x => x.ID == claim.DiagnosisId && x.IsDelete == false)
                            .Select(x => x.Name)
                            .FirstOrDefault();

                        claim.DiagnosisName = criticalIllnessName;
                    }
                    else if(noDiagnosisList.Contains(claim.ClaimFormType) == false)
                    {
                        claim.DiagnosisName = !string.IsNullOrEmpty(claim.DiagnosisNameEn) ? claim.DiagnosisNameEn : claim.CausedByNameEn;
                    }
                }

                var result = new PagedList<ClaimResponse>(
                   source: list,
                   totalCount: count?.SelectCount ?? 0,
                   pageNumber: model.Page ?? 0,
                   pageSize: model.Size ?? 0);

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Claim,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<ClaimResponse>>(ErrorCode.E0, result);
              
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<ClaimResponse>>(ErrorCode.E400);
            }
        }

       



        private QueryStrings PrepareListQuery(Model.Cms.Request.ClaimRequest model)
        {
            #region #CountQuery
            var countQuery = @"SELECT COUNT(ClaimTran.ClaimId) AS SelectCount  ";
            var asQuery = @"";
            #endregion

            #region #DataQuery
            var dataQuery = @"SELECT
                                ClaimTran.MainId AS MainClaimId,
                                ClaimTran.ClaimId,
	                            ClaimTran.PolicyNo,
	                            ClaimTran.ClaimType,
								ClaimTran.IndividualMemberID as ClientNo,                               
								ClaimTran.MemberName As MemberName,                                
								ClaimTran.MemberPhone AS MemberPhone,
                                '' AS RemainingHour,
                                ClaimTran.ILStatus,
                                ClaimTran.UpdatedBy AS UpdatedBy,
                                ClaimTran.UpdatedOn AS UpdatedDt,
                                ClaimTran.TransactionDate AS TranDate,
                                ClaimTran.ProductNameEn as ProductType,
                                ClaimTran.ClaimStatus as ClaimStatus,
                                ClaimTran.ClaimStatusCode as ClaimStatusCode,
                                ClaimTran.MemberType as MemberType,
                                ClaimTran.GroupMemberID as GroupClientNo,
                                ClaimTran.CausedById as DiagnosisId,
                                ClaimTran.ClaimFormType as ClaimFormType,
                                ClaimTran.DiagnosisNameEn as DiagnosisNameEn,
                                ClaimTran.CausedByNameEn as CausedByNameEn ";
            #endregion

            #region #FromQuery
            var fromQuery = @"FROM ClaimTran ";
            #endregion

            #region #GroupQuery

            var groupQuery = @" ";


            var groupQueryForCount = @"";
            #endregion

            #region #OrderQuery
            var orderQuery = @"Order by ClaimTran.TransactionDate desc ";
            var orderQueryForCount = @" ";
            #endregion



            #region #FilterQuery

            var filterQuery = @"where 1 = 1 ";
            if (!string.IsNullOrEmpty(model.MemberName))
            {
                filterQuery += @"AND ClaimTran.MemberName like '%" + model.MemberName + "%' ";
            }

            if (!string.IsNullOrEmpty(model.ClientNo))
            {
                filterQuery += $@"AND (ClaimTran.IndividualMemberID  like '%" + model.ClientNo + "%' OR ClaimTran.GroupMemberID  like '%" + model.ClientNo + "%') ";
            }

            if (!string.IsNullOrEmpty(model.RequestId))
            {
                filterQuery += @"AND ClaimTran.MainId LIKE '%" + model.RequestId + "%' ";
            }

            if (!string.IsNullOrEmpty(model.DetailId))
            {
                filterQuery += @"AND ClaimTran.ClaimId LIKE '%" + model.DetailId + "%' ";
            }

            if (!string.IsNullOrEmpty(model.MemberPhone))
            {
                filterQuery += @"AND ClaimTran.MemberPhone LIKE '%" + model.MemberPhone + "%' ";
            }

            if (!string.IsNullOrEmpty(model.PolicyNo))
            {
                filterQuery += @"AND ClaimTran.PolicyNo LIKE '%" + model.PolicyNo + "%' ";
            }

            if (model.FromDate != null && model.ToDate != null)
            {
                filterQuery += $@"AND CONVERT(DATE, ClaimTran.TransactionDate) >= '{model.FromDate.Value.ToString("yyyy-MM-dd")}' AND CONVERT(DATE, ClaimTran.TransactionDate) <= '{model.ToDate.Value.ToString("yyyy-MM-dd")}' ";
            }

            //if (model.ClaimType != null && model.ClaimType.Any())
            //{
            //    filterQuery += "AND ClaimTran.ClaimFormType IN ('" + string.Join("', '", model.ClaimType) + "') ";
            //}


            if(model.QueryType == EnumQueryType.List)
            {
                if (model.ClaimType != null && !string.IsNullOrEmpty(model.ClaimType.ToString()))
                {

                    try
                    {
                        if (model.ClaimType.ToString().Contains("["))
                        {
                            string[] stringArray = JsonConvert.DeserializeObject<string[]>(model.ClaimType.ToString());

                            filterQuery += "AND ClaimTran.ClaimFormType IN ('" + string.Join("', '", stringArray) + "') ";
                        }
                        else
                        {
                            filterQuery += $"AND ClaimTran.ClaimFormType = '{model.ClaimType.ToString()}' ";
                        }

                    }
                    catch { }

                }
            }
            else if (model.QueryType == EnumQueryType.Export 
                && model.ClaimTypeList?.Any() ==true)
            {

                filterQuery += "AND ClaimTran.ClaimFormType IN ('" + string.Join("', '", model.ClaimTypeList) + "') ";

            }
            

            if (!string.IsNullOrEmpty(model.ClaimStatus))
            {
                filterQuery += "AND ClaimTran.ClaimStatus = '" + model.ClaimStatus + "' ";
            }

            if (model.ILStatus != null)
            {
                if (model.ILStatus == EnumILStatus.Success)
                {
                    filterQuery += "AND ClaimTran.ILStatus = 'success' ";
                }
                else
                {
                    filterQuery += "AND ClaimTran.ILStatus <> 'success' ";
                }
                
            }
            #endregion

            #region #OffsetQuery

            #endregion
            var offsetQuery = "";
            if (model.QueryType == EnumQueryType.List)
            {
                offsetQuery = $"OFFSET {(model.Page - 1) * model.Size} ROWS FETCH NEXT {model.Size} ROWS ONLY";
            }

            countQuery = $"{countQuery}{fromQuery}{filterQuery}{groupQueryForCount}{asQuery}";
            var listQuery = $"{dataQuery}{fromQuery}{filterQuery}{groupQuery}{orderQuery}{offsetQuery}";

            return new QueryStrings { CountQuery = countQuery, ListQuery = listQuery };
        }

        private QueryStrings PrepareImagingListQuery(ImagingLogRequest model)
        {
            #region #CountQuery
            var countQuery = @"select Count(ClaimDocuments.ClaimId) as SelectCount ";
            var asQuery = @" ";
            #endregion

            #region #DataQuery
            var dataQuery = @"select 
                                    ClaimDocuments.ClaimId,
                                    ClaimDocuments.MainClaimId,
                                    ClaimTran.PolicyNo,
                                    ClaimTran.ClaimType,
                                    policies.product_type as ProductType,
                                    ClaimTran.TransactionDate as TranDate,
                                    ClaimDocuments.DocTypeName,
                                    ClaimDocuments.DocName2 as DocName,
                                    ClaimDocuments.UploadId,
                                    ClaimDocuments.UploadStatus as Code,
                                    ClaimDocuments.CmsResponse as Message,  
                                    ClaimDocuments.DocTypeId as FormID ";
            #endregion

            #region #FromQuery
            var fromQuery = @"from
                                    ClaimDocuments
                                    left outer join ClaimTran on ClaimTran.ClaimId = ClaimDocuments.ClaimId
                                    left outer join policies on policies.policy_no = ClaimTran.PolicyNo
                                    left outer join Product on Product.Product_Type_Short = policies.product_type ";
            #endregion

            #region #GroupQuery

            var groupQuery = @" ";
            #endregion

            #region #OrderQuery
            var orderQuery = @"order by ClaimDocuments.CmsRequestOn desc ";
            #endregion

           

            #region #FilterQuery
            var filterQuery = @"where Product.Is_Delete = 0 ";

            if (!string.IsNullOrEmpty(model.MainClaimId))
            {
                filterQuery += @"AND ClaimDocuments.MainClaimId LIKE '%" + model.MainClaimId + "%' ";
            }

            if (!string.IsNullOrEmpty(model.ClaimId))
            {
                filterQuery += @"AND ClaimDocuments.ClaimId LIKE '%" + model.ClaimId + "%' ";
            }

            if (!string.IsNullOrEmpty(model.PolicyNo))
            {
                filterQuery += @"AND ClaimTran.PolicyNo LIKE '%" + model.PolicyNo + "%' ";
            }

            if (model.ClaimType != null && model.ClaimType.Any())
            {
                filterQuery += "AND ClaimTran.ClaimFormType IN ('" + string.Join("', '", model.ClaimType) + "') ";
            }

            if (!string.IsNullOrEmpty(model.FormID))
            {
                filterQuery += @"AND ClaimDocuments.DocTypeId LIKE '%" + model.FormID + "%' ";
            }

            if (model.FromDate != null && model.ToDate != null)
            {
                filterQuery += $@"AND CONVERT(DATE, ClaimDocuments.CmsRequestOn) >= '{model.FromDate.Value.ToString("yyyy-MM-dd")}' AND CONVERT(DATE, ClaimDocuments.CmsRequestOn) <= '{model.ToDate.Value.ToString("yyyy-MM-dd")}' ";
            }

            if (!string.IsNullOrEmpty(model.ResponseStatus))
            {
                if (model.ResponseStatus == "success")
                {
                    filterQuery += $@"AND ClaimDocuments.UploadStatus =  'success' ";
                }
                else
                {
                    filterQuery += $@"AND ClaimDocuments.UploadStatus <>  'success' ";
                }
            }

            if (!string.IsNullOrEmpty(model.ProductCode))
            {
                
                    filterQuery += $@"AND Product.Product_Type_Short = '{model.ProductCode}' ";
            }

            #endregion

            #region #OffsetQuery

            var offsetQuery = "";
            if (model.QueryType == EnumQueryType.List)
            {
                offsetQuery = $"OFFSET {(model.Page - 1) * model.Size} ROWS FETCH NEXT {model.Size} ROWS ONLY";
            }
            #endregion

            countQuery = $"{countQuery}{fromQuery}{filterQuery}{groupQuery}{asQuery}";
            var listQuery = $"{dataQuery}{fromQuery}{filterQuery}{groupQuery}{orderQuery}{offsetQuery}";

            return new QueryStrings { CountQuery = countQuery, ListQuery = listQuery };
        }

        ResponseModel<ImageLogDetail> IClaimRepository.ImagingLogDetail(string claimId, string uploadId)
        {
            try
            {

                var claim = unitOfWork.GetRepository<Entities.ClaimTran>()
                    .Query(x => x.ClaimId == new Guid(claimId))
                    .FirstOrDefault();

                var policy = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == claim.PolicyNo)
                    .FirstOrDefault();

                var holder = unitOfWork.GetRepository<Entities.Client>()
                    .Query(x => x.ClientNo == policy.PolicyHolderClientNo)
                    .FirstOrDefault();

                var insurred = unitOfWork.GetRepository<Entities.Client>()
                    .Query(x => x.ClientNo == policy.InsuredPersonClientNo)
                    .FirstOrDefault();

                var status = unitOfWork.GetRepository<Entities.PolicyStatus>()
                    .Query(x => x.ShortDesc == policy.PolicyStatus)
                    .FirstOrDefault();

                var product = unitOfWork.GetRepository<Entities.Product>()
                    .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false)
                    .FirstOrDefault();

                var clientNoList  = GetClientNoListByIdValue(claim.AppMemberId);

                var issuedDate = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => clientNoList.Contains(x.PolicyHolderClientNo))
                    .OrderBy(x => x.PolicyIssueDate ?? x.OriginalCommencementDate ?? x.CreatedDate)
                    .Select(x => x.PolicyIssueDate)
                    .FirstOrDefault();

                var doc = unitOfWork.GetRepository<Entities.ClaimDocument>()
                    .Query(x => x.ClaimId == new Guid(claimId) && x.UploadId == Guid.Parse(uploadId))
                    .FirstOrDefault();

                var policyDetail = new ImagePolicyDetail();
                policyDetail.ClaimId = claimId;
                policyDetail.InsurredId = insurred?.ClientNo;
                policyDetail.InsurredName = insurred?.Name;
                policyDetail.HolderName = holder?.Name;
                policyDetail.HolderId = claim?.IndividualMemberID;
                policyDetail.Components = policy?.Components;
                policyDetail.PolicyStatus = status?.LongDesc;
                policyDetail.ProductType = product?.TitleEn;
                policyDetail.CommenceDate = policy?.OriginalCommencementDate;
                policyDetail.SinceDate = issuedDate;
                policyDetail.PaymentFrequency = Utils.GetPaymentFrequency(policy?.PaymentFrequency);

                var requestData = new ImageRequestData();
                requestData.PhoneNo = holder?.PhoneNo;
                requestData.ClaimType = claim?.ClaimType;
                requestData.ClaimDate = claim?.TransactionDate;
                requestData.ErrorMessage = doc?.CmsResponse;                


                var result = new ImageLogDetail
                { 
                   ImagePolicyDetail = policyDetail,
                   ImageRequestData = requestData,
                };

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Claim,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<ImageLogDetail>(ErrorCode.E0, result);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<ImageLogDetail>(ErrorCode.E400);
            }
        }

        ResponseModel<List<ClaimStatusResp>> IClaimRepository.GetClaimStatus()
        {
            try
            {

                var resp = GetClaimStatusResps();


                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Claim,
                        objectAction: EnumObjectAction.Update);
                return errorCodeProvider.GetResponseModel<List<ClaimStatusResp>>(ErrorCode.E0, resp);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<ClaimStatusResp>>(ErrorCode.E400);
            }
        }

        private List<ClaimStatusResp> GetClaimStatusResps()
        {
            var resp = new List<ClaimStatusResp>()
                {
                    new ClaimStatusResp { Message = EnumClaimStatusDesc.Received.ToString(), Code = EnumClaimStatus.RC } ,
                    new ClaimStatusResp { Message = EnumClaimStatusDesc.Approved.ToString(), Code = EnumClaimStatus.AL},
                    new ClaimStatusResp { Message = GetEnumDescription(EnumClaimStatusDesc.Followedup), Code = EnumClaimStatus.FU},
                    new ClaimStatusResp { Message = EnumClaimStatusDesc.Paid.ToString(), Code = EnumClaimStatus.PD},
                    new ClaimStatusResp { Message = EnumClaimStatusDesc.Closed.ToString(), Code = EnumClaimStatus.CS},
                    new ClaimStatusResp { Message = EnumClaimStatusDesc.Withdrawn.ToString(), Code = EnumClaimStatus.WD},
                    new ClaimStatusResp { Message = EnumClaimStatusDesc.Rejected.ToString(), Code = EnumClaimStatus.RJ},
                };

           
                return resp;
        }

        ResponseModel<string> IClaimRepository.UpdateClaimStatus(ClaimStatusUpdateRequest model)
        {
            try
            {

                var resp = GetClaimStatusResps();

                

                string message = resp.Where(x => x.Code.ToString() == model.Status).FirstOrDefault().Message.ToString();

                ClaimsStatusUpdate data = new ClaimsStatusUpdate();
                data.Id = Guid.NewGuid();
                data.ClaimId = model.ClaimId.ToString();
                data.NewStatus = model.Status;
                data.CreatedDate = Utils.GetDefaultDate();
                data.IsDone = true;
                data.ChangedByAiaPlus = true;
                data.NewStatusDesc = message == "Followedup" ? "Followed-up" : message;
                data.NewStatusDescMm = message;
                data.Reason = model.Reason; // message == "Followedup" ? model.Reason : ""; 
                data.PayableAmountFromIL = model.EligibleAmount;


                unitOfWork.GetRepository<Entities.ClaimsStatusUpdate>().Add(data);


                var claimTran1 = unitOfWork.GetRepository<Entities.ClaimTran>()
                    .Query(x => x.ClaimId == model.ClaimId).FirstOrDefault();

                claimTran1.UpdatedBy = GetCmsUser()?.Name;

                claimTran1.ClaimStatusCode = model.Status; //TODO
                claimTran1.ClaimStatus = message == "Followedup" ? "Followed-up" : message ; //TODO

                claimTran1.UpdatedOn = Utils.GetDefaultDate();
                unitOfWork.SaveChanges();

                #region #Send noti
                try
                {

                    var productName = "";

                    var enumClaimStatus = ((EnumClaimStatus)(Enum.Parse(typeof(EnumClaimStatus), model.Status)));

                    var claimTran = unitOfWork.GetRepository<Entities.ClaimTran>()
                        .Query(x => x.ClaimId == model.ClaimId)
                        .FirstOrDefault();

                    if (claimTran != null)
                    {
                        var claimentNo = claimTran.HolderClientNo;

                        var idenValue = unitOfWork.GetRepository<Entities.Client>()
                            .Query(x => x.ClientNo == claimentNo)
                            .Select(x => new { x.Nrc, x.PassportNo, x.Other })
                            .FirstOrDefault();

                        if (idenValue != null)
                        {
                            var clientNoList = unitOfWork.GetRepository<Entities.Client>()
                            .Query(x =>
                            (!string.IsNullOrEmpty(x.Nrc) && (x.Nrc == idenValue.Nrc))
                            || (!string.IsNullOrEmpty(x.PassportNo) && x.PassportNo == idenValue.PassportNo)
                            || (!string.IsNullOrEmpty(x.Other) && x.Other == idenValue.Other))
                            .Select(x => x.ClientNo)
                            .ToList();


                            var policy = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => x.PolicyNo == claimTran.PolicyNo).FirstOrDefault();

                            if (policy != null)
                            {
                                var product = unitOfWork.GetRepository<Entities.Product>()
                                .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();

                                productName = claimTran.ClaimType;
                            }

                            var appMemberIdList = unitOfWork.GetRepository<Entities.MemberClient>()
                                .Query(x => clientNoList.Contains(x.ClientNo))
                                .Select(x => x.MemberId)
                                .ToList();

                            appMemberIdList = appMemberIdList.Distinct().ToList();

                            if (appMemberIdList != null && appMemberIdList.Any())
                            {
                                foreach (var appMember in appMemberIdList)
                                {
                                    CmsErrorLog("UpdateClaimStatus => Send noti "
                                        , $"appMember => {appMember.Value}, ClaimId => {model.ClaimId}, enumClaimStatus => {enumClaimStatus.ToString()}, productName => {productName}"
                                        , ""
                                        , httpContext?.HttpContext.Request.Path);

                                    try
                                    {
                                        var notiMsg = notificationService.SendClaimNoti(appMember.Value, model.ClaimId, enumClaimStatus, productName, model.Reason);

                                        try
                                        {
                                            data.FormattedReason = notiMsg ?? "";
                                            unitOfWork.GetRepository<Entities.ClaimsStatusUpdate>().Update(data);
                                            unitOfWork.SaveChanges();
                                        }
                                        catch { }
                                        

                                    }
                                    catch (Exception ex)
                                    {
                                        MobileErrorLog($"UpdateClaimStatus => Exc {model.ClaimId}", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                                    }


                                }
                            }


                        }



                    }


                }
                catch (Exception ex)
                {
                    CmsErrorLog("UpdateClaimStatus => Send noti ", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                }
                #endregion



                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);
            }
        }

        static string GetEnumDescription(EnumClaimStatusDesc value)
        {
            var field = value.GetType().GetField(value.ToString());

            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));

            return attribute == null ? value.ToString() : attribute.Description;
        }

        ResponseModel<PagedList<CrmFailedLogResponse>> IClaimRepository.CrmFailedLog(FailedLogRequest model)
        {
            try
            {

                var query = unitOfWork.GetRepository<Entities.ClaimTran>()
                    .Query()
                    .Include(x => x.Client)
                    .OrderByDescending(x => x.TransactionDate)
                    .AsQueryable()
                    ;

                #region #JoinClaim

                #endregion

                if (!string.IsNullOrEmpty(model.ClaimId))
                {
                    query = query.Where(x => x.ClaimId.ToString().Contains(model.ClaimId));
                }

                if (!string.IsNullOrEmpty(model.MainClaimId))
                {
                    query = query.Where(x => x.MainId.ToString().Contains(model.MainClaimId));
                }

                if (!string.IsNullOrEmpty(model.PolicyNo))
                {
                    query = query.Where(x => x.PolicyNo.Contains(model.PolicyNo));
                }

                if (model.ClaimType != null && model.ClaimType.Any())
                {
                    var claimTypeStr = new List<string>();
                    foreach (var claimType in model.ClaimType)
                    {
                        claimTypeStr.Add(claimType.ToString());
                    }
                    query = query.Where(x => claimTypeStr.Contains(x.ClaimFormType));
                }

                if (!string.IsNullOrEmpty(model.PhoneNo))
                {
                    query = query.Where(x => x.MemberPhone.Contains(model.PhoneNo));
                }

                int totalCount = 0;
                totalCount = query.Count();

                var source = query.Skip(((model.Page ?? 0) - 1) * (model.Size ?? 0)).Take(model.Size ?? 0)
                    .Select(x => new CrmFailedLogResponse(x))
                    .ToList();

                var data = new PagedList<CrmFailedLogResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: model.Page ?? 0,
                    pageSize: model.Size ?? 0);

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Claim,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<CrmFailedLogResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<CrmFailedLogResponse>>(ErrorCode.E400);
            }
        }

        ResponseModel<List<ClaimResponse>> IClaimRepository.Export(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var model = new Model.Cms.Request.ClaimRequest();
                model.QueryType = EnumQueryType.Export;
                model.FromDate = fromDate;
                model.ToDate = toDate;

                var queryStrings = PrepareListQuery(model);

                var list = unitOfWork.GetRepository<ClaimResponse>()
                    .FromSqlRaw(queryStrings?.ListQuery, null, CommandType.Text)
                    .ToList();

                CmsErrorLog("IClaimRepository => Export", $"{queryStrings?.ListQuery}", "", httpContext?.HttpContext.Request.Path);

                foreach (var claim in list)
                {
                    claim.RemainingHour = GetProgressAndContactHour(claim.TranDate.Value)?.Hours ?? "";

                    if (claim.ClaimFormType == EnumBenefitFormType.CriticalIllnessBenefit.ToString() && claim.DiagnosisId != null)
                    {
                        var criticalIllnessName = unitOfWork.GetRepository<Entities.CriticalIllness>()
                            .Query(x => x.ID == claim.DiagnosisId && x.IsDelete == false)
                            .Select(x => x.Name)
                            .FirstOrDefault();

                        claim.DiagnosisName = criticalIllnessName;
                    }
                }

                

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Claim,
                        objectAction: EnumObjectAction.Export);
                return errorCodeProvider.GetResponseModel<List<ClaimResponse>>(ErrorCode.E0, list);

            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<ClaimResponse>>(ErrorCode.E500);
            }
        }

        ResponseModel<PagedList<ClaimValidateMessageResponse>> IClaimRepository.GetClaimValidateMessageList(ClaimValidateMessageRequest model)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.ClaimValidateMessage>().Query();


                if (!string.IsNullOrEmpty(model.PolicyNumber))
                {
                    query = query.Where(x => x.PolicyNumber.Contains(model.PolicyNumber));
                }
                if(model.ClaimType?.Any() == true)
                {
                    query = query.Where(x => model.ClaimType.Contains(x.ClaimFormType));
                }
                if(!string.IsNullOrEmpty(model.MemberID))
                {
                    query = query.Where(x => x.MemberId.Contains(model.MemberID));
                }
                if (!string.IsNullOrEmpty(model.PhoneNo))
                {
                    query = query.Where(x => x.MemberPhone.Contains(model.PhoneNo));
                }
                if (!string.IsNullOrEmpty(model.MemberName))
                {
                    query = query.Where(x => x.MemberName.Contains(model.MemberName));
                }
                if (model.FromDate != null && model.ToDate != null)
                {
                    query = query.Where(x => x.Date >= model.FromDate && x.Date <= model.ToDate);
                }


                int totalCount = 0;
                totalCount = query.Count();

                var source = query.OrderByDescending(x => x.Date).Skip(((model.Page ?? 0) - 1) * (model.Size ?? 0)).Take(model.Size ?? 0)
                    .Select(x => new ClaimValidateMessageResponse
                    { 
                        ID = x.Id,
                        Date = x.Date,
                        PolicyNumber = x.PolicyNumber,
                        ClaimType = x.ClaimType,
                        MemberID = x.MemberId,
                        MemberName = x.MemberName,
                        MemberPhone = x.MemberPhone,
                        Message = x.Message,
                    }
                    )
                    .ToList();

                var data = new PagedList<ClaimValidateMessageResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: model.Page ?? 0,
                    pageSize: model.Size ?? 0);

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Claim,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<ClaimValidateMessageResponse>>(ErrorCode.E0, data);

            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<ClaimValidateMessageResponse>>(ErrorCode.E500);
            }
        }

        ResponseModel<object> IClaimRepository.GetProductList()
        {
            var productList = unitOfWork.GetRepository<Entities.Product>()
                .Query(x => x.IsActive == true && x.IsDelete == false)
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new { x.ProductTypeShort, x.TitleEn })
                .ToList();

            return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, productList);
        }

        ResponseModel<List<ClaimResponse>> IClaimRepository.Export(Model.Cms.Request.ClaimRequest model)
        {
            try
            {
                model.QueryType = EnumQueryType.Export;
                var queryStrings = PrepareListQuery(model);

                var count = unitOfWork.GetRepository<ClaimCount>()
                    .FromSqlRaw(queryStrings?.CountQuery, null, CommandType.Text)
                    .FirstOrDefault();


                var queryStartTime = Utils.GetDefaultDate();

                var list = unitOfWork.GetRepository<ClaimResponse>()
                    .FromSqlRaw(queryStrings?.ListQuery, null, CommandType.Text)
                    .ToList();

                Console.WriteLine($"ClaimExportExcuteQueryTime => {(Utils.GetDefaultDate() - queryStartTime).TotalMilliseconds}");

                queryStartTime = Utils.GetDefaultDate();

                //////foreach (var claim in list)
                //////{
                //////    claim.RemainingHour = GetProgressAndContactHour(claim.TranDate.Value)?.Hours ?? "";
                //////}
                ///

                

                Console.WriteLine($"ClaimExport72HourCalculateTime => {(Utils.GetDefaultDate() - queryStartTime).TotalMilliseconds}");

                List<string> noDiagnosisList = new List<string>()
                {
                    EnumBenefitFormType.DentalCare.ToString(),
                    EnumBenefitFormType.MaternityCare.ToString(),
                    EnumBenefitFormType.PhysicalCheckup.ToString(),
                    EnumBenefitFormType.Vaccination.ToString(),
                    EnumBenefitFormType.VisionCare.ToString(),
                    EnumBenefitFormType.AcceleratedCancerBenefit.ToString(),
                };

                list?.ForEach(item =>
                {
                    if (item.ClaimFormType == EnumBenefitFormType.CriticalIllnessBenefit.ToString() && item.DiagnosisId != null)
                    {
                        var criticalIllnessName = unitOfWork.GetRepository<Entities.CriticalIllness>()
                            .Query(x => x.ID == item.DiagnosisId && x.IsDelete == false)
                            .Select(x => x.Name)
                            .FirstOrDefault();

                        item.DiagnosisName = criticalIllnessName;
                    }
                    else if (noDiagnosisList.Contains(item.ClaimFormType) == false)
                    {
                        item.DiagnosisName = !string.IsNullOrEmpty(item.DiagnosisNameEn) ? item.DiagnosisNameEn : item.CausedByNameEn;
                    }
                });

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.Claim,
                        objectAction: EnumObjectAction.Export);
                return errorCodeProvider.GetResponseModel<List<ClaimResponse>>(ErrorCode.E0, list);

            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<ClaimResponse>>(ErrorCode.E400);
            }
        }
    }

    public class QueryStrings
    {
        public string? CountQuery { get; set; }
        public string? ListQuery { get; set; }
    }
}