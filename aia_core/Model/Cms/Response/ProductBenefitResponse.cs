using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Response
{
    public class ProductBenefitResponse
    {
        [Required]
        public string? TitleEn { get; set; }

        [Required]
        public string? TitleMm { get; set; }

        public string? DescriptionEn { get; set; }

        public string? DescriptionMm { get; set; }

        public ProductBenefitResponse() { }
        public ProductBenefitResponse(Entities.ProductBenefit entity)
        {
            TitleEn = entity.TitleEn;
            TitleMm = entity.TitleMm;
            DescriptionEn = entity.DescriptionEn;
            DescriptionMm = entity.DescriptionMm;
        }
    }
}
