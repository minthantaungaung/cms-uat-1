using aia_core.Model.Cms.Response;
using aia_core;
using aia_core.Repository.Cms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using aia_core.Model.Cms.Request;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/staffs")]
    public class StaffsController : BaseController
    {
        private readonly IStaffRepository staffRepository;
        public StaffsController(IStaffRepository staffRepository)
        {
            this.staffRepository = staffRepository;
        }

        /// <summary>
        /// [staffs list]
        /// </summary>
        /// <response code="200">success</response>
        #region #list
        [HttpGet]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<StaffResponse>>), 200)]
        public async Task<IActionResult> List(
            [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size,
            [MaxLength(50)][FromQuery]string? email,
            [MaxLength(50)][FromQuery] string? name,
            [FromQuery] Guid[]? roles,
            [FromQuery]bool? status)
        {
            var response = await staffRepository.List(page ?? 1, size ?? 10, email, name, roles, status);
            return Ok(response);
        }
        #endregion


        /// <summary>
        /// [staffs list]
        /// </summary>
        /// <response code="200">success</response>
        #region #list
        [HttpGet("list-by-role")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<StaffResponse>>), 200)]
        public async Task<IActionResult> ListByRole(
            [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size,
            [Required][FromQuery] string? roleId)
        {


            var response = await staffRepository.ListByRole(page ?? 1, size ?? 10, roleId);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [staffs details]
        /// </summary>
        /// <response code="200">success</response>
        #region #details
        [HttpGet("{staffId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<StaffResponse>), 200)]
        public async Task<IActionResult> Get([Required][FromRoute] Guid? staffId)
        {
            var response = await staffRepository.Get(staffId ?? Guid.Empty);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [create staffs]
        /// </summary>
        /// <response code="200">success</response>
        #region #create
        [HttpPost]
        //[Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ResponseModel<StaffResponse>), 200)]
        public async Task<IActionResult> Create([FromForm] CreateStaffRequest model)
        {
            var response = await staffRepository.Create(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update staffs]
        /// </summary>
        /// <response code="200">success</response>
        #region #create
        [HttpPut]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<StaffResponse>), 200)]
        public async Task<IActionResult> Update([FromForm] UpdateStaffRequest model)
        {
            var response = await staffRepository.Update(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [delete staffs]
        /// </summary>
        /// <response code="200">success</response>
        #region #details
        [HttpDelete("{staffId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<RoleResponse>), 200)]
        public async Task<IActionResult> Delete([Required][FromRoute] Guid? staffId)
        {
            var response = await staffRepository.Delete(staffId ?? Guid.Empty);
            return Ok(response);
        }
        #endregion
    }
}
