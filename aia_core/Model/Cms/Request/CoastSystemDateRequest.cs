using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class CoastSystemDateRequest
    {
        public bool? Coast_Claim_IsSystemDate { get; set; }
        public DateTime? Coast_Claim_CustomDate { get; set; }
        public bool? Coast_Servicing_IsSystemDate { get; set; }
        public DateTime? Coast_Servicing_CustomDate { get; set; }
    }
}