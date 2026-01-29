using aia_core.Extension;
using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class ProductRequest
    {
        [Required]
        public string? TitleEn { get; set; }

        [Required]
        public string? TitleMm { get; set; }

        public string? ShortEn { get; set; }

        public string? ShortMm { get; set; }

        [Required, StringLength(50)]
        public string? ProductTypeShort { get; set; }

        [Required]
        public string? IntroEn { get; set; }

        [Required]
        public string? IntroMm { get; set; }

        [Required]
        public string? TaglineEn { get; set; }

        [Required]
        public string? TaglineMm { get; set; }

        [Required]
        public string? IssuedAgeFrom { get; set; }

        [Required]
        public string? IssuedAgeTo { get; set; }

        [Required]
        public string? IssuedAgeFromMm { get; set; }

        [Required]
        public string? IssuedAgeToMm { get; set; }

        [Required]
        public string? PolicyTermUpToEn { get; set; }

        [Required]
        public string? PolicyTermUpToMm { get; set; }

        public Guid[]? ProductCoverages { get; set; }

        public ProductBenefitRequest[]? ProductBenefits { get; set; }

        public string? WebsiteLink { get; set; }

        public string? Brochure { get; set; }

        public string? CreditingLink { get; set; }

        public bool? NotAllowedInProductList { get; set; }
    }

    public class CreateProductRequest : ProductRequest
    {
        [Required]
        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? LogoImage { get; set; }


        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? CoverImage { get; set; }
    }

    public class UpdateProductRequest: ProductRequest
    {
        [Required]
        public Guid? ProductId { get; set; }

        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? LogoImage { get; set; }

        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? CoverImage { get; set; }
    }
}
