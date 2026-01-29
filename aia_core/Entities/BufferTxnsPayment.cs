using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Entities
{
    public class BufferTxnsPayment
    {
        public string TransactionId { get; set; }
        public Guid UserId { get; set; }
        public string PremiumPolicyNo { get; set; }
        public string? ProductCode { get; set; }
        public decimal Amount { get; set; }
        public bool IsGenereatePaymentLinkSuccess { get; set; }
        public DateTime CreatedAt { get; set; }

        [InverseProperty("BufferTxnsPayment")]
        public TxnsPayment TxnsPayment { get; set; }
    }
}
