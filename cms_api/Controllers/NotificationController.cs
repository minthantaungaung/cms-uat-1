using aia_core.Model.Cms.Request;
using aia_core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using aia_core.Model.Cms.Response;
using aia_core.UnitOfWork;
using aia_core.Model.Cms.Response.Notification;
using aia_core.Model.Cms.Request.Notification;
using System.ComponentModel.DataAnnotations;
using aia_core.Repository.Cms;
using aia_core.Model.Cms.Request.Faq;
using aia_core.Repository.Mobile;

namespace cms_api.Controllers
{

    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/notification")]
    public class NotificationController : BaseController
    {

        private readonly aia_core.Repository.Cms.INotificationRepository notificationRepository;


        public NotificationController(aia_core.Repository.Cms.INotificationRepository _notificationRepository)
        {
            this.notificationRepository = _notificationRepository;
        }


        [HttpPost("list")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<NotificationResponse>>), 200)]
        public async Task<IActionResult> List(NotificationRequest model)
        {

            return Ok(notificationRepository.GetList(model));

        }


        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<NotificationResponse>), 200)]

        public async Task<IActionResult> Create([FromForm] CreateNotificationRequest model)
        {

            return Ok(notificationRepository.Create(model));

        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]

        [ProducesResponseType(typeof(ResponseModel<NotificationDetailResponse>), 200)]
        public async Task<IActionResult> Get([Required] Guid id)
        {

            return Ok(notificationRepository.Get(id));
        }


        [HttpPut]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<NotificationResponse>), 200)]
        public async Task<IActionResult> Update([FromForm] UpdateNotificationRequest model)
        {

            return Ok(notificationRepository.Update(model));
        }

        [HttpDelete]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<NotificationResponse>), 200)]
        public async Task<IActionResult> Delete([Required] Guid id)
        {

            return Ok(notificationRepository.Delete(id));

        }


        [HttpPut("toggle-active")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel), 200)]
        public async Task<IActionResult> ToggleActive(ToggleFaqRequest model)
        {
            var response = await notificationRepository.ToggleActive(model.Id);
            return Ok(response);
        }

        [HttpPost("on-demand")]
        /*[Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)*/
        [AllowAnonymous]
        [ProducesResponseType(typeof(ThirdPartyNotificationResponse), 200)]

        public async Task<IActionResult> CreateNotifications([FromForm] ThirdPartyNotificationRequest model)
        {
            //var response = notificationRepository.CreateNotificationOnDemand(model);
            return Ok(new ThirdPartyNotificationResponse() { isSuccess = true, ClientId = model.ClientId });
        }
    }
}
