using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.RecurringJobs;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Transactions;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace aia_core.Repository.Cms
{
    public interface IProductRepository 
    {
        Task<ResponseModel<PagedList<ProductResponse>>> List(int page, int size, string? productName, Guid[]? coverages);
        Task<ResponseModel<ProductResponse>> Get(Guid productId);
        Task<ResponseModel<ProductResponse>> Create(CreateProductRequest model);
        Task<ResponseModel<ProductResponse>> Update(UpdateProductRequest model);
        Task<ResponseModel<ProductResponse>> Delete(Guid productId);
    }
    public class ProductRepository: BaseRepository, IProductRepository
    {
        private readonly IRecurringJobRunner recurringJobRunner;
        private readonly INotificationService notificationService;
        private readonly IServiceProvider serviceProvider;

        public ProductRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Context> unitOfWork, IRecurringJobRunner recurringJobRunner, INotificationService notificationService, IServiceProvider serviceProvider)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.recurringJobRunner = recurringJobRunner;
            this.notificationService = notificationService;
            this.serviceProvider = serviceProvider;
        }

        #region #list
        public async Task<ResponseModel<PagedList<ProductResponse>>> List(int page, int size, string? productName, Guid[]? coverages)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.Product>().Query(
                    expression: r => r.IsDelete == false && r.IsActive == true,
                    include: i => i.Include(x => x.ProductBenefits)
                    .Include(x => x.ProductCoverages.Where(r => r.Coverage.IsDelete == false)).ThenInclude(x => x.Coverage))
                    .OrderBy(x => x.CreatedDate).AsQueryable() ;

                #region #filters
                if (!string.IsNullOrWhiteSpace(productName))
                {
                    query = query.Where(r => r.TitleEn.Contains(productName) || r.TitleMm.Contains(productName));
                }
                if(coverages != null && coverages.Any())
                {
                    query = query.Where(x => x.ProductCoverages.Any(x => coverages.ToList().Contains(x.CoverageId.Value)));
                }
                #endregion

                //query = query.OrderByDescending(x => x.CreatedDate);

                int totalCount = 0;
                totalCount = await query.CountAsync();

                var source = (from r in query.AsEnumerable()
                              select new ProductResponse(r, GetFileFullUrl))
                              .Skip((page - 1) * size).Take(size).ToList();

                var data = new PagedList<ProductResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Products,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<ProductResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<ProductResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        #region #details
        public async Task<ResponseModel<ProductResponse>> Get(Guid productId)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Product>().Query(expression: r => r.ProductId == productId && r.IsDelete == false,
                    include: i => i.Include(x => x.ProductBenefits)
                    .Include(x => x.ProductCoverages.Where(r => r.Coverage.IsDelete == false)).ThenInclude(x => x.Coverage)
                    ).FirstOrDefaultAsync();
                if (entity == null)
                    return errorCodeProvider.GetResponseModel<ProductResponse>(ErrorCode.E400);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Products,
                        objectAction: EnumObjectAction.View,
                        objectId: entity.ProductId,
                        objectName: entity.TitleEn);
                return errorCodeProvider.GetResponseModel<ProductResponse>(ErrorCode.E0, new ProductResponse(entity, GetFileFullUrl));
            }
            catch (Exception ex)
            {
                return errorCodeProvider.GetResponseModel<ProductResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #create
        public async Task<ResponseModel<ProductResponse>> Create(CreateProductRequest model)
        {
            try
            {
                var productType = await unitOfWork.GetRepository<Entities.ProductType>().Query(expression: r => r.ShortDesc == model.ProductTypeShort).FirstOrDefaultAsync();

                if(model.ProductCoverages != null
                    && model.ProductCoverages.Any())
                {
                    var hasCoverage = await unitOfWork.GetRepository<Entities.Coverage>().Query(expression: 
                        r => model.ProductCoverages.Any(x => x == r.CoverageId)).AnyAsync();
                    if(!hasCoverage)
                        return new ResponseModel<ProductResponse> { Code = 400, Message = "Inavlid coverages." };
                }

                var hasSameProductType = unitOfWork.GetRepository<Entities.Product>().Query(x => x.ProductTypeShort == model.ProductTypeShort && x.IsActive == true && x.IsDelete == false).Any();

                if(hasSameProductType)
                {
                    return errorCodeProvider.GetResponseModel<ProductResponse>(ErrorCode.E400);
                }

                var isDuplicateName = unitOfWork.GetRepository<Entities.Product>()
                    .Query(x => (x.TitleEn == model.TitleEn || x.TitleMm == model.TitleMm) && x.IsDelete == false && x.IsActive == true).Any();

                if(isDuplicateName)
                {
                    return new ResponseModel<ProductResponse> { Code = 400, Message = "This product name is duplicated." };
                }


                var entity = new Entities.Product 
                {
                    ProductId = Guid.NewGuid(),
                    TitleEn = model.TitleEn,
                    TitleMm = model.TitleMm,
                    IntroEn = model.IntroEn,
                    IntroMm = model.IntroMm,
                    ShortEn = model.ShortEn,
                    ShortMm = model.ShortMm,
                    TaglineEn = model.TaglineEn,
                    TaglineMm = model.TaglineMm,
                    CreditingLink = model.CreditingLink,
                    WebsiteLink = model.WebsiteLink,
                    Brochure = model.Brochure,
                    IssuedAgeFrom = model.IssuedAgeFrom,
                    IssuedAgeTo = model.IssuedAgeTo,
                    IssuedAgeFromMm = model.IssuedAgeFromMm,
                    IssuedAgeToMm = model.IssuedAgeToMm,
                    PolicyTermUpToEn = model.PolicyTermUpToEn,
                    PolicyTermUpToMm = model.PolicyTermUpToMm,
                    ProductTypeId = productType?.Id,
                    ProductTypeShort = model.ProductTypeShort,
                    CreatedDate = Utils.GetDefaultDate(),
                    IsActive = true,
                    IsDelete = false,
                    NotAllowedInProductList = model.NotAllowedInProductList?? false,
                };

                #region #upload-logo
                if(model.LogoImage != null)
                {
                    string logoImageName = $"{Utils.GetDefaultDate().Ticks}-{model.LogoImage.FileName}";
                    var resultLogoImage = await azureStorage.UploadAsync(logoImageName, model.LogoImage);
                    entity.LogoImage = resultLogoImage.Code == (int)HttpStatusCode.OK ? logoImageName : null;
                }
                #endregion

                //#region #upload-cover
                //if (model.CoverImage != null)
                //{
                //    string coverImageName = $"{Utils.GetDefaultDate().Ticks}-{model.CoverImage.FileName}";
                //    var resultCoverImage = await azureStorage.UploadAsync(coverImageName, model.CoverImage);
                //    entity.CoverImage = resultCoverImage.Code == (int)HttpStatusCode.OK ? coverImageName : null;
                //}
                //#endregion

                #region #product-benefits
                if (model.ProductBenefits != null
                    && model.ProductBenefits.Any())
                {
                    int sorting = 0;
                    foreach (var benefit in model.ProductBenefits)
                    {
                        sorting++;
                        entity.ProductBenefits.Add(new ProductBenefit
                        {
                            ProductBenefitId = Guid.NewGuid(),
                            ProductId = entity.ProductId,
                            TitleEn = benefit.TitleEn,
                            TitleMm = benefit.TitleMm,
                            DescriptionEn = benefit.DescriptionEn,
                            DescriptionMm = benefit.DescriptionMm,
                            CreatedDate = Utils.GetDefaultDate(),
                            IsActive = true,
                            IsDelete = false,
                            Sort = sorting,
                        });
                    }
                }
                #endregion

                #region #product-coverages
                if(model.ProductCoverages != null
                    && model.ProductCoverages.Any())
                {
                    foreach(var coverage in model.ProductCoverages)
                    {
                        entity.ProductCoverages.Add(new ProductCoverage 
                        {
                            Id = Guid.NewGuid(),
                            ProductId = entity.ProductId,
                            CoverageId = coverage,
                        });
                    }
                }
                #endregion

                //using (var scope = new TransactionScope(
                //        scopeOption: TransactionScopeOption.Suppress,
                //        scopeTimeout: TimeSpan.FromMinutes(3),
                //        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                //        ))
                //{
                    await unitOfWork.GetRepository<Entities.Product>().AddAsync(entity);
                    await unitOfWork.SaveChangesAsync();


                #region #Noti

                if (model.NotAllowedInProductList == false)
                {
                    Task.Run(() =>
                    {
                        using (var scope = serviceProvider.CreateScope())
                        {
                            var _notiService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                            _ = _notiService.SendNewSetupItemNoti(EnumSystemNotiType.Product, entity.ProductId.ToString());

                        }
                    });
                }

                    
                #endregion

                //scope.Complete();






                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Products,
                        objectAction: EnumObjectAction.Create,
                        objectId: entity.ProductId,
                        objectName: entity.TitleEn,
                        newData: System.Text.Json.JsonSerializer.Serialize(new ProductResponse(entity, GetFileFullUrl)));
                    return errorCodeProvider.GetResponseModel<ProductResponse>(ErrorCode.E0, new ProductResponse(entity, GetFileFullUrl));
                //}
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ProductCreateException => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<ProductResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #update
        public async Task<ResponseModel<ProductResponse>> Update(UpdateProductRequest model)
        {
            try
            {
                var productType = await unitOfWork.GetRepository<Entities.ProductType>().Query(expression: r => r.ShortDesc == model.ProductTypeShort).FirstOrDefaultAsync();
                var entity = await unitOfWork.GetRepository<Entities.Product>().Query(expression: r => r.ProductId == model.ProductId && r.IsDelete == false,
                    include: i => i.Include(x => x.ProductBenefits)
                    .Include(x => x.ProductCoverages).ThenInclude(x => x.Coverage)
                    ).FirstOrDefaultAsync();
                if (entity == null)
                    return errorCodeProvider.GetResponseModel<ProductResponse>(ErrorCode.E400);

                var isDuplicateName = unitOfWork.GetRepository<Entities.Product>()
                    .Query(x => x.ProductId != model.ProductId && x.IsDelete == false && x.IsActive == true 
                    && (x.TitleEn == model.TitleEn || x.TitleMm == model.TitleMm)).Any();

                if (isDuplicateName)
                {
                    return new ResponseModel<ProductResponse> { Code = 400, Message = "This product name is duplicated." };
                }

                //var hasSameProductType = unitOfWork.GetRepository<Entities.Product>().Query(x => x.ProductTypeShort == model.ProductTypeShort && x.IsActive == true && x.IsDelete == false).Any();

                //if (hasSameProductType)
                //{
                //    return errorCodeProvider.GetResponseModel<ProductResponse>(ErrorCode.E400);
                //}

                var oldData = System.Text.Json.JsonSerializer.Serialize(new ProductResponse(entity, GetFileFullUrl));
                using (var scope = new TransactionScope(
                        scopeOption: TransactionScopeOption.Suppress,
                        scopeTimeout: TimeSpan.FromMinutes(3),
                        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                        ))
                {
                    #region #upload-logo
                    if (model.LogoImage != null)
                    {
                        string logoImageName = $"{Utils.GetDefaultDate().Ticks}-{model.LogoImage.FileName}";
                        var resultLogoImage = await azureStorage.UploadAsync(logoImageName, model.LogoImage);
                        entity.LogoImage = resultLogoImage.Code == (int)HttpStatusCode.OK ? logoImageName : null;
                    }
                    #endregion

                    //#region #upload-cover
                    //if (model.CoverImage != null)
                    //{
                    //    string coverImageName = $"{Utils.GetDefaultDate().Ticks}-{model.CoverImage.FileName}";
                    //    var resultCoverImage = await azureStorage.UploadAsync(coverImageName, model.CoverImage);
                    //    entity.CoverImage = resultCoverImage.Code == (int)HttpStatusCode.OK ? coverImageName : null;
                    //}
                    //#endregion

                    #region #product-benefits
                    if(entity.ProductBenefits != null) unitOfWork.GetRepository<Entities.ProductBenefit>().Delete(entity.ProductBenefits);
                    if (model.ProductBenefits != null
                        && model.ProductBenefits.Any())
                    {
                        int sorting = 0;
                        foreach (var benefit in model.ProductBenefits)
                        {
                            sorting++;
                            entity.ProductBenefits.Add(new ProductBenefit
                            {
                                ProductBenefitId = Guid.NewGuid(),
                                ProductId = entity.ProductId,
                                TitleEn = benefit.TitleEn,
                                TitleMm = benefit.TitleMm,
                                DescriptionEn = benefit.DescriptionEn,
                                DescriptionMm = benefit.DescriptionMm,
                                CreatedDate = Utils.GetDefaultDate(),
                                IsActive = true,
                                IsDelete = false,
                                Sort = sorting,
                            });
                        }
                    }
                    #endregion

                    #region #product-coverages
                    if(entity.ProductCoverages != null) unitOfWork.GetRepository<Entities.ProductCoverage>().Delete(entity.ProductCoverages);
                    if (model.ProductCoverages != null
                        && model.ProductCoverages.Any())
                    {
                        foreach (var coverage in model.ProductCoverages)
                        {
                            entity.ProductCoverages.Add(new ProductCoverage
                            {
                                Id = Guid.NewGuid(),
                                ProductId = entity.ProductId,
                                CoverageId = coverage,
                            });
                        }
                    }
                    #endregion

                    entity.TitleEn = model.TitleEn;
                    entity.TitleMm = model.TitleMm;
                    entity.IntroEn = model.IntroEn;
                    entity.IntroMm = model.IntroMm;
                    entity.ShortEn = model.ShortEn;
                    entity.ShortMm = model.ShortMm;
                    entity.TaglineEn = model.TaglineEn;
                    entity.TaglineMm = model.TaglineMm;
                    entity.CreditingLink = model.CreditingLink; //?? entity.CreditingLink;
                    entity.WebsiteLink = model.WebsiteLink;
                    entity.Brochure = model.Brochure;
                    entity.IssuedAgeFrom = model.IssuedAgeFrom;
                    entity.IssuedAgeTo = model.IssuedAgeTo;
                    entity.IssuedAgeFromMm = model.IssuedAgeFromMm;
                    entity.IssuedAgeToMm = model.IssuedAgeToMm;
                    entity.PolicyTermUpToEn = model.PolicyTermUpToEn;
                    entity.PolicyTermUpToMm = model.PolicyTermUpToMm;
                    entity.ProductTypeId = productType?.Id;
                    entity.ProductTypeShort = model.ProductTypeShort;

                    entity.UpdatedDate = Utils.GetDefaultDate();

                    entity.NotAllowedInProductList = model.NotAllowedInProductList ?? entity.NotAllowedInProductList?? false;

                    await unitOfWork.SaveChangesAsync();
                    scope.Complete();

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Products,
                        objectAction: EnumObjectAction.Update,
                        objectId: entity.ProductId,
                        objectName: entity.TitleEn,
                        oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(new ProductResponse(entity, GetFileFullUrl)));
                    return errorCodeProvider.GetResponseModel<ProductResponse>(ErrorCode.E0, new ProductResponse(entity, GetFileFullUrl));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ProductCreateException => {ex?.Message} {ex?.InnerException?.Message}");
                return errorCodeProvider.GetResponseModel<ProductResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #delete
        public async Task<ResponseModel<ProductResponse>> Delete(Guid productId)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Product>().Query(expression: r => r.ProductId == productId && r.IsDelete == false,
                    include: i => i.Include(x => x.ProductBenefits)
                    .Include(x => x.ProductCoverages).ThenInclude(x => x.Coverage)
                    ).FirstOrDefaultAsync();
                if (entity == null)
                    return errorCodeProvider.GetResponseModel<ProductResponse>(ErrorCode.E400);

                entity.IsDelete = true;
                entity.UpdatedDate = Utils.GetDefaultDate();
                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Products,
                        objectAction: EnumObjectAction.Delete,
                        objectId: entity.ProductId,
                        objectName: entity.TitleEn);
                return errorCodeProvider.GetResponseModel<ProductResponse>(ErrorCode.E0, new ProductResponse(entity, GetFileFullUrl));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<ProductResponse>(ErrorCode.E500);
            }
        }
        #endregion
    }
}
