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
using aia_core.Model.Cms.Response.Hospital;
using aia_core.Model.Cms.Request.Hospital;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/hospital")]
    public class HospitalController : BaseController
    {
        private readonly IHospitalRepository hospitalRepository;
        public HospitalController(IHospitalRepository hospitalRepository)
        {
            this.hospitalRepository = hospitalRepository;
        }

        #region #list
        [HttpGet]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<HospitalResponse>>), 200)]
        public async Task<IActionResult> List(
            [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size,
            [MaxLength(500)][FromQuery]string? name)
        {
            var response = await hospitalRepository.List(page ?? 1, size ?? 10, name);
            return Ok(response);
        }
        #endregion

        #region #create
        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Create([FromForm] CreateHospitalRequest model)
        {
            var response = await hospitalRepository.Create(model);
            return Ok(response);
        }
        #endregion

        #region #details
        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<HospitalResponse>), 200)]
        public async Task<IActionResult> Get([Required][FromRoute] Guid? id)
        {
            var response = await hospitalRepository.Get(id ?? Guid.Empty);
            return Ok(response);
        }
        #endregion

        #region #update
        [HttpPut]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Update([FromForm] UpdateHospitalRequest model)
        {
            var response = await hospitalRepository.Update(model);
            return Ok(response);
        }
        #endregion

        #region #change-status
        [HttpPost("changestatus")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> ChangeStatus([FromForm] ChangeStatusRequest model)
        {
            var response = await hospitalRepository.ChangeStatus(model);
            return Ok(response);
        }
        #endregion

        #region #delete
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Delete([Required][FromRoute] Guid id)
        {
            var response = await hospitalRepository.Delete(id);
            return Ok(response);
        }
        #endregion
    }
}