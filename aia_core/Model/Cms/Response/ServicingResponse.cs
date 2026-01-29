using aia_core.Model.Mobile.Response;
using aia_core.Model.Mobile.Response.Common;
using aia_core.Model.Mobile.Response.MemberPolicyResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Servicing.Data.Response
{
    public class ServiceTypeResponse
    {
        public string? MainServiceTypeName { get; set; }
        public string? MainServiceTypeNameMm { get; set; }
        public EnumMainServiceType? MainServiceType { get; set; }

        [JsonIgnore]
        public int? MainSort { get; set; }
        public List<SubServiceType>? ServiceTypeList { get; set; }
    }

    public class SubServiceType
    {
        public string? ServiceTypeName { get; set; }
        public string? ServiceTypeNameMm { get; set; }
        public EnumServiceType? ServiceType { get; set; }

        public bool IsOtpRequired { get; set; } = true;

        [JsonIgnore]
        public int? MainSort { get; set; }

        [JsonIgnore]
        public string? MainServiceTypeName { get; set; }

        [JsonIgnore]
        public string? MainServiceTypeNameMm { get; set; }

        [JsonIgnore]
        public EnumMainServiceType? MainServiceType { get; set; }

        [JsonIgnore]
        public int? SubSort { get; set; }
    }


    public class ServiceTypeDataResponse
    {
        public string? MainServiceTypeNameEn { get; set; }
        public string? MainServiceTypeNameMm { get; set; }
        public string? MainServiceTypeEnum { get; set; }
        public string? ServiceTypeNameEn { get; set; }
        public string? ServiceTypeNameMm { get; set; }
        public string? ServiceTypeEnum { get; set; }
        [JsonIgnore]
        public int? MainSort { get; set; }
        [JsonIgnore]
        public int? SubSort { get; set; }
    }

    public class PolicyHolderResponse : PolicyHolder
    {
        public TextForUpdateAllProfiles? TextForUpdateAllProfiles { get; set; }
        public Guid? ServicingId { get; set; }
    }


    public class PolicyHolder
    {
        public string? ClientNo { get; set; }
        public string? Name { get; set; }
        public string? Occupation { get; set; }
        public string? Nrc { get; set; }
        public EnumServiceStatus? ServiceStatus { get; set; }
        public string? Gender { get; set; }
        public DateTime? Dob { get; set; }
        public string? MarriedStatus { get; set; }
        public string? FatherName { get; set; }
        public SeparateCountryCodeModel? Phone { get; set; }
        public string? Email { get; set; }
        public AddressInfo? addressInfo { get; set; }
    }
    public class InsuredPersonResponse
    {
        //public PolicyHolder? PolicyHolder { get; set; }
        public string? ClientNo { get; set; }
        public string? Name { get; set; }
        public string? Occupation { get; set; }
        public string? Nrc { get; set; }
        public EnumServiceStatus? ServiceStatus { get; set; }
        public string? Gender { get; set; }
        public DateTime? Dob { get; set; }
        public string? MarriedStatus { get; set; }
        public string? FatherName { get; set; }
        public SeparateCountryCodeModel? Phone { get; set; }
        public string? Email { get; set; }
        public AddressInfo? addressInfo { get; set; }
        public TextForUpdateAllProfiles? TextForUpdateAllProfiles { get; set; }
        public List<OwnershipPolicy>? Policies { get; set; }
        public Guid? ServicingId { get; set; }
    }

    public class TextForUpdateAllProfiles
    {
        public string? En { get; set; }
        public string? Mm { get; set; }
    }

    public class AddressInfo
    {
        public Country? Country { get; set; }
        public Province? Province { get; set; }
        public District? District { get; set; }
        public Township? Township { get; set; }
        public string? Street { get; set; }
        public string? BuildingOrUnitNo { get; set; }
    }


    public class Country
    {
        public string? id { get; set; }
        public string? code { get; set; }
        public string? description { get; set; }
        public string? bur_description { get; set; }
    }

    public class Province
    {
        public string? country_code { get; set; }
        public string? province_code { get; set; }
        public string? province_eng_name { get; set; }
        public string? province_bur_name { get; set; }
    }

    public class District
    {
        public string? province_code { get; set; }
        public string? district_code { get; set; }
        public string? district_eng_name { get; set; }
        public string? district_bur_name { get; set; }
    }

    public class Township
    {
        public string? district_code { get; set; }
        public string? township_code { get; set; }
        public string? township_eng_name { get; set; }
        public string? township_bur_name { get; set; }
    }

    public class ServiceListResponse
    {
        public Guid? ServiceId { get; set; }
        public string? ServiceType { get; set; }
        public string? ServiceTypeNameEn { get; set; }
        public string? ServiceTypeNameMm { get; set; }
        public DateTime? TransactionDate { get; set; }
        public List<CommonStatus>? ServiceStatusList { get; set; }
            = new List<CommonStatus> {
                new CommonStatus { Status = "Received", IsCompleted = true, Sort = 1 , Remove = false },
                new CommonStatus { Status = "Approved", IsCompleted = false, Sort = 2 , Remove = false },
                new CommonStatus { Status = "NotApproved", IsCompleted = false, Sort = 3, Remove = true },
                new CommonStatus { Status = "Paid", IsCompleted = false, Sort = 4, Remove = false }
            };

        [JsonIgnore]
        public string? Status { get; set; }
    }

    public class ServiceRequestDetailResponse
    {
        public Guid? ServiceId { get; set; }
        public string? ServiceType { get; set; }
        public string? ServiceTypeNameEn { get; set; }
        public string? ServiceTypeNameMm { get; set; }
        public DateTime? TransactionDate { get; set; }
        public List<CommonStatus>? ServiceStatusList { get; set; }
            = new List<CommonStatus> {
                new CommonStatus { Status = "Received", IsCompleted = true, Sort = 1 },
                new CommonStatus { Status = "Approved", IsCompleted = false, Sort = 2 , Remove = false },
                new CommonStatus { Status = "NotApproved", IsCompleted = false, Sort = 3, Remove = true },
                new CommonStatus { Status = "Paid", IsCompleted = false, Sort = 4, Remove = false }
            };

        //[JsonIgnore]
        public string? Status { get; set; }
        public CommonProgress? Progress { get; set; }

        public ServiceRequestDetail? ServiceRequestDetail { get; set; }

        public string? InternalRemark { get; set; }
    }

    public class ServiceRequestDetail
    {
        public OldNewDetail? OldNewDetail { get; set; }
        public AmountDetail? AmountDetail { get; set; }
        public BankDetail? BankDetail { get; set; }
        public ChangedFrequency? ChangedFrequency { get; set; }
        public ServiceBeneficiaryResponseModel? Beneficiary {get;set;}
    }

    public class OldNewDetail
    {
        public ChangeValue? MarriedStatus { get; set; }
        public ChangeValue? FatherName { get; set; }
        public ChangeValue? Phone { get; set; }
        public ChangeValue? Email { get; set; }
        public ChangeValue? Country { get; set; }
        public ChangeValue? Province { get; set; }
        public ChangeValue? Distinct { get; set; }
        public ChangeValue? Township { get; set; }
        public ChangeValue? Building { get; set; }
        public ChangeValue? Street { get; set; }
    }

    public class AmountDetail
    {
        public double? amount { get; set; }
        public string? reason { get; set; }
    }

    public class BankDetail
    {
        public string? BankCode { get; set; }
        public string? OriginalBankLogo { get; set; }
        public string? BankLogo { get; set; }
        public string? BankName { get; set; }
        public string? AccountName { get; set; }
        public string? AccountNumber { get; set; }
    }

    public class ChangeValue
    {
        public string? Old { get; set; }
        public string? New { get; set; }
    }

    public class ChangedFrequency
    {
        public ChangeValue? Frequency { get; set; }
        public ChangeValue? Amount { get; set; }
    }

    public class ServiceBeneficiaryResponseModel
    {
        public List<ServiceBeneficiaryNewModelResponse> newBeneficiaries {get;set;}
        public List<ServiceBeneficiaryExistingModelResponse> existingBeneficiaries {get;set;}
        public List<ServiceBeneficiaryPolicyModelResponse> policy {get;set;}
    }

    public class ServiceBeneficiaryNewModelResponse
    {
        public string? Name { get; set; }
        public string? Gender { get; set; }  
        public DateTime? Dob { get; set; }
        public string? MobileNo { get; set; }
        public string? IdType { get; set; }
        public string? IdValue { get; set; }
        public string? IdFrontImage { get; set; }
        public string? IdBackImage { get; set; }
    }

    public class ServiceBeneficiaryExistingModelResponse
    {
        public string? Name { get; set; }
        public string? Gender { get; set; }
        public DateTime? Dob { get; set; }
        public string? OldMobileNo { get; set; }
        public string? NewMobileNo { get; set; }
        public string? IdType { get; set; }
        public string? IdValue { get; set; }
    }

    public class ServiceBeneficiaryPolicyModelResponse
    {
        public string PolicyName {get;set;}
        public string PolicyNameMm { get; set; }
        public string PolicyNo { get; set; }
        public List<ServbiceBeneficiaryPolicyShareModelResponse> beneficiaries {get;set;}
    }

    public class ServbiceBeneficiaryPolicyShareModelResponse
    {
        public string? Name { get; set; }
        public string? Relationship { get; set; }
        public decimal? BeneficiaryShare { get; set; }
        public bool IsNew { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
