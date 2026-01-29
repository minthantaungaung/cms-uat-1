using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request.Dashboard
{
    public class DashboardChartRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public EnumChartType Type { get; set; }
    }

    public class DashboardChartTotalRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}