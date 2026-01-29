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
using aia_core.Model.Cms.Response.CriticalIllness;
using aia_core.Model.Cms.Request.CriticalIllness;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/criticalIllness")]
    public class CriticalIllnessController : BaseController
    {
        private readonly ICriticalIllnessRepository criticalIllnessRepository;
        public CriticalIllnessController(ICriticalIllnessRepository criticalIllnessRepository)
        {
            this.criticalIllnessRepository = criticalIllnessRepository;
        }

        #region #list
        [HttpGet]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<CriticalIllnessResponse>>), 200)]
        public async Task<IActionResult> List(
            [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size,
            [MaxLength(500)][FromQuery]string? name, [FromQuery] List<string>? productCodes)
        {
            var response = await criticalIllnessRepository.List(page ?? 1, size ?? 10, name, productCodes);
            return Ok(response);
        }
        #endregion

        #region #create
        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Create([FromForm] CreateCriticalIllnessRequest model)
        {
            if (model.ProductCodeList?.Any() == false)
            {
                return Ok(new ResponseModel<string> { Code = 400, Message = "Required ProductCodeList" });

            }

            var response = await criticalIllnessRepository.Create(model);
            return Ok(response);
        }
        #endregion

        #region #details
        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<CriticalIllnessResponse>), 200)]
        public async Task<IActionResult> Get([Required][FromRoute] Guid? id)
        {
            var response = await criticalIllnessRepository.Get(id ?? Guid.Empty);
            return Ok(response);
        }
        #endregion

        #region #update
        [HttpPut]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Update([FromForm] UpdateCriticalIllnessRequest model)
        {
            var response = await criticalIllnessRepository.Update(model);
            return Ok(response);
        }
        #endregion

        #region #change-status
        [HttpPost("changestatus")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> ChangeStatus([FromForm] ChangeStatusRequest model)
        {
            var response = await criticalIllnessRepository.ChangeStatus(model);
            return Ok(response);
        }
        #endregion

        #region #delete
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Delete([Required][FromRoute] Guid id)
        {
            var response = await criticalIllnessRepository.Delete(id);
            return Ok(response);
        }
        #endregion
    }
}