using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class ProductBenefitRequest
    {
        [Required]
        public string? TitleEn { get; set; }

        [Required]
        public string? TitleMm { get; set; }

        public string? DescriptionEn { get; set; }

        public string? DescriptionMm { get; set; }
    }
}
