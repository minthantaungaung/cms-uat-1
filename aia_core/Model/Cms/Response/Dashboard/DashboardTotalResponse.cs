using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Response.Dashboard
{
    public class DashboardTotalResponse
    {
        public int? totalRegister { get; set; }
        public int? totalActive { get; set; }
        public int? totalInactive { get; set; }
        public int? totalClaim { get; set; }
        public int? totalSuccessClaim { get; set; }
        public int? totalFailClaim {get;set;}
        public int? totalService {get;set;}
        public int? totalSuccessService {get;set;}
        public int? totalFailService {get;set;}
    }
}