using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Request
{
    public class OtpBruteForceAttemptsRateLimitModel
    {
        public RateLimitOtpType RateLimitOtpType { get; set; }
        public string UserIdentifier { get; set; }
        public string OtpCode { get; set; }
        public bool IsSuccess { get; set; }
        public int MaxAttemptsPerUser { get; set; } = 3;
        public int IntervalInSecondsPerUser { get; set; } = 300; // 5 minutes
        public int MaxAttemptsPerGlobal { get; set; } = 1000;
        public int IntervalInSecondsPerGlobal { get; set; } = 60; // 1 minute
    }
}
