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
using aia_core.Model.Cms.Response.PartialDisability;
using aia_core.Model.Cms.Request.PartialDisability;
using System.Linq;
using System.Data;

namespace aia_core.Repository.Cms
{
    public interface IPartialDisabilityRepository
    {
        Task<ResponseModel<PagedList<PartialDisabilityResponse>>> List(int page, int size, string? name = null, List<string>? productCodes = null);
        Task<ResponseModel<PartialDisabilityResponse>> Get(Guid id);
        Task<ResponseModel<string>> Create(CreatePartialDisabilityRequest model);
        Task<ResponseModel<string>> Update(UpdatePartialDisabilityRequest model);
        Task<ResponseModel<string>> ChangeStatus(ChangeStatusRequest model);
        Task<ResponseModel<string>> Delete(Guid id);
    }

    public class PartialDisabilityRepository : BaseRepository, IPartialDisabilityRepository
    {
        #region "const"
        private readonly IRecurringJobRunner recurringJobRunner;
        public PartialDisabilityRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Context> unitOfWork, IRecurringJobRunner recurringJobRunner)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.recurringJobRunner = recurringJobRunner;

        }
        #endregion

        #region "get list"
        public async Task<ResponseModel<PagedList<PartialDisabilityResponse>>> List(int page, int size, string? name = null, List<string>? productCodes = null)
        {
            try
            {
                var queryStrings = PrepareListQuery(page, size, name, productCodes);

                var count = unitOfWork.GetRepository<GetCountByRawQuery>()
                    .FromSqlRaw(queryStrings?.CountQuery, null, CommandType.Text)
                    .FirstOrDefault();

                var list = unitOfWork.GetRepository<PartialDisabilityResponse>()
                    .FromSqlRaw(queryStrings.ListQuery, null, CommandType.Text)
                    .ToList();

                Console.WriteLine($"queryStrings?.CountQuery => {queryStrings?.CountQuery} queryStrings.ListQuery => {queryStrings.ListQuery}");

                list?.ForEach(item =>
                {
                    var productGuidList = unitOfWork.GetRepository<Entities.PartialDisabilityProduct>()
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

                var data = new PagedList<PartialDisabilityResponse>(
                    source: list,
                    totalCount: count?.SelectCount ?? 0,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.PartialDisability,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<PartialDisabilityResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<PagedList<PartialDisabilityResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        #region "get"
        public async Task<ResponseModel<PartialDisabilityResponse>> Get(Guid id)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.PartialDisability>().Query(x => x.ID == id).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<PartialDisabilityResponse>(ErrorCode.E400);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.PartialDisability,
                        objectAction: EnumObjectAction.View,
                        objectId: entity.ID,
                        objectName: entity.Name);

                var productIdList = unitOfWork.GetRepository<Entities.PartialDisabilityProduct>()
                     .Query(x => x.DisabiltiyId == id)
                     .Select(x => $"{x.ProductId}")
                     .ToList();

                var response = new PartialDisabilityResponse(entity);

                if (productIdList?.Any() == true)
                {
                    response.ProductCodeList = productIdList;
                }

                return errorCodeProvider.GetResponseModel<PartialDisabilityResponse>(ErrorCode.E0, response);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<PartialDisabilityResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region "create"
        public async Task<ResponseModel<string>> Create(CreatePartialDisabilityRequest model)
        {
            try
            {
                bool isNameExist = unitOfWork.GetRepository<Entities.PartialDisability>().Query(x => x.Name == model.Name && x.IsDelete == false).Any();
                if (isNameExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E201);

                bool isCodeExist = unitOfWork.GetRepository<Entities.PartialDisability>().Query(x => x.Code == model.Code && x.IsDelete == false).Any();
                if (isCodeExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E202);

                var entity = new Entities.PartialDisability
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

                await unitOfWork.GetRepository<Entities.PartialDisability>().AddAsync(entity);

                model.ProductCodeList?.ForEach(productCode =>
                {
                    unitOfWork.GetRepository<Entities.PartialDisabilityProduct>()
                    .Add(new PartialDisabilityProduct
                    {
                        Id = Guid.NewGuid() ,
                        DisabiltiyId = entity.ID,
                        ProductId = Guid.Parse(productCode),
                        CreatedOn = Utils.GetDefaultDate(),
                        IsDeleted = false,
                    });
                });

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.PartialDisability,
                        objectAction: EnumObjectAction.Create,
                        objectId: entity.ID,
                        objectName: entity.Name,
                        newData: System.Text.Json.JsonSerializer.Serialize(new PartialDisabilityResponse(entity)));

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
        public async Task<ResponseModel<string>> Update(UpdatePartialDisabilityRequest model)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.PartialDisability>().Query(
                    expression: r => r.ID == model.ID && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                bool isNameExist = unitOfWork.GetRepository<Entities.PartialDisability>().Query(x => x.Name == model.Name && x.IsDelete == false && x.ID != model.ID).Any();
                if (isNameExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E201);

                bool isCodeExist = unitOfWork.GetRepository<Entities.PartialDisability>().Query(x => x.Code == model.Code && x.IsDelete == false && x.ID != model.ID).Any();
                if (isCodeExist) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E202);

                var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

                entity.Name = model.Name;
                entity.Name_MM = model.Name_MM;
                entity.Code = model.Code;
                entity.IsActive = model.IsActive;
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.UpdatedBy = new Guid(GetCmsUser().ID);

                if(model.ProductCodeList?.Any() == true)
                {
                    //Delete OldList
                    var oldList = unitOfWork.GetRepository<Entities.PartialDisabilityProduct>()
                        .Query(x => x.DisabiltiyId == model.ID)
                        .ToList();
                    unitOfWork.GetRepository<Entities.PartialDisabilityProduct>().Delete(oldList);

                    //Add NewList
                    model.ProductCodeList?.ForEach(productCode =>
                    {
                        unitOfWork.GetRepository<Entities.PartialDisabilityProduct>()
                        .Add(new PartialDisabilityProduct
                        {
                            Id = Guid.NewGuid(),
                            DisabiltiyId = entity.ID,
                            ProductId = Guid.Parse(productCode),
                            CreatedOn = Utils.GetDefaultDate(),
                            IsDeleted = false,
                        });
                    });
                }

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.PartialDisability,
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
                var entity = await unitOfWork.GetRepository<Entities.PartialDisability>().Query(
                    expression: r => r.ID == model.ID && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

                entity.IsActive = model.IsActive;
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.UpdatedBy = new Guid(GetCmsUser().ID);

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.PartialDisability,
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
                var entity = await unitOfWork.GetRepository<Entities.PartialDisability>().Query(
                    expression: r => r.ID == id && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

                entity.IsDelete = true;
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.UpdatedBy = new Guid(GetCmsUser().ID);

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.PartialDisability,
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

        private aia_core.Repository.QueryStrings PrepareListQuery(int page, int size, string? name = null, List<string>? productCodes = null)
        {
            #region #CountQuery
            var countQuery = @"SELECT COUNT(Distinct(PartialDisability.ID)) AS SelectCount ";
            var asQuery = @"";
            #endregion

            #region #DataQuery
            var dataQuery = @"SELECT 
                            PartialDisability.ID AS ID,
                            PartialDisability.Name AS Name,
                            PartialDisability.Name_MM AS Name_MM,
                            PartialDisability.Code AS Code,
                            PartialDisability.IsActive AS IsActive ";
            #endregion

            #region #FromQuery
            var fromQuery = @"FROM 
                                PartialDisability
                            LEFT JOIN 
                                PartialDisabilityProduct ON PartialDisabilityProduct.DisabiltiyId = PartialDisability.ID
                            LEFT JOIN 
                                Product ON Product.Product_ID = PartialDisabilityProduct.ProductId ";
            #endregion

            #region #GroupQuery

            var groupQuery = @"Group by PartialDisability.ID,
                                PartialDisability.Name,
                                PartialDisability.Name_MM,
                                PartialDisability.Code,
                                PartialDisability.IsActive ";


            var groupQueryForCount = @"";
            #endregion

            #region #OrderQuery
            var orderQuery = @"ORDER BY PartialDisability.Name ASC ";
            var orderQueryForCount = @" ";
            #endregion



            #region #FilterQuery

            var filterQuery = @"WHERE 
                                (PartialDisability.IsActive = 1 AND PartialDisability.IsDelete = 0)";
                                ////AND (Product.Is_Active = 1 AND Product.Is_Delete = 0) ";

            if (!string.IsNullOrEmpty(name))
            {
                filterQuery += $@"AND (PartialDisability.Name LIKE '%{name}%' OR PartialDisability.Name_MM LIKE '%{name}%') ";
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