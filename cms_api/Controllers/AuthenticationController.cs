using aia_core;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.Repository.Cms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/auth")]
    public class AuthenticationController : BaseController
    {
        private readonly IAuthRepository authRepository;
        public AuthenticationController(IAuthRepository authRepository)
        {
            this.authRepository = authRepository;
        }

        /// <summary>
        /// [Sign In]
        /// </summary>
        /// <response code="200">success</response>
        #region #login
        [HttpPost]
        [Route("login")]
        [ApiVersion("1.0")]
        [AllowAnonymous]
        //[Authorize(AuthenticationSchemes = DefaultConstants.BasicAuthentication)]
        [ProducesResponseType(typeof(ResponseModel<LoginResponse>), 200)]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            return Ok(authRepository.Login(model));
        }
        #endregion


        /// <summary>
        /// [GetPermissions]
        /// </summary>
        /// <response code="200">success</response>
        #region #GetPermissions
        [HttpPost]
        [Route("GetPermissions")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<Permission>), 200)]
        public async Task<IActionResult> GetPermissions()
        {
            return Ok(authRepository.GetPermissions());
        }
        #endregion


        /// <summary>
        /// [logout]
        /// </summary>
        /// <response code="200">success</response>
        #region #logout
        [HttpPost]
        [Route("logout")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel), 200)]
        public async Task<IActionResult> Logout()
        {
            return Ok(authRepository.Logout());
        }
        #endregion
    }
}
