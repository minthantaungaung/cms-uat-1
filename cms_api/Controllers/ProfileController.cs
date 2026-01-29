using aia_core.Model.Cms.Response;
using aia_core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using aia_core.Repository.Cms;
using aia_core.Model.Cms;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/profile")]
    public class ProfileController : BaseController
    {
        private readonly IProfileRepository profileRepository;
        public ProfileController(IProfileRepository profileRepository)
        {
            this.profileRepository = profileRepository; 
        }


        #region #details
        [HttpGet]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<CmsAccessUser>), 200)]
        public async Task<IActionResult> Get()
        {
            var cmsUser = profileRepository.Get().Result;
            return Ok(cmsUser);
        }
        #endregion



        #region #details
        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<StaffResponse>), 200)]
        public async Task<IActionResult> Update(CmsAccessUserUpdate model)
        {
            var cmsUser = profileRepository.Update(model).Result;
            return Ok(cmsUser);
        }
        #endregion
    }
}
