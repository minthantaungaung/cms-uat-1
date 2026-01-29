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
using aia_core.Model.Cms.Response.CriticalIllness;
using aia_core.Model.Cms.Request.CriticalIllness;
using aia_core.Model.Cms.Response.PartialDisability;
using System.Data;

namespace aia_core.Repository.Cms
{
    public interface ICriticalIllnessRepository
    {
        Task<ResponseModel<PagedList<CriticalIllnessResponse>>> List(int page, int size, string? name = null);
        Task<ResponseModel<CriticalIllnessResponse>> Get(Guid id);
        Task<ResponseModel<string>> Create(CreateCriticalIllnessRequest model);
        Task<ResponseModel<string>> Update(UpdateCriticalIllnessRequest model);
        Task<ResponseModel<string>> ChangeStatus(ChangeStatusRequest model);
        Task<ResponseModel<string>> Delete(Guid id);

        Task<ResponseModel<PagedList<CriticalIllnessResponse>>> List(int page, int size, string? name = null, List<string>? productCodes = null);
    }

    public class CriticalIllnessRepository : BaseRepository, ICriticalIllnessRepository
    {
        #region "const"
        private readonly IRecurringJobRunner recurringJobRunner;
        public CriticalIllnessRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Context> unitOfWork, IRecurringJobRunner recurringJobRunner)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.recurringJobRunner = recurringJobRunner;

        }
        #endregion

        #region "get list"
        public async Task<ResponseModel<PagedList<CriticalIllnessResponse>>> List(int page, int size, string? name = null)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.CriticalIllness>().Query(
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
                              select new CriticalIllnessResponse(r))
                              .Skip((page - 1) * size).Take(size).ToList();

                var data = new PagedList<CriticalIllnessResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.CriticalIllness,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<CriticalIllnessResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<PagedList<CriticalIllnessResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        #region "get"
        public async Task<ResponseModel<CriticalIllnessResponse>> Get(Guid id)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.CriticalIllness>().Query(x => x.ID == id).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<CriticalIllnessResponse>(ErrorCode.E400);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.CriticalIllness,
                        objectAction: EnumObjectAction.View,
                        objectId: entity.ID,
                        objectName: entity.Name);

                #region CI & Product

                var productIdList = unitOfWork.GetRepository<Entities.CI_Product>()
                     .Query(x => x.DisabiltiyId == id)
                     .Select(x => $"{x.ProductId}")
                     .ToList();

                var response = new CriticalIllnessResponse(entity);

                if (productIdList?.Any() == true)
                {
                    response.ProductCodeList = productIdList;
                }
                #endregion


                return errorCodeProvider.GetResponseModel<CriticalIllnessResponse>(ErrorCode.E0, response);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<CriticalIllnessResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region "create"
        public async Task<ResponseModel<string>> Create(CreateCriticalIllnessRequest model)
        {
            try
            {
                bool isNameExist = unitOfWork.GetRepository<Entities.CriticalIllness>().Query(x => x.Name == model.Name && x.IsDelete == false).Any();
                if (isNameExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E201);

                bool isCodeExist = unitOfWork.GetRepository<Entities.CriticalIllness>().Query(x => x.Code == model.Code && x.IsDelete == false).Any();
                if (isCodeExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E202);

                var entity = new Entities.CriticalIllness
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

                await unitOfWork.GetRepository<Entities.CriticalIllness>().AddAsync(entity);


                #region CI & Product

                model.ProductCodeList?.ForEach(productCode =>
                {
                    unitOfWork.GetRepository<Entities.CI_Product>()
                    .Add(new CI_Product
                    {
                        Id = Guid.NewGuid(),
                        DisabiltiyId = entity.ID,
                        ProductId = Guid.Parse(productCode),
                        CreatedOn = Utils.GetDefaultDate(),
                        IsDeleted = false,
                    });
                });

                #endregion

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.CriticalIllness,
                        objectAction: EnumObjectAction.Create,
                        objectId: entity.ID,
                        objectName: entity.Name,
                        newData: System.Text.Json.JsonSerializer.Serialize(new CriticalIllnessResponse(entity)));

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
        public async Task<ResponseModel<string>> Update(UpdateCriticalIllnessRequest model)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.CriticalIllness>().Query(
                    expression: r => r.ID == model.ID && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                bool isNameExist = unitOfWork.GetRepository<Entities.CriticalIllness>().Query(x => x.Name == model.Name && x.IsDelete == false && x.ID != model.ID).Any();
                if (isNameExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E201);

                bool isCodeExist = unitOfWork.GetRepository<Entities.CriticalIllness>().Query(x => x.Code == model.Code && x.IsDelete == false && x.ID != model.ID).Any();
                if (isCodeExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E202);

                var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

                entity.Name = model.Name;
                entity.Name_MM = model.Name_MM;
                entity.Code = model.Code;
                entity.IsActive = model.IsActive;
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.UpdatedBy = new Guid(GetCmsUser().ID);

                #region CI & Product
                if (model.ProductCodeList?.Any() == true)
                {
                    //Delete OldList
                    var oldList = unitOfWork.GetRepository<Entities.CI_Product>()
                        .Query(x => x.DisabiltiyId == model.ID)
                        .ToList();
                    unitOfWork.GetRepository<Entities.CI_Product>().Delete(oldList);

                    //Add NewList
                    model.ProductCodeList?.ForEach(productCode =>
                    {
                        unitOfWork.GetRepository<Entities.CI_Product>()
                        .Add(new CI_Product
                        {
                            Id = Guid.NewGuid(),
                            DisabiltiyId = entity.ID,
                            ProductId = Guid.Parse(productCode),
                            CreatedOn = Utils.GetDefaultDate(),
                            IsDeleted = false,
                        });
                    });
                }
                #endregion

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.CriticalIllness,
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
                var entity = await unitOfWork.GetRepository<Entities.CriticalIllness>().Query(
                    expression: r => r.ID == model.ID && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

                entity.IsActive = model.IsActive;
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.UpdatedBy = new Guid(GetCmsUser().ID);

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.CriticalIllness,
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
                var entity = await unitOfWork.GetRepository<Entities.CriticalIllness>().Query(
                    expression: r => r.ID == id && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

                entity.IsDelete = true;
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.UpdatedBy = new Guid(GetCmsUser().ID);

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.CriticalIllness,
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

        public async Task<ResponseModel<PagedList<CriticalIllnessResponse>>> List(int page, int size, string? name, List<string>? productCodes = null)
        {
            try
            {
                var queryStrings = PrepareListQuery(page, size, name, productCodes);

                var count = unitOfWork.GetRepository<GetCountByRawQuery>()
                    .FromSqlRaw(queryStrings?.CountQuery, null, CommandType.Text)
                    .FirstOrDefault();

                var list = unitOfWork.GetRepository<CriticalIllnessResponse>()
                    .FromSqlRaw(queryStrings.ListQuery, null, CommandType.Text)
                    .ToList();

                Console.WriteLine($"queryStrings?.CountQuery => {queryStrings?.CountQuery} queryStrings.ListQuery => {queryStrings.ListQuery}");

                list?.ForEach(item =>
                {
                    var productGuidList = unitOfWork.GetRepository<Entities.CI_Product>()
                    .Query(x => x.DisabiltiyId == item.ID)
                    .Select(x => x.ProductId)
                    .ToList();

                    if (productGuidList?.Any() == true)
                    {
                        item.ProductNameList = unitOfWork.GetRepository<Entities.Product>()
                    .Query(x => productGuidList.Contains(x.ProductId) && x.IsActive == true && x.IsDelete == false)
                    .Select(x => x.TitleEn)
                    .ToList()
                    .OrderBy(x => x)
                    .ToList();
                    }


                });
                var data = new PagedList<CriticalIllnessResponse>(
                    source: list,
                    totalCount: count?.SelectCount?? 0,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.CriticalIllness,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<CriticalIllnessResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<PagedList<CriticalIllnessResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        private aia_core.Repository.QueryStrings PrepareListQuery(int page, int size, string? name = null, List<string>? productCodes = null)
        {
            #region #CountQuery
            var countQuery = @"SELECT COUNT(Distinct(CriticalIllness.ID)) AS SelectCount ";
            var asQuery = @"";
            #endregion

            #region #DataQuery
            var dataQuery = @"SELECT 
                            CriticalIllness.ID AS ID,
                            CriticalIllness.Name AS Name,
                            CriticalIllness.Name_MM AS Name_MM,
                            CriticalIllness.Code AS Code,
                            CriticalIllness.IsActive AS IsActive ";
            #endregion

            #region #FromQuery
            var fromQuery = @"FROM 
                                CriticalIllness
                            LEFT JOIN 
                                CI_Product ON CI_Product.DisabiltiyId = CriticalIllness.ID
                            LEFT JOIN 
                                Product ON Product.Product_ID = CI_Product.ProductId ";
            #endregion

            #region #GroupQuery

            var groupQuery = @"Group by CriticalIllness.ID,
                                CriticalIllness.Name,
                                CriticalIllness.Name_MM,
                                CriticalIllness.Code,
                                CriticalIllness.IsActive ";


            var groupQueryForCount = @"";
            #endregion

            #region #OrderQuery
            var orderQuery = @"ORDER BY CriticalIllness.Name ASC ";
            var orderQueryForCount = @" ";
            #endregion



            #region #FilterQuery

            var filterQuery = @"WHERE 
                                (CriticalIllness.IsActive = 1 AND CriticalIllness.IsDelete = 0)";
            ////AND (Product.Is_Active = 1 AND Product.Is_Delete = 0) ";

            if (!string.IsNullOrEmpty(name))
            {
                filterQuery += $@"AND (CriticalIllness.Name LIKE '%{name}%' OR CriticalIllness.Name_MM LIKE '%{name}%') ";
            }

            if (productCodes?.Any() == true)
            {
                var productCodesList = productCodes.Select(x => $"'{x}'").ToList();
                var productCodesString = string.Join(", ", productCodesList);

                filterQuery += $@"AND Product.Product_Type_Short IN ({productCodesString}) ";
            }

            #endregion

            #region #OffsetQuery

            #endregion
            var offsetQuery = "";
            offsetQuery = $"OFFSET {(page - 1) * size} ROWS FETCH NEXT {size} ROWS ONLY";

            countQuery = $"{countQuery}{fromQuery}{filterQuery}{groupQueryForCount}{asQuery}";
            var listQuery = $"{dataQuery}{fromQuery}{filterQuery}{groupQuery}{orderQuery}{offsetQuery}";

            return new aia_core.Repository.QueryStrings { CountQuery = countQuery, ListQuery = listQuery };
        }
    }
}