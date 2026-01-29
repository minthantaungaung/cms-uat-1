using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Services.AIA
{
    public class RewardTransactionResponseModel
    {
        public int code { get; set; }
        public string message { get; set; }
        public List<RewardsTransaction> result { get; set; }
    }

    public class RewardsTransaction
    {
        public string transactionId { get; set; }
        public string clientId { get; set; }
        public string transactionType { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public DateTime transactionDate { get; set; }
        public string requestedBy { get; set; }
        public string campaignDescription { get; set; }
        public string campaignCode { get; set; }
        public DateTime? effectiveDate { get; set; }
        public DateTime? expirationDate { get; set; }
    }

}
