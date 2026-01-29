using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Reflection;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace aia_core.Repository.Cms
{
    public interface IGeneralRepository
    {
        Task<ResponseModel<MasterDataResponse>> GetMasterData();
        Task<ResponseModel<AppVersionResponse>> AppVersion();
        Task<ResponseModel<AppVersionResponse>> AppVersion(AppVersionRequest model);
        Task<ResponseModel<AppConfigResponse>> AppConfig();
        Task<ResponseModel<AppConfigResponse>> AppConfig(AppConfigRequest model);
        Task<ResponseModel<List<SampleDocumentResponseModel>>> UploadSampleDoc(IFormFile doc, EnumClaimDoc claimDocType);
        Task<ResponseModel<List<SampleDocumentResponseModel>>> DeleteSampleDoc(Guid id);
        Task<ResponseModel<MaintenanceResponse>> GetMaintenance();
        Task<ResponseModel<string>> UpdateMaintenance(MaintenanceRequest model);
        Task<ResponseModel<CoastSystemDateResponse>> GetCoastSystemDate();
        Task<ResponseModel<string>> UpdateCoastSystemDate(CoastSystemDateRequest model);

    }
    public class GeneralRepository:BaseRepository, IGeneralRepository
    {
        public GeneralRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Context> unitOfWork)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {

        }

        public async Task<ResponseModel<MasterDataResponse>> GetMasterData()
        {
            try
            {
                

                var propositoryCategories = await unitOfWork.GetRepository<Entities.PropositionCategory>()
                    .Query(expression: r => r.IsDelete == false && r.IsAiaBenefitCategory != true, order: o => o.OrderBy(x => x.CreatedOn)).ToListAsync();
                
                var aiaPropositoryCategories = await unitOfWork.GetRepository<Entities.PropositionCategory>()
                    .Query(expression: r => r.IsDelete == false && r.IsAiaBenefitCategory == true, order: o => o.OrderBy(x => x.CreatedOn)).FirstOrDefaultAsync();
                
                propositoryCategories.Add(aiaPropositoryCategories);
                
                var coverages = await unitOfWork.GetRepository<Entities.Coverage>().Query(expression: r => r.IsDelete == false, order: o => o.OrderBy(x => x.CoverageNameEn)).ToListAsync();
                var products = await unitOfWork.GetRepository<Entities.Product>().Query(expression: r => r.IsDelete == false, order: o => o.OrderBy(x => x.TitleEn)).ToListAsync();

                var blogFeatureCount = await unitOfWork.GetRepository<Entities.Blog>().Query(x => x.IsDelete == false && x.IsFeature == true).CountAsync();
                return errorCodeProvider.GetResponseModel<MasterDataResponse>(ErrorCode.E0, new MasterDataResponse
                {
                    PropositionCategories = propositoryCategories.Select(s => new PropositionCategoryMasterData
                    {
                        Id = s.Id,
                        NameEn = s.NameEn,
                        NameMm = s.NameMm,
                        IconImage = s.IconImage,
                        BackgroundImage = s.BackgroundImage,
                    }).ToArray(),
                    Coverages = coverages.Select(s => new CoverageMasterData
                    {
                        CoverageId = s.CoverageId,
                        CoverageNameEn = s.CoverageNameEn,
                        CoverageNameMm = s.CoverageNameMm,
                        CoverageIcon = s.CoverageIcon,
                    }).ToArray(),
                    Products = products.Select(s => new ProductMasterData
                    {
                        ProductId = s.ProductId,
                        TitleEn = s.TitleEn,
                        TitleMm = s.TitleMm,
                        LogoImage = s.LogoImage,
                    }).ToArray(),
                    Blog = new BlogMasterData { FeaturedCount = blogFeatureCount }
                });
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<MasterDataResponse>(ErrorCode.E500);
            }
            
        }

        public async Task<ResponseModel<AppVersionResponse>> AppVersion()
        {
            try
            {
                var appVersion = await unitOfWork.GetRepository<Entities.AppVersion>().Query().FirstOrDefaultAsync();
                if (appVersion == null) return errorCodeProvider.GetResponseModel<AppVersionResponse>(ErrorCode.E400);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.AppVersion,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<AppVersionResponse>(ErrorCode.E0, new AppVersionResponse(appVersion));
            }
            catch (Exception ex)
            {
                return errorCodeProvider.GetResponseModel<AppVersionResponse>(ErrorCode.E500);
            }
            
        }

        public async Task<ResponseModel<AppVersionResponse>> AppVersion(AppVersionRequest model)
        {
            try
            {
                var appVersion = await unitOfWork.GetRepository<Entities.AppVersion>().Query().FirstOrDefaultAsync();
                if (appVersion == null) return errorCodeProvider.GetResponseModel<AppVersionResponse>(ErrorCode.E400);

                var oldData = System.Text.Json.JsonSerializer.Serialize(new AppVersionResponse(appVersion));
                appVersion.MinimumAndroidVersion = model.MinimumAndroidVersion ?? appVersion.MinimumAndroidVersion;
                appVersion.LatestAndroidVersion = model.LatestAndroidVersion ?? appVersion.LatestAndroidVersion;
                appVersion.MinimumIosVersion = model.MinimumIosVersion ?? appVersion.MinimumIosVersion;
                appVersion.LatestIosVersion = model.LatestIosVersion ?? appVersion.LatestIosVersion;

                appVersion.UpdatedDate = Utils.GetDefaultDate();
                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.AppVersion,
                        objectAction: EnumObjectAction.Update,
                        oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(new AppVersionResponse(appVersion)));
                return errorCodeProvider.GetResponseModel<AppVersionResponse>(ErrorCode.E0, new AppVersionResponse(appVersion));
            }
            catch (Exception ex)
            {
                return errorCodeProvider.GetResponseModel<AppVersionResponse>(ErrorCode.E500);
            }
            
        }

        public async Task<ResponseModel<AppConfigResponse>> AppConfig()
        {
            try
            {
                var appConfig = await unitOfWork.GetRepository<Entities.AppConfig>().Query().FirstOrDefaultAsync();
                if (appConfig == null) return errorCodeProvider.GetResponseModel<AppConfigResponse>(ErrorCode.E400);

                var data = new AppConfigResponse(appConfig);

                var claimDocTypes = unitOfWork.GetRepository<Entities.ClaimDocType>()
                    .Query()
                    .OrderBy(x => x.Sort)
                    .ToList();

                var sampleDocumentList = new List<SampleDocumentResponseModel>();

                claimDocTypes?.ForEach(claimDoc => 
                {
                    var sampleDocuments = unitOfWork.GetRepository<Entities.InsuranceClaimDocument>()
                    .Query(x => x.IsActive == true && x.IsDeleted == false && x.DocTypeName == claimDoc.Name)
                    .OrderByDescending(x => x.CreatedOn)
                    .ToList();

                    var docInfoItem = new SampleDocumentResponseModel();
                    docInfoItem.DocTypeName = claimDoc.Name;
                    docInfoItem.sampleDoc = new List<InsuranceClaimDocumentResponse>();

                    if (sampleDocuments?.Any() == true)
                    {
                        
                        
                        sampleDocuments?.ForEach(sampleDoc =>
                        {
                            docInfoItem.sampleDoc.Add(new InsuranceClaimDocumentResponse(sampleDoc, GetFileFullUrl));
                        });

                        
                    }


                    sampleDocumentList.Add(docInfoItem);


                });

                data.sampleDocuments = sampleDocumentList;


                #region #CashlessClaimConfig
                var cashlessClaimConfig = unitOfWork.GetRepository<Entities.CashlessClaimConfig>().Query().FirstOrDefault();

                if(cashlessClaimConfig != null)
                {
                    data.localCashlessClaimInfo = new CashlessClaimInfo
                    {
                        TitleEn = cashlessClaimConfig.LocalTitleEn,
                        TitleMm = cashlessClaimConfig.LocalTitleMm,
                        DescriptionEn = cashlessClaimConfig.LocalDescriptionEn,
                        DescriptionMm = cashlessClaimConfig.LocalDescriptionMm,
                        ButtonTextEn = cashlessClaimConfig.LocalButtonTextEn,
                        ButtonTextMm = cashlessClaimConfig.LocalButtonTextMm,
                        Deeplink = cashlessClaimConfig.LocalDeeplink,
                    };

                    data.overseasCashlessClaimInfo = new CashlessClaimInfo
                    {
                        TitleEn = cashlessClaimConfig.OverseasTitleEn,
                        TitleMm = cashlessClaimConfig.OverseasTitleMm,
                        DescriptionEn = cashlessClaimConfig.OverseasDescriptionEn,
                        DescriptionMm = cashlessClaimConfig.OverseasDescriptionMm,
                        ButtonTextEn = cashlessClaimConfig.OverseasButtonTextEn,
                        ButtonTextMm = cashlessClaimConfig.OverseasButtonTextMm,
                        Deeplink = cashlessClaimConfig.OverseasDeeplink,
                    };
                }
                #endregion



                return errorCodeProvider.GetResponseModel<AppConfigResponse>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                return errorCodeProvider.GetResponseModel<AppConfigResponse>(ErrorCode.E500);
            }
            
        }

        public async Task<ResponseModel<AppConfigResponse>> AppConfig(AppConfigRequest model)
        {
            try
            {
                var appConfig = await unitOfWork.GetRepository<Entities.AppConfig>().Query().FirstOrDefaultAsync();
                if (appConfig == null) return errorCodeProvider.GetResponseModel<AppConfigResponse>(ErrorCode.E400);

                var oldData = System.Text.Json.JsonSerializer.Serialize(new AppConfigResponse(appConfig));
                appConfig.SherContactNumber = model.SherContactNumber;
                appConfig.AiaCustomerCareEmail = model.AiaCustomerCareEmail;
                appConfig.AiaMyanmarWebsite = model.AiaMyanmarWebsite;
                appConfig.AiaMyanmarFacebookUrl = model.AiaMyanmarFacebookUrl;
                appConfig.AiaMyanmarInstagramUrl = model.AiaMyanmarInstagramUrl;
                appConfig.AiaMyanmarAddresses = model.AiaMyanmarAddresses;
                appConfig.ClaimTatHours = model.ClaimTatHours;
                appConfig.ServicingTatHours = model.ServicingTatHours;
                appConfig.ClaimArchiveFrequency = model.ClaimArchiveFrequency;
                appConfig.ImagingIndividualFileSizeLimit = model.ImagingIndividualFileSizeLimit;
                appConfig.ImagingTotalFileSizeLimit = model.ImagingTotalFileSizeLimit;
                appConfig.ClaimEmail = model.ClaimEmail;
                appConfig.ServicingEmail = model.ServicingEmail;
                appConfig.ServicingArchiveFrequency = model.ServicingArchiveFrequency;
                appConfig.Vitamin_Supply_Note = model.Vitamin_Supply_Note;
                appConfig.Doc_Upload_Note = model.Doc_Upload_Note;
                appConfig.Bank_Info_Upload_Note = model.Bank_Info_Upload_Note;
                appConfig.UpdatedDate = Utils.GetDefaultDate();
                appConfig.Proposition_Request_Receiver = model.Proposition_Request_Receiver;


                if(model.localCashlessClaimInfo != null || model.overseasCashlessClaimInfo != null)
                {
                    var cashlessClaimConfig = unitOfWork.GetRepository<Entities.CashlessClaimConfig>().Query().FirstOrDefault();

                    if (cashlessClaimConfig != null)
                    {
                       
                        cashlessClaimConfig.LocalTitleEn = model.localCashlessClaimInfo?.TitleEn ?? cashlessClaimConfig.LocalTitleEn;
                        cashlessClaimConfig.LocalTitleMm = model.localCashlessClaimInfo?.TitleMm ?? cashlessClaimConfig.LocalTitleMm;
                        cashlessClaimConfig.LocalDescriptionEn = model.localCashlessClaimInfo?.DescriptionEn ?? cashlessClaimConfig.LocalDescriptionEn;
                        cashlessClaimConfig.LocalDescriptionMm = model.localCashlessClaimInfo?.DescriptionMm ?? cashlessClaimConfig.LocalDescriptionMm;
                        cashlessClaimConfig.LocalButtonTextEn = model.localCashlessClaimInfo?.ButtonTextEn ?? cashlessClaimConfig.LocalButtonTextEn;
                        cashlessClaimConfig.LocalButtonTextMm = model.localCashlessClaimInfo?.ButtonTextMm ?? cashlessClaimConfig.LocalButtonTextMm;
                        cashlessClaimConfig.LocalDeeplink = model.localCashlessClaimInfo?.Deeplink ?? cashlessClaimConfig.LocalDeeplink;

                        cashlessClaimConfig.OverseasTitleEn = model.overseasCashlessClaimInfo?.TitleEn ?? cashlessClaimConfig.OverseasTitleEn;
                        cashlessClaimConfig.OverseasTitleMm = model.overseasCashlessClaimInfo?.TitleMm ?? cashlessClaimConfig.OverseasTitleMm;
                        cashlessClaimConfig.OverseasDescriptionEn = model.overseasCashlessClaimInfo?.DescriptionEn ?? cashlessClaimConfig.OverseasDescriptionEn;
                        cashlessClaimConfig.OverseasDescriptionMm = model.overseasCashlessClaimInfo?.DescriptionMm ?? cashlessClaimConfig.OverseasDescriptionMm;
                        cashlessClaimConfig.OverseasButtonTextEn = model.overseasCashlessClaimInfo?.ButtonTextEn ?? cashlessClaimConfig.OverseasButtonTextEn;
                        cashlessClaimConfig.OverseasButtonTextMm = model.overseasCashlessClaimInfo?.ButtonTextMm ?? cashlessClaimConfig.OverseasButtonTextMm;
                        cashlessClaimConfig.OverseasDeeplink = model.overseasCashlessClaimInfo?.Deeplink ?? cashlessClaimConfig.OverseasDeeplink;

                        cashlessClaimConfig.UpdatedBy = GetCmsUser()?.Name;
                        cashlessClaimConfig.UpdatedOn = Utils.GetDefaultDate();
                    }
                    else
                    {
                        unitOfWork.GetRepository<Entities.CashlessClaimConfig>().Add(new CashlessClaimConfig
                        {

                            Id = Guid.NewGuid(),
                            LocalTitleEn = model.localCashlessClaimInfo?.TitleEn,
                            LocalTitleMm = model.localCashlessClaimInfo?.TitleMm,
                            LocalDescriptionEn = model.localCashlessClaimInfo?.DescriptionEn,
                            LocalDescriptionMm = model.localCashlessClaimInfo?.DescriptionMm,
                            LocalButtonTextEn = model.localCashlessClaimInfo?.ButtonTextEn,
                            LocalButtonTextMm = model.localCashlessClaimInfo?.ButtonTextMm,
                            LocalDeeplink = model.localCashlessClaimInfo?.Deeplink,

                            OverseasTitleEn = model.overseasCashlessClaimInfo?.TitleEn,
                            OverseasTitleMm = model.overseasCashlessClaimInfo?.TitleMm,
                            OverseasDescriptionEn = model.overseasCashlessClaimInfo?.DescriptionEn,
                            OverseasDescriptionMm = model.overseasCashlessClaimInfo?.DescriptionMm,
                            OverseasButtonTextEn = model.overseasCashlessClaimInfo?.ButtonTextEn,
                            OverseasButtonTextMm = model.overseasCashlessClaimInfo?.ButtonTextMm,
                            OverseasDeeplink = model.overseasCashlessClaimInfo?.Deeplink,

                            UpdatedBy = GetCmsUser()?.Name,
                            UpdatedOn = Utils.GetDefaultDate(),
                        });
                    }
                }
                

                await unitOfWork.SaveChangesAsync();

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.AppConfig,
                        objectAction: EnumObjectAction.Update,
                oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(new AppConfigResponse(appConfig)));
                return errorCodeProvider.GetResponseModel<AppConfigResponse>(ErrorCode.E0, new AppConfigResponse(appConfig));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AppConfig Update Ex => {ex.Message} {ex.StackTrace}");
                return errorCodeProvider.GetResponseModel<AppConfigResponse>(ErrorCode.E500);
            }
            
        }

        public async Task<ResponseModel<List<SampleDocumentResponseModel>>> UploadSampleDoc(IFormFile doc, EnumClaimDoc claimDocType)
        {
            try
            {
                var docName = $"sample-{Utils.GetDefaultDate().Ticks}-{doc.FileName}";
                var result = await azureStorage.UploadAsync(docName, doc);
                //entity.CoverImage = result.Code == 200 ? coverImageName : null;
                if(result.Code == 200)
                {
                    ClaimDocType docType = await unitOfWork.GetRepository<Entities.ClaimDocType>().Query(x=> x.Name == claimDocType.ToString().Replace("_"," ")).FirstOrDefaultAsync();

                    InsuranceClaimDocument data = new InsuranceClaimDocument();
                    data.DocumentId = Guid.NewGuid();
                    data.DocTypeId = docType.Code;
                    data.DocTypeName = docType.Name;
                    data.DocumentUrl = docName;
                    data.IsActive = true;
                    data.IsDeleted = false;
                    data.CreatedOn = Utils.GetDefaultDate();
                    data.UpdatedOn = Utils.GetDefaultDate();

                    unitOfWork.GetRepository<InsuranceClaimDocument>().Add(data);
                    await unitOfWork.SaveChangesAsync();

                    await CmsAuditLog(
                        objectGroup: EnumObjectGroup.AppConfig,
                        objectAction: EnumObjectAction.Update,
                    oldData: null,
                            newData: System.Text.Json.JsonSerializer.Serialize(data));

                    List<InsuranceClaimDocument> sampleDocuments = unitOfWork.GetRepository<Entities.InsuranceClaimDocument>().Query(x=> x.IsActive == true && x.IsDeleted==false).ToList();
                    
                    var groupedDocuments = sampleDocuments
                        .GroupBy(doc => doc.DocTypeName)
                    .Select(group => new SampleDocumentResponseModel
                    {
                        DocTypeName = group.Key,
                        sampleDoc = group.Select(doc => new InsuranceClaimDocumentResponse(doc, GetFileFullUrl)).ToList()
                    })
                    .ToList();
                    return errorCodeProvider.GetResponseModel<List<SampleDocumentResponseModel>>(ErrorCode.E0,groupedDocuments);
                }
                else
                {
                    return errorCodeProvider.GetResponseModel<List<SampleDocumentResponseModel>>(ErrorCode.E500);
                }
            }
            catch (System.Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<SampleDocumentResponseModel>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<List<SampleDocumentResponseModel>>> DeleteSampleDoc(Guid id)
        {
            try
            {
                InsuranceClaimDocument document = await unitOfWork.GetRepository<Entities.InsuranceClaimDocument>().Query(x=> x.DocumentId == id).FirstOrDefaultAsync();

                var result = await azureStorage.DeleteAsync(document.DocumentUrl);
                if(result)
                {
                    document.IsDeleted = true;
                    unitOfWork.SaveChanges();

                    List<InsuranceClaimDocument> sampleDocuments = unitOfWork.GetRepository<Entities.InsuranceClaimDocument>().Query(x=> x.IsActive == true && x.IsDeleted==false).ToList();
                    
                    var groupedDocuments = sampleDocuments
                        .GroupBy(doc => doc.DocTypeName)
                    .Select(group => new SampleDocumentResponseModel
                    {
                        DocTypeName = group.Key,
                        sampleDoc = group.Select(doc => new InsuranceClaimDocumentResponse(doc, GetFileFullUrl)).ToList()
                    })
                    .ToList();
                    return errorCodeProvider.GetResponseModel<List<SampleDocumentResponseModel>>(ErrorCode.E0,groupedDocuments);
                }
                return errorCodeProvider.GetResponseModel<List<SampleDocumentResponseModel>>(ErrorCode.E500);
            }
            catch (System.Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<SampleDocumentResponseModel>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<MaintenanceResponse>> GetMaintenance()
        {
            try
            {
                var appConfig = await unitOfWork.GetRepository<Entities.AppConfig>().Query().FirstOrDefaultAsync();
                MaintenanceResponse res = new MaintenanceResponse();
                res.Maintenance_On = appConfig.Maintenance_On;
                res.Maintenance_Title = appConfig.Maintenance_Title;
                res.Maintenance_Desc = appConfig.Maintenance_Desc;

                return errorCodeProvider.GetResponseModel<MaintenanceResponse>(ErrorCode.E0, res);
            }
            catch (Exception ex)
            {
                return errorCodeProvider.GetResponseModel<MaintenanceResponse>(ErrorCode.E500);
            }
            
        }

        public async Task<ResponseModel<string>> UpdateMaintenance(MaintenanceRequest model)
        {
            try
            {
                var appConfig = await unitOfWork.GetRepository<Entities.AppConfig>().Query().FirstOrDefaultAsync();
                appConfig.Maintenance_On = model.Maintenance_On;
                appConfig.Maintenance_Title = model.Maintenance_Title;
                appConfig.Maintenance_Desc = model.Maintenance_Desc;
                unitOfWork.SaveChanges();

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0);
            }
            catch (Exception ex)
            {
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
            
        }

        public async Task<ResponseModel<CoastSystemDateResponse>> GetCoastSystemDate()
        {
            try
            {
                var appConfig = await unitOfWork.GetRepository<Entities.AppConfig>().Query().FirstOrDefaultAsync();
                CoastSystemDateResponse res = new CoastSystemDateResponse();

                res.Coast_Claim_IsSystemDate = appConfig.Coast_Claim_IsSystemDate;
                res.Coast_Claim_CustomDate = appConfig.Coast_Claim_CustomDate;
                res.Coast_Servicing_IsSystemDate = appConfig.Coast_Servicing_IsSystemDate;
                res.Coast_Servicing_CustomDate = appConfig.Coast_Servicing_CustomDate;

                return errorCodeProvider.GetResponseModel<CoastSystemDateResponse>(ErrorCode.E0, res);
            }
            catch (Exception ex)
            {
                return errorCodeProvider.GetResponseModel<CoastSystemDateResponse>(ErrorCode.E500);
            }
            
        }

        public async Task<ResponseModel<string>> UpdateCoastSystemDate(CoastSystemDateRequest model)
        {
            try
            {
                var appConfig = await unitOfWork.GetRepository<Entities.AppConfig>().Query().FirstOrDefaultAsync();
                appConfig.Coast_Claim_IsSystemDate = model.Coast_Claim_IsSystemDate;
                appConfig.Coast_Claim_CustomDate = model.Coast_Claim_CustomDate;
                appConfig.Coast_Servicing_IsSystemDate = model.Coast_Servicing_IsSystemDate;
                appConfig.Coast_Servicing_CustomDate = model.Coast_Servicing_CustomDate;
                unitOfWork.SaveChanges();

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0);
            }
            catch (Exception ex)
            {
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
            
        }
    }
}
