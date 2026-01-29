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
using mobile_api.Helper;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/members")]
    public class MembersController : ControllerBase
    {

        private readonly IMemberRepository memberRepository;
        public MembersController(IMemberRepository memberRepository)
        {
            this.memberRepository = memberRepository;
        }
       
        [HttpPost]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<MemberListResponse>>), 200)]
        public async Task<IActionResult> List(ListMemberRequest model)
        {
            var response = await memberRepository.List(model);
            return Ok(response);
        }

        [HttpGet("export")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public async Task<IActionResult> Export([FromQuery] ListMemberRequest model)
        {
            try
            {
                var response = await memberRepository.Export(model);


                //if(response != null && response.Code == 0 && response.Data != null)
                //{
                //    var stream = new MemoryStream();
                //    using (var writeFile = new StreamWriter(stream, leaveOpen: true))
                //    {
                //        var csv = new CsvWriter(writeFile, CultureInfo.InvariantCulture, true);
                //        IEnumerable ie = (IEnumerable)response.Data;
                //        csv.WriteRecords(ie);
                //    }
                //    stream.Position = 0;

                //    return File(stream, "application/octet-stream", "membership.csv");
                //}

                Console.WriteLine($"Member Export => {response?.Code} {response?.Data?.Count}");
                if (response?.Code == 0 && response?.Data != null)
                {
                    var excelResult = ExcelGenerator.Generate(response.Data.ToArray(), "MemberList");

                    return File(excelResult.Content, excelResult.ContentType, excelResult.FileName);
                }

            }
            catch (Exception ex)
            { 
                
            }


            return Ok();
        }

        [HttpGet("{AppRegMemberId}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<MemberResponse>), 200)]
        public async Task<IActionResult> Get([Required][FromRoute] string? AppRegMemberId)
        {
            var response = await memberRepository.Get(AppRegMemberId);
            return Ok(response);
        }

        [HttpPut]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<MemberResponse>), 200)]
        public async Task<IActionResult> Update(UpdateMemberRequest model)
        {
            var response = await memberRepository.Update(model);
            return Ok(response);
        }

    }
}
