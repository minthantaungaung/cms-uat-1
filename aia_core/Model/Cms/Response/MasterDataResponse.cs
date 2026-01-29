namespace aia_core.Model.Cms.Response
{
    public class MasterDataResponse
    {
        public PropositionCategoryMasterData[]? PropositionCategories { get; set; }
        public CoverageMasterData[]? Coverages { get; set; }
        public ProductMasterData[]? Products { get; set; }
        public BlogMasterData? Blog { get; set; }
    }

    public class PropositionCategoryMasterData
    {
        public Guid? Id { get; set; }
        public string? NameEn { get; set; }
        public string? NameMm { get; set; }
        public string? IconImage { get; set; }
        public string? BackgroundImage { get; set; }
    }
    public class CoverageMasterData
    {
        public Guid CoverageId { get; set; }
        public string? CoverageNameEn { get; set; }
        public string? CoverageNameMm { get; set; }
        public string? CoverageIcon { get; set; }
    }
    public class ProductMasterData
    {
        public Guid? ProductId { get; set; }
        public string? TitleEn { get; set; }
        public string? TitleMm { get; set; }
        public string? LogoImage { get; set; }
    }

    public class BlogMasterData
    {
        public int? FeaturedCount { get; set; }
    }
}
