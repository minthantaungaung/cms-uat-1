using System.Linq.Expressions;
using System.Reflection;
using aia_core.Entities;
using aia_core.Model.Cms.Response.ServicingDetail;
using aia_core.Model.Mobile.Request.Blog;
using aia_core.Model.Mobile.Response;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace aia_core.Repository.Mobile
{
    public interface IProductRepository
    {
        ResponseModel<ProductListResponse> GetList(ProductListRequest model);
        ResponseModel<ProductDetailResponse> GetDetail(string id);
    }
    public class ProductRepository : BaseRepository, IProductRepository
    {
        #region "const"
        private readonly ICommonRepository commonRepository;

        public ProductRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork,
            ICommonRepository commonRepository)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.commonRepository = commonRepository;
        }
        #endregion

        #region "Get List"
        public ResponseModel<ProductListResponse> GetList(ProductListRequest model)
        {
            try
            {
                ProductListResponse productResponse = new ProductListResponse();

                var query = unitOfWork.GetRepository<Entities.Product>()
                .Query(x => x.IsDelete == false && x.IsActive == true && (x.NotAllowedInProductList == null || x.NotAllowedInProductList == false));

                query = query.OrderBy(o => o.CreatedDate);
                int filterCont = 0;
                filterCont = query.Count();

                var bloglist = query.Skip((model.PageIndex - 1) * model.PageSize).Take(model.PageSize).ToList();

                List<ProductResponse> data = new List<ProductResponse>();
                foreach (var item in bloglist)
                {
                    data.Add(new ProductResponse
                    {
                        ID = item.ProductId.ToString(),
                        ProductName_EN = item.TitleEn,
                        ProductName_MM = item.TitleMm,
                        TagLine_EN = item.TaglineEn,
                        TagLine_MM = item.TaglineMm,
                        IssuedAgeFrom_EN = item.IssuedAgeFrom,
                        IssuedAgeFrom_MM = item.IssuedAgeFromMm,
                        IssuedAgeEnd_EN = item.IssuedAgeTo,
                        IssuedAgeEnd_MM = item.IssuedAgeToMm,
                        LogoImage = string.IsNullOrEmpty(item.LogoImage) ? item.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Product, item.LogoImage),
                        IconImage = string.IsNullOrEmpty(item.LogoImage) ? item.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Product, item.LogoImage),
                        ProductCode = item.ProductTypeShort,
                    });
                }

                var productData = new PagaingResponseModel<ProductResponse>
                {
                    CurrentPage = model.PageIndex,
                    Data = data,
                    CountPerPage = model.PageSize,
                    TotalCount = filterCont
                };

                productResponse.list = productData;

                return errorCodeProvider.GetResponseModel<ProductListResponse>(ErrorCode.E0, productResponse);
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<ProductListResponse>(ErrorCode.E400);
            }

        }
        #endregion

        #region "Get Detail"
        public ResponseModel<ProductDetailResponse> GetDetail(string id)
        {
            try
            {
                var product = unitOfWork.GetRepository<Product>()
                .Query(x => x.ProductId == new Guid(id) && x.IsActive == true && x.IsDelete == false).FirstOrDefault();

                if (product == null)
                {
                    var responseModel = errorCodeProvider.GetResponseModel<ProductDetailResponse>(ErrorCode.E0);
                    responseModel.Message = "No product found.";

                    return responseModel;
                }
                    

                ProductDetailModel detailModel = new ProductDetailModel();
                detailModel.ID = product.ProductId.ToString();
                detailModel.ProductName_EN = product.TitleEn;
                detailModel.ProductName_MM = product.TitleMm;
                detailModel.Short_EN = product.ShortEn;
                detailModel.Short_MM = product.ShortMm;
                detailModel.Intro_EN = product.IntroEn;
                detailModel.Intro_MM = product.IntroMm;
                detailModel.TagLine_EN = product.TaglineEn;
                detailModel.TagLine_MM = product.TaglineMm;
                detailModel.IssuedAgeFrom_EN = product.IssuedAgeFrom;
                detailModel.IssuedAgeFrom_MM = product.IssuedAgeFromMm;
                detailModel.IssuedAgeEnd_EN = product.IssuedAgeTo;
                detailModel.IssuedAgeEnd_MM = product.IssuedAgeToMm;
                detailModel.PolicyTermUp_EN = product.PolicyTermUpToEn;
                detailModel.PolicyTermUp_MM = product.PolicyTermUpToMm;
                detailModel.WebSiteLink = product.WebsiteLink;
                detailModel.Brochure = product.Brochure;
                detailModel.CreditingLink = product.CreditingLink;


                detailModel.CoverImage = string.IsNullOrEmpty(product.CoverImage) ? product.CoverImage : commonRepository.GetFileFullUrl(EnumFileType.Product, product.CoverImage);
                detailModel.LogoImage = string.IsNullOrEmpty(product.LogoImage) ? product.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Product, product.LogoImage);
                detailModel.IconImage = string.IsNullOrEmpty(product.LogoImage) ? product.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Product, product.LogoImage);

                detailModel.ProductCode = product.ProductTypeShort;

                ProductDetailResponse response = new ProductDetailResponse();
                response.detail = detailModel;
                response.benefits = new List<ProductBenefitsModels>();

                var productBenefits = unitOfWork.GetRepository<Entities.ProductBenefit>()
                .Query(x=>x.ProductId == product.ProductId && x.IsActive == true && x.IsDelete == false)
                .OrderBy(o=> o.Sort).ToList();

                foreach (var item in productBenefits)
                {
                    response.benefits.Add(new ProductBenefitsModels(){
                        Title_EN = item.TitleEn,
                        Title_MM = item.TitleMm,
                        Description_EN = item.DescriptionEn,
                        Description_MM = item.DescriptionMm
                    });
                }

                var otherProductList = unitOfWork.GetRepository<Product>()
                .Query(x => x.ProductId != new Guid(id) && x.IsActive == true && x.IsDelete == false && (x.NotAllowedInProductList == null || x.NotAllowedInProductList == false)).ToList();

                var otherProducts = new List<ProductDetailModel>();
                foreach (var item in otherProductList)
                {

                    ProductDetailModel otherProduct = new ProductDetailModel();
                    otherProduct.ID = item.ProductId.ToString();
                    otherProduct.ProductName_EN = item.TitleEn;
                    otherProduct.ProductName_MM = item.TitleMm;
                    otherProduct.Short_EN = item.ShortEn;
                    otherProduct.Short_MM = item.ShortMm;
                    otherProduct.Intro_EN = item.IntroEn;
                    otherProduct.Intro_MM = item.IntroMm;
                    otherProduct.TagLine_EN = item.TaglineEn;
                    otherProduct.TagLine_MM = item.TaglineMm;
                    otherProduct.IssuedAgeFrom_EN = item.IssuedAgeFrom;
                    otherProduct.IssuedAgeFrom_MM = item.IssuedAgeFrom;
                    otherProduct.IssuedAgeEnd_EN = item.IssuedAgeFrom;
                    otherProduct.IssuedAgeEnd_MM = item.IssuedAgeFrom;
                    otherProduct.PolicyTermUp_EN = item.PolicyTermUpToEn;
                    otherProduct.PolicyTermUp_MM = item.PolicyTermUpToMm;
                    otherProduct.WebSiteLink = item.WebsiteLink;
                    otherProduct.Brochure = item.Brochure;
                    otherProduct.CreditingLink = item.CreditingLink;


                    otherProduct.CoverImage = string.IsNullOrEmpty(item.CoverImage) ? item.CoverImage : commonRepository.GetFileFullUrl(EnumFileType.Product, item.CoverImage);
                    otherProduct.LogoImage = string.IsNullOrEmpty(item.LogoImage) ? item.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Product, item.LogoImage);
                    otherProduct.IconImage = string.IsNullOrEmpty(item.LogoImage) ? item.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Product, item.LogoImage);

                    otherProduct.ProductCode = item.ProductTypeShort;

                    otherProducts.Add(otherProduct);
                }

                response.OtherProducts = otherProducts;

                return errorCodeProvider.GetResponseModel<ProductDetailResponse>(ErrorCode.E0, response);
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);


                return errorCodeProvider.GetResponseModel<ProductDetailResponse>(ErrorCode.E500);
            }

        }
        #endregion
    }
}
