using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Cms.Request.PaymentChangeConfig
{
    public class PaymentChangeConfigRequest
    {
        [Required]
        public decimal? Value { get; set; }

        [Required]
        public string? DescEn { get; set; }

        [Required]
        public string? DescMm { get; set; }

        [Required]
        public string? Code { get; set; }

        [Required]
        public EnumPaymentChangeConfigValidationType? Type { get; set; }

        public bool? Status { get; set; }
    }

    public class PaymentChangeConfigUpdateRequest : PaymentChangeConfigRequest
    {

        [Required]
        public Guid? Id { get; set; }
    }
}