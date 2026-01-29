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
using aia_core.Model.Cms.Request.Dashboard;
using aia_core.Model.Cms.Response.Dashboard;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/dashboard")]
    public class DashboardController : BaseController
    {
        private readonly IDashboardRepository dashboardRepository;
        public DashboardController(IDashboardRepository dashboardRepository)
        {
            this.dashboardRepository = dashboardRepository;
        }

        #region #get chart
        [HttpGet("chart")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<DashboardChartResponse>), 200)]
        public async Task<IActionResult> GetChart([FromQuery] DashboardChartRequest model)
        {
            if (model.Type == EnumChartType.ClaimType)
            {
                return Ok(await dashboardRepository.GetChartByClaim(model));
            }
            else if (model.Type == EnumChartType.ClaimProductType)
            {
                return Ok(await dashboardRepository.GetChartByProduct(model));
            }
            else if (model.Type == EnumChartType.ClaimPerformance)
            {
                return Ok(await dashboardRepository.GetChartByPerformance(model));
            }
            else if (model.Type == EnumChartType.ClaimStatus)
            {
                return Ok(await dashboardRepository.GetChartByClaimStatus(model));
            }
            else if (model.Type == EnumChartType.ClaimFailLog)
            {
                return Ok(await dashboardRepository.GetChartByFailLog(model));
            }
            else if (model.Type == EnumChartType.ServiceType)
            {
                return Ok(await dashboardRepository.GetChartByServiceType(model));
            }
            else if (model.Type == EnumChartType.ServicePerformance)
            {
                return Ok(await dashboardRepository.GetChartByServicePerformance(model));
            }
            else if (model.Type == EnumChartType.ServiceStatus)
            {
                return Ok(await dashboardRepository.GetChartByServiceStatus(model));
            }
            else if (model.Type == EnumChartType.ServiceFailLog)
            {
                return Ok(await dashboardRepository.GetChartByServiceFailLog(model));
            }
            else
            {
                return Ok();
            }
        }
        #endregion

        #region #get total
        [HttpGet("total")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<DashboardTotalResponse>), 200)]
        public async Task<IActionResult> GetTotal([FromQuery] DashboardChartTotalRequest model)
        {

            return Ok(await dashboardRepository.GetChartTotal(model));

        }
        #endregion
    }
}