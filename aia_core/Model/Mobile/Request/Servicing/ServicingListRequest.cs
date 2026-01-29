using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace aia_core.Model.Mobile.Request.Servicing
{
    

    public class ServicingListRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int? Page { get; set; }

        [Required]
        [Range(10, 100)]
        [DefaultValue(10)]
        public int? Size { get; set; }

        public EnumServiceType? ServiceType { get; set; }

        public string? ServiceStatus { get; set; }

        [JsonIgnore]
        public List<string>? ClientNoList { get; set; }

        [JsonIgnore]
        public Guid? MemberId { get; set; }
    }

    public class CheckPaymentFrequencyRequest
    {
        [Required]
        public string? paymentFrequencyCode { get; set; }

        [Required]
        public string? amount { get; set; }

        [Required]
        public string? policyNo { get; set; }
    }
}
