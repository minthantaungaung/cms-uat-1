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
using aia_core.Model.Cms.Request.Crm;
using Newtonsoft.Json;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/crm")]
    public class CrmController : BaseController
    {
        private readonly ICrmRepository crmRepository;
        public CrmController(ICrmRepository crmRepository)
        {
            this.crmRepository = crmRepository;
        }

        #region #update
        [HttpPost("update")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.CustomBasicAuthentication)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Update([FromBody]UpdateClaimCrmRequest model)
        {
            var logId = Guid.NewGuid();
            crmRepository.SaveCrmRequest(logId, JsonConvert.SerializeObject(model));

            var response = await crmRepository.Update(model);

            crmRepository.SaveCrmResponse(logId, JsonConvert.SerializeObject(response));

            return Ok(response);
        }
        #endregion
    }
}