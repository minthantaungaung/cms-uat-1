using aia_core.Entities;
using aia_core.Model.Mobile.Response;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Newtonsoft.Json;
using aia_core.Model.Mobile.Request;
using aia_core.Model.Mobile.Request.Servicing;
using Microsoft.AspNetCore.Hosting;
using DinkToPdf;
using DinkToPdf.Contracts;
using aia_core.Model.Mobile.Response.AiaILApiResponse;
using aia_core.Model.AiaCrm;
using aia_core.Model.Mobile.Servicing.Data.Response;
using aia_core.Model.Cms.Response;
using System.Data;
using aia_core.Model.Mobile.Response.MemberPolicyResponse;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http.HttpResults;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.Metrics;
using aia_core.Repository.Cms;
using aia_core.Model;
using Azure;
using aia_core.Model.Cms.Response.Servicing;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using static Google.Apis.Requests.BatchRequest;
using FirebaseAdmin.Messaging;
using Irony;
using System.Security.Claims;
using DocumentFormat.OpenXml.Wordprocessing;

namespace aia_core.Repository.Mobile
{
    public interface IServicingDataRepository
    {
        Task<ResponseModel<List<ServiceTypeResponse>>> GetServiceTypeList(Guid? memberId = null);
        Task<ResponseModel<List<ServiceTypeResponse>>> TestGetServiceTypeList(string nrc, string otp);
        Task<ResponseModel<List<PolicyHolderResponse>>> GetPolicyHolderList();
        Task<ResponseModel<List<Model.Mobile.Servicing.Data.Response.InsuredPersonResponse>>> GetInsuredPersonList();
        Task<ResponseModel<PagedList<ServiceListResponse>>> GetServiceRequestList(ServicingListRequest model);
        Task<ResponseModel<ServiceRequestDetailResponse>> GetServiceRequestDetail(Guid? serviceId, EnumServiceType serviceType);
        Task<ResponseModel<List<OwnershipPolicy>>> GetOwnershipPolicies(EnumServiceType serviceType);

        Task<ResponseModel<List<BeneficiaryShare>>> GetOwnershipBeneficiaries();

        Task<ResponseModel<CheckPaymentFrequencyResponse>> CheckPaymentFrequecy(CheckPaymentFrequencyRequest model);

        Task<ResponseModel<CheckPaymentFrequencyResponse>> CheckPaymentFrequecyCanSwitchType(CheckPaymentFrequencyRequest model);

        Task<ResponseModel<List<BeneficiaryShare>>> GetOwnershipBeneficiariesByPolicyType(string policyNo);

        ResponseModel<List<PolicyHolderResponse>> GetPolicyHolderListForValidation();
        ResponseModel<List<Model.Mobile.Servicing.Data.Response.InsuredPersonResponse>> GetInsuredPersonListForValidation();


    }
    public class ServicingDataRepository : BaseRepository, IServicingDataRepository
    {
        #region "const"

        private readonly ICommonRepository commonRepository;
        private readonly IAiaILApiService aiaILApiService;
        private readonly IHostingEnvironment environment;
        private readonly IConverter converter;
        private readonly IAiaCmsApiService aiaCmsApiService;
        private readonly IAiaCrmApiService aiaCrmApiService;
        private readonly IPhoneNumberService phoneNumberService;
        private readonly ITemplateLoader templateLoader;

        private readonly IMemberPolicyRepository memberPolicyRepository;
        private readonly IServiceProvider serviceProvider;

        public ServicingDataRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork,
            ICommonRepository commonRepository,
            IAiaILApiService aiaILApiService,
            IHostingEnvironment environment,
            IConverter converter,
            IAiaCrmApiService aiaCrmApiService,
            IAiaCmsApiService aiaCmsApiService, IPhoneNumberService phoneNumberService, IMemberPolicyRepository memberPolicyRepository, ITemplateLoader templateLoader, IServiceProvider serviceProvider)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.commonRepository = commonRepository;
            this.aiaILApiService = aiaILApiService;
            this.environment = environment;
            this.converter = converter;
            this.aiaCmsApiService = aiaCmsApiService;
            this.aiaCrmApiService = aiaCrmApiService;

            this.phoneNumberService = phoneNumberService;
            this.memberPolicyRepository = memberPolicyRepository;
            this.templateLoader = templateLoader;
            this.serviceProvider = serviceProvider;
        }

        public async Task<ResponseModel<List<Model.Mobile.Servicing.Data.Response.InsuredPersonResponse>>> GetInsuredPersonList()
        {
            


            try
            {
                var memberId = GetMemberIDFromToken();

                if (CheckAuthorization(memberId, null)?.InsuredDetails == false)
                    return new ResponseModel<List<Model.Mobile.Servicing.Data.Response.InsuredPersonResponse>> 
                    { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var clientNoList = GetClientNoListByIdValue(memberId);

                var policyClientNoList = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => clientNoList.Contains(x.PolicyHolderClientNo)
                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                    //&& !Utils.GetPremiumStatus().Contains(x.PremiumStatus)
                    )
                    .Select(x => new {x.PolicyHolderClientNo, x.InsuredPersonClientNo })
                    .ToList()
                    .DistinctBy(x => x.InsuredPersonClientNo)
                    .ToList();

                var policyNoList = unitOfWork.GetRepository<Entities.Policy>().Query(x => clientNoList.Contains(x.PolicyHolderClientNo)
                && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                    .Select(x => x.PolicyNo)
                    .ToList();


                policyClientNoList = policyClientNoList.Distinct().ToList();

                var responseList = new List<Model.Mobile.Servicing.Data.Response.InsuredPersonResponse>();

                if (policyClientNoList != null)
                {
                    foreach (var polcyClientNo in policyClientNoList)
                    {
                        var insuredPerson = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == polcyClientNo.InsuredPersonClientNo).FirstOrDefault();
                        var policyHolder = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == polcyClientNo.PolicyHolderClientNo).FirstOrDefault();

                        if (insuredPerson != null)
                        {
                            var occupation = unitOfWork.GetRepository<Entities.Occupation>().Query(x => x.Code == insuredPerson.Occupation).FirstOrDefault();
                            var holderOccupation = unitOfWork.GetRepository<Entities.Occupation>().Query(x => x.Code == policyHolder.Occupation).FirstOrDefault();

                            SeparateCountryCodeModel? insuredPhoneNo = null;
                            if (!string.IsNullOrEmpty(insuredPerson.PhoneNo))
                                insuredPhoneNo = phoneNumberService.GetMobileNumberSeparateCode(insuredPerson.PhoneNo);

                            var response = new Model.Mobile.Servicing.Data.Response.InsuredPersonResponse
                            {
                                ClientNo = insuredPerson.ClientNo,
                                Name = insuredPerson.Name,
                                Email = insuredPerson.Email,
                                Nrc = !string.IsNullOrEmpty(insuredPerson.Nrc) ? insuredPerson.Nrc :
                                    (!string.IsNullOrEmpty(insuredPerson.PassportNo) ? insuredPerson.PassportNo : insuredPerson.Other),


                                Occupation = occupation?.Description,
                                ServiceStatus = EnumServiceStatus.Approved,
                                Dob = insuredPerson.Dob,
                                Gender = Utils.GetGender(insuredPerson.Gender),
                                Phone = insuredPhoneNo,
                                MarriedStatus = insuredPerson?.MaritalStatus,
                                FatherName = insuredPerson?.FatherName,

                                //PolicyHolder = new PolicyHolder
                                //{
                                //    ClientNo = policyHolder?.ClientNo,
                                //    Name = policyHolder?.Name,
                                //    Gender = Utils.GetGender(policyHolder?.Gender),
                                //    Nrc = !string.IsNullOrEmpty(policyHolder?.Nrc) ? policyHolder?.Nrc :
                                //    (!string.IsNullOrEmpty(policyHolder?.PassportNo) ? policyHolder?.PassportNo : policyHolder?.Other),
                                //    Occupation = holderOccupation?.Description,
                                //    Dob = policyHolder?.Dob,
                                //},
                            };

                            

                            var entity = unitOfWork.GetRepository<Entities.ServiceMain>()
                            .Query(x => x.MemberID == polcyClientNo.InsuredPersonClientNo && x.LoginMemberID == memberId
                            && x.ServiceType == EnumServiceType.InsuredPersonInformation.ToString())
                            .OrderByDescending(x => x.CreatedDate)
                            .FirstOrDefault();

                            response.ServicingId = entity?.ServiceID;

                            try
                            {
                                if (!string.IsNullOrEmpty(entity?.ServiceStatus))
                                    response.ServiceStatus = (EnumServiceStatus)Enum.Parse(typeof(EnumServiceStatus), entity.ServiceStatus);
                            }
                            catch { }
                            ///

                            var country = unitOfWork.GetRepository<Entities.Country>()
                            .Query(x => x.code == insuredPerson.Address6 || x.description == insuredPerson.Address6)
                            .FirstOrDefault();

                            var province = unitOfWork.GetRepository<Entities.Province>()
                            .Query(x => x.province_code == insuredPerson.Address5 || x.province_eng_name == insuredPerson.Address5)
                            .FirstOrDefault();


                            var district = unitOfWork.GetRepository<Entities.District>()
                            .Query(x => x.district_code == insuredPerson.Address4 || x.district_eng_name == insuredPerson.Address4)
                            .FirstOrDefault();

                            var township = unitOfWork.GetRepository<Entities.Township>()
                            .Query(x => x.township_code == insuredPerson.Address3 || x.township_eng_name == insuredPerson.Address3)
                            .FirstOrDefault();


                            var address = new AddressInfo
                            {
                                Country = new Model.Mobile.Servicing.Data.Response.Country
                                {
                                    code = country?.code ?? insuredPerson.Address6 ?? "",
                                    description = country?.description ?? insuredPerson.Address6 ?? "",
                                    bur_description = country?.bur_description ?? insuredPerson.Address6 ?? "",
                                },
                                Province = new Model.Mobile.Servicing.Data.Response.Province
                                {
                                    country_code = province?.country_code ?? insuredPerson.Address6,
                                    province_code = province?.province_code ?? insuredPerson.Address5,
                                    province_eng_name = province?.province_eng_name ?? insuredPerson.Address5,
                                    province_bur_name = province?.province_bur_name ?? insuredPerson.Address5,
                                },
                                District = new Model.Mobile.Servicing.Data.Response.District
                                {
                                    province_code = district?.province_code ?? insuredPerson.Address5,
                                    district_code = district?.district_code ?? insuredPerson.Address4,
                                    district_eng_name = district?.district_eng_name ?? insuredPerson.Address4,
                                    district_bur_name = district?.district_bur_name ?? insuredPerson.Address4,
                                },
                                Township = new Model.Mobile.Servicing.Data.Response.Township
                                {
                                    district_code = township?.district_code ?? insuredPerson.Address4,
                                    township_code = township?.township_code ?? insuredPerson.Address3,
                                    township_eng_name = township?.township_eng_name ?? insuredPerson.Address3,
                                    township_bur_name = township?.township_bur_name ?? insuredPerson.Address3,
                                }

                                ,

                                Street = insuredPerson?.Address2 ?? "",
                                BuildingOrUnitNo = insuredPerson?.Address1 ?? "",
                            };

                            response.addressInfo = address;

                            using (var scope = serviceProvider.CreateScope())
                            {
                                var servicingDataRepository = scope.ServiceProvider.GetRequiredService<IServicingDataRepository>();

                                var policyResult = servicingDataRepository.GetOwnershipPolicies(EnumServiceType.InsuredPersonInformation).Result;

                                if (policyResult != null && policyResult.Data != null)
                                {
                                    response.Policies = policyResult?.Data
                                    .Where(x => x.InsuredClientNo == polcyClientNo.InsuredPersonClientNo)
                                    .OrderBy(x => x.PolicyDate)
                                    .ToList();
                                }
                            }



                            #region #TextForAllProfiles



                            

                            var isSameInsuredAsHolder = policyClientNoList
                                .Select(x => x.PolicyHolderClientNo)
                                .ToList()
                                .Contains(polcyClientNo.InsuredPersonClientNo); 
                            
                            var isAlsoInsured = isSameInsuredAsHolder;

                            var isExistBeneficiary = unitOfWork.GetRepository<Entities.Beneficiary>()
                             .Query(x => polcyClientNo.InsuredPersonClientNo == x.BeneficiaryClientNo && policyNoList.Contains(x.PolicyNo))
                             .Any();

                            var isAlsoBeneficiary = isSameInsuredAsHolder && isExistBeneficiary;

                            var locale = templateLoader.GetLocalizationJson();
                            if (locale != null)
                            {
                                if (isAlsoInsured)
                                    response.TextForUpdateAllProfiles = new TextForUpdateAllProfiles { En = locale["HolderByInsured"]?.En, Mm = locale["HolderByInsured"]?.Mm };
                                if (isAlsoBeneficiary)
                                    response.TextForUpdateAllProfiles = new TextForUpdateAllProfiles { En = locale["BeneficiaryByInsured"]?.En, Mm = locale["BeneficiaryByInsured"]?.Mm };
                                if (isAlsoInsured && isAlsoBeneficiary)
                                    response.TextForUpdateAllProfiles = new TextForUpdateAllProfiles { En = locale["BothByInsured"]?.En, Mm = locale["BothByInsured"]?.Mm };
                            }

                            #endregion

                            responseList.Add(response);
                        }
                    }
                }               

                return errorCodeProvider.GetResponseModel<List<Model.Mobile.Servicing.Data.Response.InsuredPersonResponse>>(ErrorCode.E0, responseList);

            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<Model.Mobile.Servicing.Data.Response.InsuredPersonResponse>>(ErrorCode.E500);
            }

        }

        public async Task<ResponseModel<List<PolicyHolderResponse>>> GetPolicyHolderList()
        {
            try
            {
                var memberId = GetMemberIDFromToken();

                if (CheckAuthorization(memberId, null)?.PolicyHolderDetails == false)
                    return new ResponseModel<List<PolicyHolderResponse>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var clientNoList = GetClientNoListByIdValue(memberId);

                var holderClientNoList = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => clientNoList.Contains(x.PolicyHolderClientNo) && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                    .Select(x => x.PolicyHolderClientNo)
                    .ToList();

                holderClientNoList = holderClientNoList.Distinct().ToList();

                var policyNoList = unitOfWork.GetRepository<Entities.Policy>().Query(x => clientNoList.Contains(x.PolicyHolderClientNo)
                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                        .Select(x => x.PolicyNo)
                        .ToList();

                var responseList = new List<PolicyHolderResponse>();

                if (holderClientNoList != null)
                {
                    foreach (var holderClientNo in holderClientNoList)
                    {
                        var client = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == holderClientNo).FirstOrDefault();


                        if (client != null)
                        {

                            var occupation = unitOfWork.GetRepository<Entities.Occupation>().Query(x => x.Code == client.Occupation).FirstOrDefault();

                            SeparateCountryCodeModel? holderPhoneNo = null;
                            if (!string.IsNullOrEmpty(client.PhoneNo))
                                holderPhoneNo = phoneNumberService.GetMobileNumberSeparateCode(client.PhoneNo);

                            var response = new PolicyHolderResponse
                            {
                                ClientNo = client.ClientNo,
                                Name = client.Name,
                                Email = client.Email,
                                Nrc = !string.IsNullOrEmpty(client.Nrc) ? client.Nrc :
                                    (!string.IsNullOrEmpty(client.PassportNo) ? client.PassportNo : client.Other),


                                Occupation = occupation?.Description,
                                ServiceStatus = EnumServiceStatus.Approved,
                                Dob = client.Dob,
                                Gender = Utils.GetGender(client.Gender),
                                Phone = holderPhoneNo,

                                MarriedStatus = client?.MaritalStatus,
                                FatherName = client?.FatherName,
                            };

                            var entity = unitOfWork.GetRepository<Entities.ServiceMain>()
                            .Query(x => x.MemberID == client.ClientNo && x.LoginMemberID == memberId 
                            && x.ServiceType == EnumServiceType.PolicyHolderInformation.ToString())
                            .OrderByDescending(x => x.CreatedDate)
                            .FirstOrDefault();

                            response.ServicingId = entity?.ServiceID;

                            try
                            {
                                if (!string.IsNullOrEmpty(entity?.ServiceStatus))
                                    response.ServiceStatus = (EnumServiceStatus)Enum.Parse(typeof(EnumServiceStatus), entity.ServiceStatus);
                            }
                            catch { }


                            

                            var country = unitOfWork.GetRepository<Entities.Country>()
                            .Query(x => x.code == client.Address6 || x.description == client.Address6)
                            .FirstOrDefault();

                            var province = unitOfWork.GetRepository<Entities.Province>()
                            .Query(x => x.province_code == client.Address5 || x.province_eng_name == client.Address5)
                            .FirstOrDefault();


                            var district = unitOfWork.GetRepository<Entities.District>()
                            .Query(x => x.district_code == client.Address4 || x.district_eng_name == client.Address4)
                            .FirstOrDefault();

                            var township = unitOfWork.GetRepository<Entities.Township>()
                            .Query(x => x.township_code == client.Address3 || x.township_eng_name == client.Address3)
                            .FirstOrDefault();


                            var address = new AddressInfo
                            {
                                Country = new Model.Mobile.Servicing.Data.Response.Country
                                {
                                    code = country != null ? country.code : client.Address6,
                                    description = country != null ? country.description : client.Address6,
                                    bur_description = country != null ? country.bur_description : client.Address6,
                                },
                                Province = new Model.Mobile.Servicing.Data.Response.Province
                                {
                                    country_code = province != null ? province.country_code : client.Address6,
                                    province_code = province != null ? province.province_code : client.Address5,
                                    province_eng_name = province != null ? province.province_eng_name : client.Address5,
                                    province_bur_name = province != null ? province.province_bur_name : client.Address5,
                                },
                                District = new Model.Mobile.Servicing.Data.Response.District
                                {
                                    province_code = district != null ? district.province_code : client.Address5,
                                    district_code = district != null ? district.district_code : client.Address4,
                                    district_eng_name = district != null ? district.district_eng_name : client.Address4,
                                    district_bur_name = district != null ? district.district_bur_name : client.Address4,
                                },
                                Township = new Model.Mobile.Servicing.Data.Response.Township
                                {
                                    district_code = township != null ? township.district_code : client.Address4,
                                    township_code = township != null ? township.township_code : client.Address3,
                                    township_eng_name = township != null ? township.township_eng_name : client.Address3,
                                    township_bur_name = township != null ? township.township_bur_name : client.Address3,
                                }

                                ,

                                Street = client?.Address2 ?? "",
                                BuildingOrUnitNo = client?.Address1 ?? "",
                            };

                            response.addressInfo = address;


                            #region #TextForAllProfiles
                            var isAlsoInsured = unitOfWork.GetRepository<Entities.Policy>()
                                        .Query(x => clientNoList.Contains(x.InsuredPersonClientNo) 
                                        && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                                        .Any();

                            var isAlsoBeneficiary = unitOfWork.GetRepository<Entities.Beneficiary>()
                             .Query(x => clientNoList.Contains(x.BeneficiaryClientNo) && policyNoList.Contains(x.PolicyNo))
                             .Any();


                            var locale = templateLoader.GetLocalizationJson();
                            if (locale != null)
                            {
                                if (isAlsoInsured)
                                    response.TextForUpdateAllProfiles = new TextForUpdateAllProfiles { En = locale["Insured"]?.En, Mm = locale["Insured"]?.Mm };
                                if (isAlsoBeneficiary)
                                    response.TextForUpdateAllProfiles = new TextForUpdateAllProfiles { En = locale["Beneficiary"]?.En, Mm = locale["Beneficiary"]?.Mm };
                                if (isAlsoInsured && isAlsoBeneficiary)
                                    response.TextForUpdateAllProfiles = new TextForUpdateAllProfiles { En = locale["Both"]?.En, Mm = locale["Both"]?.Mm };
                            }

                            #endregion

                            responseList.Add(response);
                        }
                    }
                }

                return errorCodeProvider.GetResponseModel<List<PolicyHolderResponse>>(ErrorCode.E0, responseList);

            }
            catch (Exception ex)
            {
                MobileErrorLog("GetPolicyHolderList Ex", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<PolicyHolderResponse>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<List<ServiceTypeResponse>>> GetServiceTypeList(Guid? _memberId = null)
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();

                if(_memberId != null)
                    memberId = _memberId;

                var auth = CheckAuthorization(memberId, null);

                //var IsUAT = AppSettingsHelper.GetSetting("Env"); // value is UAT

                if ((auth.PolicyHolderDetails == true
                    || auth.InsuredDetails == true
                    || auth.BeneficiaryInfo == true
                    || auth.LapseReinstatement == true
                    || auth.HealthRenewal == true
                    || auth.PolicyLoan == true
                    || auth.ACP == true
                    || auth.AdhocTopup == true
                    || auth.PartialWithdrawal == true
                    || auth.PolicyPaidup == true
                    || auth.PolicySurrender == true
                    || auth.PaymentFrequency == true
                    || auth.SumAssuredChange == true
                    || auth.RefundofPayment == true
                    || auth.PolicyLoanRepayment == true) == false
                    )
                    return new ResponseModel<List<ServiceTypeResponse>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var response = new List<ServiceTypeResponse>();

                #region #CreateQueryString

                MobileErrorLog($"GetServiceTypeList authMatrix {memberId}", "", JsonConvert.SerializeObject(auth), httpContext?.HttpContext.Request.Path);

                var authMatrix = new List<string>();
                if (auth.PolicyHolderDetails) authMatrix.Add(EnumServiceType.PolicyHolderInformation.ToString());
                if (auth.InsuredDetails) authMatrix.Add(EnumServiceType.InsuredPersonInformation.ToString());
                if (auth.BeneficiaryInfo) authMatrix.Add(EnumServiceType.BeneficiaryInformation.ToString());

                //if(!string.IsNullOrEmpty(IsUAT) && IsUAT == "UAT")
                //{
                if (auth.LapseReinstatement) authMatrix.Add(EnumServiceType.LapseReinstatement.ToString());
                if (auth.HealthRenewal) authMatrix.Add(EnumServiceType.HealthRenewal.ToString());
                //}

                if (auth.PolicyLoan) authMatrix.Add(EnumServiceType.PolicyLoan.ToString());
                if (auth.ACP) authMatrix.Add(EnumServiceType.AcpLoanRepayment.ToString());
                if (auth.AdhocTopup) authMatrix.Add(EnumServiceType.AdHocTopup.ToString());
                if (auth.PartialWithdrawal) authMatrix.Add(EnumServiceType.PartialWithdraw.ToString());
                if (auth.PolicyPaidup) authMatrix.Add(EnumServiceType.PolicyPaidUp.ToString());
                if (auth.PolicySurrender) authMatrix.Add(EnumServiceType.PolicySurrender.ToString());
                if (auth.PaymentFrequency) authMatrix.Add(EnumServiceType.PaymentFrequency.ToString());
                if (auth.SumAssuredChange) authMatrix.Add(EnumServiceType.SumAssuredChange.ToString());
                if (auth.RefundofPayment) authMatrix.Add(EnumServiceType.RefundOfPayment.ToString());
                if (auth.PolicyLoanRepayment) authMatrix.Add(EnumServiceType.PolicyLoanRepayment.ToString());

                

                var authMatrixString = string.Join(",", authMatrix.Select(s => $"'{s}'"));

                #region #CountQuery
                var countQuery = @" ";
                var asQuery = @" ";
                #endregion

                #region #DataQuery
                var dataQuery = @"select
                                ServiceType.MainServiceTypeID,
                                MainServiceType.MainServiceTypeEnum,
                                MainServiceType.MainServiceTypeNameEn,
                                MainServiceType.MainServiceTypeNameMm,
                                MainServiceType.Sort as MainSort,
                                ServiceType.ServiceTypeNameEn,
                                ServiceType.ServiceTypeNameMm,
                                ServiceType.ServiceTypeEnum,
                                ServiceType.Sort as SubSort ";
                #endregion

                #region #FromQuery
                var fromQuery = @"from ServiceType
                            left join MainServiceType on MainServiceType.MainServiceTypeID = ServiceType.MainServiceTypeID ";
                #endregion

                #region #GroupQuery

                var groupQuery = @" ";
                #endregion

                #region #OrderQuery
                var orderQuery = @"order by MainServiceType.Sort, ServiceType.Sort ";
                #endregion



                #region #FilterQuery

                var filterQuery = $@"where ServiceType.ServiceTypeEnum in ({authMatrixString}) ";

                #endregion

                #region #OffsetQuery

                #endregion
                var offsetQuery = $" ";

                countQuery = $"{countQuery}{fromQuery}{filterQuery}{groupQuery}{asQuery}";
                var listQuery = $"{dataQuery}{fromQuery}{filterQuery}{groupQuery}{orderQuery}{offsetQuery}";
                #endregion


                MobileErrorLog($"GetServiceTypeList listQuery {listQuery}", "", JsonConvert.SerializeObject(auth), httpContext?.HttpContext.Request.Path);

                var list = unitOfWork.GetRepository<ServiceTypeDataResponse>()
                        .FromSqlRaw(listQuery, null, CommandType.Text)
                        .ToList();

                var serviceTypeList = new List<SubServiceType>();

                list?.ForEach((item) =>
                {
                    serviceTypeList.Add(new SubServiceType
                    {
                        ServiceType = (EnumServiceType)Enum.Parse(typeof(EnumServiceType), item.ServiceTypeEnum),
                        ServiceTypeName = item.ServiceTypeNameEn,
                        ServiceTypeNameMm = item.ServiceTypeNameMm,
                        MainServiceType = (EnumMainServiceType)Enum.Parse(typeof(EnumMainServiceType), item.MainServiceTypeEnum),
                        MainServiceTypeName = item.MainServiceTypeNameEn,
                        MainServiceTypeNameMm = item.MainServiceTypeNameMm,
                        MainSort = item.MainSort,
                        SubSort = item.SubSort,
                    }
                    );
                }
                );

                var serviceTypeListGrp = serviceTypeList?.GroupBy(x => x.MainServiceType).ToList();



                serviceTypeListGrp?.ForEach(serviceTypeListGrpItem =>
                {
                    var responseItem = new ServiceTypeResponse
                    {
                        MainServiceType = serviceTypeListGrpItem.First().MainServiceType,
                        MainServiceTypeName = serviceTypeListGrpItem.First().MainServiceTypeName,
                        MainServiceTypeNameMm = serviceTypeListGrpItem.First()?.MainServiceTypeNameMm,
                        ServiceTypeList = serviceTypeListGrpItem.ToList(),
                    };

                    response.Add(responseItem);
                });

                MobileErrorLog("GetServiceTypeList Response", "", JsonConvert.SerializeObject(response), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<ServiceTypeResponse>>(ErrorCode.E0, response);
            }
            catch (Exception ex)
            {
                MobileErrorLog("GetServiceTypeList Ex", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<ServiceTypeResponse>>(ErrorCode.E500);
            }
            
        }


        public async Task<ResponseModel<List<ServiceTypeResponse>>> TestGetServiceTypeList(string nrc, string otp)
        {
            try
            {
                if (ValidateTestEndpointsOtp(otp) == false)
                {
                    return errorCodeProvider.GetResponseModel<List<ServiceTypeResponse>>(ErrorCode.E403);
                }

                var entityMember = unitOfWork.GetRepository<Entities.Member>().
                    Query(x => (x.Nrc == nrc || x.Passport == nrc || x.Others == nrc) && x.IsActive == true && x.IsVerified == true)
                    .FirstOrDefault();

                if (entityMember == null)
                    return errorCodeProvider.GetResponseModel<List<ServiceTypeResponse>>(ErrorCode.E404);

                

                var memberId = entityMember.MemberId;

                var auth = CheckAuthorization(memberId, null);

                if ((auth.PolicyHolderDetails == true
                    || auth.InsuredDetails == true
                    || auth.BeneficiaryInfo == true
                    //|| auth.LapseReinstatement == true
                    //|| auth.HealthRenewal == true
                    || auth.PolicyLoan == true
                    || auth.ACP == true
                    || auth.AdhocTopup == true
                    || auth.PartialWithdrawal == true
                    || auth.PolicyPaidup == true
                    || auth.PolicySurrender == true
                    || auth.PaymentFrequency == true
                    || auth.SumAssuredChange == true
                    || auth.RefundofPayment == true
                    || auth.PolicyLoanRepayment == true) == false
                    )
                    return new ResponseModel<List<ServiceTypeResponse>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var response = new List<ServiceTypeResponse>();

                #region #CreateQueryString

                MobileErrorLog($"GetServiceTypeList authMatrix {memberId}", "", JsonConvert.SerializeObject(auth), httpContext?.HttpContext.Request.Path);

                var authMatrix = new List<string>();
                if (auth.PolicyHolderDetails) authMatrix.Add(EnumServiceType.PolicyHolderInformation.ToString());
                if (auth.InsuredDetails) authMatrix.Add(EnumServiceType.InsuredPersonInformation.ToString());
                if (auth.BeneficiaryInfo) authMatrix.Add(EnumServiceType.BeneficiaryInformation.ToString());
                //if (auth.LapseReinstatement) authMatrix.Add(EnumServiceType.LapseReinstatement.ToString());
                //if (auth.HealthRenewal) authMatrix.Add(EnumServiceType.HealthRenewal.ToString());
                if (auth.PolicyLoan) authMatrix.Add(EnumServiceType.PolicyLoan.ToString());
                if (auth.ACP) authMatrix.Add(EnumServiceType.AcpLoanRepayment.ToString());
                if (auth.AdhocTopup) authMatrix.Add(EnumServiceType.AdHocTopup.ToString());
                if (auth.PartialWithdrawal) authMatrix.Add(EnumServiceType.PartialWithdraw.ToString());
                if (auth.PolicyPaidup) authMatrix.Add(EnumServiceType.PolicyPaidUp.ToString());
                if (auth.PolicySurrender) authMatrix.Add(EnumServiceType.PolicySurrender.ToString());
                if (auth.PaymentFrequency) authMatrix.Add(EnumServiceType.PaymentFrequency.ToString());
                if (auth.SumAssuredChange) authMatrix.Add(EnumServiceType.SumAssuredChange.ToString());
                if (auth.RefundofPayment) authMatrix.Add(EnumServiceType.RefundOfPayment.ToString());
                if (auth.PolicyLoanRepayment) authMatrix.Add(EnumServiceType.PolicyLoanRepayment.ToString());

                //authMatrix.Add(EnumServiceType.PolicyHolderInformation.ToString());
                //authMatrix.Add(EnumServiceType.InsuredPersonInformation.ToString());
                //authMatrix.Add(EnumServiceType.BeneficiaryInformation.ToString());
                //authMatrix.Add(EnumServiceType.LapseReinstatement.ToString());
                //authMatrix.Add(EnumServiceType.HealthRenewal.ToString());
                //authMatrix.Add(EnumServiceType.PolicyLoan.ToString());
                //authMatrix.Add(EnumServiceType.AcpLoanRepayment.ToString());
                //authMatrix.Add(EnumServiceType.AdHocTopup.ToString());
                //authMatrix.Add(EnumServiceType.PartialWithdraw.ToString());
                //authMatrix.Add(EnumServiceType.PolicyPaidUp.ToString());
                //authMatrix.Add(EnumServiceType.PolicySurrender.ToString());
                //authMatrix.Add(EnumServiceType.PaymentFrequency.ToString());
                //authMatrix.Add(EnumServiceType.SumAssuredChange.ToString());
                //authMatrix.Add(EnumServiceType.RefundOfPayment.ToString());
                //authMatrix.Add(EnumServiceType.PolicyLoanRepayment.ToString());

                var authMatrixString = string.Join(",", authMatrix.Select(s => $"'{s}'"));

                #region #CountQuery
                var countQuery = @" ";
                var asQuery = @" ";
                #endregion

                #region #DataQuery
                var dataQuery = @"select
                                ServiceType.MainServiceTypeID,
                                MainServiceType.MainServiceTypeEnum,
                                MainServiceType.MainServiceTypeNameEn,
                                MainServiceType.MainServiceTypeNameMm,
                                MainServiceType.Sort as MainSort,
                                ServiceType.ServiceTypeNameEn,
                                ServiceType.ServiceTypeNameMm,
                                ServiceType.ServiceTypeEnum,
                                ServiceType.Sort as SubSort ";
                #endregion

                #region #FromQuery
                var fromQuery = @"from ServiceType
                            left join MainServiceType on MainServiceType.MainServiceTypeID = ServiceType.MainServiceTypeID ";
                #endregion

                #region #GroupQuery

                var groupQuery = @" ";
                #endregion

                #region #OrderQuery
                var orderQuery = @"order by MainServiceType.Sort, ServiceType.Sort ";
                #endregion



                #region #FilterQuery

                var filterQuery = $@"where ServiceType.ServiceTypeEnum in ({authMatrixString}) ";

                #endregion

                #region #OffsetQuery

                #endregion
                var offsetQuery = $" ";

                countQuery = $"{countQuery}{fromQuery}{filterQuery}{groupQuery}{asQuery}";
                var listQuery = $"{dataQuery}{fromQuery}{filterQuery}{groupQuery}{orderQuery}{offsetQuery}";
                #endregion


                MobileErrorLog($"GetServiceTypeList listQuery {listQuery}", "", JsonConvert.SerializeObject(auth), httpContext?.HttpContext.Request.Path);

                var list = unitOfWork.GetRepository<ServiceTypeDataResponse>()
                        .FromSqlRaw(listQuery, null, CommandType.Text)
                        .ToList();

                var serviceTypeList = new List<SubServiceType>();

                list?.ForEach((item) =>
                {
                    serviceTypeList.Add(new SubServiceType
                    {
                        ServiceType = (EnumServiceType)Enum.Parse(typeof(EnumServiceType), item.ServiceTypeEnum),
                        ServiceTypeName = item.ServiceTypeNameEn,
                        ServiceTypeNameMm = item.ServiceTypeNameMm,
                        MainServiceType = (EnumMainServiceType)Enum.Parse(typeof(EnumMainServiceType), item.MainServiceTypeEnum),
                        MainServiceTypeName = item.MainServiceTypeNameEn,
                        MainServiceTypeNameMm = item.MainServiceTypeNameMm,
                        MainSort = item.MainSort,
                        SubSort = item.SubSort,
                    }
                    );
                }
                );

                var serviceTypeListGrp = serviceTypeList?.GroupBy(x => x.MainServiceType).ToList();



                serviceTypeListGrp?.ForEach(serviceTypeListGrpItem =>
                {
                    var responseItem = new ServiceTypeResponse
                    {
                        MainServiceType = serviceTypeListGrpItem.First().MainServiceType,
                        MainServiceTypeName = serviceTypeListGrpItem.First().MainServiceTypeName,
                        MainServiceTypeNameMm = serviceTypeListGrpItem.First()?.MainServiceTypeNameMm,
                        ServiceTypeList = serviceTypeListGrpItem.ToList(),
                    };

                    response.Add(responseItem);
                });

                MobileErrorLog("GetServiceTypeList Response", "", JsonConvert.SerializeObject(response), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<ServiceTypeResponse>>(ErrorCode.E0, response);
            }
            catch (Exception ex)
            {
                MobileErrorLog("GetServiceTypeList Ex", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<ServiceTypeResponse>>(ErrorCode.E500);
            }

        }

        public async Task<ResponseModel<PagedList<ServiceListResponse>>> GetServiceRequestList(ServicingListRequest model)
        {
            try
            {
                var memberId = GetMemberIDFromToken();

                var auth = CheckAuthorization(memberId, null);

                if ((auth.PolicyHolderDetails == true
                || auth.InsuredDetails == true
                || auth.BeneficiaryInfo == true
                || auth.LapseReinstatement == true
                || auth.HealthRenewal == true
                || auth.PolicyLoan == true
                || auth.ACP == true
                || auth.AdhocTopup == true
                || auth.PartialWithdrawal == true
                || auth.PolicyPaidup == true
                || auth.PolicySurrender == true
                || auth.PaymentFrequency == true
                || auth.SumAssuredChange == true
                || auth.RefundofPayment == true
                || auth.PolicyLoanRepayment == true) == false
                    )
                    return new ResponseModel<PagedList<ServiceListResponse>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };


                #region #ServiceMain
                var query = unitOfWork.GetRepository<Entities.ServiceMain>().Query(x => x.LoginMemberID == memberId);


                if (model.ServiceType != null)
                {
                    query = query.Where(x => x.ServiceType == model.ServiceType.ToString());
                }               

                if (!string.IsNullOrEmpty(model.ServiceStatus))
                { 
                    query = query.Where(x => x.ServiceStatus == model.ServiceStatus);
                }

                var count = query.Count();

                query = query.OrderByDescending(x => x.CreatedDate);
                
                var list = query.Skip((model.Page.Value - 1) * model.Size.Value).Take(model.Size.Value).ToList()
                    .Select(data => new ServiceListResponse
                    {
                        ServiceId = data.ServiceID,
                        ServiceType = data.ServiceType,
                        TransactionDate = data.CreatedDate,
                        Status = data.ServiceStatus,
                        ServiceTypeNameEn = "",
                        ServiceTypeNameMm = "",
                    }
                    )
                    .ToList();

                MobileErrorLog("GetServiceRequestList", $"memberId => {memberId} , list => {list?.Count}", "", httpContext?.HttpContext.Request.Path);

                list?.ForEach(item =>
                {
                    var servicetype = unitOfWork.GetRepository<Entities.ServiceType>().Query(x => x.ServiceTypeEnum == item.ServiceType).FirstOrDefault();
                    item.ServiceTypeNameEn = servicetype?.ServiceTypeNameEn;
                    item.ServiceTypeNameMm = servicetype?.ServiceTypeNameMm;
                });

                #endregion

                #region #Status change Track

                #endregion

                list?.ForEach(item =>
                {
                    

                    var changedStatusList = unitOfWork.GetRepository<Entities.ServiceStatusUpdate>()
                    .Query(update => update.ServiceID == item.ServiceId)
                    .GroupBy(update => update.NewStatus)
                    .Select(group => new
                    {
                        NewStatus = group.Key,
                        CreatedDate = group.Max(update => update.CreatedDate)
                    })
                    .OrderByDescending(result => result.CreatedDate)
                    .ToList();

                    if (!(item.ServiceType == EnumServiceType.PartialWithdraw.ToString()
                    || item.ServiceType == EnumServiceType.PolicyLoan.ToString()
                    //|| item.ServiceType == EnumServiceType.PolicyPaidUp.ToString()
                    || item.ServiceType == EnumServiceType.PolicySurrender.ToString()
                    || item.ServiceType == EnumServiceType.RefundOfPayment.ToString())
                    )
                    {

                        item.ServiceStatusList?.RemoveAll(x => x.Status == "Paid");

                        if (changedStatusList != null)
                        {
                            changedStatusList.RemoveAll(x => x.NewStatus == "Paid");
                        }
                    }

                    if (changedStatusList?.Any() ?? false)
                    {

                        var changedList = changedStatusList.Select(x => x.NewStatus).ToList();

                        var matchedList = item.ServiceStatusList?.Where(x => changedList.Contains(x.Status)).ToList();



                        matchedList?.ForEach(matched =>
                        {
                            matched.IsCompleted = true;
                            matched.Remove = false;
                            matched.StatusChangedDt = changedStatusList.Where(x => x.NewStatus == matched.Status).Select(x => x.CreatedDate).FirstOrDefault();
                        });


                        item.ServiceStatusList?.RemoveAll(x => x.Remove == true);

                        #region Removed Approved

                        if (item.ServiceStatusList?.Where(x => x.Status == "Paid").Any() == true)
                        {
                            var notApprovedStatus = item.ServiceStatusList?.Where(x => x.Status == "NotApproved" && x.IsCompleted == true).FirstOrDefault();
                            if (notApprovedStatus != null)
                            {
                                var approvedStatus = item.ServiceStatusList?.Where(x => x.Status == "Approved").FirstOrDefault();
                                if (approvedStatus != null)
                                {
                                    approvedStatus.Remove = true;
                                }
                            }

                            item.ServiceStatusList?.RemoveAll(x => x.Remove == true);
                        }

                        
                        #endregion
                    }


                }
                );


                var result = new PagedList<ServiceListResponse>(
                   source: list,
                   totalCount: count,
                   pageNumber: model.Page ?? 0,
                   pageSize: model.Size ?? 0);

                return errorCodeProvider.GetResponseModel<PagedList<ServiceListResponse>>(ErrorCode.E0, result);

            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<PagedList<ServiceListResponse>>(ErrorCode.E500);
            }
        }

        #endregion


        private QueryStrings PrepareListQuery(ServicingListRequest model)
        {
            #region #CountQuery
            var countQuery = @"select count(service_tbl.ServiceId) as SelectCount ";
            var asQuery = @" ";
            #endregion

            #region #DataQuery
            var dataQuery = $@"select 
                            service_tbl.ServiceId,
                            service_tbl.ServiceType,
                            service_tbl.TransactionDate,
                            service_tbl.Status,
                            ServiceType.ServiceTypeNameEn,
                            ServiceType.ServiceTypeNameMm ";
            #endregion

            #region #FromQuery
            var fromQuery = $@"from
                            (
                            select ServicingId as ServiceId, ServicingType as ServiceType, CreatedOn as TransactionDate, Status from ServicingRequest where MemberID = '{model.MemberId}' union
                            select ID as ServiceId, 'LapseReinstatement' as ServiceType, CreatedOn as TransactionDate, Status from ServiceLapseReinstatement where MemberID = '{model.MemberId}'  union
                            select ID as ServiceId, 'HealthRenewal' as ServiceType, CreatedOn as TransactionDate, Status from ServiceHealthRenewal where MemberID = '{model.MemberId}'  union 
                            select ID as ServiceId, 'PolicyLoanRepayment' as ServiceType, CreatedOn as TransactionDate, Status from ServicePolicyLoanRepayment where MemberID = '{model.MemberId}'  union
                            select ID as ServiceId, 'AcpLoanRepayment' as ServiceType, CreatedOn as TransactionDate, Status from ServiceACPLoanRepayment where MemberID = '{model.MemberId}'  union
                            select ID as ServiceId, 'AdHocTopup' as ServiceType, CreatedOn as TransactionDate, Status from ServiceAdhocTopup where MemberID = '{model.MemberId}'  union
                            select ID as ServiceId, 'PartialWithdraw' as ServiceType, CreatedOn as TransactionDate, Status from ServicePartialWithdraw where MemberID = '{model.MemberId}'  union
                            select ID as ServiceId, 'PolicyLoan' as ServiceType, CreatedOn as TransactionDate, Status from ServicePolicyLoan where MemberID = '{model.MemberId}'  union
                            select ID as ServiceId, 'PolicyPaidUp' as ServiceType, CreatedOn as TransactionDate, Status from ServicePolicyPaidUp where MemberID = '{model.MemberId}'  union
                            select ID as ServiceId, 'PolicySurrender' as ServiceType, CreatedOn as TransactionDate, Status from ServicePolicySurrender where MemberID = '{model.MemberId}'  union
                            select ID as ServiceId, 'PaymentFrequency' as ServiceType, CreatedOn as TransactionDate, Status from ServicePaymentFrequency  where MemberID = '{model.MemberId}' union
                            select ID as ServiceId, 'SumAssuredChange' as ServiceType, CreatedOn as TransactionDate, Status from ServiceSumAssuredChange where MemberID = '{model.MemberId}' union 
                            select ID as ServiceId, 'RefundOfPayment' as ServiceType, CreatedOn as TransactionDate, Status from ServiceRefundOfPayment  where MemberID = '{model.MemberId}' 
                            ) as service_tbl
                            left join ServiceType on ServiceType.ServiceTypeEnum = service_tbl.ServiceType ";
            #endregion

            #region #GroupQuery

            var groupQuery = @" ";
            #endregion

            #region #OrderQuery
            var orderQuery = @"Order by service_tbl.TransactionDate desc ";
            #endregion



            #region #FilterQuery

            var filterQuery = @"where 1 = 1 ";            

            if (model.ServiceType != null)
            {
                filterQuery += "AND service_tbl.ServiceType = '" + model.ServiceType.ToString() + "' ";
            }

            if (!string.IsNullOrEmpty(model.ServiceStatus))
            {
                filterQuery += "AND service_tbl.Status = '" + model.ServiceStatus + "' ";
            }
            #endregion

            #region #OffsetQuery

            #endregion
            var offsetQuery = $"OFFSET {(model.Page - 1) * model.Size} ROWS FETCH NEXT {model.Size} ROWS ONLY";           

            countQuery = $"{countQuery}{fromQuery}{filterQuery}{groupQuery}{asQuery}";
            var listQuery = $"{dataQuery}{fromQuery}{filterQuery}{groupQuery}{orderQuery}{offsetQuery}";

            return new QueryStrings { CountQuery = countQuery, ListQuery = listQuery };
        }

        public async Task<ResponseModel<ServiceRequestDetailResponse>> GetServiceRequestDetail(Guid? serviceId, EnumServiceType serviceType)
        {
            MobileErrorLog("GetServiceRequestDetail => request", $"serviceId => {serviceId.ToString()}, serviceType => {serviceType.ToString()}", "", httpContext?.HttpContext.Request.Path);

            try
            {
                var memberId = GetMemberIDFromToken(); /*new Guid("4F8C8726-2D2A-47B6-842E-0A105798B627"); // */
                var auth = CheckAuthorization(memberId, null);

                if ((auth.PolicyHolderDetails == true
                || auth.InsuredDetails == true
                || auth.BeneficiaryInfo == true
                || auth.LapseReinstatement == true
                || auth.HealthRenewal == true
                || auth.PolicyLoan == true
                || auth.ACP == true
                || auth.AdhocTopup == true
                || auth.PartialWithdrawal == true
                || auth.PolicyPaidup == true
                || auth.PolicySurrender == true
                || auth.PaymentFrequency == true
                || auth.SumAssuredChange == true
                || auth.RefundofPayment == true
                || auth.PolicyLoanRepayment == true) == false
                    )
                    return new ResponseModel<ServiceRequestDetailResponse> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };


                ServiceRequestDetailResponse? response = null;

                

                if (serviceType == EnumServiceType.InsuredPersonInformation || serviceType == EnumServiceType.PolicyHolderInformation)
                {
                    var servicingRequest = unitOfWork.GetRepository<Entities.ServicingRequest>()
                        .Query(x => x.ServicingID == serviceId && x.MemberID == memberId)
                        .FirstOrDefault();

                    if (servicingRequest != null)
                    {
                        response = new ServiceRequestDetailResponse
                        {
                            ServiceId = servicingRequest.ServicingID,
                            TransactionDate = servicingRequest.CreatedOn,
                            ServiceType = servicingRequest.ServicingType,
                            Status = servicingRequest.Status,

                        };

                        var countryOld = unitOfWork.GetRepository<Entities.Country>()
                        .Query(x => x.code == servicingRequest.Country_Old)
                        .Select(x => x.description)
                        .FirstOrDefault();

                        var countryNew = unitOfWork.GetRepository<Entities.Country>()
                        .Query(x => x.code == servicingRequest.Country_New)
                        .Select(x => x.description)
                        .FirstOrDefault();


                        response.ServiceRequestDetail = new ServiceRequestDetail
                        {
                            OldNewDetail = new OldNewDetail
                            {
                                MarriedStatus = new ChangeValue { Old = Utils.GetMaritalStatus(servicingRequest?.MaritalStatus_Old), New = Utils.GetMaritalStatus(servicingRequest?.MaritalStatus_New) },
                                FatherName = new ChangeValue { Old = servicingRequest?.FatherName_Old, New = servicingRequest?.FatherName_New },
                                Phone = new ChangeValue { Old = servicingRequest?.PhoneNumber_Old, New = servicingRequest?.PhoneNumber_New },
                                Email = new ChangeValue { Old = servicingRequest?.EmailAddress_Old, New = servicingRequest?.EmailAddress_New },
                                Country = new ChangeValue { Old = countryOld ?? servicingRequest?.Country_Old, New = countryNew ?? servicingRequest?.Country_New },
                                Province = new ChangeValue { Old = servicingRequest?.Province_Old, New = servicingRequest?.Province_New },
                                Distinct = new ChangeValue { Old = servicingRequest?.Distinct_Old, New = servicingRequest?.Distinct_New },
                                Township = new ChangeValue { Old = servicingRequest?.Township_Old, New = servicingRequest?.Township_New },
                                Building = new ChangeValue { Old = servicingRequest?.Building_Old, New = servicingRequest?.Building_New },
                                Street = new ChangeValue { Old = servicingRequest?.Street_Old, New = servicingRequest?.Street_New },

                            },
                        };
                    }
                }
                #region #LapseReinstatement, HealthRenewal, PolicyLoanRepayment, AcpLoanRepayment, AdHocTopup, SumAssuredChange
                else if (serviceType == EnumServiceType.LapseReinstatement)
                {
                    var entity = unitOfWork.GetRepository<Entities.ServiceLapseReinstatement>()
                            .Query(x => x.ID == serviceId && x.MemberID == memberId)
                            .FirstOrDefault();


                    if (entity != null)
                    {
                        response = new ServiceRequestDetailResponse
                        {
                            ServiceId = entity.ID,
                            TransactionDate = entity.CreatedOn,
                            ServiceType = EnumServiceType.LapseReinstatement.ToString(),
                            Status = entity.Status,

                            ServiceRequestDetail = new ServiceRequestDetail
                            {
                                AmountDetail = new AmountDetail
                                {
                                    amount = entity.Amount,
                                    reason = entity.Reason,
                                }
                            },
                        };


                    }
                }
                else if (serviceType == EnumServiceType.HealthRenewal)
                {
                    var entity = unitOfWork.GetRepository<Entities.ServiceHealthRenewal>()
                            .Query(x => x.ID == serviceId && x.MemberID == memberId)
                            .FirstOrDefault();


                    if (entity != null)
                    {
                        response = new ServiceRequestDetailResponse
                        {
                            ServiceId = entity.ID,
                            TransactionDate = entity.CreatedOn,
                            ServiceType = EnumServiceType.HealthRenewal.ToString(),
                            Status = entity.Status,

                            ServiceRequestDetail = new ServiceRequestDetail
                            {
                                AmountDetail = new AmountDetail
                                {
                                    amount = entity.Amount,
                                    reason = entity.Reason,
                                }
                            },
                        };


                    }
                }
                else if (serviceType == EnumServiceType.PolicyLoanRepayment)
                {
                    var entity = unitOfWork.GetRepository<Entities.ServicePolicyLoanRepayment>()
                            .Query(x => x.ID == serviceId && x.MemberID == memberId)
                            .FirstOrDefault();


                    if (entity != null)
                    {
                        response = new ServiceRequestDetailResponse
                        {
                            ServiceId = entity.ID,
                            TransactionDate = entity.CreatedOn,
                            ServiceType = EnumServiceType.PolicyLoanRepayment.ToString(),
                            Status = entity.Status,

                            ServiceRequestDetail = new ServiceRequestDetail
                            {
                                AmountDetail = new AmountDetail
                                {
                                    amount = entity.Amount,
                                    reason = entity.Reason,
                                }
                            },
                        };


                    }
                }
                else if (serviceType == EnumServiceType.AcpLoanRepayment)
                {
                    var entity = unitOfWork.GetRepository<Entities.ServiceACPLoanRepayment>()
                            .Query(x => x.ID == serviceId && x.MemberID == memberId)
                            .FirstOrDefault();


                    if (entity != null)
                    {
                        response = new ServiceRequestDetailResponse
                        {
                            ServiceId = entity.ID,
                            TransactionDate = entity.CreatedOn,
                            ServiceType = EnumServiceType.AcpLoanRepayment.ToString(),
                            Status = entity.Status,

                            ServiceRequestDetail = new ServiceRequestDetail
                            {
                                AmountDetail = new AmountDetail
                                {
                                    amount = entity.Amount,
                                    reason = entity.Reason,
                                }
                            },
                        };


                    }
                }
                else if (serviceType == EnumServiceType.AdHocTopup)
                {
                    var entity = unitOfWork.GetRepository<Entities.ServiceAdhocTopup>()
                            .Query(x => x.ID == serviceId && x.MemberID == memberId)
                            .FirstOrDefault();


                    if (entity != null)
                    {
                        response = new ServiceRequestDetailResponse
                        {
                            ServiceId = entity.ID,
                            TransactionDate = entity.CreatedOn,
                            ServiceType = EnumServiceType.AdHocTopup.ToString(),
                            Status = entity.Status,

                            ServiceRequestDetail = new ServiceRequestDetail
                            {
                                AmountDetail = new AmountDetail
                                {
                                    amount = entity.Amount,
                                    reason = entity.Reason,
                                }
                            },
                        };


                    }
                }
                else if (serviceType == EnumServiceType.SumAssuredChange)
                {
                    var entity = unitOfWork.GetRepository<Entities.ServiceSumAssuredChange>()
                            .Query(x => x.ID == serviceId && x.MemberID == memberId)
                            .FirstOrDefault();


                    if (entity != null)
                    {
                        response = new ServiceRequestDetailResponse
                        {
                            ServiceId = entity.ID,
                            TransactionDate = entity.CreatedOn,
                            ServiceType = EnumServiceType.SumAssuredChange.ToString(),
                            Status = entity.Status,

                            ServiceRequestDetail = new ServiceRequestDetail
                            {
                                AmountDetail = new AmountDetail
                                {
                                    amount = entity.Amount,
                                    reason = entity.Reason,
                                }
                            },
                        };


                    }
                }
                #endregion

                #region #PartialWithdraw, PolicyLoan, PolicySurrender, PolicyPaidUp, RefundOfPayment
                else if (serviceType == EnumServiceType.PartialWithdraw 
                    || serviceType == EnumServiceType.PolicyLoan
                    || serviceType == EnumServiceType.PolicySurrender
                    || serviceType == EnumServiceType.PolicyPaidUp
                    || serviceType == EnumServiceType.RefundOfPayment)
                {
                    ServicingDetailData? serviceDetail = null;

                    if (serviceType == EnumServiceType.PartialWithdraw)
                    {
                        serviceDetail = unitOfWork.GetRepository<Entities.ServicePartialWithdraw>()
                                .Query(x => x.ID == serviceId && x.MemberID == memberId)
                                .Select(x => new ServicingDetailData
                                { 
                                ServicingId = x.ID,
                                CreatedOn = x.CreatedOn,
                                ServiceType = EnumServiceType.PartialWithdraw.ToString(),
                                ServiceStatus = x.Status,
                                Amount = x.Amount,
                                Reason = x.Reason,
                                BankCode = x.BankCode,
                                BankName = x.BankName,
                                AccountName = x.BankAccountName,
                                AccountNumber = x.BankAccountNumber,
                                }
                                )
                                .FirstOrDefault();
                    }
                    else if (serviceType == EnumServiceType.PolicyLoan)
                    {
                        serviceDetail = unitOfWork.GetRepository<Entities.ServicePolicyLoan>()
                                .Query(x => x.ID == serviceId && x.MemberID == memberId)
                                .Select(x => new ServicingDetailData
                                {
                                    ServicingId = x.ID,
                                    CreatedOn = x.CreatedOn,
                                    ServiceType = EnumServiceType.PolicyLoan.ToString(),
                                    ServiceStatus = x.Status,
                                    Amount = x.Amount,
                                    Reason = x.Reason,
                                    BankCode = x.BankCode,
                                    BankName = x.BankName,
                                    AccountName = x.BankAccountName,
                                    AccountNumber = x.BankAccountNumber,
                                }
                                )
                                .FirstOrDefault();
                    }
                    else if (serviceType == EnumServiceType.PolicySurrender)
                    {
                        serviceDetail = unitOfWork.GetRepository<Entities.ServicePolicySurrender>()
                                .Query(x => x.ID == serviceId && x.MemberID == memberId)
                                .Select(x => new ServicingDetailData
                                {
                                    ServicingId = x.ID,
                                    CreatedOn = x.CreatedOn,
                                    ServiceType = EnumServiceType.PolicySurrender.ToString(),
                                    ServiceStatus = x.Status,
                                    Amount = x.Amount,
                                    Reason = x.Reason,
                                    BankCode = x.BankCode,
                                    BankName = x.BankName,
                                    AccountName = x.BankAccountName,
                                    AccountNumber = x.BankAccountNumber,
                                }
                                )
                                .FirstOrDefault();
                    }
                    else if (serviceType == EnumServiceType.PolicyPaidUp)
                    {
                        serviceDetail = unitOfWork.GetRepository<Entities.ServicePolicyPaidUp>()
                                .Query(x => x.ID == serviceId && x.MemberID == memberId)
                                .Select(x => new ServicingDetailData
                                {
                                    ServicingId = x.ID,
                                    CreatedOn = x.CreatedOn,
                                    ServiceType = EnumServiceType.PolicyPaidUp.ToString(),
                                    ServiceStatus = x.Status,
                                    Amount = x.Amount,
                                    Reason = x.Reason,
                                    BankCode = x.BankCode,
                                    BankName = x.BankName,
                                    AccountName = x.BankAccountName,
                                    AccountNumber = x.BankAccountNumber,
                                }
                                )
                                .FirstOrDefault();
                    }
                    else if (serviceType == EnumServiceType.RefundOfPayment)
                    {
                        serviceDetail = unitOfWork.GetRepository<Entities.ServiceRefundOfPayment>()
                                .Query(x => x.ID == serviceId && x.MemberID == memberId)
                                .Select(x => new ServicingDetailData
                                {
                                    ServicingId = x.ID,
                                    CreatedOn = x.CreatedOn,
                                    ServiceType = EnumServiceType.RefundOfPayment.ToString(),
                                    ServiceStatus = x.Status,
                                    Amount = x.Amount,
                                    Reason = x.Reason,
                                    BankCode = x.BankCode,
                                    BankName = x.BankName,
                                    AccountName = x.BankAccountName,
                                    AccountNumber = x.BankAccountNumber,
                                }
                                )
                                .FirstOrDefault();
                    }


                    if (serviceDetail != null)
                    {
                        var bank = unitOfWork.GetRepository<Entities.Bank>()
                            .Query(x => x.BankCode == serviceDetail.BankCode)
                            .FirstOrDefault();

                        response = new ServiceRequestDetailResponse
                        {
                            ServiceId = serviceDetail.ServicingId,
                            TransactionDate = serviceDetail.CreatedOn,
                            ServiceType = serviceDetail.ServiceType,
                            Status = serviceDetail.ServiceStatus,

                            ServiceRequestDetail = new ServiceRequestDetail
                            {
                                AmountDetail = new AmountDetail
                                {
                                    amount = serviceDetail.Amount,
                                    reason = serviceDetail.Reason,
                                },
                                BankDetail = new Model.Mobile.Servicing.Data.Response.BankDetail
                                {
                                    BankLogo = GetFileFullUrl(EnumFileType.Bank, bank?.BankLogo ?? ""),
                                    OriginalBankLogo = bank?.BankLogo,
                                    BankName = serviceDetail.BankName,
                                    BankCode = serviceDetail.BankCode,
                                    AccountName = serviceDetail.AccountName,
                                    AccountNumber = serviceDetail.AccountNumber,
                                }
                            },
                        };


                    }
                }
                
                #endregion
                
                else if (serviceType == EnumServiceType.PaymentFrequency)
                {
                    var entity = unitOfWork.GetRepository<Entities.ServicePaymentFrequency>()
                            .Query(x => x.ID == serviceId && x.MemberID == memberId)
                            .FirstOrDefault();


                    if (entity != null)
                    {
                        response = new ServiceRequestDetailResponse
                        {
                            ServiceId = entity.ID,
                            TransactionDate = entity.CreatedOn,
                            ServiceType = EnumServiceType.PaymentFrequency.ToString(),
                            Status = entity.Status,

                            ServiceRequestDetail = new ServiceRequestDetail
                            {
                                ChangedFrequency = new ChangedFrequency
                                {
                                    Frequency = new ChangeValue
                                    { 
                                        Old = Utils.GetPaymentFrequency(entity.FrequencyType_Old),
                                        New = Utils.GetPaymentFrequency(entity.FrequencyType_New),
                                    },
                                    Amount = new ChangeValue
                                    { 
                                    Old = entity.Amount_Old.ToString(),
                                    New = entity.Amount_New.ToString(),
                                    }
                                }
                            },
                        };


                    }
                }
                else if (serviceType == EnumServiceType.BeneficiaryInformation)
                {
                    var entity = unitOfWork.GetRepository<Entities.ServiceBeneficiary>()
                            .Query(x => x.ID == serviceId && x.MemberID == memberId)
                            .FirstOrDefault();
                    
                    


                    if (entity != null)
                    {
                        var serviceList = unitOfWork.GetRepository<Entities.ServiceBeneficiary>()
                            .Query(x => x.MainID == entity.MainID)
                            .ToList();
                        
                        List<Guid> guidList = serviceList.Select(s=>s.ID).ToList();

                        List<ServiceBeneficiaryPersonalInfo> personalInfo = unitOfWork.GetRepository<Entities.ServiceBeneficiaryPersonalInfo>()
                            .Query(x => guidList.Contains(x.ServiceBeneficiaryID.Value))
                            .ToList();
                        
                        List<ServiceBeneficiaryShareInfo> shareInfo = unitOfWork.GetRepository<Entities.ServiceBeneficiaryShareInfo>()
                            .Query(x => guidList.Contains(x.ServiceBeneficiaryID.Value) /*&& x.Type != EnumBeneficiaryShareInfoType.Remove.ToString()*/  )
                            .ToList();

                        List<ServiceBeneficiaryNewModelResponse> newPersonalInfoList = new List<ServiceBeneficiaryNewModelResponse>();
                        List<ServiceBeneficiaryExistingModelResponse> updatePersonalInfoList = new List<ServiceBeneficiaryExistingModelResponse>();
                        List<ServiceBeneficiaryPolicyModelResponse> policyModelList = new List<ServiceBeneficiaryPolicyModelResponse>();

                        List<string> clientNoList = shareInfo.Select(s => s.ClientNo).ToList();
                        List<Client> clientList = unitOfWork.GetRepository<Entities.Client>()
                                .Query(x => clientNoList.Contains(x.ClientNo))
                                .ToList();

                        foreach (var np in personalInfo.Where(x=>x.IsNewBeneficiary == true).DistinctBy(d=> d.IdValue))
                        {
                            ServiceBeneficiaryNewModelResponse data = new ServiceBeneficiaryNewModelResponse();
                            data.Name = np.Name;
                            data.Gender = np.Gender;
                            data.Dob = np.Dob;
                            data.MobileNo = np.MobileNumber;
                            data.IdType = np.IdType;
                            data.IdValue = np.IdValue;
                            data.IdFrontImage = GetFileFullUrl(EnumFileType.Product, np.IdFrontImageName);
                            data.IdBackImage = GetFileFullUrl(EnumFileType.Product, np.IdBackImageName);
                            newPersonalInfoList.Add(data);
                        }

                        foreach (var np in personalInfo.Where(x=>x.IsNewBeneficiary == false).DistinctBy(d=> d.ClientNo))
                        {
                            var _client = clientList.Where(x => x.ClientNo == np.ClientNo).FirstOrDefault();
                            ServiceBeneficiaryExistingModelResponse data = new ServiceBeneficiaryExistingModelResponse();
                            data.Name = np.Name;
                            data.Gender = _client.Gender=="M"?"Male":"Female";
                            data.Dob = _client.Dob;
                            data.NewMobileNo = np.NewMobileNumber;
                            data.OldMobileNo = np.OldMobileNumber;

                            string idtype = "";string idvalue = "";
                            if (!String.IsNullOrEmpty(_client.Nrc))
                            {
                                idtype = "NRC";
                                idvalue = _client.Nrc;
                            }
                            else if (!String.IsNullOrEmpty(_client.PassportNo))
                            {
                                idtype = "Passport";
                                idvalue = _client.PassportNo;
                            }
                            else if (!String.IsNullOrEmpty(_client.Other))
                            {
                                idtype = "Others";
                                idvalue = _client.Other;
                            }

                            data.IdType = idtype;
                            data.IdValue = idvalue;
                            updatePersonalInfoList.Add(data);
                        }

                        foreach (var p in serviceList)
                        {
                            ServiceBeneficiaryPolicyModelResponse policyModel = new ServiceBeneficiaryPolicyModelResponse();

                            Policy policy = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => x.PolicyNo == p.PolicyNumber).FirstOrDefault();

                            Product productType = unitOfWork.GetRepository<Entities.Product>()
                            .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();

                            policyModel.PolicyNo = p.PolicyNumber;
                            policyModel.PolicyName = productType?.TitleEn;
                            policyModel.PolicyNameMm = productType?.TitleMm;
                            policyModel.beneficiaries = new List<ServbiceBeneficiaryPolicyShareModelResponse>();

                           

                            foreach (var share in shareInfo.Where(x=> x.ServiceBeneficiaryID == p.ID))
                            {
                                var _client = clientList.Where(x => x.ClientNo == share.ClientNo).FirstOrDefault();
                                ServbiceBeneficiaryPolicyShareModelResponse data = new ServbiceBeneficiaryPolicyShareModelResponse();
                                if(_client!=null)
                                {
                                    data.Name = _client?.Name;
                                }
                                else
                                {
                                    var newPInfo = personalInfo.Where(x=> x.IdValue == share.IdValue).FirstOrDefault();
                                    data.Name = newPInfo?.Name;
                                }
                                
                                data.Relationship = share.NewRelationShipCode;
                                data.BeneficiaryShare = share.NewPercentage;
                                if (share.Type == EnumBeneficiaryShareInfoType.New.ToString())
                                {
                                    data.IsNew = true;
                                }
                                else if (share.Type == EnumBeneficiaryShareInfoType.Update.ToString())
                                {
                                    data.IsNew = false;
                                }
                                else if (share.Type == EnumBeneficiaryShareInfoType.Remove.ToString())
                                {
                                    data.IsDeleted = true;
                                    data.BeneficiaryShare = 0;
                                }

                                policyModel.beneficiaries.Add(data);
                            }
                            policyModelList.Add(policyModel);
                        }


                        response = new ServiceRequestDetailResponse
                        {
                            ServiceId = entity.ID,
                            TransactionDate = entity.CreatedOn,
                            ServiceType = EnumServiceType.BeneficiaryInformation.ToString(),
                            Status = entity.Status,

                            ServiceRequestDetail = new ServiceRequestDetail
                            {
                                Beneficiary = new ServiceBeneficiaryResponseModel
                                {
                                    newBeneficiaries = newPersonalInfoList,
                                    existingBeneficiaries = updatePersonalInfoList,
                                    policy = policyModelList
                                }
                            },
                        };


                    }
                }


                #region #Common
                if (response != null)
                {
                    //var serviceStatusUpdateList = unitOfWork.GetRepository<Entities.ServiceStatusUpdate>()
                    //.Query(x => x.ServiceID == response.ServiceId)
                    //.Select(x => new { x.NewStatus, x.CreatedDate })
                    //.ToList();


                    //var changedStatusList = serviceStatusUpdateList?
                    //.GroupBy(x => new { x.NewStatus })
                    //.Select(group => group.OrderByDescending(g => g.CreatedDate).First())
                    //.ToList()
                    //.OrderByDescending(x => x.CreatedDate)
                    //.ToList();

                    //select* from ServiceMain where ServiceID = '0411a97f-99e8-4c62-bedb-4e70a5784c28' order by CreatedDate desc;
                    //SELECT NewStatus, MAX(CreatedDate) AS CreatedDate
                    //FROM ServiceStatusUpdate
                    //WHERE ServiceID = '0411a97f-99e8-4c62-bedb-4e70a5784c28'
                    //GROUP BY NewStatus
                    //order by CreatedDate desc


                    var changedStatusList = unitOfWork.GetRepository<Entities.ServiceStatusUpdate>()
                    .Query(update => update.ServiceID == serviceId)
                    .GroupBy(update => update.NewStatus)
                    .Select(group => new
                    {
                        NewStatus = group.Key,
                        CreatedDate = group.Max(update => update.CreatedDate)
                    })
                    .OrderByDescending(result => result.CreatedDate)
                    .ToList();

                    if (!(response.ServiceType == EnumServiceType.PartialWithdraw.ToString()
                    || response.ServiceType == EnumServiceType.PolicyLoan.ToString()
                    //|| response.ServiceType == EnumServiceType.PolicyPaidUp.ToString()
                    || response.ServiceType == EnumServiceType.PolicySurrender.ToString()
                    || response.ServiceType == EnumServiceType.RefundOfPayment.ToString())
                    )
                    {

                        response.ServiceStatusList?.RemoveAll(x => x.Status == "Paid");

                        if(changedStatusList != null)
                        {
                            changedStatusList.RemoveAll(x => x.NewStatus == "Paid");
                        }

                    }

                    var serviceType1 = unitOfWork.GetRepository<Entities.ServiceType>()
                                .Query(x => x.ServiceTypeEnum == response.ServiceType)
                                .FirstOrDefault();

                    response.ServiceTypeNameEn = serviceType1?.ServiceTypeNameEn;
                    response.ServiceTypeNameMm = serviceType1?.ServiceTypeNameMm;

                    var serviceMain = unitOfWork.GetRepository<Entities.ServiceMain>().Query(x => x.ServiceID == serviceId).FirstOrDefault();
                    response.Status = serviceMain?.ServiceStatus;

                    #region #StatusList

                    


                    if (changedStatusList?.Any() ?? false)
                    {

                        

                        var changedList = changedStatusList.Select(x => x.NewStatus).ToList();

                        var matchedList = response.ServiceStatusList?.Where(x => changedList.Contains(x.Status)).ToList();



                        matchedList?.ForEach(matched =>
                        {
                            matched.IsCompleted = true;
                            matched.Remove = false;
                            matched.StatusChangedDt = changedStatusList.Where(x => x.NewStatus == matched.Status).Select(x => x.CreatedDate).FirstOrDefault();
                        });

                                              


                        response.ServiceStatusList?.RemoveAll(x => x.Remove == true);

                        #region Removed Approved

                        if (response.ServiceStatusList?.Where(x => x.Status == "Paid").Any() == true)
                        {
                            var notApprovedStatus = response.ServiceStatusList?.Where(x => x.Status == "NotApproved" && x.IsCompleted == true).FirstOrDefault();
                            if (notApprovedStatus != null)
                            {
                                var approvedStatus = response.ServiceStatusList?.Where(x => x.Status == "Approved").FirstOrDefault();
                                if (approvedStatus != null)
                                {
                                    approvedStatus.Remove = true;
                                }
                            }

                            response.ServiceStatusList?.RemoveAll(x => x.Remove == true);
                        }


                        #endregion


                    }



                    #endregion

                    #region #Progress
                    var contact = GetProgressAndContactHour(serviceMain.CreatedDate.Value, EnumProgressType.Service);
                    response.Progress = new Model.Mobile.Response.Common.CommonProgress
                    {
                        Progress = contact?.Percent,
                        ClaimContactHours = contact?.Hours,
                        IsTodayHoliday = IsHoliday(),
                    };
                    #endregion

                    var internalRemark = unitOfWork.GetRepository<Entities.ServiceMain>()
                            .Query(x => x.ServiceID == serviceId)
                            .Select(x => x.InternalRemark)
                            .FirstOrDefault();

                    response.InternalRemark = internalRemark;
                }
                #endregion

                MobileErrorLog("GetServiceRequestDetail => response", "", JsonConvert.SerializeObject(response), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<ServiceRequestDetailResponse>(ErrorCode.E0, response);

            }
            catch (Exception ex)
            {
                MobileErrorLog("GetServiceRequestDetail => ex", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<ServiceRequestDetailResponse>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<List<OwnershipPolicy>>> GetOwnershipPolicies(EnumServiceType serviceType)
        {
            try
            {

                var memberId = GetMemberIDFromToken(); /*new Guid("4F8C8726-2D2A-47B6-842E-0A105798B627");*/

                var auth = CheckAuthorization(memberId, null);

                if ((auth.PolicyHolderDetails == true
                || auth.InsuredDetails == true
                || auth.BeneficiaryInfo == true
                || auth.LapseReinstatement == true
                || auth.HealthRenewal == true
                || auth.PolicyLoan == true
                || auth.ACP == true
                || auth.AdhocTopup == true
                || auth.PartialWithdrawal == true
                || auth.PolicyPaidup == true
                || auth.PolicySurrender == true
                || auth.PaymentFrequency == true
                || auth.SumAssuredChange == true
                || auth.RefundofPayment == true
                || auth.PolicyLoanRepayment == true) == false
                    )
                    return new ResponseModel<List<OwnershipPolicy>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };


                #region "Upcoming Premium"

                

                var clientNoList = GetClientNoListByIdValue(memberId);

                

                List<string>? eligibleProductList = null;
                List<string>? eligiblePolicyStatusList = null;
                List<string>? eligiblePremiumStatusList = null;

                var servicePolicyMapping  = unitOfWork.GetRepository<Entities.ServicePolicyMapping>()
                        .Query(x => x.ServiceType == serviceType.ToString())
                        .Select(x => new { x.ProductType, x.PolicyStatus, x.PremiumStatus })
                        .FirstOrDefault();

                eligibleProductList = servicePolicyMapping?.ProductType?.Trim().Split(",")?.Select(x => x.Trim()).ToList();
                eligiblePolicyStatusList = servicePolicyMapping?.PolicyStatus?.Trim().Split(",")?.Select(x => x.Trim()).ToList();
                eligiblePremiumStatusList = servicePolicyMapping?.PremiumStatus?.Trim().Split(",")?.Select(x => x.Trim()).ToList();

                var policies = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => clientNoList.Contains(x.PolicyHolderClientNo)
                    && eligiblePolicyStatusList.Contains(x.PolicyStatus)
                    && eligibleProductList.Contains(x.ProductType)
                    && eligiblePremiumStatusList.Contains(x.PremiumStatus)
                    )
                    .ToList();

                if(serviceType == EnumServiceType.LapseReinstatement && policies?.Count > 0)
                {
                    policies.RemoveAll(policy =>
                    {
                        if ((policy.ProductType == "END" || policy.ProductType == "IED")
                            && policy.PolicyLapsedDate != null)
                        {
                            var lapsedDate = policy.PolicyLapsedDate.Value.AddYears(1);
                            return Utils.GetDefaultDate() > lapsedDate; // remove if expired
                        }
                        else if ((policy.ProductType == "OHI" || policy.ProductType == "ULI" ||
                                  policy.ProductType == "CIS" || policy.ProductType == "TLS")
                                  && policy.PolicyLapsedDate != null)
                        {
                            var lapsedDate = policy.PolicyLapsedDate.Value.AddYears(2);
                            return Utils.GetDefaultDate() > lapsedDate; // remove if expired
                        }
                        return true; // remove if product type not in allowed list or has null lapsed date
                    });
                }


                MobileErrorLog($"GetOwnershipPolicies FE Req => {serviceType} {memberId} {string.Join(",", clientNoList)}"
                    , $"auth => {JsonConvert.SerializeObject(auth)}"
                    , $"policies => {JsonConvert.SerializeObject(policies)}", httpContext?.HttpContext.Request.Path);

                var ownPolicyList = new List<OwnershipPolicy>();

                if (policies != null)
                {
                    foreach (var policy in policies)
                    {
                        try
                        {
                            var product = unitOfWork.GetRepository<Entities.Product>()
                            .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false)
                            .FirstOrDefault();

                            var holder = unitOfWork.GetRepository<Entities.Client>()
                            .Query(x => x.ClientNo == policy.PolicyHolderClientNo)
                            .Select(x => new { x.Name, x.Nrc, x.PassportNo, x.Other })
                            .FirstOrDefault();

                            

                            var holderNrc = !string.IsNullOrEmpty(holder?.Nrc) ? holder?.Nrc : (!string.IsNullOrEmpty(holder?.PassportNo) ? holder?.PassportNo : holder?.Other);

                            var insured = unitOfWork.GetRepository<Entities.Client>()
                            .Query(x => x.ClientNo == policy.InsuredPersonClientNo)
                            .Select(x => new { x.Name, x.Nrc, x.PassportNo, x.Other })
                            .FirstOrDefault();

                            var insuredNrc = insured?.Nrc ?? insured?.PassportNo ?? insured?.Other;


                            var ownershipPolicy = new OwnershipPolicy
                            {
                                PolicyNumber = policy.PolicyNo,
                                PolicyDate = policy.PolicyIssueDate,
                                ProductName = product?.TitleEn,
                                ProductNameMm = product?.TitleMm,
                                ProductLogo = GetFileFullUrl(EnumFileType.Product, product?.LogoImage),



                                InsuredClientNo = policy.InsuredPersonClientNo,
                                InsuredName = insured?.Name,
                                InsuredNrc = insuredNrc,

                                SumAssured = policy.SumAssured,
                                InstallmentAmount = policy.InstallmentPremium,
                                PaymentFrequency = Utils.GetPaymentFrequency(policy.PaymentFrequency),
                            };

                            var beneficiaries = unitOfWork.GetRepository<Entities.Beneficiary>()
                                .Query(x => x.PolicyNo == policy.PolicyNo && x.BeneficiaryClientNo != null)
                                .ToList()?.DistinctBy(x => x.BeneficiaryClientNo)
                                .OrderBy(x => x.CreatedDate)
                                .ToList();

                            #region #Beneficiary



                            if (beneficiaries != null)
                            {
                                ownershipPolicy.beneficiaries = new List<BeneficiaryShare>();

                                foreach (var beneficiary in beneficiaries)
                                {
                                    var beneficiaryClient = unitOfWork.GetRepository<Entities.Client>()
                                                .Query(x => x.ClientNo == beneficiary.BeneficiaryClientNo)
                                                .FirstOrDefault();

                                    SeparateCountryCodeModel? phone = null;

                                    if (beneficiaryClient != null && !string.IsNullOrEmpty(beneficiaryClient.PhoneNo))
                                    {
                                        MobileErrorLog("GetOwnershipBeneficiaries", $"{beneficiary.BeneficiaryClientNo} {beneficiaryClient.PhoneNo}", "", httpContext?.HttpContext.Request.Path);
                                        phone = phoneNumberService.GetMobileNumberSeparateCode(beneficiaryClient.PhoneNo);
                                    }

                                    var percent = 0;
                                    if (beneficiary.Percentage != null)
                                        percent = Convert.ToInt32(beneficiary.Percentage);

                                    //var _relationship = unitOfWork.GetRepository<Entities.Relationship>()
                                    //    .Query(x => x.Code == beneficiary.Relationship).FirstOrDefault();

                                    ownershipPolicy.beneficiaries.Add(new BeneficiaryShare
                                    {
                                        BeneficiaryClientNo = beneficiary.BeneficiaryClientNo,
                                        SharePercent = percent,
                                        RelationshipName = beneficiary.Relationship,
                                        BeneficiaryName = beneficiaryClient?.Name,
                                        Dob = beneficiaryClient?.Dob,
                                        Nrc = !string.IsNullOrEmpty(beneficiaryClient?.Nrc) 
                                        ? beneficiaryClient?.Nrc : (!string.IsNullOrEmpty(beneficiaryClient?.PassportNo) ? beneficiaryClient?.PassportNo: beneficiaryClient?.Other),


                                        

                                        Gender = Utils.GetGender(beneficiaryClient?.Gender),


                                        Phone = phone,
                                    }
                                    );
                                }
                            }


                            #endregion


                            #region #PaymentFrequency
                            if (!string.IsNullOrEmpty(policy.PaymentFrequency) && (policy.AnnualizedPremium != null && policy.AnnualizedPremium != 0))
                            {
                                if (ownershipPolicy.PaymentFrequencies != null)
                                {
                                    foreach (var frequency in ownershipPolicy.PaymentFrequencies)
                                    {
                                        if (frequency.FrequencyCode != null)
                                        {
                                            frequency.FrequencyAmount = Convert.ToInt32 (policy.AnnualizedPremium / Convert.ToInt32(frequency.FrequencyCode));

                                            if (frequency.FrequencyCode == policy.PaymentFrequency)
                                            {
                                                frequency.IsCurrent = true;
                                            }
                                        }
                                    }
                                }

                            }
                            #endregion


                            var serviceData = GetServiceData(serviceType, memberId, policy.PolicyNo, null);

                            ownershipPolicy.ServiceStatus = serviceData?.ServiceStatusEnum;
                            ownershipPolicy.ServicingId = serviceData?.ServiceId;

                            MobileErrorLog($"GetOwnershipPolicies => {memberId} {serviceType.ToString()} {policy.PolicyNo}", "", "", httpContext?.HttpContext.Request.Path);


                            ownPolicyList.Add(ownershipPolicy);
                        }
                        catch(Exception ex) 
                        {
                            MobileErrorLog("GetOwnershipPolicies Ex in Loop", $"{policy.PolicyNo} => {ex.Message}", JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                        }
                        
                    }
                }

                return errorCodeProvider.GetResponseModel<List<OwnershipPolicy>>(ErrorCode.E0, ownPolicyList);

                #endregion
            }
            catch (Exception ex)
            {
                MobileErrorLog("GetOwnershipPolicies Ex", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<OwnershipPolicy>>(ErrorCode.E500);
            }


        }
        

        private ServiceData GetServiceData(EnumServiceType serviceType, Guid? memberId, string? policyNo, string? clientNo)
        {
            #region #ServiceStatus
            var response = new ServiceData();

            if (serviceType == EnumServiceType.PolicyHolderInformation
            || serviceType == EnumServiceType.InsuredPersonInformation)
            {
                var entity = unitOfWork.GetRepository<Entities.ServiceMain>()
                        .Query(x => x.MemberID == clientNo && x.LoginMemberID == memberId && x.ServiceType == serviceType.ToString())
                        .OrderByDescending(x => x.OriginalCreatedDate)
                        .FirstOrDefault();

                response.ServiceStatus = entity?.ServiceStatus;
                response.ServiceId = entity?.ServiceID;
            }
            else
            {
                var entity = unitOfWork.GetRepository<Entities.ServiceMain>()
                        .Query(x => x.PolicyNumber == policyNo && x.LoginMemberID == memberId && x.ServiceType == serviceType.ToString())
                        .OrderByDescending (x => x.OriginalCreatedDate)
                        .FirstOrDefault();

                response.ServiceStatus = entity?.ServiceStatus;
                response.ServiceId = entity?.ServiceID;
            }


            if (!string.IsNullOrEmpty(response.ServiceStatus))
            {
                try
                {
                    response.ServiceStatusEnum = (EnumServiceStatus)Enum.Parse(typeof(EnumServiceStatus), response.ServiceStatus);
                }
                catch { }
                
            }


            return response;
            #endregion
        }

        public async Task<ResponseModel<List<BeneficiaryShare>>> GetOwnershipBeneficiaries()
        {
            try
            {
                var memberId = GetMemberIDFromToken(); /*new Guid("4F8C8726-2D2A-47B6-842E-0A105798B627");*/

                var auth = CheckAuthorization(memberId, null);

                if ((auth.PolicyHolderDetails == true
                || auth.InsuredDetails == true
                || auth.BeneficiaryInfo == true
                || auth.LapseReinstatement == true
                || auth.HealthRenewal == true
                || auth.PolicyLoan == true
                || auth.ACP == true
                || auth.AdhocTopup == true
                || auth.PartialWithdrawal == true
                || auth.PolicyPaidup == true
                || auth.PolicySurrender == true
                || auth.PaymentFrequency == true
                || auth.SumAssuredChange == true
                || auth.RefundofPayment == true
                || auth.PolicyLoanRepayment == true) == false
                    )
                    return new ResponseModel<List<BeneficiaryShare>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };


                var clientNoList = GetClientNoListByIdValue(memberId);    //new string[] { "20018762" }; //            

                var policies = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => clientNoList.Contains(x.PolicyHolderClientNo)
                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus) /*&& (x.PolicyNo == "S003950907" || x.PolicyNo == "S003950809")*/
                    )
                    .ToList();

                MobileErrorLog("GetOwnershipBeneficiaries policies =>", $"policies => {policies?.Count}", "", httpContext?.HttpContext.Request.Path);

                List<BeneficiaryShare>? responseList = null;

                if (policies != null)
                {
                    responseList = new List<BeneficiaryShare>();

                    foreach ( var policy in policies)
                    {
                        

                        var beneficiaries = unitOfWork.GetRepository<Entities.Beneficiary>()
                                .Query(x => x.PolicyNo == policy.PolicyNo && x.BeneficiaryClientNo != null)
                                .ToList()?.DistinctBy(x => x.BeneficiaryClientNo)
                                .OrderBy(x => x.CreatedDate)
                                .ToList();

                        MobileErrorLog("GetOwnershipBeneficiaries beneficiaries =>"
                            , $"policy => {policy?.PolicyNo} beneficiaries => {beneficiaries?.Count}", "", httpContext?.HttpContext.Request.Path);
                        

                        if (beneficiaries != null)
                        {
                            

                            foreach (var beneficiary in beneficiaries)
                            {
                                var pencent = 0;
                                if (beneficiary.Percentage != null)
                                {
                                    pencent = Convert.ToInt32(beneficiary.Percentage);
                                }

                                //var relationship = unitOfWork.GetRepository<Entities.Relationship>().Query(x => x.Code == beneficiary.Relationship).FirstOrDefault();
                                
                                var responseItem = new BeneficiaryShare
                                {
                                    BeneficiaryClientNo = beneficiary.BeneficiaryClientNo,
                                    RelationshipName = beneficiary.Relationship,
                                    SharePercent = pencent,
                                };

                                MobileErrorLog("GetOwnershipBeneficiaries response =>", $"response => {JsonConvert.SerializeObject(responseItem)}", "", httpContext?.HttpContext.Request.Path);

                                var beneficiaryClient = unitOfWork.GetRepository<Entities.Client>()
                                .Query(x => x.ClientNo == beneficiary.BeneficiaryClientNo)
                                .FirstOrDefault();

                                if (beneficiaryClient != null && !string.IsNullOrEmpty(beneficiaryClient.PhoneNo))
                                {
                                    responseItem.Phone = phoneNumberService.GetMobileNumberSeparateCode(beneficiaryClient.PhoneNo);
                                }

                                responseItem.BeneficiaryName = beneficiaryClient?.Name;
                                responseItem.Dob = beneficiaryClient?.Dob;
                                responseItem.Gender = Utils.GetGender(beneficiaryClient?.Gender);


                                responseItem.Nrc = string.IsNullOrEmpty(beneficiaryClient?.Nrc)
                                                    ? (string.IsNullOrEmpty(beneficiaryClient?.PassportNo)
                                                    ? (beneficiaryClient?.Other)
                                                    : beneficiaryClient?.PassportNo)
                                                    : beneficiaryClient?.Nrc;

                                responseList.Add(responseItem);
                            }
                        
                        }

                    }

                }

                if (responseList != null)
                {
                    responseList = responseList.DistinctBy(x => x.BeneficiaryClientNo).ToList();
                }

                MobileErrorLog("GetOwnershipBeneficiaries", $"responseList => {JsonConvert.SerializeObject(responseList)}", "", httpContext?.HttpContext.Request.Path);


                return errorCodeProvider.GetResponseModel<List<BeneficiaryShare>>(ErrorCode.E0, responseList);

            }
            catch(Exception ex)
            {
                MobileErrorLog("GetOwnershipBeneficiaries Ex =>", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<BeneficiaryShare>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<List<BeneficiaryShare>>> GetOwnershipBeneficiariesByPolicyType(string policyNo)
        {
            try
            {
                var memberId = GetMemberIDFromToken(); /*new Guid("4F8C8726-2D2A-47B6-842E-0A105798B627");*/

                var auth = CheckAuthorization(memberId, null);

                if ((auth.PolicyHolderDetails == true
                || auth.InsuredDetails == true
                || auth.BeneficiaryInfo == true
                || auth.LapseReinstatement == true
                || auth.HealthRenewal == true
                || auth.PolicyLoan == true
                || auth.ACP == true
                || auth.AdhocTopup == true
                || auth.PartialWithdrawal == true
                || auth.PolicyPaidup == true
                || auth.PolicySurrender == true
                || auth.PaymentFrequency == true
                || auth.SumAssuredChange == true
                || auth.RefundofPayment == true
                || auth.PolicyLoanRepayment == true) == false
                    )
                    return new ResponseModel<List<BeneficiaryShare>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };


                var clientNoList = GetClientNoListByIdValue(memberId);    //new string[] { "20018762" }; //            

                var policies = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => clientNoList.Contains(x.PolicyHolderClientNo)
                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus) /*&& (x.PolicyNo == "S003950907" || x.PolicyNo == "S003950809")*/
                    )
                    .ToList();

                MobileErrorLog("GetOwnershipBeneficiaries policies =>", $"policies => {policies?.Count}", "", httpContext?.HttpContext.Request.Path);

                List<BeneficiaryShare>? responseList = null;

                if (policies != null)
                {
                    responseList = new List<BeneficiaryShare>();

                    foreach (var policy in policies)
                    {


                        var beneficiaries = unitOfWork.GetRepository<Entities.Beneficiary>()
                                .Query(x => x.PolicyNo == policy.PolicyNo && x.BeneficiaryClientNo != null)
                                .ToList()?.DistinctBy(x => x.BeneficiaryClientNo)
                                .OrderBy(x => x.CreatedDate)
                                .ToList();

                        MobileErrorLog("GetOwnershipBeneficiaries beneficiaries =>"
                            , $"policy => {policy?.PolicyNo} beneficiaries => {beneficiaries?.Count}", "", httpContext?.HttpContext.Request.Path);


                        if (beneficiaries != null)
                        {


                            foreach (var beneficiary in beneficiaries)
                            {
                                var pencent = 0;
                                if (beneficiary.Percentage != null)
                                {
                                    pencent = Convert.ToInt32(beneficiary.Percentage);
                                }

                                //var relationship = unitOfWork.GetRepository<Entities.Relationship>().Query(x => x.Code == beneficiary.Relationship).FirstOrDefault();

                                var responseItem = new BeneficiaryShare
                                {
                                    BeneficiaryClientNo = beneficiary.BeneficiaryClientNo,
                                    RelationshipName = beneficiary.Relationship,
                                    SharePercent = pencent,
                                };

                                MobileErrorLog("GetOwnershipBeneficiaries response =>", $"response => {JsonConvert.SerializeObject(responseItem)}", "", httpContext?.HttpContext.Request.Path);

                                var beneficiaryClient = unitOfWork.GetRepository<Entities.Client>()
                                .Query(x => x.ClientNo == beneficiary.BeneficiaryClientNo)
                                .FirstOrDefault();

                                if (beneficiaryClient != null && !string.IsNullOrEmpty(beneficiaryClient.PhoneNo))
                                {
                                    responseItem.Phone = phoneNumberService.GetMobileNumberSeparateCode(beneficiaryClient.PhoneNo);
                                }

                                responseItem.BeneficiaryName = beneficiaryClient?.Name;
                                responseItem.Dob = beneficiaryClient?.Dob;
                                responseItem.Gender = Utils.GetGender(beneficiaryClient?.Gender);


                                responseItem.Nrc = string.IsNullOrEmpty(beneficiaryClient?.Nrc)
                                                    ? (string.IsNullOrEmpty(beneficiaryClient?.PassportNo)
                                                    ? (beneficiaryClient?.Other)
                                                    : beneficiaryClient?.PassportNo)
                                                    : beneficiaryClient?.Nrc;

                                responseList.Add(responseItem);
                            }

                        }

                    }

                }

                if (responseList != null)
                {
                    responseList = responseList.DistinctBy(x => x.BeneficiaryClientNo).ToList();

                    if(policyNo.Length == 10) //IL Policy
                    {
                        responseList = responseList.Where(x => x.BeneficiaryClientNo.Length == 8).ToList(); //client number is 8 digit
                    }
                    else if (policyNo.Length > 10) //COAST Policy
                    {
                        responseList = responseList.Where(x => x.BeneficiaryClientNo.Length > 8).ToList();
                    }
                }

                MobileErrorLog("GetOwnershipBeneficiaries", $"responseList => {JsonConvert.SerializeObject(responseList)}", "", httpContext?.HttpContext.Request.Path);


                return errorCodeProvider.GetResponseModel<List<BeneficiaryShare>>(ErrorCode.E0, responseList);

            }
            catch (Exception ex)
            {
                MobileErrorLog("GetOwnershipBeneficiaries Ex =>", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<BeneficiaryShare>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<CheckPaymentFrequencyResponse>> CheckPaymentFrequecy(CheckPaymentFrequencyRequest model)
        {
            try
            {
                var memberId = GetMemberIDFromToken();

                var auth = CheckAuthorization(memberId, null);

                if ((auth.PolicyHolderDetails == true
                || auth.InsuredDetails == true
                || auth.BeneficiaryInfo == true
                || auth.LapseReinstatement == true
                || auth.HealthRenewal == true
                || auth.PolicyLoan == true
                || auth.ACP == true
                || auth.AdhocTopup == true
                || auth.PartialWithdrawal == true
                || auth.PolicyPaidup == true
                || auth.PolicySurrender == true
                || auth.PaymentFrequency == true
                || auth.SumAssuredChange == true
                || auth.RefundofPayment == true
                || auth.PolicyLoanRepayment == true) == false
                    )
                    return new ResponseModel<CheckPaymentFrequencyResponse> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var response = new CheckPaymentFrequencyResponse();

                var policy = unitOfWork.GetRepository<Entities.Policy>().Query(x => x.PolicyNo == model.policyNo).FirstOrDefault();
                if (policy == null) return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E400);

                decimal? oldAmount = 0;

                if (!string.IsNullOrEmpty(policy.PaymentFrequency) && (policy.AnnualizedPremium != null && policy.AnnualizedPremium != 0))
                {
                    oldAmount = policy.AnnualizedPremium / Convert.ToInt32(policy.PaymentFrequency);
                }

                var member = unitOfWork.GetRepository<Entities.Member>()
                    .Query(x => x.MemberId == memberId && x.IsVerified == true && x.IsActive == true)
                    .Include(x => x.MemberClients)
                    .FirstOrDefault();

                var clientNo = member?.MemberClients?.FirstOrDefault()?.ClientNo;

                if (!string.IsNullOrEmpty(policy.PaymentFrequency))
                {
                    var ruleMatrix = unitOfWork.GetRepository<Entities.RulesMatrix>()
                        .Query(x => x.FromFrequency == policy.PaymentFrequency)
                        .FirstOrDefault();

                    var ruleResult = "";
                    if (model.paymentFrequencyCode == "12")
                    {
                        ruleResult = ruleMatrix?.Monthly;
                    }
                    else if (model.paymentFrequencyCode == "04")
                    {
                        ruleResult = ruleMatrix?.Quarterly;
                    }
                    else if (model.paymentFrequencyCode == "02")
                    {
                        ruleResult = ruleMatrix?.SemiAnnually;
                    }
                    else if (model.paymentFrequencyCode == "01")
                    {
                        ruleResult = ruleMatrix?.Annually;
                    }

                    ruleResult = ruleResult?.Trim();

                    MobileErrorLog("CheckPaymentFrequecy ruleResult", $"{ruleResult}", JsonConvert.SerializeObject(model), httpContext?.HttpContext.Request.Path);

                    if (!string.IsNullOrEmpty(ruleResult))
                    {
                        var ruleCodeList = ruleResult.Split(",")?.Select(x => x.Trim()).ToList();

                        foreach (var ruleCode in ruleCodeList)
                        {
                            var config = unitOfWork.GetRepository<Entities.PaymentChangeConfig>()
                                    .Query(x => x.Code.Trim() == ruleCode.Trim() && x.Status == true)
                                    .FirstOrDefault();



                            if (config != null)
                            {
                                var LogMessage = "";
                                if (ruleCode == "R3")
                                {
                                    var isValid = false;
                                    var message = "";

                                    DateTime issuedDate = policy.PolicyIssueDate.Value;
                                    DateTime paidToDate = policy.PaidToDate.Value;
                                    DateTime currentDate = Utils.GetDefaultDate();

                                    var paymentFrequency = Convert.ToInt32(policy.PaymentFrequency);
                                    List<(DateTime FromDate, DateTime ToDate)> dateRanges = GenerateDateRanges(paidToDate, 4, paymentFrequency);

                                    var quarterList = "";
                                    foreach (var dateRange in dateRanges)
                                    {

                                        quarterList += $"{{ fromDate: {dateRange.FromDate:dd-MM-yyyy}, toDate: {dateRange.ToDate:dd-MM-yyyy} }}\n\n";
                                    }

                                    LogMessage += $"Policy Issued Date => {issuedDate}\n";
                                    LogMessage += $"Paid To Date => {paidToDate}\n";
                                    LogMessage += $"Current Date => {currentDate}\n";
                                    LogMessage += $"Quarter Date Range => {quarterList}\n";

                                    // 1. find the current date between which quarter
                                    int rangeIndex = FindDateRangeIndex(dateRanges, currentDate);

                                    if (rangeIndex != -1)
                                    {
                                        var range = dateRanges[rangeIndex];

                                        LogMessage += $"Falling Date Range => {currentDate:dd-MM-yyyy} is in the range: {{ fromDate: {range.FromDate:dd-MM-yyyy}, toDate: {range.ToDate:dd-MM-yyyy} }}\n";

                                        // 2. Already paid till "To" Quarter date
                                        if (paidToDate == range.ToDate)
                                        {
                                            isValid = true;
                                            message = "Already paid till To Quarter date";
                                        }
                                        else
                                        {
                                            // Paid to From Quarter date
                                            if (paidToDate == range.FromDate)
                                            {
                                                // PaidToDate is within 30 days from the current date.
                                                if (currentDate <= paidToDate.AddDays(30))
                                                {
                                                    isValid = true;
                                                    message = "PaidToDate is within 30 days from the current date.";
                                                }
                                                else
                                                {
                                                    isValid = false;
                                                    message = "PaidToDate is more than 30 days from the current date.";
                                                }
                                            }
                                            else
                                            {
                                                isValid = false;
                                                message = "Not Paid to From Quarter date";
                                            }
                                        }

                                    }
                                    else
                                    {
                                        isValid = false;
                                        message = $"Current date {currentDate:dd-MM-yyyy} is not in any of the provided ranges.";

                                    }

                                    LogMessage += $"ValidationResult => IsValid => {isValid}, Message => {message}\n";
                                    MobileErrorLog("CheckPaymentFrequecy ValidationResult", $"{LogMessage}", "", httpContext?.HttpContext.Request.Path);

                                    if (!isValid)
                                    {
                                        response = new CheckPaymentFrequencyResponse
                                        {
                                            IsValid = false,
                                            ValidationMessageList = new List<ValidateResultMessage> { new ValidateResultMessage { MessageEn = config.DescEn, MessageMm = config.DescMm } }
                                        };


                                        LogMessage += $"{ruleCode} DescEn => {config.DescEn}\n";
                                        MobileErrorLog("CheckPaymentFrequecy ValidationResult", $"{LogMessage}", "", httpContext?.HttpContext.Request.Path);

                                        #region #SavedValidateMessage

                                        var formattedOldAmount = "0";
                                        var formattedNewAmount = "0";
                                        if (oldAmount != null)
                                        {
                                            formattedOldAmount = $"{oldAmount:N0}";
                                        }
                                        if (!string.IsNullOrEmpty(model.amount))
                                        {
                                            formattedNewAmount = $"{model.amount:N0}";
                                        }
                                        unitOfWork.GetRepository<Entities.ServicePaymentFrequencyValidateMessage>().Add(new ServicePaymentFrequencyValidateMessage
                                        {
                                            Id = Guid.NewGuid(),
                                            PolicyNumber = model.policyNo,
                                            Date = Utils.GetDefaultDate(),
                                            ClientNo = clientNo,
                                            MobileNumber = member?.Mobile,
                                            New = $"{Utils.GetPaymentFrequency(model.paymentFrequencyCode)} {formattedNewAmount}",
                                            Old = $"{Utils.GetPaymentFrequency(policy.PaymentFrequency)} {formattedOldAmount}",
                                            Message = response?.ValidationMessageList?[0].MessageEn,
                                        });

                                        unitOfWork.SaveChanges();
                                        #endregion


                                        return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E0, response);

                                    }
                                    else
                                    {
                                        response = new CheckPaymentFrequencyResponse
                                        {
                                            IsValid = true,
                                        };
                                    }
                                }
                                else
                                {
                                    if (config.Type == EnumPaymentChangeConfigValidationType.Rule.ToString())
                                    {
                                        if (Convert.ToDecimal(model.amount) <= config.Value)
                                        {
                                            response = new CheckPaymentFrequencyResponse
                                            {
                                                IsValid = false,
                                                ValidationMessageList = new List<ValidateResultMessage> { new ValidateResultMessage { MessageEn = config.DescEn, MessageMm = config.DescMm } }
                                            };

                                            MobileErrorLog("CheckPaymentFrequecy ValidationResult", JsonConvert.SerializeObject(response), "", httpContext?.HttpContext.Request.Path);

                                            #region #SavedValidateMessage

                                            var formattedOldAmount = "0";
                                            var formattedNewAmount = "0";
                                            if (oldAmount != null)
                                            {
                                                formattedOldAmount = $"{oldAmount:N0}";
                                            }
                                            if (!string.IsNullOrEmpty(model.amount))
                                            {
                                                formattedNewAmount = $"{model.amount:N0}";
                                            }
                                            unitOfWork.GetRepository<Entities.ServicePaymentFrequencyValidateMessage>().Add(new ServicePaymentFrequencyValidateMessage
                                            {
                                                Id = Guid.NewGuid(),
                                                PolicyNumber = model.policyNo,
                                                Date = Utils.GetDefaultDate(),
                                                ClientNo = clientNo,
                                                MobileNumber = member?.Mobile,
                                                New = $"{Utils.GetPaymentFrequency(model.paymentFrequencyCode)} {formattedNewAmount}",
                                                Old = $"{Utils.GetPaymentFrequency(policy.PaymentFrequency)} {formattedOldAmount}",
                                                Message = response?.ValidationMessageList?[0].MessageEn,
                                            });

                                            unitOfWork.SaveChanges();

                                            #endregion

                                            return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E0, response);
                                        }
                                        else
                                        {
                                            response = new CheckPaymentFrequencyResponse
                                            {
                                                IsValid = true,
                                            };
                                        }
                                    }
                                    else
                                    {
                                        response = new CheckPaymentFrequencyResponse
                                        {
                                            IsValid = true,
                                            ValidationMessageList = new List<ValidateResultMessage> { new ValidateResultMessage { MessageEn = config.DescEn, MessageMm = config.DescMm } }
                                        };
                                    }



                                }
                            }
                            else
                            {
                                response = new CheckPaymentFrequencyResponse
                                {
                                    IsValid = true,
                                };
                            }

                        }


                        if (response?.IsValid == false)
                        {
                            #region #SavedValidateMessage
                            unitOfWork.GetRepository<Entities.ServicePaymentFrequencyValidateMessage>().Add(new ServicePaymentFrequencyValidateMessage
                            {
                                Id = Guid.NewGuid(),
                                PolicyNumber = model.policyNo,
                                Date = Utils.GetDefaultDate(),
                                ClientNo = clientNo,
                                MobileNumber = member?.Mobile,
                                New = $"{Utils.GetPaymentFrequency(model.paymentFrequencyCode)} {model.amount}",
                                Old = $"{Utils.GetPaymentFrequency(policy.PaymentFrequency)} {oldAmount}",
                                Message = response?.ValidationMessageList?[0].MessageEn,
                            });

                            unitOfWork.SaveChanges();
                            #endregion
                        }
                        MobileErrorLog("CheckPaymentFrequecy ValidationResult", JsonConvert.SerializeObject(response), "", httpContext?.HttpContext.Request.Path);

                        return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E0, response);
                    }
                    else
                    {
                        response = new CheckPaymentFrequencyResponse
                        {
                            IsValid = false,
                            ValidationMessageList = new List<ValidateResultMessage> { new ValidateResultMessage { MessageEn = "No rule matrix found." } }
                        };


                        MobileErrorLog("CheckPaymentFrequecy ValidationResult", JsonConvert.SerializeObject(response), "", httpContext?.HttpContext.Request.Path);

                        #region #SavedValidateMessage
                        unitOfWork.GetRepository<Entities.ServicePaymentFrequencyValidateMessage>().Add(new ServicePaymentFrequencyValidateMessage
                        {
                            Id = Guid.NewGuid(),
                            PolicyNumber = model.policyNo,
                            Date = Utils.GetDefaultDate(),
                            ClientNo = clientNo,
                            MobileNumber = member?.Mobile,
                            New = $"{Utils.GetPaymentFrequency(model.paymentFrequencyCode)} {model.amount}",
                            Old = $"{Utils.GetPaymentFrequency(policy.PaymentFrequency)} {oldAmount}",
                            Message = response?.ValidationMessageList?[0].MessageEn,
                        });

                        unitOfWork.SaveChanges();
                        #endregion

                        return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E0, response);
                    }



                }

                return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E500);

            }
            catch (Exception ex)
            {
                MobileErrorLog("CheckPaymentFrequecy => Ex", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E500);
            }


        }

        public async Task<ResponseModel<CheckPaymentFrequencyResponse>> CheckPaymentFrequecyCanSwitchType(CheckPaymentFrequencyRequest model)
        {
            var oldFrequency = "";

            try
            {
                var memberId = GetMemberIDFromToken(); 

                var auth = CheckAuthorization(memberId, null);

                if ((auth.PolicyHolderDetails == true
                || auth.InsuredDetails == true
                || auth.BeneficiaryInfo == true
                || auth.LapseReinstatement == true
                || auth.HealthRenewal == true
                || auth.PolicyLoan == true
                || auth.ACP == true
                || auth.AdhocTopup == true
                || auth.PartialWithdrawal == true
                || auth.PolicyPaidup == true
                || auth.PolicySurrender == true
                || auth.PaymentFrequency == true
                || auth.SumAssuredChange == true
                || auth.RefundofPayment == true
                || auth.PolicyLoanRepayment == true) == false
                    )
                    return new ResponseModel<CheckPaymentFrequencyResponse> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var response = new CheckPaymentFrequencyResponse();
                response.IsValid = true;

                var policy = unitOfWork.GetRepository<Entities.Policy>().Query(x => x.PolicyNo == model.policyNo).FirstOrDefault();
                if (policy == null) return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E400);

                decimal? oldAmount = 0;

                oldFrequency = policy.PaymentFrequency;


                if (!string.IsNullOrEmpty(policy.PaymentFrequency) && (policy.AnnualizedPremium != null && policy.AnnualizedPremium != 0))
                {
                    oldAmount = policy.AnnualizedPremium / Convert.ToInt32(policy.PaymentFrequency);
                }

                var member = unitOfWork.GetRepository<Entities.Member>()
                    .Query(x => x.MemberId == memberId && x.IsVerified == true && x.IsActive == true)
                    .Include(x => x.MemberClients)
                    .FirstOrDefault();

                var clientNo = member?.MemberClients?.FirstOrDefault()?.ClientNo;

                if (!string.IsNullOrEmpty(policy.PaymentFrequency))
                {
                    var ruleMatrix = unitOfWork.GetRepository<Entities.RulesMatrix>()
                        .Query(x => x.FromFrequency == policy.PaymentFrequency)
                        .FirstOrDefault();

                    var ruleResult = "";
                    if (model.paymentFrequencyCode == "12")
                    {
                        ruleResult = ruleMatrix?.Monthly;
                    }
                    else if (model.paymentFrequencyCode == "04")
                    {
                        ruleResult = ruleMatrix?.Quarterly;
                    }
                    else if (model.paymentFrequencyCode == "02")
                    {
                        ruleResult = ruleMatrix?.SemiAnnually;
                    }
                    else if (model.paymentFrequencyCode == "01")
                    {
                        ruleResult = ruleMatrix?.Annually;
                    }

                    ruleResult = ruleResult?.Trim();

                    //MobileErrorLog("CheckPaymentFrequecy ruleResult", $"{ruleResult}", JsonConvert.SerializeObject(model), httpContext?.HttpContext.Request.Path);

                    Console.WriteLine($"CheckPaymentFrequecy Old Frequency {policy.PaymentFrequency} New Frequency {model.paymentFrequencyCode} {ruleResult}");

                    Console.WriteLine($"CheckPaymentFrequecy " +
                            $"PolicyNo {model.policyNo} " +
                            $"Old Frequency {oldFrequency} " +
                            $"New Frequency {model.paymentFrequencyCode} " +
                            $"ruleResult {ruleResult} ");

                    if (!string.IsNullOrEmpty(ruleResult))
                    {
                        var ruleCodeList = ruleResult.Split(",")?.Select(x => x.Trim()).ToList();

                        foreach (var ruleCode in ruleCodeList)
                        {
                            var config = unitOfWork.GetRepository<Entities.PaymentChangeConfig>()
                                    .Query(x => x.Code.Trim() == ruleCode.Trim() && x.Status == true)
                                    .FirstOrDefault();

                            Console.WriteLine($"CheckPaymentFrequecy " +
                            $"PolicyNo {model.policyNo} " +
                            $"Old Frequency {oldFrequency} " +
                            $"New Frequency {model.paymentFrequencyCode} " +
                            $"ruleCode {ruleCode} " +
                            $"PaymentChangeConfig {JsonConvert.SerializeObject(config)}");

                            if (config != null)
                            {
                                var LogMessage = "";
                                if (ruleCode == "R3" && config.Type == EnumPaymentChangeConfigValidationType.Rule.ToString())
                                {
                                    var isValid = false;
                                    var message = "";

                                    DateTime issuedDate = policy.PolicyIssueDate.Value;
                                    DateTime paidToDate = policy.PaidToDate.Value;
                                    DateTime currentDate = Utils.GetDefaultDate();

                                    var paymentFrequency = Convert.ToInt32(policy.PaymentFrequency);
                                    List<(DateTime FromDate, DateTime ToDate)> dateRanges = GenerateDateRanges(paidToDate, 4, paymentFrequency);

                                    var quarterList = "";
                                    foreach (var dateRange in dateRanges)
                                    {

                                        quarterList += $"{{ fromDate: {dateRange.FromDate:dd-MM-yyyy}, toDate: {dateRange.ToDate:dd-MM-yyyy} }}\n\n";
                                    }

                                    LogMessage += $"Policy Issued Date => {issuedDate}\n";
                                    LogMessage += $"Paid To Date => {paidToDate}\n";
                                    LogMessage += $"Current Date => {currentDate}\n";
                                    LogMessage += $"Quarter Date Range => {quarterList}\n";

                                    // 1. find the current date between which quarter
                                    int rangeIndex = FindDateRangeIndex(dateRanges, currentDate);

                                    if (rangeIndex != -1)
                                    {
                                        var range = dateRanges[rangeIndex];

                                        LogMessage += $"Falling Date Range => {currentDate:dd-MM-yyyy} is in the range: {{ fromDate: {range.FromDate:dd-MM-yyyy}, toDate: {range.ToDate:dd-MM-yyyy} }}\n";

                                        // 2. Already paid till "To" Quarter date
                                        if (paidToDate == range.ToDate)
                                        {
                                            isValid = true;
                                            message = "Already paid till To Quarter date";
                                        }
                                        else
                                        {
                                            // Paid to From Quarter date
                                            if (paidToDate == range.FromDate)
                                            {
                                                // PaidToDate is within 30 days from the current date.
                                                if (currentDate <= paidToDate.AddDays(30))
                                                {
                                                    isValid = true;
                                                    message = "PaidToDate is within 30 days from the current date.";
                                                }
                                                else
                                                {
                                                    isValid = false;
                                                    message = "PaidToDate is more than 30 days from the current date.";
                                                }
                                            }
                                            else
                                            {
                                                isValid = false;
                                                message = "Not Paid to From Quarter date";
                                            }
                                        }

                                    }
                                    else
                                    {
                                        isValid = false;
                                        message = $"Current date {currentDate:dd-MM-yyyy} is not in any of the provided ranges.";

                                    }

                                    LogMessage += $"ValidationResult => IsValid => {isValid}, Message => {message}\n";
                                    MobileErrorLog("CheckPaymentFrequecy ValidationResult", $"{LogMessage}", "", httpContext?.HttpContext.Request.Path);

                                    if (!isValid)
                                    {
                                        response = new CheckPaymentFrequencyResponse
                                        {
                                            IsValid = false,
                                            ValidationMessageList = new List<ValidateResultMessage> { new ValidateResultMessage { MessageEn = config.DescEn, MessageMm = config.DescMm } }
                                        };


                                        LogMessage += $"{ruleCode} DescEn => {config.DescEn}\n";
                                        MobileErrorLog("CheckPaymentFrequecy ValidationResult", $"{LogMessage}", "", httpContext?.HttpContext.Request.Path);

                                        #region #SavedValidateMessage

                                        var formattedOldAmount = "0";
                                        var formattedNewAmount = "0";
                                        if (oldAmount != null)
                                        {
                                            formattedOldAmount = $"{oldAmount:N0}";
                                        }
                                        if (!string.IsNullOrEmpty(model.amount))
                                        {
                                            formattedNewAmount = $"{model.amount:N0}";
                                        }
                                        unitOfWork.GetRepository<Entities.ServicePaymentFrequencyValidateMessage>().Add(new ServicePaymentFrequencyValidateMessage
                                        {
                                            Id = Guid.NewGuid(),
                                            PolicyNumber = model.policyNo,
                                            Date = Utils.GetDefaultDate(),
                                            ClientNo = clientNo,
                                            MobileNumber = member?.Mobile,
                                            New = $"{Utils.GetPaymentFrequency(model.paymentFrequencyCode)} {formattedNewAmount}",
                                            Old = $"{Utils.GetPaymentFrequency(policy.PaymentFrequency)} {formattedOldAmount}",
                                            Message = response?.ValidationMessageList?[0].MessageEn,
                                        });

                                        unitOfWork.SaveChanges();
                                        #endregion


                                        Console.WriteLine($"CheckPaymentFrequecy " +
                                        $"PolicyNo {model.policyNo} " +
                                        $"Old Frequency {oldFrequency} " +
                                        $"New Frequency {model.paymentFrequencyCode} " +
                                        $"ValidationResult {JsonConvert.SerializeObject(response)}");

                                        return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E0, response);

                                    }
                                    else // If Rule Checking Is Valid, Must Not Return Response, So That Can Continue Alert Checking
                                    {
                                        response = new CheckPaymentFrequencyResponse
                                        {
                                            IsValid = true,
                                        };
                                    }
                                }
                                
                                else
                                {
                                    if (config.Type == EnumPaymentChangeConfigValidationType.Rule.ToString())
                                    {
                                        if ((config.Value == 0) || (Convert.ToDecimal(model.amount) <= config.Value) ) 
                                        {
                                            response = new CheckPaymentFrequencyResponse
                                            {
                                                IsValid = false,
                                                ValidationMessageList = new List<ValidateResultMessage> { new ValidateResultMessage { MessageEn = config.DescEn, MessageMm = config.DescMm } }
                                            };
                                            

                                            #region #SavedValidateMessage

                                            var formattedOldAmount = "0";
                                            var formattedNewAmount = "0";
                                            if (oldAmount != null)
                                            {
                                                formattedOldAmount = $"{oldAmount:N0}";
                                            }
                                            if (!string.IsNullOrEmpty(model.amount))
                                            {
                                                formattedNewAmount = $"{model.amount:N0}";
                                            }
                                            unitOfWork.GetRepository<Entities.ServicePaymentFrequencyValidateMessage>().Add(new ServicePaymentFrequencyValidateMessage
                                            {
                                                Id = Guid.NewGuid(),
                                                PolicyNumber = model.policyNo,
                                                Date = Utils.GetDefaultDate(),
                                                ClientNo = clientNo,
                                                MobileNumber = member?.Mobile,
                                                New = $"{Utils.GetPaymentFrequency(model.paymentFrequencyCode)} {formattedNewAmount}",
                                                Old = $"{Utils.GetPaymentFrequency(policy.PaymentFrequency)} {formattedOldAmount}",
                                                Message = response?.ValidationMessageList?[0].MessageEn,
                                            });

                                            unitOfWork.SaveChanges();

                                            #endregion

                                            Console.WriteLine($"CheckPaymentFrequecy " +
                                                                    $"PolicyNo {model.policyNo} " +
                                                                    $"Old Frequency {oldFrequency} " +
                                                                    $"New Frequency {model.paymentFrequencyCode} " +
                                                                    $"ValidationResult {JsonConvert.SerializeObject(response)}");

                                            return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E0, response);

                                        }
                                        else // If Rule Checking Is Valid, Must Not Return Response, So That Can Continue Alert Checking
                                        {
                                            response = new CheckPaymentFrequencyResponse
                                            {
                                                IsValid = true,
                                            };
                                        }
                                    }
                                    else if(config.Type == EnumPaymentChangeConfigValidationType.Alert.ToString())
                                    {
                                        response = new CheckPaymentFrequencyResponse
                                        {
                                            IsValid = true,
                                            ValidationMessageList = new List<ValidateResultMessage> { new ValidateResultMessage { MessageEn = config.DescEn, MessageMm = config.DescMm } }
                                        };

                                        Console.WriteLine($"CheckPaymentFrequecy " +
                                       $"PolicyNo {model.policyNo} " +
                                       $"Old Frequency {oldFrequency} " +
                                       $"New Frequency {model.paymentFrequencyCode} " +
                                       $"ValidationResult {JsonConvert.SerializeObject(response)}");

                                        return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E0, response);

                                    }



                                }
                            }

                        } // Loop Rule by Rule (Eg. ,R2,A2-Q)


                        Console.WriteLine($"CheckPaymentFrequecy " +
                                       $"PolicyNo {model.policyNo} " +
                                       $"Old Frequency {oldFrequency} " +
                                       $"New Frequency {model.paymentFrequencyCode} " +
                                       $"ValidationResult {JsonConvert.SerializeObject(response)}");

                        return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E0, response);

                    }
                    else // No Rule Defined in RuleMatrix
                    {
                        var message = "No Rule Defined in RuleMatrix";
                        response = new CheckPaymentFrequencyResponse
                        {
                            IsValid = true,
                            ValidationMessageList = new List<ValidateResultMessage>
                                    { new ValidateResultMessage { MessageEn = message, MessageMm = message } }

                        };

                        Console.WriteLine($"CheckPaymentFrequecy " +
                        $"PolicyNo {model.policyNo} " +
                        $"Old Frequency {oldFrequency} " +
                        $"New Frequency {model.paymentFrequencyCode} " +
                        $"ValidationResult {JsonConvert.SerializeObject(response)}");

                        return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E0, response);
                    }

                    

                }
                else
                {
                    // No Old Frequency Found
                    return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E500);
                }

                

            }
            catch (Exception ex)
            {
                Console.WriteLine($"CheckPaymentFrequecy " +
                    $"PolicyNo {model.policyNo} " +
                    $"Old Frequency {oldFrequency} " +
                    $"New Frequency {model.paymentFrequencyCode} " +
                    $"Ex {JsonConvert.SerializeObject(ex)}");

                
                return errorCodeProvider.GetResponseModel<CheckPaymentFrequencyResponse>(ErrorCode.E500);
            }
            
            
        }

        private List<(DateTime FromDate, DateTime ToDate)> GenerateDateRanges(DateTime paymentDate, int numberOfSegments, int monthsPerSegment)
        {
            List<(DateTime FromDate, DateTime ToDate)> dateRanges = new List<(DateTime FromDate, DateTime ToDate)>();

            DateTime currentDate = paymentDate;
            DateTime endDate = paymentDate.AddMonths(numberOfSegments * monthsPerSegment);

            while (currentDate < endDate)
            {
                DateTime fromDate = currentDate.AddMonths(1);
                DateTime toDate = currentDate.AddMonths(monthsPerSegment);

                if (toDate > endDate)
                {
                    toDate = endDate;
                }

                dateRanges.Add((fromDate, toDate));
                currentDate = toDate;
            }

            return dateRanges;
        }

        private int FindDateRangeIndex(List<(DateTime FromDate, DateTime ToDate)> dateRanges, DateTime givenDate)
        {
            for (int i = 0; i < dateRanges.Count; i++)
            {
                if (givenDate >= dateRanges[i].FromDate && givenDate <= dateRanges[i].ToDate)
                {
                    return i;
                }
            }

            return -1;
        }

        ResponseModel<List<PolicyHolderResponse>> IServicingDataRepository.GetPolicyHolderListForValidation()
        {
            try
            {
                var memberId = GetMemberIDFromToken();
                var clientNoList = GetClientNoListByIdValue(memberId);

                var holderClientNoList = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => clientNoList.Contains(x.PolicyHolderClientNo) && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                    .Select(x => x.PolicyHolderClientNo)
                    .ToList();

                holderClientNoList = holderClientNoList.Distinct().ToList();

                var policyNoList = unitOfWork.GetRepository<Entities.Policy>().Query(x => clientNoList.Contains(x.PolicyHolderClientNo)
                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                        .Select(x => x.PolicyNo)
                        .ToList();

                var responseList = new List<PolicyHolderResponse>();

                if (holderClientNoList != null)
                {
                    foreach (var holderClientNo in holderClientNoList)
                    {
                        var client = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == holderClientNo).FirstOrDefault();

                        if (client != null)
                        {
                           

                            var response = new PolicyHolderResponse
                            {
                                ClientNo = client.ClientNo,
                                Name = client.Name,
                                Email = client.Email,
                                Nrc = !string.IsNullOrEmpty(client.Nrc) ? client.Nrc :
                                    (!string.IsNullOrEmpty(client.PassportNo) ? client.PassportNo : client.Other),
                                ServiceStatus = EnumServiceStatus.Approved,
                            };

                            var entity = unitOfWork.GetRepository<Entities.ServiceMain>()
                           .Query(x => x.MemberID == client.ClientNo && x.LoginMemberID == memberId
                           && x.ServiceType == EnumServiceType.PolicyHolderInformation.ToString())
                           .OrderByDescending(x => x.CreatedDate)
                           .FirstOrDefault();

                            if (!string.IsNullOrEmpty(entity?.ServiceStatus))
                                response.ServiceStatus = (EnumServiceStatus)Enum.Parse(typeof(EnumServiceStatus), entity.ServiceStatus);

                            responseList.Add(response);
                        }
                    }
                }

                return errorCodeProvider.GetResponseModel<List<PolicyHolderResponse>>(ErrorCode.E0, responseList);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetPolicyHolderListForValidation => {ex.Message} {ex.StackTrace}");
                return errorCodeProvider.GetResponseModel<List<PolicyHolderResponse>>(ErrorCode.E500);
            }
        }

        ResponseModel<List<Model.Mobile.Servicing.Data.Response.InsuredPersonResponse>> IServicingDataRepository.GetInsuredPersonListForValidation()
        {
            try
            {
                var memberId = GetMemberIDFromToken();               

                var clientNoList = GetClientNoListByIdValue(memberId);

                var policyClientNoList = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => clientNoList.Contains(x.PolicyHolderClientNo)
                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                    //&& !Utils.GetPremiumStatus().Contains(x.PremiumStatus)
                    )
                    .Select(x => new { x.PolicyHolderClientNo, x.InsuredPersonClientNo })
                    .ToList()
                    .DistinctBy(x => x.InsuredPersonClientNo)
                    .ToList();

                var policyNoList = unitOfWork.GetRepository<Entities.Policy>().Query(x => clientNoList.Contains(x.PolicyHolderClientNo)
                && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                    .Select(x => x.PolicyNo)
                    .ToList();


                policyClientNoList = policyClientNoList.Distinct().ToList();

                var responseList = new List<Model.Mobile.Servicing.Data.Response.InsuredPersonResponse>();

                if (policyClientNoList != null)
                {
                    foreach (var polcyClientNo in policyClientNoList)
                    {
                        var insuredPerson = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == polcyClientNo.InsuredPersonClientNo).FirstOrDefault();
                        var policyHolder = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == polcyClientNo.PolicyHolderClientNo).FirstOrDefault();

                        if (insuredPerson != null)
                        {
                            

                            var response = new Model.Mobile.Servicing.Data.Response.InsuredPersonResponse
                            {
                                ClientNo = insuredPerson.ClientNo,
                                Name = insuredPerson.Name,
                                Email = insuredPerson.Email,
                                Nrc = !string.IsNullOrEmpty(insuredPerson.Nrc) ? insuredPerson.Nrc :
                                    (!string.IsNullOrEmpty(insuredPerson.PassportNo) ? insuredPerson.PassportNo : insuredPerson.Other),

                                ServiceStatus = EnumServiceStatus.Approved,
                            };

                            var entity = unitOfWork.GetRepository<Entities.ServiceMain>()
                           .Query(x => x.MemberID == polcyClientNo.InsuredPersonClientNo && x.LoginMemberID == memberId
                           && x.ServiceType == EnumServiceType.InsuredPersonInformation.ToString())
                           .OrderByDescending(x => x.CreatedDate)
                           .FirstOrDefault();

                            if (!string.IsNullOrEmpty(entity?.ServiceStatus))
                                response.ServiceStatus = (EnumServiceStatus)Enum.Parse(typeof(EnumServiceStatus), entity.ServiceStatus);

                            responseList.Add(response);
                        }
                    }
                }

                return errorCodeProvider.GetResponseModel<List<Model.Mobile.Servicing.Data.Response.InsuredPersonResponse>>(ErrorCode.E0, responseList);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetInsuredPersonListForValidation => {ex.Message} {ex.StackTrace}");

                return errorCodeProvider.GetResponseModel<List<Model.Mobile.Servicing.Data.Response.InsuredPersonResponse>>(ErrorCode.E500);
            }
        }
    }

    public class ServiceData
    {
        public Guid? ServiceId { get; set; }
        public string? ServiceStatus { get; set; }
        public EnumServiceStatus? ServiceStatusEnum { get; set; }
    }

    public class ServicingDetailData
    {
        public string? ServiceType { get; set; }
        public Guid? ServicingId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? ServiceStatus { get; set; }
        public double? Amount { get; set; }
        public string? Reason { get; set; }
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? AccountName { get; set; }
        public string? AccountNumber { get; set; }
    }
}