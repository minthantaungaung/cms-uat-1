using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.Model.Mobile.Response;
using aia_core.RecurringJobs;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Transactions;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using DocumentFormat.OpenXml.Office2010.Excel;
using Hangfire;

namespace aia_core.Repository.Cms
{
    public interface IBlogRepository 
    {
        Task<ResponseModel<List<BlogResponse>>> GetAll();
        Task<ResponseModel<PagedList<BlogResponse>>> List(int page, int size, string? title = null, string? topic = null, EnumBlogStatus[]? status = null, bool? feature = null);
        Task<ResponseModel<BlogResponse>> Get(Guid blogId);
        Task<ResponseModel<BlogResponse>> Create(CreateBlogRequest model);
        Task<ResponseModel<BlogResponse>> Update(UpdateBlogRequest model);
        Task<ResponseModel<BlogResponse>> Delete(Guid blogId);
        Task<ResponseModel<BlogResponse>> Order(Guid blogId, int sort);
        Task<ResponseModel<BlogResponse>> Order(List<BlogOrderRequest> model);
        Task<ResponseModel<BlogResponse>> Feature(Guid blogId, bool isFeature);
    }
    public class BlogRepository: BaseRepository, IBlogRepository
    {
        private readonly IRecurringJobRunner recurringJobRunner;
        private readonly INotificationService notificationService;
        private readonly IServiceProvider serviceProvider;
        public BlogRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Context> unitOfWork, IRecurringJobRunner recurringJobRunner, INotificationService notificationService, IServiceProvider serviceProvider)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.recurringJobRunner = recurringJobRunner;
            this.notificationService = notificationService;
            this.serviceProvider = serviceProvider;
        }

        #region #get-all
        public async Task<ResponseModel<List<BlogResponse>>> GetAll()
        {
            try
            {
                var entities = await unitOfWork.GetRepository<Entities.Blog>().Query(expression: r => r.IsDelete == false,
                    order: o => o.OrderBy(x=> x.Sort)).ToListAsync();

                var data = entities.Select(s => new BlogResponse(s, GetFileFullUrl)).ToList();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Blogs,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<List<BlogResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<BlogResponse>>(ErrorCode.E500);
            }
        }
        #endregion

        #region #list
        public async Task<ResponseModel<PagedList<BlogResponse>>> List(int page, int size, string? title, string? topic, EnumBlogStatus[]? status = null, bool? feature = null)
        {
            try
            {

                var query = unitOfWork.GetRepository<Entities.Blog>().Query(
                    expression: r => r.IsDelete == false,
                    include: i => i.Include(x => x.PromotionProducts));

                #region #filters
                if(!string.IsNullOrWhiteSpace(title))
                {
                    query = query.Where(r => r.TitleEn.Contains(title));
                }
                if(!string.IsNullOrWhiteSpace(topic))
                {
                    query = query.Where(r => r.TopicEn.Contains(topic));
                }
                if(feature.HasValue)
                {
                    query = query.Where(r => r.IsFeature == feature.Value);
                }

                if(status != null && status.Any())
                {
                    Expression<Func<Entities.Blog, bool>> expression = null;                    
                    if(status.Any(x=> x == EnumBlogStatus.pending))
                    {
                        expression = expression == null ? e => e.CategoryType == $"{EnumCategoryType.promotion}" && e.PromotionStart > Utils.GetDefaultDate()
                            : new ExpressionHelper<Entities.Blog>().Combine(ExpressionType.Or, expression,
                            e => e.CategoryType == $"{EnumCategoryType.promotion}" && e.PromotionStart > Utils.GetDefaultDate());
                    }

                    if (status.Any(x => x == EnumBlogStatus.started))
                    {
                        expression = expression == null ? e => e.CategoryType == $"{EnumCategoryType.promotion}" && e.PromotionStart <= Utils.GetDefaultDate() && e.PromotionEnd >= Utils.GetDefaultDate()
                            : new ExpressionHelper<Entities.Blog>().Combine(ExpressionType.Or, expression,
                            e => e.CategoryType == $"{EnumCategoryType.promotion}" && e.PromotionStart <= Utils.GetDefaultDate() && e.PromotionEnd >= Utils.GetDefaultDate());
                    }

                    if (status.Any(x => x == EnumBlogStatus.expired))
                    {
                        expression = expression == null ? e => e.CategoryType == $"{EnumCategoryType.promotion}" && e.PromotionEnd < Utils.GetDefaultDate()
                            : new ExpressionHelper<Entities.Blog>().Combine(ExpressionType.Or, expression,
                            e => e.CategoryType == $"{EnumCategoryType.promotion}" && e.PromotionEnd < Utils.GetDefaultDate());
                    }

                    if(expression != null) query = query.Where(expression);
                }
                #endregion

                int totalCount = 0;
                totalCount = await query.CountAsync();

                query = query.OrderByDescending(x => x.CreatedDate);

                var source = (from r in query.AsEnumerable()
                              select new BlogResponse(r, GetFileFullUrl))
                              .Skip((page - 1) * size).Take(size).ToList();

                var data = new PagedList<BlogResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Blogs,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<BlogResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<BlogResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        #region #details
        public async Task<ResponseModel<BlogResponse>> Get(Guid blogId)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Blog>().Query(
                    expression: r => r.Id == blogId && r.IsDelete == false,
                    include: i => i.Include(x=> x.PromotionProducts)
                    ).FirstOrDefaultAsync();
                if(entity == null) return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E400);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Blogs,
                        objectAction: EnumObjectAction.View,
                        objectId: entity.Id,
                        objectName: entity.TitleEn);
                return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E0, new BlogResponse(entity, GetFileFullUrl));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #create
        public async Task<ResponseModel<BlogResponse>> Create(CreateBlogRequest model)
        {
            try
            {
                if (model.CategoryType == EnumCategoryType.promotion 
                    && model.Products != null
                    && model.Products.Any())
                {
                    var hasProducts = await unitOfWork.GetRepository<Entities.Product>().Query(expression:
                        r => model.Products.Any(x => x == r.ProductId)).AnyAsync();
                    if (!hasProducts)
                        return new ResponseModel<BlogResponse> { Code = 400, Message = "Invalid product id." };
                }
                var maxSort = unitOfWork.GetRepository<Entities.Blog>()
                    .Query(expression: r => r.IsDelete == false && r.IsActive == true)
                    .Select(s => s.Sort ?? 0).Max(x => (int?)x) ?? 0;

                maxSort = maxSort + 1;

                if (model.IsFeature == true)
                {
                    var countFeature = await unitOfWork.GetRepository<Entities.Blog>().Query(expression: r => r.IsDelete == false && r.IsFeature == true).CountAsync();
                    if(countFeature >= 5) return new ResponseModel<BlogResponse> { Code = 400, Message = "Execeed max feature." };
                }

                var entity = new Entities.Blog
                {
                    Id = Guid.NewGuid(),
                    CategoryType = $"{model.CategoryType}",
                    CreatedDate = DateTime.Now,
                    TitleEn = model.TitleEn,
                    TitleMm = model.TitleMm,
                    TopicEn = model.TopicEn,
                    TopicMm = model.TopicMm,
                    ReadMinEn = model.ReadMinEn,
                    ReadMinMm = model.ReadMinMm,
                    BodyEn = model.BodyEn,
                    BodyMm = model.BodyMm,
                    ShareableLink = model.ShareableLink,

                    
                    PromotionStart = model.PromotionStart == null ? null : Utils.ConvertUtcDateToMMDate(model.PromotionStart.Value),
                    PromotionEnd = model.PromotionEnd == null ? null : Utils.ConvertUtcDateToMMDate(model.PromotionEnd.Value),
                    IsFeature = model.IsFeature,
                    Sort = maxSort,
                    IsActive = true,
                    IsDelete = false,
                };

                #region #upload-cover-image
                if (model.CoverImage != null)
                {
                    var coverImageName = $"{Utils.GetDefaultDate().Ticks}-{model.CoverImage.FileName}";
                    var result = await azureStorage.UploadAsync(coverImageName, model.CoverImage);
                    entity.CoverImage = result.Code == 200 ? coverImageName : null;
                }
                #endregion

                #region #upload-cover-image
                if (model.ThumbnailImage != null)
                {
                    var thumbnailImage = $"{Utils.GetDefaultDate().Ticks}-{model.ThumbnailImage.FileName}";
                    var result = await azureStorage.UploadAsync(thumbnailImage, model.ThumbnailImage);
                    entity.ThumbnailImage = result.Code == 200 ? thumbnailImage : null;
                }
                #endregion

                if (model.CategoryType == EnumCategoryType.promotion
                    && model.Products != null)
                    entity.PromotionProducts = model.Products.Select(pid => new PromotionProduct { Id = Guid.NewGuid(), BlogId = entity.Id, ProductId = pid }).ToList();

                //using (var scope = new TransactionScope(
                //        scopeOption: TransactionScopeOption.Suppress,
                //        scopeTimeout: TimeSpan.FromMinutes(3),
                //        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                //        ))
                //{
                    //await unitOfWork.GetRepository<Entities.Blog>().AddAsync(entity);
                    //await unitOfWork.SaveChangesAsync();
                ////scope.Complete();

                #region #Noti
               
                if (model.CategoryType == EnumCategoryType.activity)
                {
                    Task.Run(() =>
                    {
                        using (var scope = serviceProvider.CreateScope())
                        {
                            var _notiService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                            _ = _notiService.SendNewSetupItemNoti(EnumSystemNotiType.Promotion, entity.Id.ToString());

                        }
                    });
                }
                else if(model.CategoryType == EnumCategoryType.promotion)
                {
                    if (model.PromotionStart != null)
                    { 
                        var jobId = BackgroundJob.Schedule(() 
                            => notificationService.SendNewSetupItemNoti(EnumSystemNotiType.Promotion, entity.Id.ToString()), model.PromotionStart.Value);

                        entity.JobId = jobId;
                    }
                }

                
                await unitOfWork.GetRepository<Entities.Blog>().AddAsync(entity);
                await unitOfWork.SaveChangesAsync();


                #endregion

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Blogs,
                        objectAction: EnumObjectAction.Create,
                        objectId: entity.Id,
                        objectName: entity.TitleEn,
                        newData: System.Text.Json.JsonSerializer.Serialize(new BlogResponse(entity, GetFileFullUrl)));
                    return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E0, new BlogResponse(entity, GetFileFullUrl));
                //}
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #update
        public async Task<ResponseModel<BlogResponse>> Update(UpdateBlogRequest model)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Blog>().Query(
                    expression: r => r.Id == model.Id && r.IsDelete == false,
                    include: i => i.Include(x=> x.PromotionProducts)).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E400);

                var oldBlog = entity;
                var oldData = System.Text.Json.JsonSerializer.Serialize(new BlogResponse(entity, GetFileFullUrl));

                if (model.IsFeature == true)
                {
                    var countFeature = await unitOfWork.GetRepository<Entities.Blog>()
                        .Query(expression: r => r.Id != model.Id && r.IsDelete == false && r.IsFeature == true).CountAsync();
                    if (countFeature >= 5) return new ResponseModel<BlogResponse> { Code = 400, Message = "Execeed max feature." };
                }

                if (model.CategoryType == EnumCategoryType.promotion
                    && model.Products != null
                    && model.Products.Any())
                {
                    var hasProducts = await unitOfWork.GetRepository<Entities.Product>().Query(expression:
                        r => model.Products.Any(x => x == r.ProductId)).AnyAsync();
                    if (!hasProducts)
                        return new ResponseModel<BlogResponse> { Code = 400, Message = "Execeed product id." };
                }
                if(entity.PromotionProducts != null
                    && entity.PromotionProducts.Any())
                    unitOfWork.GetRepository<Entities.PromotionProduct>().Delete(entity.PromotionProducts);

                entity.TitleEn = model.TitleEn;
                entity.TitleMm = model.TitleMm;
                entity.TopicEn = model.TopicEn;
                entity.TopicMm = model.TopicMm;
                entity.ReadMinEn = model.ReadMinEn;
                entity.ReadMinMm = model.ReadMinMm;
                entity.BodyEn = model.BodyEn;
                entity.BodyMm = model.BodyMm;
                entity.IsFeature = model.IsFeature;
                entity.PromotionStart = model.PromotionStart == null ? entity.PromotionStart : Utils.ConvertUtcDateToMMDate(model.PromotionStart.Value);
                entity.PromotionEnd = model.PromotionEnd == null ? entity.PromotionEnd : Utils.ConvertUtcDateToMMDate(model.PromotionEnd.Value);
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.ShareableLink = model.ShareableLink;

                #region #upload-cover-image
                if (model.CoverImage != null)
                {
                    var coverImageName = $"{Utils.GetDefaultDate().Ticks}-{model.CoverImage.FileName}";
                    var result = await azureStorage.UploadAsync(coverImageName, model.CoverImage);
                    entity.CoverImage = result.Code == 200 ? coverImageName : entity.CoverImage;
                }
                #endregion

                #region #upload-cover-image
                if (model.ThumbnailImage != null)
                {
                    var thumbnailImage = $"{Utils.GetDefaultDate().Ticks}-{model.ThumbnailImage.FileName}";
                    var result = await azureStorage.UploadAsync(thumbnailImage, model.ThumbnailImage);
                    entity.ThumbnailImage = result.Code == 200 ? thumbnailImage : entity.ThumbnailImage;
                }
                #endregion

                if (model.CategoryType == EnumCategoryType.promotion
                    && model.Products != null)
                    entity.PromotionProducts = model.Products.Select(pid => new PromotionProduct { Id = Guid.NewGuid(), BlogId = entity.Id, ProductId = pid }).ToList();

                using (var scope = new TransactionScope(
                        scopeOption: TransactionScopeOption.Suppress,
                        scopeTimeout: TimeSpan.FromMinutes(3),
                        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                        ))
                {
                    entity.UpdatedDate = Utils.GetDefaultDate();
                    await unitOfWork.SaveChangesAsync();
                    


                    if (model.CategoryType == EnumCategoryType.promotion)
                    {
                        //////if (oldBlog.PromotionStart < model.PromotionStart)
                        //////{
                        ////    var jobId = unitOfWork.GetRepository<Entities.JobId>()
                        ////        .Query(x => x.PromotionId == model.Id.ToString()).Select(x => x.JobId1).FirstOrDefault();
                        ////    if (jobId != null)
                        ////    {
                        ////        recurringJobRunner.DeleteScheduledJob(jobId);
                        ////    }
                            
                        ////    recurringJobRunner.ScheduledOrSendSystemNotificaton(EnumSystemNotiType.Promotion, entity.Id.ToString());
                        //////}
                        
                    }

                    scope.Complete();

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Blogs,
                        objectAction: EnumObjectAction.Update,
                        objectId: entity.Id,
                        objectName: entity.TitleEn,
                        oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(new BlogResponse(entity, GetFileFullUrl)));
                    return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E0, new BlogResponse(entity, GetFileFullUrl));
                }
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #delete
        public async Task<ResponseModel<BlogResponse>> Delete(Guid blogId)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Blog>().Query(
                    expression: r => r.Id == blogId && r.IsDelete == false,
                    include: i => i.Include(x => x.PromotionProducts)
                    ).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E400);

                entity.IsDelete = true;
                entity.UpdatedDate = Utils.GetDefaultDate();
                await unitOfWork.SaveChangesAsync();

                if(entity.CategoryType == EnumCategoryType.promotion.ToString() && !string.IsNullOrEmpty(entity.JobId))
                {
                    BackgroundJob.Delete(entity.JobId);
                }

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Blogs,
                        objectAction: EnumObjectAction.Delete,
                        objectId: entity.Id,
                        objectName: entity.TitleEn);
                return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E0, new BlogResponse(entity, GetFileFullUrl));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #order-update
        public async Task<ResponseModel<BlogResponse>> Order(Guid blogId, int sort)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Blog>().Query(
                    expression: r => r.Id == blogId && r.IsDelete == false,
                    include: i => i.Include(x => x.PromotionProducts)
                    ).FirstOrDefaultAsync();
                if (entity == null)
                    return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E400);

                var data = await unitOfWork.GetRepository<Entities.Blog>().Query(expression: r => r.Sort == sort).ToListAsync();

                using (var scope = new TransactionScope(
                        scopeOption: TransactionScopeOption.Suppress,
                        scopeTimeout: TimeSpan.FromMinutes(3),
                        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                        ))
                {
                    data.ForEach(e => e.Sort = entity.Sort);

                    entity.Sort = sort;
                    entity.UpdatedDate = Utils.GetDefaultDate();
                    await unitOfWork.SaveChangesAsync();

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Blogs,
                        objectAction: EnumObjectAction.UpdateSort);
                    return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E0, new BlogResponse(entity, GetFileFullUrl));
                }
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #order-update
        public async Task<ResponseModel<BlogResponse>> Order(List<BlogOrderRequest> model)
        {
            try
            {
                if(model.Count <= 0)
                    return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E400);

                var ids = model.Select(s => s.Id).ToList();
                var entity = await unitOfWork.GetRepository<Entities.Blog>().Query(
                    expression: r => ids.Any(id=> id == r.Id) && r.IsDelete == false
                    ).ToListAsync();
                if (entity.Count != model.Count)
                    return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E400);

                using (var scope = new TransactionScope(
                        scopeOption: TransactionScopeOption.Suppress,
                        scopeTimeout: TimeSpan.FromMinutes(3),
                        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                        ))
                {
                    entity.ForEach(e => e.Sort = model.Where(r => r.Id == e.Id)?.FirstOrDefault()?.Order ?? e.Sort);
                    await unitOfWork.SaveChangesAsync();
                    scope.Complete();

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Blogs,
                        objectAction: EnumObjectAction.UpdateSort);
                    return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E0);
                }
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #feature
        public async Task<ResponseModel<BlogResponse>> Feature(Guid blogId, bool isFeature)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Blog>().Query(
                    expression: r => r.Id == blogId && r.IsDelete == false,
                    include: i => i.Include(x => x.PromotionProducts)
                    ).FirstOrDefaultAsync();
                if (entity == null)
                    return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E400);
                var oldData = System.Text.Json.JsonSerializer.Serialize(new BlogResponse(entity, GetFileFullUrl));
                if (isFeature)
                {
                    var countFeature = await unitOfWork.GetRepository<Entities.Blog>()
                        .Query(expression: r => r.Id != blogId && r.IsDelete == false && r.IsFeature == true).CountAsync();
                    if (countFeature >= 5) return new ResponseModel<BlogResponse> { Code = 400, Message = "Execeed max feature." };
                }

                using (var scope = new TransactionScope(
                        scopeOption: TransactionScopeOption.Suppress,
                        scopeTimeout: TimeSpan.FromMinutes(3),
                        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                        ))
                {
                    entity.IsFeature = isFeature;
                    entity.UpdatedDate = Utils.GetDefaultDate();
                    await unitOfWork.SaveChangesAsync();

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Blogs,
                        objectAction: EnumObjectAction.UpdateFeature,
                        objectId: entity.Id,
                        objectName: entity.TitleEn,
                        oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(new BlogResponse(entity, GetFileFullUrl)));
                    return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E0, new BlogResponse(entity, GetFileFullUrl));
                }
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<BlogResponse>(ErrorCode.E500);
            }
        }
        #endregion
    }
}
