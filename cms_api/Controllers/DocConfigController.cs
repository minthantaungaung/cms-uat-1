using aia_core.Model.Cms.Response.Servicing;
using aia_core;
using aia_core.Repository.Cms;
using aia_core.Repository.Mobile;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Mvc;
using aia_core.Model.Cms.Request.PaymentChangeConfig;
using aia_core.Model.Cms.Response.PaymentChangeConfig;
using System.ComponentModel.DataAnnotations;
using aia_core.Model.Cms.Response.DocConfig;
using aia_core.Model.Cms.Request.DocConfig;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.ComponentModel;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/document-config")]
    public class DocConfigController : BaseController
    {
        private readonly aia_core.Repository.Cms.IDocConfigRepository configRepository;
        public DocConfigController(aia_core.Repository.Cms.IDocConfigRepository configRepository)
        {
            this.configRepository = configRepository;
        }



        #region #list
        [HttpGet("old")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<DocConfigResponse>>), 200)]
        public async Task<IActionResult> List([FromQuery] string? documentType, [FromQuery] string? documentTypeID)
        {
            var response = configRepository.List(documentType, documentTypeID);
            return Ok(response);
        }
        #endregion

        #region #list
        [HttpGet]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<DocConfigResponse>>), 200)]
        public async Task<IActionResult> List([FromQuery] string? documentType, [FromQuery] string? documentTypeID
            , [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size)
        {
            var response = configRepository.List(documentType, documentTypeID, page?? 1, size?? 10);
            return Ok(response);
        }
        #endregion

        #region #create
        [HttpPost]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Create(DocConfigRequest model)
        {
            var response = configRepository.Create(model);
            return Ok(response);
        }
        #endregion

        #region #update
        [HttpPut]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Update(DocConfigUpdateRequest model)
        {
            var response = configRepository.Update(model);
            return Ok(response);
        }
        #endregion

        #region #get
        [HttpGet("get")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<DocConfigResponse>), 200)]
        public async Task<IActionResult> Get([Required] Guid? id)
        {
            var response = configRepository.Get(id);
            return Ok(response);
        }
        #endregion


        #region #get
        [HttpDelete]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<DocConfigResponse>), 200)]
        public async Task<IActionResult> Delete([Required] Guid? id)
        {
            var response = configRepository.Delete(id);
            return Ok(response);
        }
        #endregion
    }
}
