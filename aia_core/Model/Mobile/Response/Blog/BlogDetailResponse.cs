using aia_core.UnitOfWork;

namespace aia_core.Model.Mobile.Response
{
    public class BlogDetailResponse
    {
        public BlogDetailModel detail { get; set; }
        public List<BlogListModelResponse> others {get;set;}
        public List<ProductResponse> products {get;set;}
    }
    public class BlogDetailModel
    {
        public string ID { get; set; }
        public string Title_EN { get; set; }
        public string Title_MM { get; set; }
        public string Topic_EN { get; set; }
        public string Topic_MM { get; set; }
        public string Body_EN {get;set;}
        public string Body_MM { get; set; }
        public string CoverImage { get; set; }
        public string ThumbnailImage { get; set; }       
        public string ReadMin_EN { get; set; }
        public string ReadMin_MM { get; set; }
        public string CategoryType { get; set; }
        public DateTime? PromotionEnd { get; set; }
        public string? ShareableLink { get; set; }
    }
}
