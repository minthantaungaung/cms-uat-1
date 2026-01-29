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

namespace aia_core.Repository.Cms
{
    public interface IBankRepository 
    {
        Task<ResponseModel<PagedList<BankResponse>>> List(int page, int size, string? bankname = null);
        Task<ResponseModel<BankResponse>> Get(Guid bankid);
        Task<ResponseModel<string>> Create(CreateBankRequest model);
        Task<ResponseModel<string>> Update(UpdateBankRequest model);
        Task<ResponseModel<string>> ChangeStatus(UpdateBankStatusRequest model);
    }

    public class BankRepository: BaseRepository, IBankRepository
    {
        private readonly IRecurringJobRunner recurringJobRunner;
        public BankRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Context> unitOfWork, IRecurringJobRunner recurringJobRunner)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.recurringJobRunner = recurringJobRunner;

        }

        #region "Create"
        public async Task<ResponseModel<string>> Create(CreateBankRequest model)
        {
            try
            {
                bool IsBankNameExist =  unitOfWork.GetRepository<Entities.Bank>().Query(
                    expression: r => r.BankName == model.BankName && r.IsDelete == false && r.IsActive == true).Any();
                if(IsBankNameExist)
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E203);
                }

                bool IsBankCodeExist =  unitOfWork.GetRepository<Entities.Bank>().Query(
                    expression: r => r.BankCode == model.BankCode && r.IsDelete == false && r.IsActive == true).Any();
                if(IsBankCodeExist)
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E204);
                }

                if(model.DigitType == EnumBankDigitType.Range && (model.DigitStartRange==null || model.DigitEndRange==null))
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);
                }
                else if(model.DigitType == EnumBankDigitType.OR && model.DigitCustom.Count!=2)
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);
                }
                else if(model.DigitType == EnumBankDigitType.Custom && model.DigitCustom.Count==0)
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);
                }
                

                var entity = new Entities.Bank
                {
                    ID = Guid.NewGuid(),
                    BankName = model.BankName,
                    BankName_MM = model.BankName_MM,
                    BankCode = model.BankCode,
                    AccountType = $"{model.AccountType}",
                    DigitType = $"{model.DigitType}",
                    DigitStartRange = model.DigitStartRange,
                    DigitEndRange = model.DigitEndRange,
                    DigitCustom = model.DigitCustom?.Any() == true ? string.Join(", ", model.DigitCustom) : "",
                    CreatedDate = Utils.GetDefaultDate(),
                    CreatedBy = new Guid(GetCmsUser().ID),
                    IsActive = true,
                    IsDelete = false,
                    IlBankCode = model.ILBankCode,
                };

                #region #upload-cover-image
                if (model.BankLogo != null)
                {
                    var bankLogoImageName = $"banklogo_{Utils.GetDefaultDate().Ticks}-{model.BankLogo.FileName}";
                    var result = await azureStorage.UploadAsync(bankLogoImageName, model.BankLogo);
                    entity.BankLogo = result.Code == 200 ? bankLogoImageName : null;
                }
                else
                {
                    // Read the file into a byte array
                    byte[] fileBytes = System.IO.File.ReadAllBytes("default-banklogo.png");

                    // Create an IFormFile instance
                    IFormFile defaultBankLogo = new FormFile(new MemoryStream(fileBytes), 0, fileBytes.Length, "default-banklogo.png", "default-banklogo.png");


                    
                    var defaultBankLogoName = $"defaultbanklogo_{Utils.GetDefaultDate().Ticks}-{entity.BankCode}-{defaultBankLogo.FileName}";
                    var result = await azureStorage.UploadAsync(defaultBankLogoName, defaultBankLogo);
                    entity.BankLogo = result.Code == 200 ? defaultBankLogoName : null;
                }
                #endregion

                await unitOfWork.GetRepository<Entities.Bank>().AddAsync(entity);
                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Blogs,
                        objectAction: EnumObjectAction.Create,
                        objectId: entity.ID,
                        objectName: entity.BankName,
                        newData: System.Text.Json.JsonSerializer.Serialize(new BankResponse(entity, GetFileFullUrl)));

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0);   
            }
            catch (System.Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
        }
        #endregion

        #region "Get Detail"
        public async Task<ResponseModel<BankResponse>> Get(Guid bankid)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Bank>().Query(x=> x.ID == bankid).FirstOrDefaultAsync();
                if(entity == null) return errorCodeProvider.GetResponseModel<BankResponse>(ErrorCode.E400);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Bank,
                        objectAction: EnumObjectAction.View,
                        objectId: entity.ID,
                        objectName: entity.BankName);

                var response = new BankResponse(entity, GetFileFullUrl);

                if (string.IsNullOrEmpty(response.BankLogo))
                {
                    var defaultCmsImage = unitOfWork.GetRepository<Entities.DefaultCmsImage>().Query().FirstOrDefault();                   

                    if (!string.IsNullOrEmpty(defaultCmsImage?.image_url))
                    {
                        response.BankLogo = GetFileFullUrl(defaultCmsImage.image_url);
                    }
                }

                return errorCodeProvider.GetResponseModel<BankResponse>(ErrorCode.E0, response);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<BankResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region "Get List"
        public async Task<ResponseModel<PagedList<BankResponse>>> List(int page, int size, string? bankname = null)
        {
             try
            {
                var query = unitOfWork.GetRepository<Entities.Bank>().Query(
                    expression: r => r.IsDelete == false)
                    .OrderByDescending(x => x.CreatedDate)
                    .AsQueryable();

                #region #filters
                if(!string.IsNullOrWhiteSpace(bankname))
                {
                    query = query.Where(r => r.BankName.Contains(bankname));
                }
                #endregion

                int totalCount = 0;
                totalCount = await query.CountAsync();

                var source = (from r in query.AsEnumerable()
                              select new BankResponse(r, GetFileFullUrl))
                              .Skip((page - 1) * size).Take(size).ToList();

                source?.ForEach(item =>
                {
                    if (string.IsNullOrEmpty(item.BankLogo))
                    {
                        var defaultCmsImage = unitOfWork.GetRepository<Entities.DefaultCmsImage>().Query().FirstOrDefault();

                        if (!string.IsNullOrEmpty(defaultCmsImage?.image_url))
                        {
                            item.BankLogo = GetFileFullUrl(defaultCmsImage.image_url);
                        }
                        
                    }
                    
                });

                var data = new PagedList<BankResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Bank,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<BankResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<BankResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        #region "Update
        public async Task<ResponseModel<string>> Update(UpdateBankRequest model)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Bank>().Query(
                    expression: r => r.ID == model.Id && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                bool IsBankNameExist =  unitOfWork.GetRepository<Entities.Bank>().Query(
                    expression: r => r.ID != model.Id && r.BankName == model.BankName && r.IsDelete == false && r.IsActive == true).Any();
                if(IsBankNameExist)
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E203);
                }

                bool IsBankCodeExist =  unitOfWork.GetRepository<Entities.Bank>().Query(
                    expression: r => r.ID != model.Id && r.BankCode == model.BankCode && r.IsDelete == false && r.IsActive == true).Any();
                if(IsBankCodeExist)
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E204);
                }

                var oldBank = entity;
                var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

                if(model.DigitType == EnumBankDigitType.Range && (model.DigitStartRange==null || model.DigitEndRange==null))
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);
                }
                else if(model.DigitType == EnumBankDigitType.OR && model.DigitCustom.Count!=2)
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);
                }
                else if(model.DigitType == EnumBankDigitType.Custom && model.DigitCustom.Count==0)
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);
                }

                entity.BankName = model.BankName;
                entity.BankName_MM = model.BankName_MM;
                entity.BankCode = model.BankCode;
                entity.AccountType = $"{model.AccountType}";
                entity.DigitType = $"{model.DigitType}";
                entity.DigitStartRange = model.DigitStartRange;
                entity.DigitEndRange = model.DigitEndRange;
                if(model.DigitCustom !=null && model.DigitCustom.Count() > 0)
                {
                    entity.DigitCustom = string.Join(", ", model.DigitCustom);
                }
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.UpdatedBy = new Guid(GetCmsUser().ID);

                entity.IlBankCode = model.ILBankCode;


                #region #upload-cover-image
                if (model.BankLogo != null)
                {
                    var bankLogoImageName = $"banklogo_{Utils.GetDefaultDate().Ticks}-{model.BankLogo.FileName}";
                    var result = await azureStorage.UploadAsync(bankLogoImageName, model.BankLogo);
                    entity.BankLogo = result.Code == 200 ? bankLogoImageName : null;
                }
                
                #endregion

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Blogs,
                        objectAction: EnumObjectAction.Update,
                        objectId: entity.ID,
                        objectName: entity.BankName,
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

        #region "Update Status"
        public async Task<ResponseModel<string>> ChangeStatus(UpdateBankStatusRequest model)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Bank>().Query(
                    expression: r => r.ID == model.Id && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                var oldBank = entity;
                var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

                entity.IsActive = model.IsActive;
                entity.UpdatedDate = Utils.GetDefaultDate();
                entity.UpdatedBy = new Guid(GetCmsUser().ID);

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Blogs,
                        objectAction: EnumObjectAction.ChangeStatus,
                        objectId: entity.ID,
                        objectName: entity.BankName,
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