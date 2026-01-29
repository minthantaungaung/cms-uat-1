using System.Linq.Expressions;
using System.Reflection;
using aia_core.Entities;
using aia_core.Model.Mobile.Request.Blog;
using aia_core.Model.Mobile.Response;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json;

namespace aia_core.Repository.Mobile
{
    public interface IBlogRepository
    {
        ResponseModel<BlogListResponse> GetList(BlogListRequest model);
        ResponseModel<BlogDetailResponse> GetDetail(string id);
    }
    public class BlogRepository : BaseRepository, IBlogRepository
    {
        #region "const"
        private readonly ICommonRepository commonRepository;

        public BlogRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork,
            ICommonRepository commonRepository)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.commonRepository = commonRepository;
        }
        #endregion

        #region "Get List"
        public ResponseModel<BlogListResponse> GetList(BlogListRequest model)
        {
            try
            {
                BlogListResponse blogResponse = new BlogListResponse();

                var featuresData = unitOfWork.GetRepository<Blog>()
                .Query(x => x.IsActive == true && x.IsDelete == false && x.IsFeature == true
                && (x.CategoryType == EnumCategoryType.activity.ToString() || (x.CategoryType == EnumCategoryType.promotion.ToString() && (x.PromotionStart.Value < Utils.GetDefaultDate() && x.PromotionEnd.Value > Utils.GetDefaultDate()))))
                .Take(5).OrderBy(x => x.Sort).ThenBy(x => x.TitleEn).ToList();

                blogResponse.features = new List<BlogListFeatureModelResponse>();
                foreach (var item in featuresData)
                {
                    BlogListFeatureModelResponse fea = new BlogListFeatureModelResponse();
                    fea.ID = item.Id.ToString();
                    fea.Title_EN = item.TitleEn;
                    fea.Title_MM = item.TitleMm;
                    fea.Topic_EN = item.TopicEn;
                    fea.Topic_MM = item.TopicMm;
                    fea.CoverImage = string.IsNullOrEmpty(item.CoverImage)  ? item.CoverImage : commonRepository.GetFileFullUrl(EnumFileType.Blog, item.CoverImage);
                    fea.ThumbnailImage = string.IsNullOrEmpty(item.ThumbnailImage) ? item.ThumbnailImage : commonRepository.GetFileFullUrl(EnumFileType.Blog, item.ThumbnailImage);
                    fea.CategoryType = item.CategoryType;
                    blogResponse.features.Add(fea);
                }

                var query = unitOfWork.GetRepository<Entities.Blog>()
                .Query(x => x.IsDelete == false && x.IsActive == true
                 && (x.CategoryType == EnumCategoryType.activity.ToString() || (x.CategoryType == EnumCategoryType.promotion.ToString() && (x.PromotionStart.Value < Utils.GetDefaultDate() && x.PromotionEnd.Value > Utils.GetDefaultDate()))));

                if (model.categoryType?.Any() == true)
                {
                    query = query.Where(x => x.CategoryType == model.categoryType);
                }

                query = query.OrderBy(o => o.Sort).ThenBy(x => x.TitleEn);
                int filterCont = 0;
                filterCont = query.Count();

                var bloglist = query.Skip((model.PageIndex - 1) * model.PageSize).Take(model.PageSize).ToList();

                List<BlogListModelResponse> data = new List<BlogListModelResponse>();
                foreach (var item in bloglist)
                {
                    data.Add(new BlogListModelResponse
                    {
                        ID = item.Id.ToString(),
                        Title_EN = item.TitleEn,
                        Title_MM = item.TitleMm,
                        Topic_EN = item.TopicEn,
                        Topic_MM = item.TopicMm,
                        CoverImage = string.IsNullOrEmpty(item.CoverImage) ? item.CoverImage : commonRepository.GetFileFullUrl(EnumFileType.Blog, item.CoverImage),
                        ThumbnailImage = string.IsNullOrEmpty(item.ThumbnailImage) ? item.ThumbnailImage : commonRepository.GetFileFullUrl(EnumFileType.Blog, item.ThumbnailImage),
                        CategoryType = item.CategoryType,
                        ReadMin_EN = item.ReadMinEn,
                        ReadMin_MM = item.ReadMinMm,
                        PromotionEnd = item.PromotionEnd,
                    });
                }

                var blogData = new PagaingResponseModel<BlogListModelResponse>
                {
                    CurrentPage = model.PageIndex,
                    Data = data,
                    CountPerPage = model.PageSize,
                    TotalCount = filterCont
                };

                blogResponse.list = blogData;

                return errorCodeProvider.GetResponseModel<BlogListResponse>(ErrorCode.E0, blogResponse);
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<BlogListResponse>(ErrorCode.E500);
            }

        }
        #endregion

        #region "Get Detail"
        public ResponseModel<BlogDetailResponse> GetDetail(string id)
        {
            try
            {
                var blog = unitOfWork.GetRepository<Blog>()
                .Query(x => x.Id == new Guid(id) && x.IsActive == true && x.IsDelete == false
                && (x.CategoryType == EnumCategoryType.activity.ToString() || (x.CategoryType == EnumCategoryType.promotion.ToString() && (x.PromotionStart.Value < Utils.GetDefaultDate() && x.PromotionEnd.Value > Utils.GetDefaultDate())))).FirstOrDefault();


                if (blog == null)
                {
                    var responseModel = errorCodeProvider.GetResponseModel<BlogDetailResponse>(ErrorCode.E0);
                    responseModel.Message = "No promotion or activity found.";

                    return responseModel;
                }

                BlogDetailModel detailModel = new BlogDetailModel();
                detailModel.ID = blog.Id.ToString();
                detailModel.Title_EN = blog.TitleEn;
                detailModel.Title_MM = blog.TitleMm;
                detailModel.Topic_EN = blog.TopicEn;
                detailModel.Topic_MM = blog.TopicMm;
                detailModel.Body_EN = blog.BodyEn;
                detailModel.Body_MM = blog.BodyMm;
                detailModel.CoverImage = string.IsNullOrEmpty(blog.CoverImage) ? blog.CoverImage : commonRepository.GetFileFullUrl(EnumFileType.Blog, blog.CoverImage);
                detailModel.ThumbnailImage = string.IsNullOrEmpty(blog.ThumbnailImage) ? blog.ThumbnailImage : commonRepository.GetFileFullUrl(EnumFileType.Blog, blog.ThumbnailImage);
                detailModel.ReadMin_EN = blog.ReadMinEn;
                detailModel.ReadMin_MM = blog.ReadMinMm;
                detailModel.CategoryType = blog.CategoryType;
                detailModel.PromotionEnd = blog.PromotionEnd;
                detailModel.ShareableLink= blog.ShareableLink;

                BlogDetailResponse response = new BlogDetailResponse();
                response.detail = detailModel;

                var otherBlog = unitOfWork.GetRepository<Entities.Blog>()
                .Query(x => x.IsDelete == false && x.IsActive == true && x.CategoryType == blog.CategoryType && x.Id != blog.Id
                 && (x.CategoryType == EnumCategoryType.activity.ToString() || (x.CategoryType == EnumCategoryType.promotion.ToString() && (x.PromotionStart.Value < Utils.GetDefaultDate() && x.PromotionEnd.Value > Utils.GetDefaultDate()))))
                .OrderByDescending(o => o.CreatedDate).Take(5).ToList();

                List<BlogListModelResponse> otherData = new List<BlogListModelResponse>();
                foreach (var item in otherBlog)
                {
                    otherData.Add(new BlogListModelResponse
                    {
                        ID = item.Id.ToString(),
                        Title_EN = item.TitleEn,
                        Title_MM = item.TitleMm,
                        Topic_EN = item.TopicEn,
                        Topic_MM = item.TopicMm,
                        CoverImage = string.IsNullOrEmpty(item.CoverImage) ? item.CoverImage : commonRepository.GetFileFullUrl(EnumFileType.Blog, item.CoverImage),
                        ThumbnailImage = string.IsNullOrEmpty(item.ThumbnailImage) ? item.ThumbnailImage : commonRepository.GetFileFullUrl(EnumFileType.Blog, item.ThumbnailImage),

                        CategoryType = item.CategoryType,
                        ReadMin_EN = item.ReadMinEn,
                        ReadMin_MM = item.ReadMinMm,
                        PromotionEnd = item.PromotionEnd,
                    });
                }
                response.others = otherData;

                if (blog.CategoryType == EnumCategoryType.promotion.ToString())
                {
                    List<Guid?> productIDs = unitOfWork.GetRepository<PromotionProduct>()
                    .Query(x=>x.BlogId == blog.Id).Select(s=> s.ProductId).ToList();

                    List<Product> products = unitOfWork.GetRepository<Product>()
                   .Query(x => x.IsActive == true && x.IsDelete == false && productIDs.Contains(x.ProductId))
                   .OrderBy(o => o.CreatedDate)
                   .ToList();

                    List<ProductResponse> productList = new List<ProductResponse>();
                    foreach (var item in products)
                    {
                        ProductResponse data = new ProductResponse();
                        data.ID = item.ProductId.ToString();
                        data.ProductName_EN = item.TitleEn;
                        data.ProductName_MM = item.TitleMm;
                        data.TagLine_EN = item.TaglineEn;
                        data.TagLine_MM = item.TaglineMm;
                        data.IssuedAgeFrom_EN = item.IssuedAgeFrom;
                        data.IssuedAgeFrom_MM = item.IssuedAgeFromMm;
                        data.IssuedAgeEnd_EN = item.IssuedAgeTo;
                        data.IssuedAgeEnd_MM = item.IssuedAgeToMm;

                        data.CoverImage = GetFileFullUrl(item.CoverImage);
                        data.LogoImage = GetFileFullUrl(item.LogoImage);
                        data.IconImage = GetFileFullUrl(item.LogoImage);


                        productList.Add(data);
                    }

                    response.products = productList;

                }
 
                return errorCodeProvider.GetResponseModel<BlogDetailResponse>(ErrorCode.E0, response);
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<BlogDetailResponse>(ErrorCode.E500);
            }

        }
        #endregion
    }
}
