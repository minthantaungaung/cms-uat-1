using aia_core.Model.Cms.Request;
using aia_core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using aia_core.Repository.Cms;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using aia_core.UnitOfWork;
using aia_core.Model.Cms.Response;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/coverages")]
    public class CoveragesController : ControllerBase
    {
        private readonly ICoverageRepository coverageRepository;
        public CoveragesController(ICoverageRepository coverageRepository)
        {
            this.coverageRepository = coverageRepository;
        }

        /// <summary>
        /// [list coverages]
        /// </summary>
        /// <response code="200">success</response>
        #region #list
        [HttpGet]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<CoverageResponse>>), 200)]
        public async Task<IActionResult> List(
            [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery]int? size,
            [MaxLength(50)][FromQuery]string? coverageName,
            [FromQuery] Guid[]? products)
        {
            var response = await coverageRepository.List(page ?? 1, size ?? 10, coverageName, products);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [details coverages]
        /// </summary>
        /// <response code="200">success</response>
        #region #details
        [HttpGet("{coverageId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<CoverageResponse>), 200)]
        public async Task<IActionResult> Get([Required][FromRoute] Guid? coverageId)
        {
            var response = await coverageRepository.Get(coverageId ?? Guid.Empty);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [create coverages]
        /// </summary>
        /// <response code="200">success</response>
        #region #create
        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<CoverageResponse>), 200)]
        public async Task<IActionResult> Create([FromForm] CreateCoverageRequest model)
        {
            var response = await coverageRepository.Create(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update coverages]
        /// </summary>
        /// <response code="200">success</response>
        #region #update
        [HttpPut]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<CoverageResponse>), 200)]
        public async Task<IActionResult> Update([FromForm] UpdateCoverageRequest model)
        {
            var response = await coverageRepository.Update(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [delete coverages]
        /// </summary>
        /// <response code="200">success</response>
        #region #delete
        [HttpDelete("{coverageId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<CoverageResponse>), 200)]
        public async Task<IActionResult> Delete([Required][FromRoute]Guid? coverageId)
        {
            var response = await coverageRepository.Delete(coverageId ?? Guid.Empty);
            return Ok(response);
        }
        #endregion
    }
}
