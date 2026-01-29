using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing.Patterns;

namespace aia_core.Model.Mobile.Request.Servicing
{
    public class ServiceBeneficiariesRequest
    {       
        public ClaimOtp? ClaimOtp { get; set; }
        public string? SignatureImage { get; set; }
        public List<NewBeneficiariesModel> NewBeneficiaries { get; set; }
        public List<ExistingBeneficiariesModel> ExistingBeneficiaries { get; set; }
        public List<ServiceBeneficiaryShareModel> ServiceBeneficiaryShare { get; set; }

        [JsonIgnore]
        public bool IsSkipOtpValidation { get; set; } = false;
    }

    public class NewBeneficiariesModel
    {
        public string Name { get; set; }
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string MobileNo {get;set;}
        public string IdType { get; set; }
        public string IdValue { get; set; }
        public string? IdFrontImageId { get; set; }
        public string? IdBackImageId {get;set;}
    }

    public class ExistingBeneficiariesModel
    {
        public string ClientNo { get; set; }
        public string Name { get; set; }
        public string OldMobileNumber { get; set; }
        public string NewMobileNumber { get; set; }
    }

    public class ServiceBeneficiaryShareModel
    {
        public string PolicyNo { get; set; }
        public List<ServiceBeneficiaryModel> ServiceBeneficiary { get; set; }
    }

    public class ServiceBeneficiaryModel
    {
        public string Name { get; set; }
        public string RelationShipCode { get; set; }
        public decimal? Percentage { get; set; }
        public string ClientNo { get; set; }
        public string IdValue { get; set; }
        public bool IsNewBeneficiary { get; set; }
    }
}