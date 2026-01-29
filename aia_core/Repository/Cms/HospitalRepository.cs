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
using aia_core.Model.Cms.Response.Hospital;
using aia_core.Model.Cms.Request.Hospital;

namespace aia_core.Repository.Cms
{
    public interface IHospitalRepository
    {
        Task<ResponseModel<PagedList<HospitalResponse>>> List(int page, int size, string? name = null);
        Task<ResponseModel<HospitalResponse>> Get(Guid id);
        Task<ResponseModel<string>> Create(CreateHospitalRequest model);
        Task<ResponseModel<string>> Update(UpdateHospitalRequest model);
        Task<ResponseModel<string>> ChangeStatus(ChangeStatusRequest model);
        Task<ResponseModel<string>> Delete(Guid id);
    }

    public class HospitalRepository : BaseRepository, IHospitalRepository
    {
        #region "const"
        private readonly IRecurringJobRunner recurringJobRunner;
        public HospitalRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Context> unitOfWork, IRecurringJobRunner recurringJobRunner)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.recurringJobRunner = recurringJobRunner;

        }
        #endregion

        #region "get list"
        public async Task<ResponseModel<PagedList<HospitalResponse>>> List(int page, int size, string? name = null)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.Hospital>().Query(
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
                              select new HospitalResponse(r))
                              .Skip((page - 1) * size).Take(size).ToList();

                var data = new PagedList<HospitalResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Hospital,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<HospitalResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<PagedList<HospitalResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        #region "get"
        public async Task<ResponseModel<HospitalResponse>> Get(Guid id)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Hospital>().Query(x => x.ID == id).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<HospitalResponse>(ErrorCode.E400);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Hospital,
                        objectAction: EnumObjectAction.View,
                        objectId: entity.ID,
                        objectName: entity.Name);
                return errorCodeProvider.GetResponseModel<HospitalResponse>(ErrorCode.E0, new HospitalResponse(entity));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<HospitalResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region "create"
        public async Task<ResponseModel<string>> Create(CreateHospitalRequest model)
        {
            try
            {
                bool isNameExist = unitOfWork.GetRepository<Entities.Hospital>().Query(x => x.Name == model.Name && x.IsDelete == false).Any();
                if (isNameExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E201);

                bool isCodeExist = unitOfWork.GetRepository<Entities.Hospital>().Query(x => x.Code == model.Code && x.IsDelete == false).Any();
                if (isCodeExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E202);

                var entity = new Entities.Hospital
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

                await unitOfWork.GetRepository<Entities.Hospital>().AddAsync(entity);
                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Hospital,
                        objectAction: EnumObjectAction.Create,
                        objectId: entity.ID,
                        objectName: entity.Name,
                        newData: System.Text.Json.JsonSerializer.Serialize(new HospitalResponse(entity)));

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
        public async Task<ResponseModel<string>> Update(UpdateHospitalRequest model)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Hospital>().Query(
                    expression: r => r.ID == model.ID && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                bool isNameExist = unitOfWork.GetRepository<Entities.Hospital>().Query(x => x.Name == model.Name && x.IsDelete == false && x.ID != model.ID).Any();
                if (isNameExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E201);

                bool isCodeExist = unitOfWork.GetRepository<Entities.Hospital>().Query(x => x.Code == model.Code && x.IsDelete == false && x.ID != model.ID).Any();
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
                        objectGroup: EnumObjectGroup.Hospital,
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
                var entity = await unitOfWork.GetRepository<Entities.Hospital>().Query(
                    expression: r => r.ID == model.ID && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

                entity.IsActive = model.IsActive;
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.UpdatedBy = new Guid(GetCmsUser().ID);

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Hospital,
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
                var entity = await unitOfWork.GetRepository<Entities.Hospital>().Query(
                    expression: r => r.ID == id && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

                entity.IsDelete = true;
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.UpdatedBy = new Guid(GetCmsUser().ID);

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Hospital,
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