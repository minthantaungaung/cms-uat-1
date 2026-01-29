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
using System.Formats.Asn1;
using System.Globalization;
using CsvHelper;
using System.Collections;
using aia_core.Entities;
using aia_core.Model.Cms.Response.MemberPolicyResponse;
using aia_core.RecurringJobs;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/memberpolicy")]
    public class MembersPolicyController : ControllerBase
    {

        private readonly IMemberRepository memberRepository;
        private readonly IMemberPolicyRepository memberPolicyRepository;
        private readonly IRecurringJobRunner recurringJobRunner;

        public MembersPolicyController(IMemberRepository memberRepository, IMemberPolicyRepository memberPolicyRepository, IRecurringJobRunner recurringJobRunner)
        {
            this.memberRepository = memberRepository;
            this.memberPolicyRepository = memberPolicyRepository;
            this.recurringJobRunner = recurringJobRunner;
        }



      

        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<MemberPolicyListResponse>>), 200)]
        public async Task<IActionResult> List(MemberPolicyListRequest model)
        {
            var response = await memberPolicyRepository.List(model);
            return Ok(response);
        }

        [HttpGet("get-policies/{memberId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<MemberPolicyResponse>), 200)]
        public async Task<IActionResult> GetPolicies([Required][FromRoute] string? memberId)
        {
            var response = await memberPolicyRepository.GetPolicies(memberId);
            return Ok(response);
        }

        [HttpGet("get-policy-detail")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PolicyDetailResponse>), 200)]
        public async Task<IActionResult> GetPolicyDetail([Required][FromQuery] string? insuredId, [Required][FromQuery] string? policyNo)
        {
            var response = await memberPolicyRepository.GetPolicyDetail(insuredId, policyNo);
            return Ok(response);
        }

        [HttpGet("get-coverages/{memberId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PolicyCoveragesResponse>), 200)]
        public async Task<IActionResult> GetCoverages([Required][FromRoute] string? memberId)
        {
            var response = await memberPolicyRepository.GetCoverages(memberId);
            return Ok(response);
        }



        [HttpGet("get-coverages-by-clientno/{clientno}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PolicyCoveragesResponse>), 200)]
        public async Task<IActionResult> GetCoveragesByClientNo([Required][FromRoute] string? clientno)
        {
            var response = await memberPolicyRepository.GetCoveragesByClientNo(clientno);
            return Ok(response);
        }
    }
}
