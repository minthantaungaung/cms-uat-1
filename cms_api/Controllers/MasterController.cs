using aia_core.Repository.Mobile;
using aia_core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using aia_core.Model.Cms.Response.MasterData;
using aia_core.Repository.Cms;


namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/master")]
    public class MasterController : BaseController
    {

        private readonly IMasterDataRepository masterDataRepository;

        public MasterController(IMasterDataRepository masterDataRepository) 
        {
            this.masterDataRepository = masterDataRepository;
        }


        #region #country
        [HttpGet("country")]
        [ApiVersion("1.0")]

        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<CountryResponse>), 200)]
        public async Task<IActionResult> GetCountry()
        {
            return Ok(masterDataRepository.GetCountry());

        }
        #endregion

        #region #province
        [HttpGet("province")]
        [ApiVersion("1.0")]

        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<ProvinceResponse>), 200)]
        public async Task<IActionResult> GetProvince()
        {
            return Ok(masterDataRepository.GetProvince());

        }
        #endregion

        #region #district
        [HttpGet("district")]
        [ApiVersion("1.0")]

        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<DistrictResponse>), 200)]
        public async Task<IActionResult> GetDistrict(string? code)
        {
            return Ok(masterDataRepository.GetDistrict(code));

        }
        #endregion

        #region #township
        [HttpGet("township")]
        [ApiVersion("1.0")]

        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<TownshipResponse>), 200)]
        public async Task<IActionResult> GetTownship(string? code)
        {
            return Ok(masterDataRepository.GetTownship(code));
        }
        #endregion


        #region #products
        [HttpGet("products")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<ProductCodeResponse>>), 200)]
        public async Task<IActionResult> GetProduct()
        {
            return Ok(masterDataRepository.GetProduct());
        }
        #endregion

        #region #policystatus
        [HttpGet("policystatus")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<PolicyStatusResponse>>), 200)]
        public async Task<IActionResult> GetPolicyStatus()
        {
            return Ok(masterDataRepository.GetPolicyStatus());

        }
        #endregion
    }
}
