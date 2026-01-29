using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core;
using aia_core.Repository.Cms;
using aia_core.Repository.Mobile;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/roles")]
    public class RolesController : BaseController
    {
        private readonly IRoleRepository roleRepository;
        public RolesController(IRoleRepository roleRepository)
        {
            this.roleRepository = roleRepository;
        }

        /// <summary>
        /// [roles list]
        /// </summary>
        /// <response code="200">success</response>
        #region #list
        [HttpGet]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<RoleResponse>>), 200)]
        public async Task<IActionResult> List(
            [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size,
            [MaxLength(50)][FromQuery]string? roleTitle, [FromQuery] string[]? title)
        {
            var response = await roleRepository.List(page ?? 1, size ?? 10, roleTitle, title);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [details roles]
        /// </summary>
        /// <response code="200">success</response>
        #region #details
        [HttpGet("{roleId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<RoleResponse>), 200)]
        public async Task<IActionResult> Get([Required][FromRoute] Guid? roleId)
        {
            var response = await roleRepository.Get(roleId ?? Guid.Empty);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [create roles]
        /// </summary>
        /// <response code="200">success</response>
        #region #create
        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<RoleResponse>), 200)]
        public async Task<IActionResult> Create([FromForm] CreateRoleRequest model)
        {
            var response = await roleRepository.Create(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update roles]
        /// </summary>
        /// <response code="200">success</response>
        #region #update
        [HttpPut]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<RoleResponse>), 200)]
        public async Task<IActionResult> Update([FromForm] UpdateRoleRequest model)
        {
            var response = await roleRepository.Update(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [delete roles]
        /// </summary>
        /// <response code="200">success</response>
        #region #details
        [HttpDelete("{roleId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<RoleResponse>), 200)]
        public async Task<IActionResult> Delete([Required][FromRoute] Guid? roleId)
        {
            var response = await roleRepository.Delete(roleId ?? Guid.Empty);
            return Ok(response);
        }
        #endregion
    }
}
