using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core;
using aia_core.Repository.Cms;
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
    [Route("/v{version:apiVersion}/propositions")]
    public class PropositionsController : BaseController
    {
        private readonly IPropositionRepository propositionRepository;
        public PropositionsController(IPropositionRepository propositionRepository)
        {
            this.propositionRepository = propositionRepository;
        }

        /// <summary>
        /// [propositions list]
        /// </summary>
        /// <response code="200">success</response>
        #region #list
        [HttpGet]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<PropositionResponse>>), 200)]
        public async Task<IActionResult> List(
            [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size,
            [MaxLength(50)][FromQuery]string? name,
            [FromQuery] Guid[]? categories,
            [FromQuery] EnumPropositionBenefit[]? eligibility)
        {
            var response = await propositionRepository.List(page ?? 1, size ?? 10, name, categories, eligibility);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [details propositions]
        /// </summary>
        /// <response code="200">success</response>
        #region #details
        [HttpGet("{propositionId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PropositionResponse>), 200)]
        public async Task<IActionResult> Get([Required][FromRoute] Guid? propositionId)
        {
            var response = await propositionRepository.Get(propositionId ?? Guid.Empty);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [create propositions]
        /// </summary>
        /// <response code="200">success</response>
        #region #create
        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PropositionResponse>), 200)]
        public async Task<IActionResult> Create([FromForm] CreatePropositionRequest model)
        {
            var response = await propositionRepository.Create(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update propositions]
        /// </summary>
        /// <response code="200">success</response>
        #region #update
        [HttpPut]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PropositionResponse>), 200)]
        public async Task<IActionResult> Update([FromForm] UpdatePropositionRequest model)
        {
            var response = await propositionRepository.Update(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [delete propositions]
        /// </summary>
        /// <response code="200">success</response>
        #region #details
        [HttpDelete("{propositionId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PropositionResponse>), 200)]
        public async Task<IActionResult> Delete([Required][FromRoute] Guid? propositionId)
        {
            var response = await propositionRepository.Delete(propositionId ?? Guid.Empty);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [get all propositions list]
        /// </summary>
        /// <response code="200">success</response>
        #region #list
        [HttpGet("order")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<PropositionResponse>>), 200)]
        public async Task<IActionResult> Order()
        {
            var response = await propositionRepository.GetAll();
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update propositions order]
        /// </summary>
        /// <response code="200">success</response>
        #region #update-order
        [HttpPut("order")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<PropositionResponse>>), 200)]
        public async Task<IActionResult> Order([Required][FromBody] List<PropositionOrderRequest> model)
        {
            var response = await propositionRepository.Order(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [proposition categories list]
        /// </summary>
        /// <response code="200">success</response>
        #region #list proposition categories
        [HttpGet("categories")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<PropositionCategoryResponse>>), 200)]
        public async Task<IActionResult> Categories([Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size,
            [MaxLength(50)][FromQuery] string? name)
        {
            var response = await propositionRepository.Categories(page ?? 1, size ?? 10, name);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [proposition category by Id]
        /// </summary>
        /// <response code="200">success</response>
        #region #list proposition categories
        [HttpGet("get-category")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PropositionCategoryResponse>), 200)]
        public async Task<IActionResult> GetCategory([Required][FromQuery] Guid Id)
        {
            var response = await propositionRepository.GetCategory(Id);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [create proposition categories]
        /// </summary>
        /// <response code="200">success</response>
        #region #create proposition categories
        [HttpPost("categories")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PropositionCategoryResponse>), 200)]
        public async Task<IActionResult> Categories([FromForm]CreatePropositionCategoryRequest model)
        {
            var response = await propositionRepository.Categories(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update proposition categories]
        /// </summary>
        /// <response code="200">success</response>
        #region #update proposition categories
        [HttpPut("categories")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PropositionCategoryResponse>), 200)]
        public async Task<IActionResult> Categories([FromForm] UpdatePropositionCategoryRequest model)
        {
            var response = await propositionRepository.Categories(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [delete proposition categories]
        /// </summary>
        /// <response code="200">success</response>
        #region #delete proposition categories
        [HttpDelete("categories/{id}")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PropositionCategoryResponse>), 200)]
        public async Task<IActionResult> Categories([Required][FromRoute] Guid? id)
        {
            var response = await propositionRepository.DeleteCategories(id ?? Guid.Empty);
            return Ok(response);
        }
        #endregion

        [HttpGet("check-duplicate-names")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<DuplicateCheckResponse>), 200)]
        public async Task<IActionResult> CheckDuplicateByName([Required][FromQuery] string? NameEn
            , [Required][FromQuery] string? NameMm
            , [Required][FromQuery] string? Type
            , [FromQuery] string? Id)
        {
            var response = await propositionRepository.CheckDuplicateByName(NameEn, NameMm, Type, Id);
            return Ok(response);


            //
        }

        /// <summary>
        /// [propositions request list]
        /// </summary>
        /// <response code="200">success</response>
        #region #list
        [HttpGet("request")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<PropositionRequestModelResponse>>), 200)]
        public async Task<IActionResult> List(
            [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size,
            [MaxLength(50)][FromQuery]string? name,
            [FromQuery]DateTime? FromDate, 
            [FromQuery]DateTime? ToDate,
            [FromQuery]Guid?[] partners,
            [FromQuery]Guid?[] categories,
            [FromQuery] string[]? membertype,
            [FromQuery] string[]? memberrole)
        {
            var response = await propositionRepository.GetRequestList(page ?? 1, size ?? 10, name, FromDate, ToDate, partners,categories,membertype,memberrole);
            return Ok(response);
        }
        #endregion



        [HttpGet("request/FilterItem/GetPartnerItemList")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<PartnerItemResponse>>), 200)]
        public async Task<IActionResult> GetPartnerItemList(
            [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size)
        {
            var response = await propositionRepository.GetPartnerItemList(page ?? 1, size ?? 10);
            return Ok(response);
        }


        [HttpGet("request/FilterItem/RoleItemList")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<PartnerItemResponse>>), 200)]
        public async Task<IActionResult> GetRoleItemList()
        {
            var response = await propositionRepository.GetRoleItemList();
            return Ok(response);
        }
    }
}
