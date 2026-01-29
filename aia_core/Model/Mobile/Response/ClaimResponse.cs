using aia_core.Entities;
using aia_core.Model.Mobile.Response.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response
{
    public class InsuredPersonResponse
    {
        public string? InsuredName { get; set; }
        public string? InsuredId { get; set; }
        public string? InsuredNrc { get; set; }
        public string? InsuredImage { get; set; }

        [JsonIgnore]
        public string? InsuredNrcToLower { get; set; }
    }


    public class InsuranceTypeResponse
    {
        public Guid? InsuranceTypeId { get; set; }
        public string? InsuranceTypeEn { get; set; }
        public string? InsuranceTypeMm { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? InsuranceTypeImage { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public Guid? BenefitId { get; set; }

        public string? EligileBenefitNameListEn { get; set; }
        public string? EligileBenefitNameListMm { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? BenefitImage { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? BenefitFormType { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? ProductCode { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? PolicyNumber { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string[]? PolicyList { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? ClaimNameEn { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string[]? ClaimTypeList { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? Components { get; set; }

        public List<BenefitResponse>? Benefits { get; set; }
    }


    public class BenefitResponse
    {
        public string? InsuredId { get; set; }
        public Guid? BenefitId { get; set; }
        public string? BenefitNameEn { get; set; }
        public string? BenefitNameMm { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? BenefitImage { get; set; }
        public string[]? ProductCode { get; set; }

        public string[]? ComponentCodes { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string[]? PolicyList { get; set; }
        public EnumBenefitFormType? BenefitFormType { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? ClaimType { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string[]? ClaimTypeList { get; set; }

        public bool IsOtpRequired { get; set; } = true;
    }

    public class BenefitListResponse
    {
        public Guid? BenefitId { get; set; }
        public string? BenefitNameEn { get; set; }
        public string? BenefitNameMm { get; set; }
        public string? BenefitNameEnEnum { get; set; }
    }


    public class SampleDocumentsResponse
    {
        public string? DocumentTypeID { get; set; }
        public string? DocumentTypeName { get; set; }
        public string? DocTypeNameMm { get; set; }

        public string? TypeNameEn { get; set; }
        public string? TypeNameMm { get; set; }
        public string? TypeNameSampleEn { get; set; }
        public string? TypeNameSampleMm { get; set; }
        public string[]? DocumentList { get; set; }
    }

    public class ClaimNowResponse
    {
        public string? DocumentTypeID { get; set; }
        public string? DocumentTypeName { get; set; }
        public string[]? DocumentList { get; set; }
    }

    public class GetSaveBankResponse
    {
        public string? BankLogo { get; set; }
        public string? BankCode { get; set; }
        public string? AccountNo { get; set; }
        public string? AccountName { get; set; }
    }


    public class BenefitSummaryResponse
    {
        public string? InsurredId { get; set; }
        public string? InsurredNrc { get; set; }
        public string? InsuredName { get; set; }
        public string? IdValue { get; set; }
        public string? OwnerName { get; set; }
        public string? PolicyNumber { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public string? BenefitType { get; set; }
        public string? BenefitTypeMm { get; set; }

        public string? InsuranceType { get; set; }
        public string? InsuranceTypeMm { get; set; }
    }

    public class ClaimTranResp
    {

        public Guid ClaimId { get; set; }
        public string? ClaimCode { get; set; }
        public string? ClaimType { get; set; }
        public string? ClaimTypeMm { get; set; }
        public string? ClaimFormType { get; set; }
        public string? ClaimStatusCode { get; set; }
        public string? ClaimStatus { get; set; }
        public DateTime? TransactionDate { get; set; }

        public int? ProgressAsPercent { get; set; }
        public string? ProgressAsHours { get; set; }
    }

    public class ClaimListRsp
    {
        public string? ClaimId { get; set; }
        public string? ClaimType { get; set; }
        public string? ClaimTypeMm { get; set; }
        public string? ClaimTypeEnum { get; set; }
        public DateTime? ClaimDate { get; set; }
        public List<ClaimStatus>? ClaimStatusList { get; set; }
            = new List<ClaimStatus> {
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.RC.ToString(), //"RC",
                    Status = "Received",
                    IsCompleted = true,
                    Remove = false,
                    Sort = 1
                },
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.FU.ToString(), //"FU",
                    Status = "Followed-up",
                    IsCompleted = false,
                    Remove = true,
                    Sort = 2
                },
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.BT.ToString(), //"AL",
                    Status = "Approved",
                    IsCompleted = false,
                    Remove = false,
                    Sort = 3
                },
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.RJ.ToString(), //"RJ",
                    Status = "Rejected",
                    IsCompleted = false,
                    Remove = true,
                    Sort = 4
                },
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.WD.ToString(), //"WD",
                    Status = "Withdrawn",
                    IsCompleted = false,
                    Remove = true,
                    Sort = 5
                },
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.CS.ToString(), //"CS",
                    Status = "Closed",
                    IsCompleted = false,
                    Remove = true,
                    Sort = 6
                },
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.PD.ToString(), //"PD",
                    Status = "Paid",
                    IsCompleted = false,
                    Remove = false,
                    Sort = 7,
                    StatusChangedDt = Utils.GetDefaultDate().AddHours(1),
                },
            };

        public int? Progress { get; set; }
        public string? ClaimContactHours { get; set; }
    }

    public class ClaimStatus
    {
        public string? Status { get; set; }

        [JsonIgnore]
        public string? StatusCode { get; set; }
        public bool? IsCompleted { get; set; }

        [JsonIgnore]
        public bool? Remove { get; set; } = false;

        [JsonIgnore]
        public int? Sort { get; set; }

        [JsonIgnore]
        public DateTime? StatusChangedDt { get; set; }
    }

    public class ClaimStatusTmp
    {
        public string? NewStatus { get; set; }
        public string? NewStatusDesc { get; set; }
        public bool? IsCompleted { get; set; }
    }

    public class ClaimDetailRsp
    {
        public string? ClaimType { get; set; }
        public string? ClaimTypeMm { get; set; }

        public string? ClaimTypeEnum { get; set; }
        public string? ClaimStatus { get; set; }
        public ClaimProgress? ClaimProgress { get; set; }


        public ClaimReq? ClaimRequest { get; set; }
        public bool? IsHoliday { get; set; } = false;
        public string? Reason { get; set; }
        public string? RemarkFromIL { get; set; }
        public decimal? EligibleAmount { get; set; }

        public List<ClaimStatus>? ClaimStatusList { get; set; }
            = new List<ClaimStatus> {
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.RC.ToString(), //"RC",
                    Status = "Received",
                    IsCompleted = true,
                    Remove = false,
                    Sort = 1
                },
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.FU.ToString(), //"FU",
                    Status = "Followed-up",
                    IsCompleted = false,
                    Remove = true,
                    Sort = 2
                },
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.BT.ToString(), //"AL",
                    Status = "Approved",
                    IsCompleted = false,
                    Remove = false,
                    Sort = 3
                },
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.RJ.ToString(), //"RJ",
                    Status = "Rejected",
                    IsCompleted = false,
                    Remove = true,
                    Sort = 4
                },
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.WD.ToString(), //"WD",
                    Status = "Withdrawn",
                    IsCompleted = false,
                    Remove = true,
                    Sort = 5
                },
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.CS.ToString(), //"CS",
                    Status = "Closed",
                    IsCompleted = false,
                    Remove = true,
                    Sort = 6
                },
                new ClaimStatus
                {
                    StatusCode = EnumClaimStatus.PD.ToString(), //"PD",
                    Status = "Paid",
                    IsCompleted = false,
                    Remove = false,
                    Sort = 7,
                    StatusChangedDt = Utils.GetDefaultDate().AddHours(1),
                },
            };
    }

    public class ClaimProgress
    {
        public int? Progress { get; set; }
        public string? ClaimContactHours { get; set; }

    }

    public class ClaimReq
    {
        public string? InsurredId { get; set; }
        public string? InsurredName { get; set; }
        public string? InsurredNrc { get; set; }
        public string? PolicyNo { get; set; }
        public string? ProductEn { get; set; }
        public string? ProductMm { get; set; }
        public string? Owner { get; set; }
        public DateTime? ClaimDate { get; set; }
        public string? ClaimStatus { get; set; }
        public DateTime? BenefitSubmittedDate { get; set; }
        public decimal? BeneficiaryAmountPayable { get; set; }
    }


    public class BenefitListGrpByReasonCode
    {
        public string? ReasonCode { get; set; }

        public string? ComponentCode { get; set; }

        public List<Entities.InOutPatientReasonBenefitCode>? BenefitList { get; set; }
    }

    public class ValidationResult
    {
        public bool? IsValid { get; set; } = true;

        public List<string>? ValidationMessageList { get; set; }
    }

    //public class BenefitList
    //{
    //    public string? ProductCode { get; set; }

    //    public string? ComponentCode { get; set; }

    //    public string? ClaimType { get; set; }

    //    public string? ReasonCode { get; set; }

    //    public string? BenefitCode { get; set; }

    //    public string? BenefitName { get; set; }

    //    public bool? CheckBenefit { get; set; }

    //    public Guid? Id { get; set; }
    //}


    public class UploadDocResponseModel
    {
        public bool blur_warning { get; set; }
        public bool detection_passed { get; set; }
    }
}
