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

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/bank")]
    public class BankController : BaseController
    {
        private readonly IBankRepository bankRepository;
        public BankController(IBankRepository bankRepository)
        {
            this.bankRepository = bankRepository;
        }

        #region #list
        [HttpGet]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<BankResponse>>), 200)]
        public async Task<IActionResult> List(
            [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size,
            [MaxLength(500)][FromQuery]string? bankname)
        {
            var response = await bankRepository.List(page ?? 1, size ?? 10, bankname);
            return Ok(response);
        }
        #endregion

        #region #create
        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Create([FromForm] CreateBankRequest model)
        {
            var response = await bankRepository.Create(model);
            return Ok(response);
        }
        #endregion

        #region #details
        [HttpGet("{bankId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<BankResponse>), 200)]
        public async Task<IActionResult> Get([Required][FromRoute] Guid? bankId)
        {
            var response = await bankRepository.Get(bankId ?? Guid.Empty);
            return Ok(response);
        }
        #endregion

        #region #update
        [HttpPut]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> Update([FromForm] UpdateBankRequest model)
        {
            var response = await bankRepository.Update(model);
            return Ok(response);
        }
        #endregion

        #region #change-status
        [HttpPost("changestatus")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> ChangeStatus([FromBody] UpdateBankStatusRequest model)
        {
            var response = await bankRepository.ChangeStatus(model);
            return Ok(response);
        }
        #endregion
    }
}