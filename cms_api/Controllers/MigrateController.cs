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
    [Route("/v{version:apiVersion}/migrate")]
    public class MigrateController : BaseController
    {
        private readonly IMigrationRepository migrationRepository;
        public MigrateController(IMigrationRepository migrationRepository)
        {
            this.migrationRepository = migrationRepository;
        }

        #region #migrate
        [HttpPost]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.BasicAuthentication)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<BankResponse>>), 200)]
        public async Task<IActionResult> Migrate(MigrateRequest model)
        {
            var response = await migrationRepository.Migrate(model.MigrateAll, model.Retry, model.Condition);
            return Ok(response);
        }
        #endregion

       
    }

    public class MigrateRequest
    {
        public bool MigrateAll { get; set; }
        public bool? Retry { get; set; }
        public string? Condition { get; set; }   
    }
}