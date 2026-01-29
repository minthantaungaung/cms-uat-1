using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Routing.Patterns;

namespace aia_core.Model.Mobile.Request.Servicing
{

    public class ILPaymentRefundRequest
    {
        public int amount { get; set; }
        public string policyNumber { get; set; }
        public string requestId { get; set; }
        public string incidentID { get; set; }
        public string bankCode { get; set; }
        public string paymentType { get; set; }

    }
}