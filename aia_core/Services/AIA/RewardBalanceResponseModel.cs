using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Services.AIA
{
    public class RewardBalanceResponseModel
    {
        public int code { get; set; }
        public string message { get; set; }
        public RewardBalance result { get; set; }
    }


    public class RewardBalance
    {
        public string clientId { get; set; }
        public decimal balance { get; set; }
        public DateTime lastUpdatedAt { get; set; }
    }
}
