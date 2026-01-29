using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.RecurringJobs;
using aia_core.Services;
using aia_core.UnitOfWork;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;
using System.Net;
using System.Transactions;
using static Azure.Core.HttpHeader;
using System.Reflection;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.Extensions.DependencyInjection;
using Azure;
using DocumentFormat.OpenXml.Bibliography;

namespace aia_core.Repository.Cms
{
    public interface IPropositionRepository
    {
        Task<ResponseModel<DuplicateCheckResponse>> CheckDuplicateByName(string nameEn, string Mm, string type, string Id);
        Task<ResponseModel<List<PropositionResponse>>> GetAll();
        Task<ResponseModel<PagedList<PropositionResponse>>> List(int page, int size, string? name, Guid[]? category, EnumPropositionBenefit[]? eligibility);
        Task<ResponseModel<PropositionResponse>> Get(Guid propositionId);
        Task<ResponseModel<PropositionResponse>> Create(CreatePropositionRequest model);
        Task<ResponseModel<PropositionResponse>> Update(UpdatePropositionRequest model);
        Task<ResponseModel<PropositionResponse>> Delete(Guid propositionId);
        Task<ResponseModel<PropositionResponse>> Order(Guid propositionId, int sort);
        Task<ResponseModel<PropositionResponse>> Order(List<PropositionOrderRequest> model);

        Task<ResponseModel<List<PropositionCategoryResponse>>> Categories();
        Task<ResponseModel<PagedList<PropositionCategoryResponse>>> Categories(int page, int size, string? name);
        Task<ResponseModel<PropositionCategoryResponse>> Categories(CreatePropositionCategoryRequest model);
        Task<ResponseModel<PropositionCategoryResponse>> Categories(UpdatePropositionCategoryRequest model);
        Task<ResponseModel<PropositionCategoryResponse>> DeleteCategories(Guid id);

        Task<ResponseModel<PropositionCategoryResponse>> GetCategory(Guid categoryId);
        Task<ResponseModel<PagedList<PropositionRequestModelResponse>>> GetRequestList(
            int page, 
            int size, 
            string? name, 
            DateTime? startdate, 
            DateTime? enddate,
            Guid?[] partners,
            Guid?[] categories,
            string[]? membertype,
            string[]? memberrole
            );

        Task<ResponseModel<PagedList<PartnerItemResponse>>> GetPartnerItemList(int page, int size);
        Task<ResponseModel<List<string>>> GetRoleItemList();
    }
    public class PropositionRepository: BaseRepository, IPropositionRepository
    {
        private readonly IRecurringJobRunner recurringJobRunner;
        private readonly INotificationService notificationService;
        private readonly IServiceProvider serviceProvider;

        public PropositionRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
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
        public async Task<ResponseModel<List<PropositionResponse>>> GetAll()
        {
            try
            {
                var entities = await unitOfWork.GetRepository<Entities.Proposition>().Query(expression: r => r.IsDelete == false,
                    order: o => o.OrderBy(x => x.Sort))
                    .Include(x => x.PropositionCategory)
                    .ToListAsync();

                var data = entities.Select(s => new PropositionResponse(s, GetFileFullUrl)).ToList();

                data = data
                    .OrderByDescending(x => x.CategoryCreatedOn)
                    .ToList();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Propositions,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<List<PropositionResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<PropositionResponse>>(ErrorCode.E500);
            }
        }
        #endregion

        #region #list
        public async Task<ResponseModel<PagedList<PropositionResponse>>> List(int page, int size, string? name, Guid[]? category, EnumPropositionBenefit[]? eligibility)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.Proposition>().Query(
                    expression: r => r.IsDelete == false,
                    include: i => i.Include(x=> x.PropositionAddresses)
                    .Include(x=> x.PropositionBenefits)
                    .Include(x=> x.PropositionBranches)
                    .Include(x=> x.PropositionCategory),
                    order: o => o.OrderByDescending(x=> x.CreatedDate));

                #region #filters
                if(!string.IsNullOrEmpty(name))
                {
                    query = query.Where(r=> r.NameEn.Contains(name));
                }
                if(category != null && category.Any())
                {
                    query = query.Where(x => category.ToList().Contains(x.PropositionCategoryId.Value));
                }
                if(eligibility != null && eligibility.Any()) 
                {
                    var  eligibilities = new List<string>();
                    foreach (var eligibilityEnum in eligibility)
                    {
                        eligibilities.Add(eligibilityEnum.ToString());
                    }


                    query = query.Where(x => eligibilities.Contains(x.Eligibility));
                }
                #endregion

                int totalCount = 0;
                totalCount = await query.CountAsync();

                var source = (from r in query.AsEnumerable()
                              select new PropositionResponse(r, GetFileFullUrl))
                              .Skip((page - 1) * size).Take(size).ToList();

                var data = new PagedList<PropositionResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Propositions,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<PropositionResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<PropositionResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        #region #details
        public async Task<ResponseModel<PropositionResponse>> Get(Guid propositionId)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Proposition>().Query(
                    expression: r => r.IsDelete == false && r.Id == propositionId,
                    include: i => i.Include(x => x.PropositionAddresses)
                    .Include(x => x.PropositionBenefits)
                    .Include(x => x.PropositionBranches).OrderBy(o=>o.Sort)
                    .Include(x=> x.PropositionCategory)
                    ).FirstOrDefaultAsync();
                if (entity == null)
                    return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E400);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Propositions,
                        objectAction: EnumObjectAction.View,
                        objectId: entity.Id,
                        objectName: entity.NameEn);
                return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E0, new PropositionResponse(entity, GetFileFullUrl));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #create
        public async Task<ResponseModel<PropositionResponse>> Create(CreatePropositionRequest model)
        {
            try
            {
                Entities.PropositionCategory propositionCategory = null;

                var CmsUserId = new Guid(GetCmsUser().ID);
                var isTestUser = unitOfWork.GetRepository<TestUsers>()
                    .Query(x => x.UserId == CmsUserId)
                    .Any();


                if (model.Type == EnumPropositionType.partner)
                {
                    propositionCategory = await unitOfWork.GetRepository<Entities.PropositionCategory>().Query(
                    expression: r => r.Id == model.PropositionCategoryId && r.IsDelete == false).FirstOrDefaultAsync();
                }
                
                if(propositionCategory == null
                    && model.Type == EnumPropositionType.partner) return new ResponseModel<PropositionResponse> { Code = 400, Message = "Invalid proposition category." };


                if (propositionCategory == null && model.Type == EnumPropositionType.aia)
                {
                    propositionCategory = unitOfWork.GetRepository<Entities.PropositionCategory>()
                        .Query(x => x.IsAiaBenefitCategory == true && x.IsDelete == false).FirstOrDefault();

                    Console.WriteLine("Aia benefits existing " + propositionCategory?.Id + ", " + propositionCategory?.NameEn);

                    if (propositionCategory == null)
                    {
                        var fileName = "aia-benefits.png";
                        var logoImage = "";

                        #region #upload-logo-image
                        
                        string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileName);
                        
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            var logoImageFile = GetFormFile(fileStream);
                            var logoImageName = $"{Utils.GetDefaultDate().Ticks}-{fileName}";
                            var result = await azureStorage.UploadAsync(logoImageName, logoImageFile);
                            logoImage = result.Code == 200 ? logoImageName : null;
                        }

                        #endregion

                        #region #upload-background-image

                        #endregion

                        var category = new PropositionCategory()
                        {
                            Id = Guid.NewGuid(),
                            NameEn = "AIA benefits",
                            NameMm = "AIA benefits",
                            IconImage = logoImage,
                            BackgroundImage = logoImage,
                            IsAiaBenefitCategory = true,
                            IsDelete = false,
                        };

                        unitOfWork.GetRepository<Entities.PropositionCategory>()
                            .Add(category);

                        unitOfWork.SaveChanges();

                        propositionCategory = category;
                        Console.WriteLine("Aia benefits new " + propositionCategory?.Id + ", " + propositionCategory?.NameEn);
                    }                    
                }


                var maxSort = await unitOfWork.GetRepository<Entities.Proposition>().Query(expression: r => r.IsDelete == false)
                    .Select(s => s.Sort ?? 0).MaxAsync(x => (int?)x) ?? 0;
                var entity = new Entities.Proposition
                {
                    Id = Guid.NewGuid(),
                    PropositionCategoryId = propositionCategory?.Id,
                    Type = $"{model.Type}",
                    NameEn = model.NameEn,
                    NameMm = model.NameMm,
                    DescriptionEn = model.DescriptionEn,
                    DescriptionMm = model.DescriptionMm,
                    HotlineType = $"{model.HotlineType}",
                    HotlineNumber = model.HotlineNumber,
                    HotlineButtonTextEn = model.HotlineButtonTextEn,
                    HotlineButtonTextMm = model.HotlineButtonTextMm,
                    PartnerPhoneNumber = model.PartnerPhoneNumber,
                    PartnerWebsiteLink = model.PartnerWebsiteLink,
                    PartnerFacebookUrl = model.PartnerFacebookUrl,
                    PartnerInstagramUrl = model.PartnerInstagramUrl,
                    PartnerTwitterUrl = model.PartnerTwitterUrl,
                    Eligibility = $"{model.Eligibility}",
                    AddressLabel = model.AddressLabel,
                    AddressLabelMm = model.AddressLabelMm,
                    Sort = maxSort++,
                    IsActive = isTestUser == true? false : true,
                    IsDelete = false,
                    CreatedDate = Utils.GetDefaultDate(),

                    #region #CashlessClaim
                    AllowToShowCashlessClaim = model.AllowToShowCashlessClaim,
                    CashlessClaimProcedureInfo = $"{model.CashlessClaimProcedureInfo}",

                    #endregion

                };

                #region #upload-logo-image
                if (model.LogoImage != null)
                {
                    var logoImageName = $"{Utils.GetDefaultDate().Ticks}-{model.LogoImage.FileName}";
                    var result = await azureStorage.UploadAsync(logoImageName, model.LogoImage);
                    entity.LogoImage = result.Code == 200 ? logoImageName : null;
                }
                #endregion

                #region #upload-background-image
                if (model.BackgroudImage != null)
                {
                    var backgroudImageName = $"{Utils.GetDefaultDate().Ticks}-{model.BackgroudImage.FileName}";
                    var result = await azureStorage.UploadAsync(backgroudImageName, model.BackgroudImage);
                    entity.BackgroudImage = result.Code == 200 ? backgroudImageName : null;
                }
                #endregion

                #region #label-addresses
                if (model.Type == EnumPropositionType.aia
                    && model.Addresses != null
                    && model.Addresses.Any())
                    entity.PropositionAddresses = model.Addresses.Select(add => new PropositionAddress {
                        Id = Guid.NewGuid(),
                        PropositionId = entity.Id,
                        NameEn = add.NameEn,
                        NameMm = add.NameMm,
                        PhoneNumberEn = add.PhoneNumberEn,
                        PhoneNumberMm = add.PhoneNumberMm,
                        AddressEn = add.AddressEn,
                        AddressMm = add.AddressMm,
                        Latitude = add.Latitude,
                        Longitude = add.Longitude,
                    }).ToList();
                #endregion

                #region #benefits-and-grouping
                if(model.PropositionBenefits != null
                    && model.PropositionBenefits.Any())
                {
                    entity.PropositionBenefits = model.PropositionBenefits.Select(s=> new PropositionBenefit {
                        Id = Guid.NewGuid(),
                        PropositionId = entity.Id,
                        Type = $"{s.Type}",
                        NameEn = s.NameEn,
                        NameMm = s.NameMm,
                        GroupNameEn = s.GroupNameEn,
                        GroupNameMm = s.GroupNameMm,
                        Sort = s.Sort,
                    }).ToList();
                }
                #endregion

                #region #branches
                int branchSort = 1;
                if(model.Type == EnumPropositionType.partner 
                    && model.PartnerBranches != null
                    && model.PartnerBranches.Any())
                {
                    entity.PropositionBranches = model.PartnerBranches.Select(s => new PropositionBranch {
                        Id = Guid.NewGuid(),
                        PropositionId = entity.Id,
                        NameEn = s.NameEn,
                        NameMm = s.NameMm
                    }).ToList();
                }
                foreach (var pb in entity.PropositionBranches)
                {
                    pb.Sort = branchSort;
                    branchSort++;
                }
                #endregion

                //using (var scope = new TransactionScope(
                //        scopeOption: TransactionScopeOption.Suppress,
                //        scopeTimeout: TimeSpan.FromMinutes(3),
                //        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                //        ))
                //{
                    await unitOfWork.GetRepository<Entities.Proposition>().AddAsync(entity);
                    await unitOfWork.SaveChangesAsync();

                    #region #Noti

                if(entity.IsActive == true && isTestUser == false)
                {
                    Task.Run(() =>
                    {
                        using (var scope = serviceProvider.CreateScope())
                        {
                            var _notiService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                            _ = _notiService.SendNewSetupItemNoti(EnumSystemNotiType.Proposition, entity.Id.ToString());

                        }
                    });
                }
                    
                #endregion

                //scope.Complete();
                    

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Propositions,
                        objectAction: EnumObjectAction.Create,
                        objectId: entity.Id,
                        objectName: entity.NameEn,
                        newData: System.Text.Json.JsonSerializer.Serialize(new PropositionResponse(entity, GetFileFullUrl)));
                    return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E0, new PropositionResponse(entity, GetFileFullUrl));
                //}
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #update
        public async Task<ResponseModel<PropositionResponse>> Update(UpdatePropositionRequest model)
        {
            try
            {
                Entities.PropositionCategory propositionCategory = null;

                

                if (model.Type == EnumPropositionType.partner)
                {
                    propositionCategory = await unitOfWork.GetRepository<Entities.PropositionCategory>().Query(
                    expression: r => r.Id == model.PropositionCategoryId && r.IsDelete == false).FirstOrDefaultAsync();
                }

                if (propositionCategory == null
                    && model.Type == EnumPropositionType.partner) return new ResponseModel<PropositionResponse> { Code = 400, Message = "Invalid proposition category." };

                var entity = await unitOfWork.GetRepository<Entities.Proposition>().Query(
                    expression: r => r.Id == model.Id && r.IsDelete == false,
                    include: i => i.Include(x=> x.PropositionAddresses)
                    .Include(x=> x.PropositionBenefits)
                    .Include(x=> x.PropositionBranches)
                    ).FirstOrDefaultAsync();
                if (entity == null)
                    return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E400);

                var oldData = System.Text.Json.JsonSerializer.Serialize(new PropositionResponse(entity, GetFileFullUrl));
                if (entity.PropositionBranches != null
                    && entity.PropositionBranches.Any())
                    unitOfWork.GetRepository<Entities.PropositionBranch>().Delete(entity.PropositionBranches);

                if (entity.PropositionAddresses != null
                    && entity.PropositionAddresses.Any())
                    unitOfWork.GetRepository<Entities.PropositionAddress>().Delete(entity.PropositionAddresses);

                if (entity.PropositionBenefits != null
                    && entity.PropositionBenefits.Any())
                    unitOfWork.GetRepository<Entities.PropositionBenefit>().Delete(entity.PropositionBenefits);

                entity.PropositionCategoryId = propositionCategory?.Id?? entity.PropositionCategoryId;
                entity.Type = $"{model.Type}";
                entity.NameEn = model.NameEn;
                entity.NameMm = model.NameMm;
                entity.DescriptionEn = model.DescriptionEn;
                entity.DescriptionMm = model.DescriptionMm;
                entity.HotlineType = $"{model.HotlineType}";
                entity.HotlineNumber = model.HotlineNumber;
                entity.HotlineButtonTextEn = model.HotlineButtonTextEn;
                entity.HotlineButtonTextMm = model.HotlineButtonTextMm;
                entity.PartnerPhoneNumber = model.PartnerPhoneNumber;
                entity.PartnerWebsiteLink = model.PartnerWebsiteLink;
                entity.PartnerFacebookUrl = model.PartnerFacebookUrl;
                entity.PartnerInstagramUrl = model.PartnerInstagramUrl;
                entity.PartnerTwitterUrl = model.PartnerTwitterUrl;
                entity.Eligibility = $"{model.Eligibility}";
                entity.AddressLabel = model.AddressLabel;
                entity.AddressLabelMm = model.AddressLabelMm;

                #region #CashlessClaim
                entity.AllowToShowCashlessClaim = model.AllowToShowCashlessClaim ?? entity.AllowToShowCashlessClaim;
                entity.CashlessClaimProcedureInfo = model.CashlessClaimProcedureInfo != null ?
                    $"{model.CashlessClaimProcedureInfo}" : entity.CashlessClaimProcedureInfo;
                #endregion

                #region #upload-logo-image
                if (model.LogoImage != null)
                {
                    var logoImageName = $"{Utils.GetDefaultDate().Ticks}-{model.LogoImage.FileName}";
                    var result = await azureStorage.UploadAsync(logoImageName, model.LogoImage);
                    entity.LogoImage = result.Code == 200 ? logoImageName : entity.LogoImage;
                }
                #endregion

                #region #upload-background-image
                if (model.BackgroudImage != null)
                {
                    var backgroudImageName = $"{Utils.GetDefaultDate().Ticks}-{model.BackgroudImage.FileName}";
                    var result = await azureStorage.UploadAsync(backgroudImageName, model.BackgroudImage);
                    entity.BackgroudImage = result.Code == 200 ? backgroudImageName : entity.BackgroudImage;
                }
                #endregion

                #region #label-addresses
                if (model.Type == EnumPropositionType.aia
                    && model.Addresses != null
                    && model.Addresses.Any())
                    entity.PropositionAddresses = model.Addresses.Select(add => new PropositionAddress
                    {
                        Id = Guid.NewGuid(),
                        PropositionId = entity.Id,
                        NameEn = add.NameEn,
                        NameMm = add.NameMm,
                        PhoneNumberEn = add.PhoneNumberEn,
                        PhoneNumberMm = add.PhoneNumberMm,
                        AddressEn = add.AddressEn,
                        AddressMm = add.AddressMm,
                        Latitude = add.Latitude,
                        Longitude = add.Longitude,
                    }).ToList();
                #endregion

                #region #benefits-and-grouping
                if (model.PropositionBenefits != null
                    && model.PropositionBenefits.Any())
                {
                    entity.PropositionBenefits = model.PropositionBenefits.Select(s => new PropositionBenefit
                    {
                        Id = Guid.NewGuid(),
                        PropositionId = entity.Id,
                        Type = $"{s.Type}",
                        NameEn = s.NameEn,
                        NameMm = s.NameMm,
                        GroupNameEn = s.GroupNameEn,
                        GroupNameMm = s.GroupNameMm,
                        Sort = s.Sort,
                    }).ToList();
                }
                #endregion

                #region #branches
                int branchSort = 1;
                if (model.Type == EnumPropositionType.partner
                    && model.PartnerBranches != null
                    && model.PartnerBranches.Any())
                {
                    entity.PropositionBranches = model.PartnerBranches.Select(s => new PropositionBranch
                    {
                        Id = Guid.NewGuid(),
                        PropositionId = entity.Id,
                        NameEn = s.NameEn,
                        NameMm = s.NameMm
                    }).ToList();
                }
                foreach (var pb in entity.PropositionBranches)
                {
                    pb.Sort = branchSort;
                    branchSort++;
                }
                #endregion

                using (var scope = new TransactionScope(
                        scopeOption: TransactionScopeOption.Suppress,
                        scopeTimeout: TimeSpan.FromMinutes(3),
                        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                        ))
                {
                    entity.UpdatedDate = Utils.GetDefaultDate();
                    await unitOfWork.SaveChangesAsync();
                    scope.Complete();

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Propositions,
                        objectAction: EnumObjectAction.Update,
                        objectId: entity.Id,
                        objectName: entity.NameEn,
                        oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(new PropositionResponse(entity, GetFileFullUrl)));
                    return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E0, new PropositionResponse(entity, GetFileFullUrl));
                }
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #delete
        public async Task<ResponseModel<PropositionResponse>> Delete(Guid propositionId)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Proposition>().Query(expression: r => r.Id == propositionId && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null)
                    return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E400);

                entity.IsDelete = true;
                entity.UpdatedDate = Utils.GetDefaultDate();
                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Propositions,
                        objectAction: EnumObjectAction.Delete,
                        objectId: entity.Id,
                        objectName: entity.NameEn);
                return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E0);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #order-update
        public async Task<ResponseModel<PropositionResponse>> Order(Guid propositionId, int sort)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.Proposition>().Query(
                    expression: r => r.IsDelete == false && r.Id == propositionId,
                    include: i => i.Include(x => x.PropositionAddresses)
                    .Include(x => x.PropositionBenefits)
                    .Include(x => x.PropositionBranches)
                    .Include(x => x.PropositionCategory)
                    ).FirstOrDefaultAsync();
                if (entity == null)
                    return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E400);

                var data = await unitOfWork.GetRepository<Entities.Proposition>().Query(expression: r => r.Sort == sort).ToListAsync();

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
                        objectGroup: EnumObjectGroup.Propositions,
                        objectAction: EnumObjectAction.UpdateSort);
                    return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E0, new PropositionResponse(entity, GetFileFullUrl));
                }
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #order-update
        public async Task<ResponseModel<PropositionResponse>> Order(List<PropositionOrderRequest> model)
        {
            try
            {
                if (model.Count <= 0)
                    return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E400);

                var ids = model.Select(s => s.Id).ToList();
                var entity = await unitOfWork.GetRepository<Entities.Proposition>().Query(
                    expression: r => ids.Any(id => id == r.Id) && r.IsDelete == false
                    ).ToListAsync();
                if (entity.Count != model.Count)
                    return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E400);

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
                        objectGroup: EnumObjectGroup.Propositions,
                        objectAction: EnumObjectAction.UpdateSort);
                    return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E0);
                }
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PropositionResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #list-categories
        public async Task<ResponseModel<List<PropositionCategoryResponse>>> Categories()
        {
            try
            {
                var query = await unitOfWork.GetRepository<Entities.PropositionCategory>().Query(
                    expression: r => r.IsDelete == false).ToListAsync();

                var data = query.Select(s => new PropositionCategoryResponse(s, GetFileFullUrl)).ToList();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.PropositionCategory,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<List<PropositionCategoryResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<PropositionCategoryResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        #region #paging-categories
        public async Task<ResponseModel<PagedList<PropositionCategoryResponse>>> Categories(int page, int size, string? name)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.PropositionCategory>().Query(
                    expression: r => r.IsDelete == false );

                #region #filters
                if (!string.IsNullOrEmpty(name))
                {
                    query = query.Where(r => r.NameEn.Contains(name));
                }
                #endregion

                int totalCount = 0;
                totalCount = await query.CountAsync();

                query = query.OrderBy(o=> o.IsAiaBenefitCategory).OrderBy(o=> o.CreatedOn);

                var source = (from r in query.AsEnumerable()
                              select new PropositionCategoryResponse(r, GetFileFullUrl))
                              .Skip((page - 1) * size).Take(size).ToList();

                var data = new PagedList<PropositionCategoryResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.PropositionCategory,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<PropositionCategoryResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<PropositionCategoryResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        #region #create-categories
        public async Task<ResponseModel<PropositionCategoryResponse>> Categories(CreatePropositionCategoryRequest model)
        {
            try
            {
                var category = await unitOfWork.GetRepository<Entities.PropositionCategory>().Query(expression: r => r.NameEn == model.NameEn && r.IsDelete == false).FirstOrDefaultAsync();
                if (category != null) return new ResponseModel<PropositionCategoryResponse> { Code = 400, Message = "Duplicate name." };

                var entity = new Entities.PropositionCategory 
                {
                    Id = Guid.NewGuid(),
                    NameEn = model.NameEn,
                    NameMm = model.NameMm,
                    IsDelete = false,
                    IsAiaBenefitCategory = false,
                    CreatedOn = Utils.GetDefaultDate(),
                };

                #region #upload-logo-image
                if (model.IconImage != null)
                {
                    var iconImageName = $"{Utils.GetDefaultDate().Ticks}-{model.IconImage.FileName}";
                    var result = await azureStorage.UploadAsync(iconImageName, model.IconImage);
                    entity.IconImage = result.Code == 200 ? iconImageName : null;
                }
                #endregion

                #region #upload-logo-image
                if (model.BackgroundImage != null)
                {
                    var backgroundImageName = $"{Utils.GetDefaultDate().Ticks}-{model.BackgroundImage.FileName}";
                    var result = await azureStorage.UploadAsync(backgroundImageName, model.BackgroundImage);
                    entity.BackgroundImage = result.Code == 200 ? backgroundImageName : null;
                }
                #endregion

                await unitOfWork.GetRepository<Entities.PropositionCategory>().AddAsync(entity);
                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.PropositionCategory,
                        objectAction: EnumObjectAction.Create,
                        objectId: entity.Id,
                        objectName: entity.NameEn,
                        newData: System.Text.Json.JsonSerializer.Serialize(new PropositionCategoryResponse(entity, GetFileFullUrl)));
                return errorCodeProvider.GetResponseModel<PropositionCategoryResponse>(ErrorCode.E0, new PropositionCategoryResponse(entity, GetFileFullUrl));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PropositionCategoryResponse>(ErrorCode.E400);
            }
        }
        #endregion

        #region #update-categories
        public async Task<ResponseModel<PropositionCategoryResponse>> Categories(UpdatePropositionCategoryRequest model)
        {
            try
            {
                var category = await unitOfWork.GetRepository<Entities.PropositionCategory>().Query(expression: r => r.Id != model.Id && r.NameEn == model.NameEn && r.IsDelete == false).FirstOrDefaultAsync();
                if (category != null) return new ResponseModel<PropositionCategoryResponse> { Code = 400, Message = "Duplicate name." };

                var entity = await unitOfWork.GetRepository<Entities.PropositionCategory>().Query(expression: r => r.Id == model.Id).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<PropositionCategoryResponse>(ErrorCode.E400);

                var oldData = System.Text.Json.JsonSerializer.Serialize(new PropositionCategoryResponse(entity, GetFileFullUrl));
                entity.NameEn = model.NameEn;
                entity.NameMm = model.NameMm;
                entity.UpdatedOn = Utils.GetDefaultDate();

                #region #upload-logo-image
                if (model.IconImage != null)
                {
                    var iconImageName = $"{Utils.GetDefaultDate().Ticks}-{model.IconImage.FileName}";
                    var result = await azureStorage.UploadAsync(iconImageName, model.IconImage);
                    entity.IconImage = result.Code == 200 ? iconImageName : entity.IconImage;
                }
                #endregion

                #region #upload-logo-image
                if (model.BackgroundImage != null)
                {
                    var backgroundImageName = $"{Utils.GetDefaultDate().Ticks}-{model.BackgroundImage.FileName}";
                    var result = await azureStorage.UploadAsync(backgroundImageName, model.BackgroundImage);
                    entity.BackgroundImage = result.Code == 200 ? backgroundImageName : entity.BackgroundImage;
                }
                #endregion

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.PropositionCategory,
                        objectAction: EnumObjectAction.Update,
                        objectId: entity.Id,
                        objectName: entity.NameEn,
                        oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(new PropositionCategoryResponse(entity, GetFileFullUrl)));
                return errorCodeProvider.GetResponseModel<PropositionCategoryResponse>(ErrorCode.E0, new PropositionCategoryResponse(entity, GetFileFullUrl));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PropositionCategoryResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #delete-categories
        public async Task<ResponseModel<PropositionCategoryResponse>> DeleteCategories(Guid id)
        {
            try
            {
                var entity = await unitOfWork.GetRepository<Entities.PropositionCategory>().Query(expression: r => r.Id == id && r.IsDelete == false).FirstOrDefaultAsync();
                if (entity == null) return errorCodeProvider.GetResponseModel<PropositionCategoryResponse>(ErrorCode.E400);

                var hasAny = await unitOfWork.GetRepository<Entities.Proposition>().Query(expression: r => r.PropositionCategoryId == id && r.IsDelete != true).AnyAsync();
                if (hasAny) return new ResponseModel<PropositionCategoryResponse> { Code = 400, Message = $"Not allow to delete since category name \"{entity.NameEn}\" is currently applying by propositions." };

                entity.IsDelete = true;
                //unitOfWork.GetRepository<Entities.PropositionCategory>().Delete(entity);
                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.PropositionCategory,
                        objectAction: EnumObjectAction.Delete,
                        objectId: entity.Id,
                        objectName: entity.NameEn);
                return errorCodeProvider.GetResponseModel<PropositionCategoryResponse>(ErrorCode.E0, new PropositionCategoryResponse(entity, GetFileFullUrl));
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PropositionCategoryResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #proposition request list
        public async Task<ResponseModel<PagedList<PropositionRequestModelResponse>>> GetRequestList(
            int page, 
            int size, 
            string? name, 
            DateTime? startdate, 
            DateTime? enddate,
            Guid?[] partners,
            Guid?[] categories,
            string[]? membertype,
            string[]? memberrole
            )
        {
            try
            {
                // var query = unitOfWork.GetRepository<Entities.PropositionRequest>().GetAll()
                //     .Include(x=> x.Member)
                //     .Include(x=> x.PropositionBranches)
                //     .Include(x=> x.Proposition).ThenInclude(t=> t.PropositionCategory);
                
                var query = unitOfWork.GetRepository<Entities.PropositionRequest>().Query(
                    include: i => i.Include(x => x.Member)
                                .Include(x=> x.PropositionBranches)
                                .Include(x=> x.Proposition).ThenInclude(t=> t.PropositionCategory));
                    

                #region #filters
                if(!String.IsNullOrEmpty(name))
                {
                    query = query.Where(x=>x.Member.Name.Contains(name));
                }

                if(startdate!=null)
                {
                    query = query.Where(x=> x.SubmissionDate.Date >= startdate.Value.Date);
                }

                if(enddate!=null)
                {
                    query = query.Where(x=> x.SubmissionDate.Date <= enddate.Value.Date);
                }

                if(partners != null && partners.Any())
                {
                    query = query.Where(x=> partners.Contains(x.PropositionID));
                }

                if(categories != null && categories.Any())
                {
                    query = query.Where(x=> categories.Contains(x.Proposition.PropositionCategoryId));
                }

                if(membertype != null && membertype.Any())
                {
                    query = query.Where(x => membertype.Select(x => x.ToLower()).ToList().Contains(x.MemberType.ToLower()));
                }

                if(memberrole != null && memberrole.Any())
                {
                    query = query.Where(x=> memberrole.Select(x => x.ToLower()).ToList().Contains(x.MemberRole.ToLower()));
                }
                #endregion

                int totalCount = 0;
                totalCount = await query.CountAsync();
                

                var source = (from r in query.AsEnumerable()
                              select new PropositionRequestModelResponse(r, GetFileFullUrl))
                              .OrderByDescending(x=> x.SubmissionDate)
                              .Skip((page - 1) * size).Take(size).ToList();

                var data = new PagedList<PropositionRequestModelResponse>(
                    source: source,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.PropositionsRequest,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<PropositionRequestModelResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog("GetRequestList", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<PropositionRequestModelResponse>>(ErrorCode.E400);
            }
        }
        #endregion

        public IFormFile GetFormFile(FileStream fileStream)
        {
            var ms = new MemoryStream();
            fileStream.CopyTo(ms);
            return new FormFile(ms, 0, ms.Length, "", "");
        }

        public async Task<ResponseModel<PropositionCategoryResponse>> GetCategory(Guid categoryId)
        {
            try
            {
                var propositionCategory = unitOfWork.GetRepository<Entities.PropositionCategory>().Query(
                    expression: r => r.IsDelete == false && r.Id == categoryId).FirstOrDefault();

                if (propositionCategory == null)
                {
                    return errorCodeProvider.GetResponseModel<PropositionCategoryResponse>(ErrorCode.E400);
                }

                var data = new PropositionCategoryResponse(propositionCategory, GetFileFullUrl);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.PropositionCategory,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PropositionCategoryResponse>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PropositionCategoryResponse>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<DuplicateCheckResponse>> CheckDuplicateByName(string nameEn, string Mm, string type, string Id)
        {
            try
            {
                if(string.IsNullOrEmpty(type) || string.IsNullOrEmpty(nameEn) || string.IsNullOrEmpty(Mm)) { return new ResponseModel<DuplicateCheckResponse> { Code = 400, Message = "Empty nameEn or nameMm or type" }; }

                if (type?.ToLower() == "create")
                {
                    var hasNameEn = unitOfWork.GetRepository<Entities.Proposition>().Query(x => x.NameEn.ToLower() == nameEn.ToLower() && x.IsActive == true && x.IsDelete == false).Any();

                    if (hasNameEn) { return errorCodeProvider.GetResponseModel<DuplicateCheckResponse>(ErrorCode.E0, new DuplicateCheckResponse { IsDuplicate = true, By = "En" }); }


                    var hasNameMm = unitOfWork.GetRepository<Entities.Proposition>().Query(x => x.NameMm.ToLower() == Mm.ToLower() && x.IsActive == true && x.IsDelete == false).Any();

                    if (hasNameMm) { return errorCodeProvider.GetResponseModel<DuplicateCheckResponse>(ErrorCode.E0, new DuplicateCheckResponse { IsDuplicate = true, By = "Mm" }); }


                }

                if (type?.ToLower() == "update")
                {
                    var hasNameEn = unitOfWork.GetRepository<Entities.Proposition>().Query(x => x.NameEn.ToLower() == nameEn.ToLower() && x.Id != Guid.Parse(Id) && x.IsActive == true && x.IsDelete == false).Any();

                    if (hasNameEn) { return errorCodeProvider.GetResponseModel<DuplicateCheckResponse>(ErrorCode.E0, new DuplicateCheckResponse { IsDuplicate = true, By = "En" }); }


                    var hasNameMm = unitOfWork.GetRepository<Entities.Proposition>().Query(x => x.NameMm.ToLower() == Mm.ToLower() && x.Id != Guid.Parse(Id) && x.IsActive == true && x.IsDelete == false).Any();

                    if (hasNameMm) { return errorCodeProvider.GetResponseModel<DuplicateCheckResponse>(ErrorCode.E0, new DuplicateCheckResponse { IsDuplicate = true, By = "Mm" }); }

                }

                return errorCodeProvider.GetResponseModel<DuplicateCheckResponse>(ErrorCode.E0, new DuplicateCheckResponse { IsDuplicate = false, By = "" });

            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<DuplicateCheckResponse>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<PagedList<PartnerItemResponse>>> GetPartnerItemList(int page, int size)
        {
            var response = unitOfWork.GetRepository<Entities.Proposition>()
               .Query(x => x.IsActive == true && x.IsDelete == false)
               .OrderBy(x => x.NameEn)
               .Select(x => new PartnerItemResponse { PartnerId = x.Id, PartnerName = x.NameEn })
               .ToList();

            var count = unitOfWork.GetRepository<Entities.Proposition>()
               .Query(x => x.IsActive == true && x.IsDelete == false)
               .Count();


            var data = new PagedList<PartnerItemResponse>(
                    source: response,
                    totalCount: count,
                    pageNumber: page,
                    pageSize: size);

            return errorCodeProvider.GetResponseModel<PagedList<PartnerItemResponse>>(ErrorCode.E0, data);
        }

        public async Task<ResponseModel<List<string>>> GetRoleItemList()
        {
            var response = unitOfWork.GetRepository<Entities.PropositionRequest>()
                .Query()
                .Select(x => x.MemberRole)
                .ToList()
                ?.Distinct()
                .ToList();



            return errorCodeProvider.GetResponseModel<List<string>>(ErrorCode.E0, response);
        }
    }
}
