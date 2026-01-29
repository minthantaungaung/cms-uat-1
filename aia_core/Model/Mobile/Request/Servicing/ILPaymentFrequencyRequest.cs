using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Routing.Patterns;

namespace aia_core.Model.Mobile.Request.Servicing
{

    public class ILPaymentFrequencyRequest
    {
        public string frequency { get; set; }
        public string policyNumber { get; set; }
        public string requestType { get; set; }
        public string requestId { get; set; }
       
    }
}