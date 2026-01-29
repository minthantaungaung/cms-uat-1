using aia_core.Model.Cms.Request;
using aia_core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using aia_core.Repository.Cms;
using aia_core.Model.Cms.Response;
using aia_core.UnitOfWork;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/products")]
    public class ProductsController : BaseController
    {
        private readonly IProductRepository productRepository;
        public ProductsController(IProductRepository productRepository) 
        {
            this.productRepository = productRepository;
        }

        /// <summary>
        /// [products list]
        /// </summary>
        /// <response code="200">success</response>
        #region #list
        [HttpGet]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<ProductResponse>>), 200)]
        public async Task<IActionResult> List(
            [Required][Range(1, int.MaxValue)][DefaultValue(1)][FromQuery] int? page,
            [Required][Range(10, 100)][DefaultValue(10)][FromQuery] int? size,
            [MaxLength(50)][FromQuery] string? productName,
            [FromQuery] Guid[]? coverages)
        {
            var response = await productRepository.List(page ?? 1, size ?? 10, productName, coverages);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [details products]
        /// </summary>
        /// <response code="200">success</response>
        #region #details
        [HttpGet("{productId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<ProductResponse>), 200)]
        public async Task<IActionResult> Get([Required][FromRoute] Guid? productId)
        {
            var response = await productRepository.Get(productId ?? Guid.Empty);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [create products]
        /// </summary>
        /// <response code="200">success</response>
        #region #create
        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<ProductResponse>), 200)]
        public async Task<IActionResult> Create([FromForm] CreateProductRequest model)
        {
            var response = await productRepository.Create(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [update products]
        /// </summary>
        /// <response code="200">success</response>
        #region #update
        [HttpPut]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<ProductResponse>), 200)]
        public async Task<IActionResult> Update([FromForm] UpdateProductRequest model)
        {
            var response = await productRepository.Update(model);
            return Ok(response);
        }
        #endregion

        /// <summary>
        /// [delete products]
        /// </summary>
        /// <response code="200">success</response>
        #region #details
        [HttpDelete("{productId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<ProductResponse>), 200)]
        public async Task<IActionResult> Delete([Required][FromRoute] Guid? productId)
        {
            var response = await productRepository.Delete(productId ?? Guid.Empty);
            return Ok(response);
        }
        #endregion
    }
}
