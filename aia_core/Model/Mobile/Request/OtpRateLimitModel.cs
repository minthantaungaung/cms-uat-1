using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Request
{
    public class OtpRateLimitModel
    {
        public RateLimitOtpType RateLimitOtpType { get; set; }
        public string UserIdentifier { get; set; }
        // Per user

        /// <summary>
        /// Limit count per user default is 5
        /// </summary>
        public int LimitCountPerUser { get; set; } = 3;

        /// <summary>
        /// Limit interval per user in seconds default is 30
        /// </summary>
        public int LimitIntervalPerUserInSeconds { get; set; } = 300; // 5 minutes

        // Per IP
        /// <summary>
        /// Limit count per IP default is 10
        /// </summary>
        public int LimitCountPerIp { get; set; } = 10;

        /// <summary>
        /// Limit interval per IP in seconds default is 10
        /// </summary>
        public int LimitIntervalPerIpInSeconds { get; set; } = 30;

        // Global
        /// <summary>
        /// Limit count global default is 1000
        /// </summary>
        public int LimitCountGlobal { get; set; } = 500;

        /// <summary>
        /// `Limit interval global in seconds default is 1
        /// </summary>
        public int LimitIntervalPerGlobalInSeconds { get; set; } = 60; // 1 minute

    }
}
