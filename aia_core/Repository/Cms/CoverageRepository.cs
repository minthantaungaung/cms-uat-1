using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace aia_core.Repository.Cms
{
    public interface ICoverageRepository 
    {
        Task<ResponseModel<PagedList<CoverageResponse>>> List(int page, int size, string? coverageName, Guid[]? products);
        Task<ResponseModel<CoverageResponse>> Get(Guid coverageId);
        Task<ResponseModel<CoverageResponse>> Create(CreateCoverageRequest model);
        Task<ResponseModel<CoverageResponse>> Update(UpdateCoverageRequest model);
        Task<ResponseModel<CoverageResponse>> Delete(Guid coverageId);
    }
    public class CoverageRepository: BaseRepository, ICoverageRepository
    {
        public CoverageRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider, 
            IUnitOfWork<Context> unitOfWork) 
            :base(httpContext, azureStorage, errorCodeProvider, unitOfWork) 
        {

        }

        #region #list
        public async Task<ResponseModel<PagedList<CoverageResponse>>> List(int page, int size, string? coverageName, Guid[]? products)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.Coverage>().Query(
                    expression: r => r.IsDelete == false,
                    include: i => i.Include(x => x.ProductCoverages.Where(r=> r.Product.IsDelete == false)).ThenInclude(x => x.Product));

                Expression<Func<Entities.Coverage, bool>> expression = e => string.IsNullOrEmpty(coverageName) ? 1 == 1
                : e.CoverageNameEn.Contains(coverageName) || e.CoverageNameMm.Contains(coverageName);

                if (products != null && products.Any())
                {
                    query = query.Where(x => x.ProductCoverages.Any(x => products.ToList().Contains(x.ProductId.Value)));
                }

                Func<IQueryable<Entities.Coverage>, IOrderedQueryable<Entities.Coverage>> order = o => o.OrderByDescending(x => x.CreatedDate);
                int totalCount = 0;
                totalCount = await query.Where(expression).CountAsync();

                var source = (from r in query.Where(expression).AsEnumerable()
                              select new CoverageResponse(r, GetFileFullUrl))
                              .Skip((page - 1) * size).Take(size).ToList();

                var data = new PagedList<CoverageResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                    objectGroup: EnumObjectGroup.Coverages,
                    objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<CoverageResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<CoverageResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        #region #details
        public async Task<ResponseModel<CoverageResponse>> Get(Guid coverageId)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Coverage>().Query(expression: r => r.CoverageId == coverageId,
                    include: i => i.Include(x=> x.ProductCoverages.Where(r=> r.Product.IsDelete == false)).ThenInclude(x=> x.Product)
                    ).FirstOrDefaultAsync();
                if (entity == null)
                    return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E400);

                await CmsAuditLog(
                    objectGroup: EnumObjectGroup.Coverages,
                    objectAction: EnumObjectAction.View,
                    objectId: entity.CoverageId,
                    objectName: entity.CoverageNameEn);
                return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E0, new CoverageResponse(entity, GetFileFullUrl));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #create
        public async Task<ResponseModel<CoverageResponse>> Create(CreateCoverageRequest model)
        {
            try
            {
                var coverage = await unitOfWork.GetRepository<Entities.Coverage>().Query(expression: r => r.IsDelete == false && (r.CoverageNameEn == model.CoverageNameEN || r.CoverageNameMm == model.CoverageNameMm)).FirstOrDefaultAsync();
                if (coverage != null) return new ResponseModel<CoverageResponse> { Code = 400, Message = "This coverage name is duplicated." };

                var coverageIconName = $"{Utils.GetDefaultDate().Ticks}-{model.CoverageIcon.FileName}";
                var result = await azureStorage.UploadAsync(coverageIconName, model.CoverageIcon);
                if (result.Code == (int)HttpStatusCode.OK)
                {
                    var entity = new Entities.Coverage
                    {
                        CoverageId = Guid.NewGuid(),
                        CoverageNameEn = model.CoverageNameEN,
                        CoverageNameMm = model.CoverageNameMm,
                        CoverageIcon = coverageIconName,
                        CreatedDate = DateTime.Now,
                        IsActive = true,
                        IsDelete = false
                    };
                    await unitOfWork.GetRepository<Entities.Coverage>().AddAsync(entity);
                    await unitOfWork.SaveChangesAsync();

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Coverages,
                        objectAction: EnumObjectAction.Create,
                        objectId: entity.CoverageId,
                        objectName: entity.CoverageNameEn,
                        newData: System.Text.Json.JsonSerializer.Serialize(new CoverageResponse(entity, GetFileFullUrl)));
                    return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E0, new CoverageResponse(entity, GetFileFullUrl));
                }
                else if (result.Code == (int)HttpStatusCode.InternalServerError)
                {
                    return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E500);
                }
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E500);
            }
            return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E400);
        }
        #endregion

        #region #update
        public async Task<ResponseModel<CoverageResponse>> Update(UpdateCoverageRequest model)
        {
            try
            {
                var coverage = await unitOfWork.GetRepository<Entities.Coverage>().Query(
                    expression: r => r.CoverageId != model.CoverageId && r.IsDelete == false && (r.CoverageNameEn == model.CoverageNameEN || r.CoverageNameMm == model.CoverageNameMm)).FirstOrDefaultAsync();
                if (coverage != null) return new ResponseModel<CoverageResponse> { Code = 400, Message = "This coverage name is duplicated." };

                var entity = await unitOfWork.GetRepository<Entities.Coverage>().Query(expression: r => r.CoverageId == model.CoverageId).FirstOrDefaultAsync();
                if (entity == null)
                    return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E400);
                var oldData = System.Text.Json.JsonSerializer.Serialize(new CoverageResponse(entity, GetFileFullUrl));

                if (model.CoverageIcon != null)
                {
                    var coverageIconName = $"{Utils.GetDefaultDate().Ticks}-{model.CoverageIcon.FileName}";
                    var result = await azureStorage.UploadAsync(coverageIconName, model.CoverageIcon);
                    if (result.Code == (int)HttpStatusCode.OK)
                    {
                        entity.CoverageIcon = coverageIconName;
                    }
                    else if (result.Code == (int)HttpStatusCode.InternalServerError)
                    {
                        return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E500);
                    }
                    else
                    {
                        return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E400);
                    }
                }

                entity.CoverageNameEn = model.CoverageNameEN;
                entity.CoverageNameMm = model.CoverageNameMm;
                entity.UpdatedDate = Utils.GetDefaultDate();
                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                    objectGroup: EnumObjectGroup.Coverages,
                    objectAction: EnumObjectAction.Update,
                    objectId: model.CoverageId,
                    objectName: model.CoverageNameEN,
                    oldData: oldData,
                    newData: System.Text.Json.JsonSerializer.Serialize(new CoverageResponse(entity, GetFileFullUrl)));
                return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E0, new CoverageResponse(entity, GetFileFullUrl));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);


                return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #delete
        public async Task<ResponseModel<CoverageResponse>> Delete(Guid coverageId)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Coverage>().Query(expression: r => r.CoverageId == coverageId).FirstOrDefaultAsync();
                if (entity == null)
                    return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E400);

                var hasProduct = await unitOfWork.GetRepository<Entities.ProductCoverage>().Query(
                    expression: r => r.CoverageId == coverageId && r.Product.IsDelete != true).AnyAsync();
                if (hasProduct) return new ResponseModel<CoverageResponse> { Code = 400, Message = $"Not allow to delete since coverage name \"{entity.CoverageNameEn}\" is currently applying by products." };

                entity.IsDelete = true;
                entity.UpdatedDate = Utils.GetDefaultDate();
                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                    objectGroup: EnumObjectGroup.Coverages,
                    objectAction: EnumObjectAction.Delete,
                    objectId: entity.CoverageId,
                    objectName: entity.CoverageNameEn);
                return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E0);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<CoverageResponse>(ErrorCode.E500);
            }
        }
        #endregion
    }
}
