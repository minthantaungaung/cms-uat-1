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
    [Route("/v{version:apiVersion}/blogs")]
    public class BlogsController : BaseController
    {
        private readonly IBlogRepository blogRepository;
        public BlogsController(IBlogRepository blogRepository)
        {
            this.blogRepository = blogRepository;
        }

        /// <summary>
        /// [blogs list]
        /// </summary>
        /// <response code="200">success</response>
        #region #list
        [HttpGet]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<BlogResponse>>), 200)]
        public async Task<IActionResult> List(
            [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size,
            [MaxLength(50)][FromQuery]string? title,
            [MaxLength(50)][FromQuery] string? topic,
            [FromQuery]EnumBlogStatus[]? status,
            [FromQuery]bool? feature)
        {
            var response = await blogRepository.List(page ?? 1, size ?? 10, title, topic, status, feature);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [details blogs]
        /// </summary>
        /// <response code="200">success</response>
        #region #details
        [HttpGet("{blogId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<BlogResponse>), 200)]
        public async Task<IActionResult> Get([Required][FromRoute] Guid? blogId)
        {
            var response = await blogRepository.Get(blogId ?? Guid.Empty);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [create blogs]
        /// </summary>
        /// <response code="200">success</response>
        #region #create
        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<BlogResponse>), 200)]
        public async Task<IActionResult> Create([FromForm] CreateBlogRequest model)
        {
            var response = await blogRepository.Create(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update blogs]
        /// </summary>
        /// <response code="200">success</response>
        #region #update
        [HttpPut]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<BlogResponse>), 200)]
        public async Task<IActionResult> Update([FromForm] UpdateBlogRequest model)
        {
            var response = await blogRepository.Update(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [delete blog]
        /// </summary>
        /// <response code="200">success</response>
        #region #details
        [HttpDelete("{blogId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<BlogResponse>), 200)]
        public async Task<IActionResult> Delete([Required][FromRoute] Guid? blogId)
        {
            var response = await blogRepository.Delete(blogId ?? Guid.Empty);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update feature status]
        /// </summary>
        /// <response code="200">success</response>
        #region #update-feature
        [HttpPut("{blogId}/feature/{status}")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<BlogResponse>), 200)]
        public async Task<IActionResult> Feature([Required][FromRoute] Guid? blogId,
            [Required][FromRoute] bool? status)
        {
            var response = await blogRepository.Feature(blogId ?? Guid.Empty, status.Value);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [get all blogs list]
        /// </summary>
        /// <response code="200">success</response>
        #region #list
        [HttpGet("order")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<BlogResponse>>), 200)]
        public async Task<IActionResult> Order()
        {
            var response = await blogRepository.GetAll();
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update order]
        /// </summary>
        /// <response code="200">success</response>
        #region #update-order
        [HttpPut("order")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<BlogResponse>>), 200)]
        public async Task<IActionResult> SortOrder([Required][FromBody] List<BlogOrderRequest> model)
        {
            var response = await blogRepository.Order(model);
            return Ok(response);
        }
        #endregion
    }
}
