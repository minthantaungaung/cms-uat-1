using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Entities
{
    public class Claim_Service_Otp_Setup
    {
        public string FormName { get; set; }
        public string FormType { get; set; }
        public bool IsOtpRequired { get; set; } = true;
    }

    public enum FormType
    {
        Claim,
        Service,
    }
}
