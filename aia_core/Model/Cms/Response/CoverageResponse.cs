using aia_core.Entities;

namespace aia_core.Model.Cms.Response
{
    public class CoverageResponse
    {
        public Guid CoverageId { get; set; }
        public string? CoverageNameEn { get; set; }
        public string? CoverageNameMm { get; set; }
        public string? CoverageIcon { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool? IsActive { get; set; }
        public ProductResponse[]? Products { get; set; }
        public CoverageResponse() { }
        public CoverageResponse(Entities.Coverage entity, Func<EnumFileType, string, string> blobUrl) 
        {
            CoverageId = entity.CoverageId;
            CoverageNameEn = entity.CoverageNameEn;
            CoverageNameMm = entity.CoverageNameMm;
            if(!string.IsNullOrEmpty(entity.CoverageIcon)) CoverageIcon = $"{blobUrl(EnumFileType.Coverage, entity.CoverageIcon)}";
            CreatedDate = entity.CreatedDate;
            UpdatedDate = entity.UpdatedDate;
            IsActive = entity.IsActive;
            if(entity.ProductCoverages?.Where(r=> r.Product != null).Any() == true)
            {
                Products = entity.ProductCoverages.Select(s => new ProductResponse
                {
                    ProductId = s.Product.ProductId,
                    ProductTypeId = s.Product.ProductTypeId,
                    ProductTypeShort = s.Product.ProductTypeShort,
                    TitleEn = s.Product.TitleEn,
                    TitleMm = s.Product.TitleMm,
                    ShortEn = s.Product.ShortEn,
                    ShortMm = s.Product.ShortMm,
                    IntroEn = s.Product.IntroEn,
                    IntroMm = s.Product.IntroMm,
                    TaglineEn = s.Product.TaglineEn,
                    TaglineMm = s.Product.TaglineMm,
                    PolicyTermUpToEn = s.Product.PolicyTermUpToMm,
                    PolicyTermUpToMm = s.Product.PolicyTermUpToMm,
                    IssuedAgeFrom = s.Product.IssuedAgeFrom,
                    IssuedAgeTo = s.Product.IssuedAgeTo,
                    CreditingLink = s.Product.CreditingLink,
                    WebsiteLink = s.Product.WebsiteLink,
                    Brochure = s.Product.Brochure,
                    CreatedDate = s.Product.CreatedDate,
                    UpdatedDate = s.Product.UpdatedDate,
                    IsActive = s.Product.IsActive,
                    LogoImage = !string.IsNullOrEmpty(s.Product.LogoImage) ? $"{blobUrl(EnumFileType.Product, s.Product.LogoImage)}" : null,
                    CoverImage = !string.IsNullOrEmpty(s.Product.CoverImage) ? $"{blobUrl(EnumFileType.Proposition, s.Product.CoverImage)}" : null
                }).ToArray();
            }
        }
    }
}
