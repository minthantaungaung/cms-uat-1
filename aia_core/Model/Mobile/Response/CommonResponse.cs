using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response.Common
{
    public class CommonStatus
    {
        public string? Status { get; set; }

        [JsonIgnore]
        public string? StatusCode { get; set; }
        public bool? IsCompleted { get; set; }

        [JsonIgnore]
        public bool? Remove { get; set; } = false;

        [JsonIgnore]
        public int? Sort { get; set; }

        [JsonIgnore]
        public DateTime? StatusChangedDt { get; set; }
    }

    public class CommonProgress
    {
        public int? Progress { get; set; }
        public string? ClaimContactHours { get; set; }
        public bool? IsTodayHoliday { get; set; }
    }
}
