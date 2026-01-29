using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response.Payment
{
    public class InitiatePaymentUrlResponseModel
    {
        public string? GeneratedPaymentUrl { get; set; }
        public string? RefPolicyNumber { get; set; }
        public string? RefOrderId { get; set; }
    }
}
