using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Request.Payment
{
    public class InitiatePaymentUrlRequestModel
    {
        [Required]
        public string? PolicyNumber { get; set; }

        [Required]
        public decimal Amount { get; set; }
    }
}
