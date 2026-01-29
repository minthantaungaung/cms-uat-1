using aia_core.Entities;
using aia_core.Model.Cms.Response.MasterData;
using aia_core.Services;
using aia_core.UnitOfWork;
using Azure;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Cms
{
    public interface IMasterDataRepository
    {
        ResponseModel<CountryResponse> GetCountry();
        ResponseModel<ProvinceResponse> GetProvince();
        ResponseModel<DistrictResponse> GetDistrict(string? code);
        ResponseModel<TownshipResponse> GetTownship(string? code);
        ResponseModel<List<ProductCodeResponse>> GetProduct();
        ResponseModel<List<PolicyStatusResponse>> GetPolicyStatus();
    }
    public class MasterDataRepository : BaseRepository, IMasterDataRepository
    {
        public MasterDataRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider, IUnitOfWork<Context> unitOfWork)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {

        }

        public ResponseModel<CountryResponse> GetCountry()
        {
            try
            {
                List<Entities.Country> list = unitOfWork.GetRepository<Entities.Country>().GetAll().ToList();
                return errorCodeProvider.GetResponseModel<CountryResponse>(ErrorCode.E0, new CountryResponse() { list = list });
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<CountryResponse>(ErrorCode.E500);
            }
        }

        public ResponseModel<ProvinceResponse> GetProvince()
        {
            try
            {
                List<Entities.Province> list = unitOfWork.GetRepository<Entities.Province>().GetAll().ToList();
                return errorCodeProvider.GetResponseModel<ProvinceResponse>(ErrorCode.E0, new ProvinceResponse() { list = list });
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<ProvinceResponse>(ErrorCode.E500);
            }
        }

        public ResponseModel<DistrictResponse> GetDistrict(string? code)
        {
            try
            {
                List<Entities.District> list = new List<District>();
                if (String.IsNullOrEmpty(code))
                {
                    list = unitOfWork.GetRepository<Entities.District>().GetAll().ToList();
                }
                else
                {
                    var codeList = code.Trim().Split(",");
                    list = unitOfWork.GetRepository<Entities.District>().Query(x => codeList.Contains(x.province_code))
                        .OrderBy(x => x.province_code)
                        .ToList();
                }
                return errorCodeProvider.GetResponseModel<DistrictResponse>(ErrorCode.E0, new DistrictResponse() { list = list });
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<DistrictResponse>(ErrorCode.E500);
            }
        }

        public ResponseModel<TownshipResponse> GetTownship(string? code)
        {
            try
            {
                List<Entities.Township> list = new List<Township>();
                if (String.IsNullOrEmpty(code))
                {
                    list = unitOfWork.GetRepository<Entities.Township>().GetAll().ToList();
                }
                else
                {
                    var codeList = code.Trim().Split(",");
                    list = unitOfWork.GetRepository<Entities.Township>().Query(x => codeList.Contains(x.district_code))
                        .OrderBy(x => x.district_code)
                        .ToList();
                }
                return errorCodeProvider.GetResponseModel<TownshipResponse>(ErrorCode.E0, new TownshipResponse() { list = list });
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<TownshipResponse>(ErrorCode.E500);
            }
        }

        ResponseModel<List<ProductCodeResponse>> IMasterDataRepository.GetProduct()
        {
            try
            {
                var response = unitOfWork.GetRepository<Entities.Product>()
                    .Query(x => x.IsActive == true && x.IsDelete == false)                    
                    .Select(x => new ProductCodeResponse
                    {
                        ProductCode = x.ProductTypeShort,
                        code = $"{x.ProductId}",
                        name = x.TitleEn,
                        CreatedOn = x.CreatedDate,
                    })
                    .ToList()?.OrderBy(x => x.CreatedOn).ToList();

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IMasterDataRepository.GetProduct => {JsonConvert.SerializeObject(ex)}");
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<ProductCodeResponse>>(ErrorCode.E500);
            }
        }

        ResponseModel<List<PolicyStatusResponse>> IMasterDataRepository.GetPolicyStatus()
        {
            try
            {
                var response = unitOfWork.GetRepository<Entities.PolicyStatus>()
                    .Query()
                    .Select(x => new PolicyStatusResponse
                    {
                        code = x.ShortDesc,
                        name = x.LongDesc,
                    })
                    .ToList();

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, response);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<PolicyStatusResponse>>(ErrorCode.E500);
            }
        }
    }
}
