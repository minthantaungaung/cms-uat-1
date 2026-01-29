using System.ComponentModel.DataAnnotations;
using aia_core.Services;

namespace aia_core.Model.Cms.Response.Dashboard
{
    public class DashboardCommonModel
    {
        public string? name { get; set; }
        public string? code {get;set;}
        public string? status { get; set; }
        public int? count {get;set;}
      
    }

    public class DashboardBClaimDecisionAvg
    {
        public decimal claimdecision { get; set; }
        public decimal paiddecision { get; set; }
    }
}