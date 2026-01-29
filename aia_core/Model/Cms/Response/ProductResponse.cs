using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Response
{
    public class ProductResponse
    {
        public Guid ProductId { get; set; }
        public decimal? ProductTypeId { get; set; }
        public string? ProductTypeShort { get; set; }
        public string? TitleEn { get; set; }
        public string? TitleMm { get; set; }
        public string? ShortEn { get; set; }
        public string? ShortMm { get; set; }
        public string? LogoImage { get; set; }
        public string? CoverImage { get; set; }
        public string? IntroEn { get; set; }
        public string? IntroMm { get; set; }
        public string? TaglineEn { get; set; }
        public string? TaglineMm { get; set; }
        public string? IssuedAgeFrom { get; set; }
        public string? IssuedAgeTo { get; set; }
        public string? IssuedAgeFromMm { get; set; }
        public string? IssuedAgeToMm { get; set; }
        public string? PolicyTermUpToEn { get; set; }
        public string? PolicyTermUpToMm { get; set; }
        public string? WebsiteLink { get; set; }
        public string? Brochure { get; set; }
        public string? CreditingLink { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool? IsActive { get; set; }
        public CoverageResponse[]? ProductCoverages { get; set; }
        public ProductBenefitResponse[]? ProductBenefits { get; set; }

        public bool? NotAllowedInProductList { get; set; }
        public ProductResponse() { }
        public ProductResponse(Entities.Product entity, Func<EnumFileType, string, string> blobUrl) 
        {
            ProductId = entity.ProductId;
            ProductTypeId = entity.ProductTypeId;
            ProductTypeShort = entity.ProductTypeShort;
            TitleEn = entity.TitleEn;
            TitleMm = entity.TitleMm;
            ShortEn = entity.ShortEn;
            ShortMm = entity.ShortMm;

            Console.WriteLine($"ProductResponse Image LogoImage => {LogoImage} {blobUrl(EnumFileType.Product, entity.LogoImage)}");
            Console.WriteLine($"ProductResponse Image CoverImage => {CoverImage} {blobUrl(EnumFileType.Product, entity.CoverImage)}");

            if (!string.IsNullOrEmpty(entity.LogoImage)) LogoImage = $"{blobUrl(EnumFileType.Product, entity.LogoImage)}";
            if(!string.IsNullOrEmpty(entity.CoverImage)) CoverImage = $"{blobUrl(EnumFileType.Proposition, entity.CoverImage)}";
            IntroEn = entity.IntroEn;
            IntroMm = entity.IntroMm;
            TaglineEn = entity.TaglineEn;
            TaglineMm = entity.TaglineMm;
            PolicyTermUpToEn = entity.PolicyTermUpToEn;
            PolicyTermUpToMm = entity.PolicyTermUpToMm;
            IssuedAgeFrom = entity.IssuedAgeFrom;
            IssuedAgeTo = entity.IssuedAgeTo;
            IssuedAgeFromMm = entity.IssuedAgeFromMm;
            IssuedAgeToMm = entity.IssuedAgeToMm;
            CreditingLink = entity.CreditingLink;
            WebsiteLink = entity.WebsiteLink;
            Brochure = entity.Brochure;
            CreatedDate = entity.CreatedDate;
            UpdatedDate = entity.UpdatedDate;
            IsActive = entity.IsActive;
            if(entity.ProductCoverages != null)
            {
                ProductCoverages = entity.ProductCoverages.Where(r => r.Coverage != null)
                    .Select(s => new CoverageResponse
                    {
                        CoverageId = s.CoverageId ?? Guid.Empty,
                        CoverageNameEn = s.Coverage?.CoverageNameEn,
                        CoverageNameMm = s.Coverage?.CoverageNameMm,
                        CreatedDate = s.Coverage?.CreatedDate,
                        UpdatedDate = s.Coverage?.UpdatedDate,
                        IsActive = s.Coverage?.IsActive,
                        CoverageIcon = !string.IsNullOrEmpty(s.Coverage?.CoverageIcon) ? $"{blobUrl(EnumFileType.Coverage, s.Coverage?.CoverageIcon)}" : null,
                    }).ToArray();
            }
            if (entity.ProductBenefits != null)
            {
                ProductBenefits = entity.ProductBenefits.OrderBy(x => x.Sort).Select(s => new ProductBenefitResponse(s)).ToArray();
            }

            NotAllowedInProductList = entity.NotAllowedInProductList ?? false;
        }
    }
}
