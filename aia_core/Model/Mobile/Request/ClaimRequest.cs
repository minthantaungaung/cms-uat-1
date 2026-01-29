using aia_core.Model.Cms.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Request
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class BankDetail
    {
        public string? BankCode { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }


        public string? BankAccHolderIdValue { get; set; }

        public DateTime? BankAccHolderDob { get; set; }
        public bool? IsSave { get; set; }
    }

    public class BenefitList
    {
        public string? Name { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal? Amount { get; set; }
        public decimal? TotalCalculatedAmount { get; set; }
    }

    public class CausedBy
    {
        public string? ById { get; set; }
        public EnumCauseByType? ByType { get; set; }
        //public string? ByNameEn { get; set; }
        //public string? ByNameMm { get; set; }
        public DateTime? ByDate { get; set; }
    }

    public class ClaimantDetail
    {
        public string? Name { get; set; }
        public DateTime? Dob { get; set; }
        public string? Relationship { get; set; }
        public string? Gender { get; set; }
        public IdValue? IdValue { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public ClaimantAddress? Address { get; set; }
    }

    public class ClaimantAddress
    {
        public string? BuildingOrUnitNo { get; set; } // 1

        public string? StreetName { get; set; } // 2

        public string? TownshipName { get; set; } // 3 

        public string? DistrictName { get; set; } // 4

        public string? ProvinceOrCityName { get; set; } // 5

        public string? CountryCode { get; set; } // country

        public string? TownshipCode { get; set; } // townshipCode

    }

    public class ClaimDocument
    {
        public string? DocTypeId { get; set; }
        public string? DocTypeName { get; set; }
        public List<string>? DocIdList { get; set; }
    }

    public class IdValue
    {
        public EnumIdenType? Type { get; set; }
        public string? Value { get; set; }
    }

    public class ClaimOtp
    {
        [Required]
        public string? ReferenceNo { get; set; }

        [Required]
        public string? OtpCode { get; set; }      

    }

    public class ClaimNowRequest : ClaimValidationRequest
    {       
        public ClaimOtp? ClaimOtp { get; set; }

        [JsonIgnore]
        public bool IsSkipOtpValidation { get; set; } = false;
    }
    public class ClaimValidationRequest
    {
        

        [Required]
        public string? InsuredId { get; set; }

        [Required]
        public EnumBenefitFormType? BenefitFormType { get; set; }

        [Required]
        public string[]? ProductCodes { get; set; }
        //public List<string>? PolicyList { get; set; }

        [JsonIgnore]
        public string? ClaimType { get; set; }
        public List<BenefitList>? BenefitList { get; set; }
        public CausedBy? CausedBy { get; set; }
        public TreatmentDetail? TreatmentDetail { get; set; }
        public ClaimantDetail? ClaimantDetail { get; set; }
        public BankDetail? BankDetail { get; set; }
        public string? IncidentSummary { get; set; }
        public List<ClaimDocument>? ClaimDocuments { get; set; }

        public string? SignatureImage { get; set; }

        public bool? IsTesting { get; set; } = false;
    }

    public class TreatmentDetail
    {
        public string? DiagnosisId { get; set; }
        public string? PolicyNo { get; set; }

        //public string? DiagnosisNameEn { get; set; }
        //public string? DiagnosisNameMm { get; set; }
        public string? HospitalId { get; set; }
        //public string? HospitalNameEn { get; set; }
        //public string? HospitalNameMm { get; set; }
        public string? LocationId { get; set; }
        //public string? LocationNameEn { get; set; }
        //public string? LocationNameMm { get; set; }
        public string? DoctorName { get; set; }
        public int? TreatmentCount { get; set; }
        public List<DateTime>? TreatmentDates { get; set; }
        public DateTime? TreatmentFromDate { get; set; }
        public DateTime? TreatmentToDate { get; set; }
        public decimal? IncurredAmount { get; set; }
    }

    public class FollowupClaimRequest
    {
        public Guid? ClaimId { get; set; }
        public ClaimDocument FollowupDoc { get; set; }


        public string? RequiredInfo { get; set; }

    }

    public class BenefitSummaryRequest
    {
        [Required]
        public string InsuredId { get; set; }

        [Required]
        public string[] ProductCodes { get; set; }

        [Required]
        public EnumBenefitFormType FormType { get; set; }

        public string? PolicyNo { get; set; }

        public Guid? criticalIllnessId { get; set; }
    }

    public class ClaimStatusListRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int? Page { get; set; }

        [Required]
        [Range(10, 100)]
        [DefaultValue(10)]
        public int? Size { get; set; }

        public EnumBenefitFormType? ClaimType { get; set; }
        //public EnumClaimStatusDesc? ClaimStatus { get; set; }

        public string? ClaimStatus { get; set; }

        [JsonIgnore]
        public List<string>? HolderClientNoList { get; set; }

        public Guid? AppMemberId { get; set; }
    }
}
