using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response.Payment
{
    public class GetPaymentHistory
    {
        public string? TransactionId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? ProductNameMM { get; set; }
        public string? PolicyNumber { get; set; }
        public DateTime? TransactionDate { get; set; }
        public decimal Amount { get; set; }        
        public string? ProductLogo { get; set; }
        public string? PaymentChannel { get; set; }
        public string? TransactionStatus { get; set; }
        public string? PaymentType { get; set; }
    }
}
