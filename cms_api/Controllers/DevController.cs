using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using aia_core;
using aia_core.Entities;
using aia_core.Model;
using aia_core.Model.AiaCrm;
using aia_core.Model.Mobile.Request.Blog;
using aia_core.Model.Mobile.Response;
using aia_core.RecurringJobs;
using aia_core.Repository;
using aia_core.Repository.Mobile;
using aia_core.Services;
using aia_core.UnitOfWork;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/dev")]
    public class DevController : BaseController
    {

        //[HttpGet("GetOS")]
        //public IActionResult GetOS()
        //{
        //    string distributionName = GetLinuxDistributionName();
        //    Console.WriteLine("Distribution: " + distributionName);

        //    return Ok($"Operating System: {distributionName}");
        //}

        //public static string GetLinuxDistributionName()
        //{
        //    string result = RunShellCommand("cat /etc/os-release");
        //    string name = "Unknown";

        //    //if (!string.IsNullOrWhiteSpace(result))
        //    //{
        //    //    string[] lines = result.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        //    //    foreach (string line in lines)
        //    //    {
        //    //        string[] parts = line.Split(new[] { '=' }, 2);
        //    //        if (parts.Length == 2 && parts[0].Trim().ToLower() == "name")
        //    //        {
        //    //            name = parts[1].Trim().Trim('"');
        //    //            break;
        //    //        }
        //    //    }
        //    //}

        //    return result; //name;
        //}

        //public static string RunShellCommand(string command)
        //{
        //    string output = string.Empty;
        //    try
        //    {
        //        ProcessStartInfo startInfo = new ProcessStartInfo
        //        {
        //            FileName = "/bin/bash",
        //            Arguments = $"-c \"{command}\"",
        //            RedirectStandardOutput = true,
        //            UseShellExecute = false,
        //            CreateNoWindow = true
        //        };

        //        using (Process process = Process.Start(startInfo))
        //        {
        //            if (process != null)
        //            {
        //                output = process.StandardOutput.ReadToEnd();
        //                process.WaitForExit();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Exception while running shell command: " + ex.Message);
        //    }

        //    return output;
        //}

        #region "const"
        private IDevRepository devRepository;
        private IAiaCrmApiService aiaCrmApiService;
        private IRecurringJobRunner recurringJobRunner;

        public DevController(IDevRepository devRepository, IAiaCrmApiService aiaCrmApiService, IRecurringJobRunner recurringJobRunner)
        {
            this.devRepository = devRepository;
            this.aiaCrmApiService = aiaCrmApiService;
            this.recurringJobRunner = recurringJobRunner;
        }
        #endregion

        [HttpGet("TestSendClaimSms")]
        public async Task<IActionResult> TestSendClaimSms()
        {
            await recurringJobRunner.SendClaimSms();
            return Ok();
        }


        [HttpGet("TestSendServicingSms")]
        public async Task<IActionResult> TestSendServicingSms()
        {
            var data = recurringJobRunner.SendServicingSms();
            return Ok(data);
        }


        [HttpGet("CheckBeneficiaryStatusAndSendNoti")]
        public async Task<IActionResult> CheckBeneficiaryStatusAndSendNoti()
        {
            var data = recurringJobRunner.CheckBeneficiaryStatusAndSendNoti();
            return Ok(data);
        }

        [HttpGet("SendServiceNotification")]
        public async Task<IActionResult> SendServiceNotification()
        {
            var data = recurringJobRunner.SendServiceNotification();
            return Ok(data);
        }

        //[HttpGet("TestUpdateClaimStatus")]
        //[Authorize(AuthenticationSchemes = DefaultConstants.BasicAuthentication)]
        //public async Task<IActionResult> TestUpdateClaimStatus([FromQuery][Required] string otp)
        //{
        //    var data = recurringJobRunner.UpdateClaimStatus(false, otp);
        //    return Ok(data);
        //}

        //[HttpGet("TestUpdateMemberDataPullFromAiaCoreTables")]
        //[Authorize(AuthenticationSchemes = DefaultConstants.BasicAuthentication)]
        //public async Task<IActionResult> UpdateMemberDataPullFromAiaCoreTables([FromQuery][Required] string otp)
        //{

        //    if(otp == "131369")
        //    {
        //        var data = recurringJobRunner.UpdateMemberDataPullFromAiaCoreTables();
        //        return Ok(data);
        //    }
        //    return Ok();
        //}

        //[HttpGet("RunScheduleTesting")]
        ////[Authorize(AuthenticationSchemes = DefaultConstants.BasicAuthentication)]
        //public IActionResult RunScheduleTesting()
        //{
        //    recurringJobRunner.UpdateMemberDataPullFromAiaCoreTables();

        //    return Ok();
        //}

        //[HttpGet("RunSchedule_UpdateMemberTypeAndGroupMemberId")]
        ////[Authorize(AuthenticationSchemes = DefaultConstants.BasicAuthentication)]
        //public IActionResult UpdateMemberTypeAndGroupMemberId()
        //{
        //    recurringJobRunner.UpdateMemberTypeAndGroupMemberId();
        //    return Ok();
        //}

        //[HttpGet("RunScheduleJob/{jobid}")]
        //[Authorize(AuthenticationSchemes = DefaultConstants.BasicAuthentication)]
        //public IActionResult RunScheduleJob([Required][FromRoute] string? jobid)
        //{
        //    if (jobid == "claim")
        //    {
        //        recurringJobRunner.SendClaimNotification();
        //    }
        //    else if (jobid == "service")
        //    {
        //        recurringJobRunner.SendServiceNotification();
        //    }

        //    return Ok();
        //}

        //// #region #Get List
        //// [HttpGet("testssl")]
        //// [ApiVersion("1.0")]
        //// [Authorize(AuthenticationSchemes = DefaultConstants.BasicAuthentication)]
        //// [ProducesResponseType(typeof(ResponseModel<BlogListResponse> ), 200)]
        //// public async Task<IActionResult> TestSSL()
        //// {
        ////     string apiUrl = "https://mmcmsuat.aiaazure.biz/cms/documents/getDocumentsList";
        ////     string cacertPath = "/app/wwwroot/cert/mmcmsuat.aiaazure.biz.cer";
        ////     string token = "0IPDXIc8WsubaQVGnZ/Phg/9uywvyAJdiQ6PQmdzmK3V7HN1yJwg53BjRAWGO2xQxvJ1OJS+GClk3iVsHrkZ";

        ////     // Create and configure the HttpClientHandler
        ////     var httpClientHandler = new HttpClientHandler();
        ////     httpClientHandler.ClientCertificates.Add(new X509Certificate2(cacertPath));
        ////     httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

        ////     using (HttpClient httpClient = new HttpClient(httpClientHandler))
        ////     {
        ////         // Create the JSON request payload
        ////         string jsonRequest = @"
        ////         {
        ////             ""templateId"": ""63fc2372e4b08ddf2a10f17b"",
        ////             ""PolicyNo"": [""H003357407""],
        ////             ""docTypeId"": [""AC01A""],
        ////             ""receiveDateStart"": ""2023-03-01"",
        ////             ""receiveDateEnd"": ""2023-12-31"",
        ////             ""pageNum"": 1,
        ////             ""pageSize"": 10
        ////         }";

        ////         // Create the request content
        ////         StringContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        ////         // Add the token header
        ////         httpClient.DefaultRequestHeaders.Add("token", token);

        ////         // Send the POST request
        ////         HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);

        ////         // Check if the response is successful
        ////         if (response.IsSuccessStatusCode)
        ////         {
        ////             string responseBody = await response.Content.ReadAsStringAsync();
        ////             Console.WriteLine(responseBody);
        ////             return Ok(responseBody);
        ////         }
        ////         else
        ////         {
        ////             Console.WriteLine("Error: " + response.StatusCode);
        ////             return Ok(response);
        ////         }
        ////     }

        //// }
        //// #endregion

        //// [HttpGet("log")]
        //// [AllowAnonymous]
        //// [ProducesResponseType(typeof(ResponseModel<PagedList<ErrorLogCms>>), 200)]
        //// public async Task<IActionResult> List([FromQuery] ErrorLogRequest model)
        //// {
        ////     var response = await devRepository.GetCMSErrorLogList(model);
        ////     return Ok(response);
        //// }

        //// #region #Test Error"
        //// [HttpGet("testerror")]
        //// [ApiVersion("1.0")]
        //// [AllowAnonymous]
        //// [ProducesResponseType(typeof(ResponseModel<string> ), 200)]
        //// public async Task<IActionResult> testerror()
        //// {
        ////     return Ok(devRepository.TestCmsError());
        //// }
        //// #endregion

        //// #region #Test crm"
        //// [HttpPost("test")]
        //// [ApiVersion("1.0")]
        //// [AllowAnonymous]
        //// [ProducesResponseType(typeof(ResponseModel<string> ), 200)]
        //// public async Task<IActionResult> test([FromBody]CaseRequest model)
        //// {
        ////     // CaseRequest model = new CaseRequest();
        ////     // model.CustomerInfo = new CustomerInfo();
        ////     // model.CustomerInfo.ClientNumber = "12345";
        ////     // model.CustomerInfo.FirstName = "KZM";
        ////     // model.CustomerInfo.LastName = "Moore";
        ////     // model.CustomerInfo.Email = "kyawzaymoore@codigo.co";

        ////     // model.PolicyInfo = new PolicyInfo();
        ////     // model.PolicyInfo.PolicyNumber = "policy number";

        ////     // model.Request = new Request();
        ////     // model.Request.CaseCategory = "";
        ////     // model.Request.Channel = "";
        ////     // model.Request.ClaimId = "";
        ////     // model.Request.CaseType = "TAC";
        ////     // model.Request.RequestId = "123";

        ////     var response = await aiaCrmApiService.CreateCase(model);
        ////     return Ok(response);
        //// }
        //// #endregion



        //[HttpGet("TestSingleQuoteFromQuery")]
        //public IActionResult TestSingleQuoteFromQuery([FromQuery] TestObject testObject)
        //{

        //    return Ok();
        //}

        //[HttpPost("TestSingleQuoteFromBody")]
        //public IActionResult TestSingleQuoteFromBody([FromBody] TestObject testObject)
        //{

        //    return Ok();
        //}

        [HttpGet("TestSingleQuoteFromRoute/{_string}")]
        public IActionResult TestSingleQuoteFromRoute([FromRoute] string _string)
        {

            return Ok();
        }

        //[HttpGet("TestSingleQuoteFromCustom")]
        //public IActionResult TestSingleQuoteFromCustom(string _string, string? _string2, decimal? _decimal)
        //{

        //    return Ok();
        //}
    }

    public class TestObject
    {
        public string? _string { get; set; }
        public decimal? _decimal { get; set; }
        public int? _int { get; set; }
        public object? _object { get; set; }
        public DateTime? _dateTime { get; set; }
        public bool? _bool { get; set; }
    }
       
}
