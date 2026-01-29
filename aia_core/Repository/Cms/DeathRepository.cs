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
using aia_core.Model.Cms.Response.Death;
using aia_core.Model.Cms.Request.Death;

namespace aia_core.Repository.Cms
{
    public interface IDeathRepository
    {
        Task<ResponseModel<PagedList<DeathResponse>>> List(int page, int size, string? name = null);
        Task<ResponseModel<DeathResponse>> Get(Guid id);
        Task<ResponseModel<string>> Create(CreateDeathRequest model);
        Task<ResponseModel<string>> Update(UpdateDeathRequest model);
        Task<ResponseModel<string>> ChangeStatus(ChangeStatusRequest model);
        Task<ResponseModel<string>> Delete(Guid id);
    }

    public class DeathRepository : BaseRepository, IDeathRepository
    {
        #region "const"
        private readonly IRecurringJobRunner recurringJobRunner;
        public DeathRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Context> unitOfWork, IRecurringJobRunner recurringJobRunner)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.recurringJobRunner = recurringJobRunner;

        }
        #endregion

        #region "get list"
        public async Task<ResponseModel<PagedList<DeathResponse>>> List(int page, int size, string? name = null)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.Death>().Query(
                    expression: r => r.IsDelete == false);

                #region #filters
                if (!string.IsNullOrWhiteSpace(name))
                {
                    query = query.Where(r => r.Name.Contains(name) || r.Name_MM.Contains(name));
                }
                #endregion

                int totalCount = 0;
                totalCount = await query.CountAsync();

                var source = (from r in query.AsEnumerable()
                              select new DeathResponse(r))
                              .Skip((page - 1) * size).Take(size).ToList();

                var data = new PagedList<DeathResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Death,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<DeathResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<PagedList<DeathResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        #region "get"
        public async Task<ResponseModel<DeathResponse>> Get(Guid id)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Death>().Query(x => x.ID == id).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<DeathResponse>(ErrorCode.E400);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Death,
                        objectAction: EnumObjectAction.View,
                        objectId: entity.ID,
                        objectName: entity.Name);
                return errorCodeProvider.GetResponseModel<DeathResponse>(ErrorCode.E0, new DeathResponse(entity));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<DeathResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region "create"
        public async Task<ResponseModel<string>> Create(CreateDeathRequest model)
        {
            try
            {
                bool isNameExist = unitOfWork.GetRepository<Entities.Death>().Query(x => x.Name == model.Name && x.IsDelete == false).Any();
                if (isNameExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E201);

                bool isCodeExist = unitOfWork.GetRepository<Entities.Death>().Query(x => x.Code == model.Code && x.IsDelete == false).Any();
                if (isCodeExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E202);

                var entity = new Entities.Death
                {
                    ID = Guid.NewGuid(),
                    Name = model.Name,
                    Name_MM = model.Name_MM,
                    Code = model.Code,
                    CreatedDate = Utils.GetDefaultDate(),
                    CreatedBy = new Guid(GetCmsUser().ID),
                    IsActive = model.IsActive,
                    IsDelete = false
                };

                await unitOfWork.GetRepository<Entities.Death>().AddAsync(entity);
                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Death,
                        objectAction: EnumObjectAction.Create,
                        objectId: entity.ID,
                        objectName: entity.Name,
                        newData: System.Text.Json.JsonSerializer.Serialize(new DeathResponse(entity)));

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
        }
        #endregion

        #region "update"
        public async Task<ResponseModel<string>> Update(UpdateDeathRequest model)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Death>().Query(
                    expression: r => r.ID == model.ID && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                bool isNameExist = unitOfWork.GetRepository<Entities.Death>().Query(x => x.Name == model.Name && x.IsDelete == false && x.ID != model.ID).Any();
                if (isNameExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E201);

                bool isCodeExist = unitOfWork.GetRepository<Entities.Death>().Query(x => x.Code == model.Code && x.IsDelete == false && x.ID != model.ID).Any();
                if (isCodeExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E202);

                var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

                entity.Name = model.Name;
                entity.Name_MM = model.Name_MM;
                entity.Code = model.Code;
                entity.IsActive = model.IsActive;
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.UpdatedBy = new Guid(GetCmsUser().ID);

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Death,
                        objectAction: EnumObjectAction.Update,
                        objectId: entity.ID,
                        objectName: entity.Name,
                        oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(entity));
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);
            }
        }
        #endregion

        #region "change status"
        public async Task<ResponseModel<string>> ChangeStatus(ChangeStatusRequest model)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Death>().Query(
                    expression: r => r.ID == model.ID && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

                entity.IsActive = model.IsActive;
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.UpdatedBy = new Guid(GetCmsUser().ID);

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Death,
                        objectAction: EnumObjectAction.ChangeStatus,
                        objectId: entity.ID,
                        objectName: entity.Name,
                        oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(entity));
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0);
            }
            catch (System.Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);
            }
        }
        #endregion

        #region "delete"
        public async Task<ResponseModel<string>> Delete(Guid id)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Death>().Query(
                    expression: r => r.ID == id && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

                entity.IsDelete = true;
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.UpdatedBy = new Guid(GetCmsUser().ID);

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Death,
                        objectAction: EnumObjectAction.Delete,
                        objectId: entity.ID,
                        objectName: entity.Name,
                        oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(entity));
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0);
            }
            catch (System.Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);
            }
        }
        #endregion
    }
}