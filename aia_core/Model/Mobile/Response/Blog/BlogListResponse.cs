using aia_core.UnitOfWork;

namespace aia_core.Model.Mobile.Response
{
    public class BlogListResponse
    {
        public List<BlogListFeatureModelResponse> features { get; set; }
        public PagaingResponseModel<BlogListModelResponse> list { get; set; }

    }

    public class BlogListFeatureModelResponse
    {
        public string ID { get; set; }
        public string Title_EN { get; set; }
        public string Title_MM { get; set; }
        public string Topic_EN { get; set; }
        public string Topic_MM { get; set; }
        public string CoverImage { get; set; }
        public string ThumbnailImage { get; set; }
        public string CategoryType { get; set; }
    }

    public class BlogListModelResponse 
    {
        public string ID { get; set; }
        public string Title_EN { get; set; }
        public string Title_MM { get; set; }
        public string Topic_EN { get; set; }
        public string Topic_MM { get; set; }
        public string CoverImage { get; set; }
        public string ThumbnailImage { get; set; }       
        public string ReadMin_EN { get; set; }
        public string ReadMin_MM { get; set; }
        public string CategoryType { get; set; }

        public DateTime? PromotionEnd { get; set; }
        public string? ShareableLink { get; set; }
    }
}
