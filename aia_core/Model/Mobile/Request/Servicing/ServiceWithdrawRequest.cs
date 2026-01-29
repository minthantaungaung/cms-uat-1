using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing.Patterns;

namespace aia_core.Model.Mobile.Request.Servicing
{
    public class ServiceWithdrawRequest
    {
       
        public ClaimOtp? ClaimOtp { get; set; }

        public string PolicyNo {get;set;}
        public EnumCommonWithdrawServicingType ServiceType { get; set; }
        public int Amount { get; set; }
        public string? Reason { get; set; }
        public string? SignatureImage { get; set; }
        public ServiceRequestBankInfo? BankInfo { get; set; }
        public List<string> DocNameList { get; set; }
        [JsonIgnore]
        public bool IsSkipOtpValidation { get; set; } = false;
    }

    public class ServiceRequestBankInfo
    {
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountName { get; set; }
        public string? BankAccountNumber { get; set; }
    }
}