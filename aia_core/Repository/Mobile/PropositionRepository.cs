using aia_core.Entities;
using aia_core.Model.Mobile.Response;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Newtonsoft.Json;
using aia_core.Model.Mobile.Request;

namespace aia_core.Repository.Mobile
{
    public interface IPropositionRepository
    {
        ResponseModel<List<PropositionGroupListResponse>> GetList();
        ResponseModel<PropositionDetailResponse> GetDetail(string id);
        ResponseModel<string> SubmitRequest(PropositionRequestModel model);
    }
    public class PropositionRepository : BaseRepository, IPropositionRepository
    {
        #region "const"

        private readonly ICommonRepository commonRepository;

        public PropositionRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork,
            ICommonRepository commonRepository)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.commonRepository = commonRepository;
        }
        #endregion

        #region "Get List"
        public ResponseModel<List<PropositionGroupListResponse>> GetList()
        {
            try
            {
                var memberGuid = GetMemberIDFromToken();
                if (CheckAuthorization(memberGuid, null)?.Proposition == false)
                    return new ResponseModel<List<PropositionGroupListResponse>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var memberType = GetMemberType()?.ToString().ToLower();
                
                var propositionList = unitOfWork.GetRepository<Proposition>()
                .Query(x => x.IsActive == true && x.IsDelete == false && (x.Eligibility == memberType || x.Eligibility == EnumPropositionBenefit.both.ToString()))
                .Include(i=> i.PropositionCategory)
                .Include(i=>i.PropositionBenefits)      
                .OrderBy(i => i.PropositionCategory.CreatedOn)
                .ToList();   
                
                //TODO

                List<PropositionGroupListResponse> groupListResponse = new List<PropositionGroupListResponse>();
                
                foreach (var item in propositionList.DistinctBy(x => x.PropositionCategoryId).Select(x => x.PropositionCategory))
                {
                    var propositionGroup = new PropositionGroupListResponse();
                    propositionGroup.CategoryID = item.Id;
                    propositionGroup.CategoryName_EN = item.NameEn;
                    propositionGroup.CategoryName_MM = item.NameMm;
                    propositionGroup.CreatedOn = item.CreatedOn;
                    propositionGroup.IsAiaBenefitCategory = item.IsAiaBenefitCategory;

                    propositionGroup.list = (from c in propositionList.Where(x => x.PropositionCategoryId == item.Id)
                                             .OrderBy(x => x.PropositionCategory.NameEn).ThenBy(x => x.Sort)
                                             select new PropositionsResponse
                            {
                                Type = c.Type,
                                Eligibility = c.Eligibility,
                                ID = c.Id.ToString(),
                                Name_EN = c.NameEn,
                                Name_MM = c.NameMm,
                                BackgroudImage = string.IsNullOrEmpty(c.BackgroudImage) ? c.BackgroudImage : commonRepository.GetFileFullUrl(EnumFileType.Proposition, c.BackgroudImage),
                                LogoImage = string.IsNullOrEmpty(c.LogoImage) ? c.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Proposition, c.LogoImage),
                                Sort = c.Sort,
                                TotalBenefits = c.PropositionBenefits.Where(x => (x.Type == memberType || x.Type == EnumPropositionBenefit.both.ToString())).Count()
                            }).ToList();

                    groupListResponse.Add(propositionGroup);
                }

                //09-07-2024
                #region #CountBenenfits 
                groupListResponse?.ForEach(group =>
                {
                    group.list?.ForEach(item  => 
                    {
                        var benefitList = unitOfWork.GetRepository<Entities.PropositionBenefit>()
                        .Query(x => x.PropositionId == new Guid(item.ID) && (x.Type == memberType || x.Type == EnumPropositionBenefit.both.ToString()))
                        .ToList();

                        var benefitCount = 0;                        

                        benefitList?
                        .GroupBy(g => g.GroupNameEn)
                        .ToList()?.ForEach(g => 
                        {
                            if (g.FirstOrDefault()?.GroupNameEn != null && g.FirstOrDefault()?.GroupNameEn != "null")
                            {
                                benefitCount += 1;
                            }
                            else
                            {
                                benefitCount += g.Select(s => s.NameEn).Count();
                            }
                        });

                        item.TotalBenefits = benefitCount;
                    });
                });
                #endregion

                groupListResponse = groupListResponse.OrderBy(x => x.CreatedOn).ToList();

                var finalList  = new List<PropositionGroupListResponse>();
                foreach (var item in groupListResponse)
                {
                    // if (item.CategoryName_EN != "AIA benefits")
                    // {
                    //     finalList.Add(item);
                    // }

                    if (item.IsAiaBenefitCategory != true)
                    {
                        finalList.Add(item);
                    }
                }

                // var aiaBenefits = groupListResponse.Where(x => x.CategoryName_EN == "AIA benefits").FirstOrDefault();
                var aiaBenefits = groupListResponse.Where(x => x.IsAiaBenefitCategory == true).FirstOrDefault();
                if(aiaBenefits != null)
                {
                    finalList.Add(aiaBenefits);
                }

                return errorCodeProvider.GetResponseModel<List<PropositionGroupListResponse>>(ErrorCode.E0, finalList);
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<PropositionGroupListResponse>>(ErrorCode.E400);
            }

        }
        #endregion

        #region "Get Detail"
        public ResponseModel<PropositionDetailResponse> GetDetail(string id)
        {
            try
            {
                var memberGuid = GetMemberIDFromToken();
                if (CheckAuthorization(memberGuid, null)?.Proposition == false)
                    return new ResponseModel<PropositionDetailResponse> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var memberType = GetMemberType()?.ToString().ToLower();

                var data = unitOfWork.GetRepository<Proposition>()
                .Query(x => x.IsActive == true && x.IsDelete == false && x.Id == new Guid(id))
                .FirstOrDefault();

                if (data == null)
                {
                    var responseModel = errorCodeProvider.GetResponseModel<PropositionDetailResponse>(ErrorCode.E0);
                    responseModel.Message = "No proposition found.";

                    return responseModel;
                }

                PropositionDetailResponse response = new PropositionDetailResponse();
                response.ID = data.Id;
                response.Name_EN = data.NameEn;
                response.Name_MM = data.NameMm;
                response.Description_EN = data.DescriptionEn;
                response.Description_MM = data.DescriptionMm;
                response.HotlineType = data.HotlineType;
                response.PartnerPhoneNumber = data.PartnerPhoneNumber;
                response.PartnerWebsiteLink = data.PartnerWebsiteLink;
                response.PartnerFacebookUrl = data.PartnerFacebookUrl;
                response.PartnerInstagramUrl = data.PartnerInstagramUrl;
                response.PartnerTwitterUrl = data.PartnerTwitterUrl;
                response.HotlineButtonTextEn = data.HotlineButtonTextEn;
                response.HotlineButtonTextMm = data.HotlineButtonTextMm;
                response.HotlineNumber = data.HotlineNumber;

                response.BackgroudImage = string.IsNullOrEmpty(data.BackgroudImage) ? data.BackgroudImage : commonRepository.GetFileFullUrl(EnumFileType.Proposition, data.BackgroudImage);
                response.LogoImage = string.IsNullOrEmpty(data.LogoImage) ? data.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Proposition, data.LogoImage);

                response.Eligibility = data.Eligibility;
                response.Type = data.Type;

                #region #CashlessClaimConfig

                response.AllowToShowCashlessClaim = data.AllowToShowCashlessClaim;
                response.CashlessClaimProcedureInfo = data.CashlessClaimProcedureInfo;

                var cashlessClaimConfig = unitOfWork.GetRepository<Entities.CashlessClaimConfig>().Query().FirstOrDefault();

                if(cashlessClaimConfig != null)
                {
                    response.cashlessClaimInfo = new CashlessClaimInfoResponse
                    {
                        local = new CashlessClaimInfo
                        {
                            TitleEn = cashlessClaimConfig.LocalTitleEn,
                            TitleMm = cashlessClaimConfig.LocalTitleMm,
                            DescriptionEn = cashlessClaimConfig.LocalDescriptionEn,
                            DescriptionMm = cashlessClaimConfig.LocalDescriptionMm,
                            ButtonTextEn = cashlessClaimConfig.LocalButtonTextEn,
                            ButtonTextMm = cashlessClaimConfig.LocalButtonTextMm,
                            Deeplink = cashlessClaimConfig.LocalDeeplink,
                        },
                        overseas = new CashlessClaimInfo
                        {
                            TitleEn = cashlessClaimConfig.OverseasTitleEn,
                            TitleMm = cashlessClaimConfig.OverseasTitleMm,
                            DescriptionEn = cashlessClaimConfig.OverseasDescriptionEn,
                            DescriptionMm = cashlessClaimConfig.OverseasDescriptionMm,
                            ButtonTextEn = cashlessClaimConfig.OverseasButtonTextEn,
                            ButtonTextMm = cashlessClaimConfig.OverseasButtonTextMm,
                            Deeplink = cashlessClaimConfig.OverseasDeeplink,
                        }
                    };
                }
                #endregion

                var benefits = unitOfWork.GetRepository<PropositionBenefit>()
                .Query(x=> x.PropositionId == data.Id && (x.Type == memberType || x.Type == EnumPropositionBenefit.both.ToString()))
                .OrderBy(x => x.Sort)
                .ToList();

                List<PropositionBenefitDetailGroup> benefitList = new List<PropositionBenefitDetailGroup>();
                foreach (var g in benefits.GroupBy(g=>g.GroupNameEn))
                {
                    PropositionBenefitDetailGroup ben = new PropositionBenefitDetailGroup();

                    if (g.FirstOrDefault()?.GroupNameEn != null && g.FirstOrDefault()?.GroupNameEn != "null")
                    {
                        ben.GroupName_EN = g.FirstOrDefault().GroupNameEn;
                    }
                    else
                    {
                        ben.GroupName_EN = "";
                    }


                    if (g.FirstOrDefault()?.GroupNameMm != null && g.FirstOrDefault()?.GroupNameMm != "null")
                    {
                        ben.GroupName_MM = g.FirstOrDefault().GroupNameMm;
                    }
                    else
                    {
                        ben.GroupName_MM = "";
                    }

                   
                    
                    ben.benefits_EN = g.Select(s=>s.NameEn).ToList();
                    ben.benefits_MM = g.Select(s=>s.NameMm).ToList();

                    benefitList.Add(ben);
                }
                response.benefits = benefitList;

                var branchList = unitOfWork.GetRepository<PropositionBranch>()
                .Query(x=>x.PropositionId == data.Id)
                .OrderBy(o=>o.Sort).ToList();
                
                List<PropositonBranchResponse> branchResponses = new List<PropositonBranchResponse>();
                foreach (var item in branchList)
                {
                    branchResponses.Add(new PropositonBranchResponse{
                        Branch_ID = item.Id,
                        Name_EN = item.NameEn,
                        Name_MM = item.NameMm
                    });
                }
                response.branchs = branchResponses;

                var addressList = unitOfWork.GetRepository<PropositionAddress>()
                .Query(x=>x.PropositionId == data.Id)
                .ToList();
                
                List<PropositonAddressResponse> addressResponses = new List<PropositonAddressResponse>();
                foreach (var item in addressList)
                {
                    addressResponses.Add(new PropositonAddressResponse{
                        Name_EN = item.NameEn,
                        Name_MM = item.NameMm,
                        PhoneNumber_EN = item.PhoneNumberEn,
                        PhoneNumber_MM = item.PhoneNumberMm,
                        Longitude = item.Longitude,
                        Latitude = item.Latitude,
                        Address_EN = item.AddressEn,
                        Address_MM=item.AddressMm,

                    });
                }
                response.address = addressResponses;
                response.AddressLabel = data?.AddressLabel;
                response.AddressLabelMm = data?.AddressLabelMm;

                var relatedPropositionList = new List<PropositionsResponse>();

                var propositions = unitOfWork.GetRepository<Proposition>()
                .Query(x => x.IsActive == true && x.IsDelete == false && x.Id != new Guid(id) && x.PropositionCategoryId == data.PropositionCategoryId
                && (x.Eligibility == memberType || x.Eligibility == EnumPropositionBenefit.both.ToString()))
                .Include(x => x.PropositionBenefits)
                .OrderBy(x => x.Sort)
                .ToList();

                foreach (var proposition in propositions)
                {
                    var propositionsResponse = new PropositionsResponse()
                    {
                        Eligibility = proposition.Eligibility,
                        ID = proposition.Id.ToString(),
                        Name_EN = proposition.NameEn,
                        Name_MM = proposition.NameMm,
                        BackgroudImage = string.IsNullOrEmpty(proposition.BackgroudImage) ? proposition.BackgroudImage : commonRepository.GetFileFullUrl(EnumFileType.Proposition, proposition.BackgroudImage),
                        LogoImage = string.IsNullOrEmpty(proposition.LogoImage) ? proposition.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Proposition, proposition.LogoImage),
                        Sort= proposition.Sort,
                        Type = proposition.Type,

                        TotalBenefits = proposition.PropositionBenefits
                        .Where(x => (x.Type == memberType || x.Type == EnumPropositionBenefit.both.ToString())).Count(),
                    };

                    relatedPropositionList.Add(propositionsResponse);
                }

                relatedPropositionList?.ForEach(item => 
                {
                    var benefitList = unitOfWork.GetRepository<Entities.PropositionBenefit>()
                        .Query(x => x.PropositionId == new Guid(item.ID) && (x.Type == memberType || x.Type == EnumPropositionBenefit.both.ToString()))
                        .ToList();

                    var benefitCount = 0;

                    benefitList?
                    .GroupBy(g => g.GroupNameEn)
                    .ToList()?.ForEach(g =>
                    {
                        if (g.FirstOrDefault()?.GroupNameEn != null && g.FirstOrDefault()?.GroupNameEn != "null")
                        {
                            benefitCount += 1;
                        }
                        else
                        {
                            benefitCount += g.Select(s => s.NameEn).Count();
                        }
                    });

                    item.TotalBenefits = benefitCount;
                });

                response.relatedPropositions = relatedPropositionList;

                return errorCodeProvider.GetResponseModel<PropositionDetailResponse>(ErrorCode.E0, response);
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PropositionDetailResponse>(ErrorCode.E400);
            }

        }
        #endregion

        #region "Submit Request"
        public ResponseModel<string> SubmitRequest(PropositionRequestModel model)
        {
            try
            {
                Guid? memberID = commonRepository.GetMemberIDFromToken();

                PropositionRequest entity = new PropositionRequest();
                entity.ID = Guid.NewGuid();
                entity.MemberID = memberID;
                entity.MemberType = GetMemberTypeByID(memberID).ToString();

                List<string> clientNoList = GetClientNoListByIdValue(memberID);

                if (clientNoList!=null && clientNoList.Count()>0)
                {
                    entity.ClientNo = clientNoList.FirstOrDefault();

                    var _VipClientNo = unitOfWork.GetRepository<Entities.Client>().Query(x => x.VipFlag == "Y" && clientNoList.Contains(x.ClientNo))
                        .Select(x => x.ClientNo)
                        .FirstOrDefault();

                    
                    if(!string.IsNullOrEmpty(_VipClientNo))
                    {
                        entity.ClientNo = _VipClientNo;
                    }

                }

                entity.MemberRole = GetPolicyHolderOrInsuredPerson(memberID);

                entity.PropositionID = model.PropositionID;
                entity.AppointmentDate = model.AppointmentDate;
                entity.AppointmentSpecialist = model.AppointmentSpecialist;
                entity.BranchID = model.BranchID;
                entity.Benefits = String.Join("|", model.Benefits);
                entity.SubmissionDate = Utils.GetDefaultDate();

                unitOfWork.GetRepository<Entities.PropositionRequest>().Add(entity);
                unitOfWork.SaveChanges();

                try
                {
                    var data = unitOfWork.GetRepository<PropositionRequest>()
                    .Query(x => x.ID == entity.ID).Include(i=>i.Member).ThenInclude(t=>t.MemberClients)
                    .Include(i=>i.Proposition).ThenInclude(t=> t.PropositionBranches)
                    .FirstOrDefault();

                    var receiverEmail = unitOfWork.GetRepository<AppConfig>().GetAll().FirstOrDefault().Proposition_Request_Receiver;

                    var _clientNoList = GetClientNoListByIdValue(GetMemberIDFromToken());
                    var policyNo = unitOfWork.GetRepository<Policy>()
                        .Query(x => (_clientNoList.Contains(x.PolicyHolderClientNo) || _clientNoList.Contains(x.InsuredPersonClientNo))
                        && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                        && x.PremiumStatus != EnumPremiumStatus.PU.ToString()
                        && x.PolicyIssueDate != null
                        )
                        .OrderBy(x => x.PolicyIssueDate)
                        .Select(x => x.PolicyNo)
                        .FirstOrDefault();

                    Utils.SendPropositonRequestEmail(receiverEmail,data,policyNo,unitOfWork);
                }
                catch (System.Exception ex)
                {
                    MobileErrorLog("Proposition Request Send Email",ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                }

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0);
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);
            }
        }
        #endregion
    }
}
