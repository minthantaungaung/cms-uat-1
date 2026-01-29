using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Entities
{
    public class TxnsPayment
    {
        public string TransactionID { get; set; }
        public string PolicyNumber { get; set; }
        public string BankCode { get; set; }
        public string PaymentType { get; set; }
        public DateTime TxnsDate { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public string TransactionStatus { get; set; }
        public string PaymentChannel { get; set; }

        [ForeignKey(nameof(TransactionID))]
        public BufferTxnsPayment BufferTxnsPayment { get; set; }
    }
}
