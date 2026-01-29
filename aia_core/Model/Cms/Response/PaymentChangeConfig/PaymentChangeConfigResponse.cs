using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Cms.Response.PaymentChangeConfig
{
    public class PaymentChangeConfigResponse
    {
        public Guid? Id { get; set; }
        public decimal? Value { get; set; }
        public string? DescEn { get; set; }
        public string? DescMm { get; set; }
        public string? Code { get; set; }
        public string? Type { get; set; }
        public bool? Status { get; set; }
    }
}
