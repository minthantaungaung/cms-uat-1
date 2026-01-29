using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing.Patterns;

namespace aia_core.Model.Mobile.Request.Servicing
{
    public class PaymentFrequencyRequest
    {
      
        public ClaimOtp? ClaimOtp { get; set; }

        public string PolicyNo {get;set;}
        public string FrequencyType_Old { get; set; }
        public string FrequencyType_New { get; set; }
        public int Amount_Old { get; set; }
        public int Amount_New { get; set; }
        public string? Reason { get; set; }
        public string? SignatureImage { get; set; }
        public List<string> DocNameList { get; set; }

        [JsonIgnore]
        public bool IsSkipOtpValidation { get; set; } = false;
    }
}