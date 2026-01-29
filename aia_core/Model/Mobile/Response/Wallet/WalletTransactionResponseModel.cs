using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response.Wallet
{
    public class WalletTransactionResponseModel
    {
        public decimal Points { get; set; }
        public DateTime TransactionDate { get; set; }
        public string CampaignDescription { get; set; }
        public WalletTransactionType TransactionType { get; set; }
    }

    public enum WalletTransactionType
    {
        Credit,
        Debit,
    }
}
