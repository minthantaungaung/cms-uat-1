using aia_core.Services;

namespace aia_core.Model.Cms.Response
{
    public class BlogResponse
    {
        public Guid Id { get; set; }
        public string? TitleEn { get; set; }
        public string? TitleMm { get; set; }
        public string? CategoryType { get; set; }
        public string? CoverImage { get; set; }
        public string? ThumbnailImage { get; set; }
        public string? TopicEn { get; set; }
        public string? TopicMm { get; set; }
        public string? ReadMinEn { get; set; }
        public string? ReadMinMm { get; set; }
        public string? BodyEn { get; set; }
        public string? BodyMm { get; set; }
        public DateTime? PromotionStart { get; set; }
        public DateTime? PromotionEnd { get; set; }
        public bool? IsFeature { get; set; }
        public EnumBlogStatus? Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? Sort { get; set; }

        public Guid[]? Products { get; set; }

        public string? ShareableLink { get; set; }
        public BlogResponse() { }
        public BlogResponse(Entities.Blog entity, Func<EnumFileType, string, string> blobUrl) 
        {
            Id = entity.Id;
            TitleEn = entity.TitleEn;
            TitleMm = entity.TitleMm;
            CategoryType = entity.CategoryType;
            if (!string.IsNullOrEmpty(entity.CoverImage)) CoverImage = $"{blobUrl(EnumFileType.Blog, entity.CoverImage)}";
            if (!string.IsNullOrEmpty(entity.ThumbnailImage)) ThumbnailImage = $"{blobUrl(EnumFileType.Blog, entity.ThumbnailImage)}";
            TopicEn = entity.TopicEn;
            TopicMm = entity.TopicMm;
            ReadMinEn = entity.ReadMinEn;
            ReadMinMm = entity.ReadMinMm;
            BodyEn = entity.BodyEn;
            BodyMm = entity.BodyMm;
            PromotionStart = entity.PromotionStart;
            PromotionEnd = entity.PromotionEnd;
            IsFeature = entity.IsFeature;
            CreatedDate = entity.CreatedDate;
            UpdatedDate = entity.UpdatedDate;
            Sort = entity.Sort;
            ShareableLink = entity.ShareableLink;

            if(entity.CategoryType == $"{EnumCategoryType.promotion}")
            {
                if (entity.PromotionStart > Utils.GetDefaultDate()) Status = EnumBlogStatus.pending;
                else if (entity.PromotionEnd < Utils.GetDefaultDate()) Status = EnumBlogStatus.expired;
                else if (entity.PromotionStart <= Utils.GetDefaultDate() && entity.PromotionEnd >= Utils.GetDefaultDate()) Status = EnumBlogStatus.started;
                else Status = EnumBlogStatus.expired;
            }

            if (entity.PromotionProducts.Any())
            {
                Products = entity.PromotionProducts.Select(x => x.ProductId.Value).ToArray();
            }
            
        }
    }
}
