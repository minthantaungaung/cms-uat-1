using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Response.Dashboard
{
    public class DashboardChartResponse
    {
        public List<ChartResponseModel> response { get; set; }
        public int totalCount { get; set; }
        public string avgReceivedDecisionTime { get; set; }
        public string avgReceivedPaidTime { get; set; }
        public ClaimFailLog claimFailLog { get; set; }
    }

    public class ChartResponseModel
    {
        public string? name { get; set; }
        public string? type { get; set; }
        public int? totalCount {get;set;}
        public List<ChartResponseStatus> status { get; set; }
    }

    public class ChartResponseStatus
    {
        public string? type { get; set; }
        public string? name { get; set; }
        public int? count { get; set; }
    }

    public class ClaimFailLog
    {
        public int successCount { get; set; }
        public double successPercentage { get; set; }
        public int failCount { get; set; }
        public double failPercentage { get; set; }
    }

}