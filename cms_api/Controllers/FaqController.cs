using aia_core;
using aia_core.Model.Cms.Request.Faq;
using aia_core.Model.Cms.Request.Notification;
using aia_core.Model.Cms.Response.Faq;
using aia_core.Model.Cms.Response.Notification;
using aia_core.Repository.Cms;
using aia_core.Repository.Mobile;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/faq")]
    public class FaqController : BaseController
    {
        private readonly aia_core.Repository.Cms.IFaqRepository faqRepository;

        public FaqController(aia_core.Repository.Cms.IFaqRepository _faqRepository)
        {
            faqRepository = _faqRepository;
        }


        [HttpPost("list")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<FaqResponse>>), 200)]
        public async Task<IActionResult> List(ListFaqRequest model)
        {
            var response = await faqRepository.List(model);
            return Ok(response);
        }


        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<FaqResponse>), 200)]
        public async Task<IActionResult> Create([FromForm] CreateFaqRequest model)
        {
            var response = await faqRepository.Create(model);
            return Ok(response);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<FaqResponse>), 200)]
        public async Task<IActionResult> Get([Required] Guid id)
        {
            var response = await faqRepository.Get(id);
            return Ok(response);
        }


        [HttpPut]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel), 200)]
        public async Task<IActionResult> Update([FromForm] UpdateFaqRequest model)
        {
            var response = await faqRepository.Update(model);
            return Ok(response);
        }

        [HttpDelete]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel), 200)]
        public async Task<IActionResult> Delete([Required] Guid id)
        {
            var response = await faqRepository.Delete(id);
            return Ok(response);
        }


        [HttpPut("toggle-active")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel), 200)]
        public async Task<IActionResult> ToggleActive(ToggleFaqRequest model)
        {
            var response = await faqRepository.ToggleActive(model.Id);
            return Ok(response);
        }
    }
}
