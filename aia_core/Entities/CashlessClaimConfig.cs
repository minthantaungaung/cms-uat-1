using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Entities
{
    public class CashlessClaimConfig
    {
        public Guid Id { get; set; }
        public string? LocalTitleEn { get; set; }
        public string? LocalTitleMm { get; set; }
        public string? LocalDescriptionEn { get; set; }
        public string? LocalDescriptionMm { get; set; }
        public string? LocalButtonTextEn { get; set; }
        public string? LocalButtonTextMm { get; set; }
        public string? LocalDeeplink { get; set; }

        public string? OverseasTitleEn { get; set; }
        public string? OverseasTitleMm { get; set; }
        public string? OverseasDescriptionEn { get; set; }
        public string? OverseasDescriptionMm { get; set; }
        public string? OverseasButtonTextEn { get; set; }
        public string? OverseasButtonTextMm { get; set; }
        public string? OverseasDeeplink { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
