using aia_core.Model.Cms.Response.Servicing;
using aia_core;
using aia_core.Repository.Cms;
using aia_core.Repository.Mobile;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Mvc;
using aia_core.Model.Cms.Request.PaymentChangeConfig;
using aia_core.Model.Cms.Response.PaymentChangeConfig;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/paymentchange-config")]
    public class PaymentChangeConfigController : BaseController
    {
        private readonly IPaymentChangeConfigRepository configRepository;
        public PaymentChangeConfigController(IPaymentChangeConfigRepository configRepository)
        {
            this.configRepository = configRepository;
        }



        #region #list
        [HttpGet]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<PaymentChangeConfigResponse>>), 200)]
        public async Task<IActionResult> List()
        {
            var response = configRepository.List();
            return Ok(response);
        }
        #endregion

        #region #create
        [HttpPost]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Create(PaymentChangeConfigRequest model)
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
        public async Task<IActionResult> Update(PaymentChangeConfigUpdateRequest model)
        {
            var response = configRepository.Update(model);
            return Ok(response);
        }
        #endregion

        #region #get
        [HttpGet("get")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PaymentChangeConfigResponse>), 200)]
        public async Task<IActionResult> Get([Required] Guid? id)
        {
            var response = configRepository.Get(id);
            return Ok(response);
        }
        #endregion
    }
}
