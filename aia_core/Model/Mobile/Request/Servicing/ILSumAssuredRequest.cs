using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Routing.Patterns;

namespace aia_core.Model.Mobile.Request.Servicing
{

    public class ILSumAssuredRequest
    {
        public int newSumAssured { get; set; }
        public string policyNumber { get; set; }
        public string requestId { get; set; }
        public string incidentID { get; set; }
       
    }
}