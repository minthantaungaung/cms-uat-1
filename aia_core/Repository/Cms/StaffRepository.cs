using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.Services;
using aia_core.UnitOfWork;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Transactions;
using Newtonsoft.Json;
using aia_core.Provider;

namespace aia_core.Repository.Cms
{
    public interface IStaffRepository 
    {
        Task<ResponseModel<PagedList<StaffResponse>>> List(int page, int size, string? email = null, string? name = null, Guid[]? roles = null, bool? status = null);

        Task<ResponseModel<PagedList<StaffResponse>>> ListByRole(int page, int size, string roleId);

        Task<ResponseModel<StaffResponse>> Get(Guid staffId);
        Task<ResponseModel<StaffResponse>> Create(CreateStaffRequest model);
        Task<ResponseModel<StaffResponse>> Update(UpdateStaffRequest model);
        Task<ResponseModel<StaffResponse>> Delete(Guid staffId);
    }
    public class StaffRepository: BaseRepository, IStaffRepository
    {
        public StaffRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {

        }

        #region #list
        public async Task<ResponseModel<PagedList<StaffResponse>>> List(int page, int size, string? email = null, string? name = null, Guid[]? roles = null, bool? status = null)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.Staff>().Query(
                    include: i => i.Include(x=> x.Role));

                if(!string.IsNullOrEmpty(email) ) query = query.Where(r=> r.Email.Contains(email));
                if (!string.IsNullOrEmpty(name)) query = query.Where(r => r.Name.Contains(name));


                if (roles != null && roles.Any())
                {
                    query = query.Where(x => roles.ToList().Contains(x.RoleId.Value));
                }


                if (status.HasValue) query = query.Where(r => r.IsActive == status);

                int totalCount = 0;
                totalCount = await query.CountAsync();

                var source = (from r in query.AsEnumerable()
                              select new StaffResponse(r))
                              .Skip((page - 1) * size).Take(size).ToList();

                var data = new PagedList<StaffResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Staffs,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<StaffResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<StaffResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        #region #details
        public async Task<ResponseModel<StaffResponse>> Get(Guid staffId)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Staff>().Query(
                    expression: r => r.Id == staffId,
                    include: i => i.Include(x => x.Role)).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<StaffResponse>(ErrorCode.E400);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Staffs,
                        objectAction: EnumObjectAction.View,
                        objectId: entity.Id,
                        objectName: entity.Name);
                return errorCodeProvider.GetResponseModel<StaffResponse>(ErrorCode.E0, new StaffResponse(entity));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<StaffResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #create
        public async Task<ResponseModel<StaffResponse>> Create(CreateStaffRequest model)
        {
            try
            {
                var role = await unitOfWork.GetRepository<Entities.Role>().Query(expression: r => r.Id == model.RoleId).FirstOrDefaultAsync();
                if (role == null) return new ResponseModel<StaffResponse> { Code = 400, Message = "Invalid role id." };

                var staff = await unitOfWork.GetRepository<Entities.Staff>().Query(expression: r => r.Email == model.Email).FirstOrDefaultAsync();
                if (staff != null) return new ResponseModel<StaffResponse> { Code = 400, Message = "Email already exist." };

                (string hash, string salt) = PasswordManager.CreatePasswordHashAndSalt(model.Password);

                var entity = new Entities.Staff
                {
                    Id = Guid.NewGuid(),
                    RoleId = model.RoleId,
                    Email = model.Email,
                    Name = model.Name,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    IsActive = model.Status ?? false,
                    CreatedDate = Utils.GetDefaultDate(),
                };

                using (var scope = new TransactionScope(
                        scopeOption: TransactionScopeOption.Suppress,
                        scopeTimeout: TimeSpan.FromMinutes(3),
                        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                        ))
                {
                    await unitOfWork.GetRepository<Entities.Staff>().AddAsync(entity);
                    await unitOfWork.SaveChangesAsync();
                    scope.Complete();

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Staffs,
                        objectAction: EnumObjectAction.Create,
                        objectId: entity.Id,
                        objectName: entity.Name,
                        newData: System.Text.Json.JsonSerializer.Serialize(new StaffResponse(entity)));
                    return errorCodeProvider.GetResponseModel<StaffResponse>(ErrorCode.E0, new StaffResponse(entity));
                }
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<StaffResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #update
        public async Task<ResponseModel<StaffResponse>> Update(UpdateStaffRequest model)
        {
            try
            {
                var role = await unitOfWork.GetRepository<Entities.Role>().Query(expression: r => r.Id == model.RoleId).FirstOrDefaultAsync();
                if (role == null) return new ResponseModel<StaffResponse> { Code = 400, Message = "Invalid role id." };

                var staff = await unitOfWork.GetRepository<Entities.Staff>().Query(expression: r => r.Id != model.Id && r.Email == model.Email).FirstOrDefaultAsync();
                if (staff != null) return new ResponseModel<StaffResponse> { Code = 400, Message = "Email already exist." };

                var entity = await unitOfWork.GetRepository<Entities.Staff>().Query(
                    expression: r => r.Id == model.Id,
                    include: i => i.Include(x => x.Role)).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<StaffResponse>(ErrorCode.E400);

                var hash = ""; var salt = "";

                if (!string.IsNullOrEmpty(model.Password) && model.Password != "undefined")
                {
                    (hash, salt) = PasswordManager.CreatePasswordHashAndSalt(model.Password);
                }
                

                var oldData = System.Text.Json.JsonSerializer.Serialize(new StaffResponse(entity));
                using (var scope = new TransactionScope(
                        scopeOption: TransactionScopeOption.Suppress,
                        scopeTimeout: TimeSpan.FromMinutes(3),
                        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                        ))
                {
                    entity.Email = model.Email;
                    entity.Name = model.Name;
                    entity.RoleId = model.RoleId;
                    entity.IsActive = model.Status ?? entity.IsActive;

                    if (!string.IsNullOrEmpty(model.Password) && model.Password != "undefined")
                    {
                        entity.PasswordHash = hash;
                        entity.PasswordSalt = salt;
                    }
                    
                    entity.UpdatedDate = DateTime.UtcNow;
                    await unitOfWork.SaveChangesAsync();
                    scope.Complete();

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Staffs,
                        objectAction: EnumObjectAction.Update,
                        objectId: entity.Id,
                        objectName: entity.Name,
                        oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(new StaffResponse(entity)));
                    return errorCodeProvider.GetResponseModel<StaffResponse>(ErrorCode.E0, new StaffResponse(entity));
                }
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<StaffResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #delete
        public async Task<ResponseModel<StaffResponse>> Delete(Guid staffId)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Staff>().Query(
                    expression: r => r.Id == staffId).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<StaffResponse>(ErrorCode.E400);

                unitOfWork.GetRepository<Entities.Staff>().Delete(entity);
                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Staffs,
                        objectAction: EnumObjectAction.Delete,
                        objectId: entity.Id,
                        objectName: entity.Name);
                return errorCodeProvider.GetResponseModel<StaffResponse>(ErrorCode.E0, new StaffResponse(entity));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<StaffResponse>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<PagedList<StaffResponse>>> ListByRole(int page, int size, string roleId)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.Staff>()
                    .Query(x => x.RoleId == Guid.Parse(roleId))
                    .Include(x => x.Role);

                int totalCount = 0;
                totalCount = await query.CountAsync();

                var source = (from r in query.AsEnumerable()
                              select new StaffResponse(r))
                              .Skip((page - 1) * size).Take(size).ToList();

                var data = new PagedList<StaffResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Staffs,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<StaffResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<StaffResponse>>(ErrorCode.E400);
            }
        }
        #endregion
    }
}
