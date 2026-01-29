using aia_core.Model.Cms.Response;
using aia_core;
using aia_core.Repository.Cms;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response.PermanentDisability;
using aia_core.Model.Cms.Request.PermanentDisability;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/permanentDisability")]
    public class PermanentDisabilityController : BaseController
    {
        private readonly IPermanentDisabilityRepository permanentDisabilityRepository;
        public PermanentDisabilityController(IPermanentDisabilityRepository permanentDisabilityRepository)
        {
            this.permanentDisabilityRepository = permanentDisabilityRepository;
        }

        #region #list
        [HttpGet]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<PermanentDisabilityResponse>>), 200)]
        public async Task<IActionResult> List(
            [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size,
            [MaxLength(500)][FromQuery]string? name)
        {
            var response = await permanentDisabilityRepository.List(page ?? 1, size ?? 10, name);
            return Ok(response);
        }
        #endregion

        #region #create
        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Create([FromForm] CreatePermanentDisabilityRequest model)
        {
            var response = await permanentDisabilityRepository.Create(model);
            return Ok(response);
        }
        #endregion

        #region #details
        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PermanentDisabilityResponse>), 200)]
        public async Task<IActionResult> Get([Required][FromRoute] Guid? id)
        {
            var response = await permanentDisabilityRepository.Get(id ?? Guid.Empty);
            return Ok(response);
        }
        #endregion

        #region #update
        [HttpPut]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Update([FromForm] UpdatePermanentDisabilityRequest model)
        {
            var response = await permanentDisabilityRepository.Update(model);
            return Ok(response);
        }
        #endregion

        #region #change-status
        [HttpPost("changestatus")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> ChangeStatus([FromForm] ChangeStatusRequest model)
        {
            var response = await permanentDisabilityRepository.ChangeStatus(model);
            return Ok(response);
        }
        #endregion

        #region #delete
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Delete([Required][FromRoute] Guid id)
        {
            var response = await permanentDisabilityRepository.Delete(id);
            return Ok(response);
        }
        #endregion
    }
}