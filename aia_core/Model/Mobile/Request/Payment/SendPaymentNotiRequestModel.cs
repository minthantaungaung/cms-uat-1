using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Request.Payment
{
    public class SendPaymentNotiRequestModel
    {
        [JsonIgnore]
        public string? ApiKey { get; set; }

        [JsonIgnore]
        public string? Signature { get; set; }

        [JsonIgnore]
        public string? Timestamp { get; set; }

        [Required]
        public string? PolicyNo { get; set; }

        [Required]
        public string? Title { get; set; }

        [Required]
        public string? Message { get; set; }

        public string? OrderId { get; set; }
    }
}
