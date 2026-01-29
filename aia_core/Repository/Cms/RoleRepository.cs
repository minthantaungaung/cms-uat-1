using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Transactions;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Security;

namespace aia_core.Repository.Cms
{
    public interface IRoleRepository
    {
        Task<ResponseModel<PagedList<RoleResponse>>> List(int page, int size, string? title, string[]? permissions);
        Task<ResponseModel<RoleResponse>> Get(Guid roleId);
        Task<ResponseModel<RoleResponse>> Create(CreateRoleRequest model);
        Task<ResponseModel<RoleResponse>> Update(UpdateRoleRequest model);
        Task<ResponseModel<RoleResponse>> Delete(Guid roleId);
    }
    public class RoleRepository : BaseRepository, IRoleRepository
    {
        public RoleRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {

        }
        #region #list
        public async Task<ResponseModel<PagedList<RoleResponse>>> List(int page, int size, string? title, string[]? permissions)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.Role>().Query();

                if (permissions != null && permissions.Length > 0)
                {
                    var moduleList = new List<int>();
                    var moduleStrList = new List<string>();

                    foreach (var permission in permissions)
                    {

                        switch (permission)
                        {
                            case "Settings": moduleStrList.Add(((int)EnumRoleModule.Settings).ToString()); break;
                            case "Member_Policy_Info": moduleStrList.Add(((int)EnumRoleModule.Member_Policy_Info).ToString()); break;
                            case "Member_Proposition": moduleStrList.Add(((int)EnumRoleModule.Member_Proposition).ToString()); break;
                            case "Claim": moduleStrList.Add(((int)EnumRoleModule.Claim).ToString()); break;
                            case "Claim_Log": moduleStrList.Add(((int)EnumRoleModule.Claim_Log).ToString()); break;
                            case "Service": moduleStrList.Add(((int)EnumRoleModule.Service).ToString()); break;
                            case "Service_Log": moduleStrList.Add(((int)EnumRoleModule.Service_Log).ToString()); break;
                            case "Activity_And_Promotions": moduleStrList.Add(((int)EnumRoleModule.Activity_And_Promotions).ToString()); break;
                            case "Products": moduleStrList.Add(((int)EnumRoleModule.Products).ToString()); break;
                        }
                    }

                    Expression<Func<Entities.Role, bool>> searchExpression = BuildSearchExpression(moduleStrList.ToArray());

                    query = query.Where(searchExpression);
                }

                if (!string.IsNullOrEmpty(title))
                {
                    query = query.Where(r => r.Title.Contains(title));
                }



                int totalCount = 0;
                totalCount = await query.CountAsync();

                var source = (from r in query.AsEnumerable()
                              select new RoleResponse(r))
                              .Skip((page - 1) * size).Take(size).ToList();

                foreach (var item in source)
                {
                    item.Permissions = item.Permissions?.Where(x => x != EnumRoleModule.SystemConfig).ToArray();
                }

                var data = new PagedList<RoleResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Roles,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<RoleResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<RoleResponse>>(ErrorCode.E500);
            }
        }
        #endregion

        #region #details
        public async Task<ResponseModel<RoleResponse>> Get(Guid roleId)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Role>().Query(
                    expression: r => r.Id == roleId,
                    include: i => i.Include(x => x.Staff)).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<RoleResponse>(ErrorCode.E400);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Roles,
                        objectAction: EnumObjectAction.View,
                        objectId: entity.Id,
                        objectName: entity.Title);
                return errorCodeProvider.GetResponseModel<RoleResponse>(ErrorCode.E0, new RoleResponse(entity));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<RoleResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #create
        public async Task<ResponseModel<RoleResponse>> Create(CreateRoleRequest model)
        {
            try
            {
                try
                {
                    List<EnumRoleModule> PermissionList = new List<EnumRoleModule>();
                    foreach (var permission in model.Permissions)
                    {
                        PermissionList.Add(permission);
                    }
                    PermissionList.Add(EnumRoleModule.SystemConfig);
                    model.Permissions = PermissionList.ToArray();
                }
                catch { }

                var entity = new Entities.Role
                {
                    Id = Guid.NewGuid(),
                    Title = model.Title,
                    Permissions = System.Text.Json.JsonSerializer.Serialize(model.Permissions),
                    CreatedDate = Utils.GetDefaultDate(),
                };

                using (var scope = new TransactionScope(
                        scopeOption: TransactionScopeOption.Suppress,
                        scopeTimeout: TimeSpan.FromMinutes(3),
                        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                        ))
                {
                    await unitOfWork.GetRepository<Entities.Role>().AddAsync(entity);
                    await unitOfWork.SaveChangesAsync();
                    scope.Complete();

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Roles,
                        objectAction: EnumObjectAction.Create,
                        objectId: entity.Id,
                        objectName: entity.Title,
                        newData: System.Text.Json.JsonSerializer.Serialize(new RoleResponse(entity)));
                    return errorCodeProvider.GetResponseModel<RoleResponse>(ErrorCode.E0, new RoleResponse(entity));
                }
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<RoleResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #update
        public async Task<ResponseModel<RoleResponse>> Update(UpdateRoleRequest model)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Role>().Query(
                    expression: r => r.Id == model.Id).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<RoleResponse>(ErrorCode.E400);

                var oldData = System.Text.Json.JsonSerializer.Serialize(new RoleResponse(entity));
                using (var scope = new TransactionScope(
                        scopeOption: TransactionScopeOption.Suppress,
                        scopeTimeout: TimeSpan.FromMinutes(3),
                        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                        ))
                {
                    entity.Title = model.Title ?? entity.Title;

                    try
                    {
                        List<EnumRoleModule> PermissionList = new List<EnumRoleModule>();
                        foreach (var permission in model.Permissions)
                        {
                            PermissionList.Add(permission);
                        }
                        PermissionList.Add(EnumRoleModule.SystemConfig);
                        model.Permissions = PermissionList.ToArray();
                    }
                    catch { }

                    entity.Permissions = System.Text.Json.JsonSerializer.Serialize(model.Permissions);
                    entity.UpdatedDate = Utils.GetDefaultDate();

                    await unitOfWork.SaveChangesAsync();
                    scope.Complete();

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Roles,
                        objectAction: EnumObjectAction.Update,
                        objectId: entity.Id,
                        objectName: entity.Title,
                        oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(new RoleResponse(entity)));
                    return errorCodeProvider.GetResponseModel<RoleResponse>(ErrorCode.E0, new RoleResponse(entity));
                }
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<RoleResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #delete
        public async Task<ResponseModel<RoleResponse>> Delete(Guid roleId)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Role>().Query(
                    expression: r => r.Id == roleId).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<RoleResponse>(ErrorCode.E400);
                if (entity?.Staff.Any() == true) return errorCodeProvider.GetResponseModel<RoleResponse>(ErrorCode.E400);

                unitOfWork.GetRepository<Entities.Role>().Delete(entity);
                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Roles,
                        objectAction: EnumObjectAction.Delete,
                        objectId: entity.Id,
                        objectName: entity.Title);
                return errorCodeProvider.GetResponseModel<RoleResponse>(ErrorCode.E0, new RoleResponse(entity));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<RoleResponse>(ErrorCode.E500);
            }
        }
        #endregion

        private Expression<Func<Entities.Role, bool>> BuildSearchExpression(string[] values)
        {
            //string[] values = searchString
            //    .Trim('[', ']')
            //    .Split(',')
            //    .Select(s => s.Trim())
            //    .ToArray();

            // Build the OR condition for each value
            Expression<Func<Entities.Role, bool>> searchExpression = entity => false; // Start with a condition that's always false
            foreach (var value in values)
            {
                searchExpression = searchExpression.OrElse(entity => EF.Functions.Like(entity.Permissions, $"%{value}%"));
            }

            return searchExpression;
        }
    }

    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }
    }
}
