using aia_core;
using aia_core.Entities;
using aia_core.Extension;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.Repository.Cms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}")]
    public class GeneralController : BaseController
    {
        private readonly IGeneralRepository generalRepository;
        public GeneralController(IGeneralRepository generalRepository)
        {
            this.generalRepository = generalRepository;
        }

        /// <summary>
        /// [master data]
        /// </summary>
        /// <response code="200">success</response>
        #region #master data
        [HttpGet("master-data")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<MasterDataResponse>), 200)]
        public async Task<IActionResult> MasterData()
        {
            var response = await generalRepository.GetMasterData();
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [get app-version]
        /// </summary>
        /// <response code="200">success</response>
        #region #app-version
        [HttpGet("app-version")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<AppVersionResponse>), 200)]
        public async Task<IActionResult> AppVersion()
        {
            var response = await generalRepository.AppVersion();
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update app-version]
        /// </summary>
        /// <response code="200">success</response>
        #region #app-version
        [HttpPut("app-version")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<AppVersionResponse>), 200)]
        public async Task<IActionResult> AppVersion([FromForm]AppVersionRequest model)
        {
            var response = await generalRepository.AppVersion(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [get app-config]
        /// </summary>
        /// <response code="200">success</response>
        #region #app-config
        [HttpGet("app-config")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<AppConfigResponse>), 200)]
        public async Task<IActionResult> AppConfig()
        {
            var response = await generalRepository.AppConfig();
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update app-config]
        /// </summary>
        /// <response code="200">success</response>
        #region #app-config
        [HttpPut("app-config")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<AppConfigResponse>), 200)]
        public async Task<IActionResult> AppConfig([FromForm] AppConfigRequest model)
        {
            var response = await generalRepository.AppConfig(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update app-config]
        /// </summary>
        /// <response code="200">success</response>
        #region #app-config
        [HttpPost("app-config/sampledoc")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<SampleDocumentResponseModel>>), 200)]
        public async Task<IActionResult> UploadSampleDoc([AllowedFileExtensions(".jpg", ".jpeg", ".png")] IFormFile doc, EnumClaimDoc docType)
        {
            var response = await generalRepository.UploadSampleDoc(doc,docType);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update app-config]
        /// </summary>
        /// <response code="200">success</response>
        #region #app-config
        [HttpDelete("app-config/sampledoc/{id}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<SampleDocumentResponseModel>>), 200)]
        public async Task<IActionResult> DeleteSampleDoc(Guid id)
        {
            var response = await generalRepository.DeleteSampleDoc(id);
            return Ok(response);
        }
        #endregion

        #region #app-maintenance
        [HttpGet("app-config/maintenance")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<MaintenanceResponse>), 200)]
        public async Task<IActionResult> GetMaintenance()
        {
            var response = await generalRepository.GetMaintenance();
            return Ok(response);
        }
        #endregion

        #region #app-maintenance
        [HttpPost("app-config/maintenance")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> UpdateMaintenance(MaintenanceRequest model)
        {
            var response = await generalRepository.UpdateMaintenance(model);
            return Ok(response);
        }
        #endregion

        #region #app-il/coast
        [HttpGet("app-config/il-coast")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<CoastSystemDateResponse>), 200)]
        public async Task<IActionResult> GetCoastSystemDate()
        {
            var response = await generalRepository.GetCoastSystemDate();
            return Ok(response);
        }
        #endregion

        #region #app-il/coast
        [HttpPost("app-config/il-coast")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> UpdateCoastSystemDate(CoastSystemDateRequest model)
        {
            var response = await generalRepository.UpdateCoastSystemDate(model);
            return Ok(response);
        }
        #endregion
    }
}
