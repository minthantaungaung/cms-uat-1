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
using Hangfire;
using ClosedXML.Excel;
using mobile_api.Helper;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/claim")]
    public class ClaimController : BaseController
    {
        private readonly IClaimRepository claimRepository;
        public ClaimController(IClaimRepository claimRepository)
        {
            this.claimRepository = claimRepository;
        }

        


        #region #list
        [HttpPost]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<ClaimResponse>>), 200)]
        public async Task<IActionResult> List(ClaimRequest model)
        {
            var queryStartTime = Utils.GetDefaultDate();
            var response = claimRepository.List(model);

            Console.WriteLine($"ClaimListQueryTime => {(Utils.GetDefaultDate() - queryStartTime).TotalMilliseconds}");

            return Ok(response);
        }
        #endregion

        [HttpGet("get/detail")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<ClaimDetailResponse>>), 200)]
        public async Task<IActionResult> Get([Required][FromQuery] Guid? claimId)
        {
            var response = claimRepository.Get(claimId);
            return Ok(response);
        }


        [HttpGet("log/failed")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<FailedLogResponse>>), 200)]
        public async Task<IActionResult> FailedLog([FromQuery] FailedLogRequest model)
        {
            var response = claimRepository.FailedLog(model);
            return Ok(response);
        }

        [HttpPost("log/imaging")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<ImagingLogResponse>>), 200)]
        public async Task<IActionResult> ImagingLog(ImagingLogRequest model)
        {
            var response = claimRepository.ImagingLog(model);
            return Ok(response);
        }


        [HttpGet("log/test")]
        [ApiVersion("1.0")]
     
        public async Task<IActionResult> Test()
        {
            
            return Ok("I ma ok");
        }

        [HttpPost("log/imaging/detail")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<ImageLogDetail>), 200)]
        public async Task<IActionResult> ImagingLogDetail(ImageDetailRequest model)
        {
            var response = claimRepository.ImagingLogDetail(model.claimId, model.uploadId);
            return Ok(response);
        }

        [HttpGet("get/claim/status")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<ClaimStatusResp>>), 200)]
        public async Task<IActionResult> GetClaimStatus()
        {
            var response = claimRepository.GetClaimStatus();
            return Ok(response);
        }

        [HttpPost("update/claim/status")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<ClaimStatusResp>>), 200)]
        public async Task<IActionResult> UpdateClaimStatus([FromBody] ClaimStatusUpdateRequest model)
        {
            var response = claimRepository.UpdateClaimStatus(model);
            return Ok(response);
        }

        [HttpGet("log/crm-failed-log")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<CrmFailedLogResponse>>), 200)]
        public async Task<IActionResult> CrmFailedLog([FromQuery] FailedLogRequest model)
        {
            var response = claimRepository.CrmFailedLog(model);
            return Ok(response);
        }

        [HttpGet("log/export")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public async Task<IActionResult> Export([FromQuery] ClaimRequest model)
        {
            var methodStartTime = Utils.GetDefaultDate();
            //if (model.FromDate == null || model.ToDate == null)
            //{
            //    return BadRequest(new ResponseModel<object> { Code = 400, Message = "Please select a time frame of up to 15 days. Kindly ensure that your selection does not exceed this limit." });

            //}

            //var days = (model.ToDate.Value - model.FromDate.Value).TotalDays;
            //if(days > 15)
            //{
            //    return BadRequest(new ResponseModel<object> { Code = 400, Message = "Please select a time frame of up to 15 days. Kindly ensure that your selection does not exceed this limit." });

            //}

            var queryStartTime = Utils.GetDefaultDate();

            var response = claimRepository.Export(model);

            Console.WriteLine($"ClaimExportQueryTime => {(Utils.GetDefaultDate() - queryStartTime).TotalMilliseconds}");

            if (response?.Code == 0 && response?.Data != null)
            {
                var excelStartTime = Utils.GetDefaultDate();

                #region #Excel

                // Prepare the Excel content
                byte[] excelContent;

                using (var workbook = new XLWorkbook())
                {
                    IXLWorksheet worksheet =
                    workbook.Worksheets.Add("ClaimRequestList");

                    var headerRowAt = 1;
                    var headerColumnAt = 1;

                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Member ID"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Member Name"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Group Member ID"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Member Type"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Member Phone"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Diagnosis Name"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Poilcy No"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Claim ID"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Main Claim ID"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Claim Type"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Status"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Product Type"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Received Date"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "IL/COAST Status"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Last Updated By"; headerColumnAt++;
                    worksheet.Cell(headerRowAt, headerColumnAt).Value = "Last Updated Time"; 

                    for (int i = 1; i <= headerColumnAt; i++)
                    {
                        worksheet.Cell(headerRowAt, i).Style.Font.Bold = true;
                        worksheet.Cell(headerRowAt, i).Style.Font.FontColor = XLColor.White;
                        worksheet.Cell(headerRowAt, i).Style.Fill.BackgroundColor = XLColor.Gray;
                    }

                    var dataRowAt = 2;

                    foreach (var dataRow in response.Data)
                    {
                        var dataColumnAt = 1;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.ClientNo; dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.MemberName; dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.GroupClientNo; dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.MemberType; dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.MemberPhone; dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.DiagnosisName; dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.PolicyNo; dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.ClaimId?.ToString(); dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.MainClaimId?.ToString(); dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.ClaimType; dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.ClaimStatus; dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.ProductType; dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.TranDate; dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.ILStatus; dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.UpdatedBy; dataColumnAt++;
                        worksheet.Cell(dataRowAt, dataColumnAt).Value = dataRow.UpdatedDt; 

                        dataRowAt++;
                    }

                    for (int i = 1; i <= headerColumnAt; i++)
                    {
                        worksheet.Column(i).AdjustToContents();
                    }


                    //required using System.IO;  
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        excelContent = stream.ToArray();
                    }

                }


                // Return the Excel file
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string fileName = "ClaimRequestList.xlsx";


                Console.WriteLine($"ClaimExportExcelTime => {(Utils.GetDefaultDate() - excelStartTime).TotalMilliseconds}");
                Console.WriteLine($"ClaimExportAllProcessTime => {(Utils.GetDefaultDate() - methodStartTime).TotalMilliseconds}{Utils.GetDefaultDate()}");

                return File(excelContent, contentType, fileName);

                #endregion


                ////var excelStartTime = Utils.GetDefaultDate();

                ////var excelResult = ExcelGenerator.Generate(response?.Data.ToArray(), "ClaimRequestList");

                ////Console.WriteLine($"ClaimExportExcelTime => {(Utils.GetDefaultDate() - excelStartTime).TotalMilliseconds}");
                ////Console.WriteLine($"ClaimExportAllProcessTime => {(Utils.GetDefaultDate() - methodStartTime).TotalMilliseconds}{Utils.GetDefaultDate()}");

                ////return File(excelResult.Content, excelResult.ContentType, excelResult.FileName);
            }


            return Ok(response);



        }

        [HttpGet("log/ClaimTestExport")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<List<ClaimResponse>>), 200)]
        public async Task<IActionResult> ClaimTestExport([FromQuery] ClaimRequest model)
        {
            if (model.FromDate == null || model.ToDate == null)
            {
                return BadRequest("Please choose that date range!! Recommeded maximum date range is 3 months!!");
            }

            var logStartDate = Utils.GetDefaultDate();

            var response = claimRepository.Export(model);

            var logEndDate = Utils.GetDefaultDate();
            var timeDiff = (logEndDate - logStartDate).TotalMicroseconds;
            Console.WriteLine($"ClaimTestExport API => {logStartDate} {logEndDate} {timeDiff}");

            
            return Ok(response);



        }


        [HttpGet("log/export-old")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public async Task<IActionResult> Export([FromQuery][Required] DateTime fromDate, [FromQuery][Required] DateTime toDate)
        {
            try
            {
                var response = claimRepository.Export(fromDate, toDate);

                if (response != null && response.Code == 0 && response.Data != null)
                {
                    #region #Excel

                    // Prepare the Excel content
                    byte[] excelContent;

                    using (var workbook = new XLWorkbook())
                    {
                        IXLWorksheet worksheet =
                        workbook.Worksheets.Add("ClaimRequestList");

                        worksheet.Cell(1, 1).Value = "Member ID";
                        worksheet.Cell(1, 2).Value = "Member Name";
                        worksheet.Cell(1, 3).Value = "Group Member ID";
                        worksheet.Cell(1, 4).Value = "Member Type";
                        worksheet.Cell(1, 5).Value = "Member Phone";
                        worksheet.Cell(1, 6).Value = "Poilcy No";
                        worksheet.Cell(1, 7).Value = "Claim ID";
                        worksheet.Cell(1, 8).Value = "Main Claim ID";
                        worksheet.Cell(1, 9).Value = "Claim Type";
                        worksheet.Cell(1, 10).Value = "Status";
                        worksheet.Cell(1, 11).Value = "Product Type";
                        worksheet.Cell(1, 12).Value = "Received Date";
                        worksheet.Cell(1, 13).Value = "Remaining Time";
                        worksheet.Cell(1, 14).Value = "IL/COAST Status";
                        worksheet.Cell(1, 15).Value = "Last Updated By";
                        worksheet.Cell(1, 16).Value = "Last Updated Time";
                        worksheet.Cell(1, 17).Value = "Diagnosis Name";

                        for (int i = 1; i <= 17; i++)
                        {
                            worksheet.Cell(1, i).Style.Font.Bold = true;
                            worksheet.Cell(1, i).Style.Font.FontColor = XLColor.White;
                            worksheet.Cell(1, i).Style.Fill.BackgroundColor = XLColor.Gray;
                        }

                        var dataRowAt = 2;

                        foreach (var dataRow in response.Data)
                        {

                            worksheet.Cell(dataRowAt, 1).Value = dataRow.ClientNo;
                            worksheet.Cell(dataRowAt, 2).Value = dataRow.MemberName;
                            worksheet.Cell(dataRowAt, 3).Value = dataRow.GroupClientNo;
                            worksheet.Cell(dataRowAt, 4).Value = dataRow.MemberType;
                            worksheet.Cell(dataRowAt, 5).Value = dataRow.MemberPhone;
                            worksheet.Cell(dataRowAt, 6).Value = dataRow.PolicyNo;
                            worksheet.Cell(dataRowAt, 7).Value = dataRow.ClaimId?.ToString();
                            worksheet.Cell(dataRowAt, 8).Value = dataRow.MainClaimId?.ToString();
                            worksheet.Cell(dataRowAt, 9).Value = dataRow.ClaimType;
                            worksheet.Cell(dataRowAt, 10).Value = dataRow.ClaimStatus;
                            worksheet.Cell(dataRowAt, 11).Value = dataRow.ProductType;
                            worksheet.Cell(dataRowAt, 12).Value = dataRow.TranDate;
                            worksheet.Cell(dataRowAt, 13).Value = dataRow.RemainingHour;
                            worksheet.Cell(dataRowAt, 14).Value = dataRow.ILStatus;
                            worksheet.Cell(dataRowAt, 15).Value = dataRow.UpdatedBy;
                            worksheet.Cell(dataRowAt, 16).Value = dataRow.UpdatedDt;
                            worksheet.Cell(dataRowAt, 17).Value = dataRow.DiagnosisName;


                            dataRowAt++;
                        }

                        for (int i = 1; i <= 16; i++)
                        {
                            worksheet.Column(i).AdjustToContents();
                        }


                        //required using System.IO;  
                        using (var stream = new MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            excelContent = stream.ToArray();
                        }

                    }


                    // Return the Excel file
                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    string fileName = "ClaimRequestList.xlsx";
                    return File(excelContent, contentType, fileName);

                    #endregion
                }
                else
                {
                    return Ok("No data!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("/servicing/download exception thrown " + ex.Message);
                return Ok("Error in downloading.");
            }

        }


        //[HttpGet("log/validate-failed-log")]
        //[ApiVersion("1.0")]
        //[Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        //[ProducesResponseType(typeof(ResponseModel<PagedList<ClaimValidateMessageResponse>>), 200)]
        //public async Task<IActionResult> GetClaimValidateMessageList([FromQuery] ClaimValidateMessageRequest model)
        //{
        //    var response = claimRepository.GetClaimValidateMessageList(model);
        //    return Ok(response);
        //}

        [HttpPost("log/validate-failed-log")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<ClaimValidateMessageResponse>>), 200)]
        public async Task<IActionResult> GetClaimValidateMessageList(ClaimValidateMessageRequest model)
        {
            var response = claimRepository.GetClaimValidateMessageList(model);
            return Ok(response);
        }


        [HttpGet("log/imaging/export")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public async Task<IActionResult> ExportImagingLog([FromQuery] ImagingLogRequest model)
        {
            var response = claimRepository.ImagingLog(model);

            if (response?.Code == 0 && response?.Data?.DataList != null)
            {
                var excelResult = ExcelGenerator.Generate(response?.Data?.DataList.ToArray(), "ClaimImageLog");

                return File(excelResult.Content, excelResult.ContentType, excelResult.FileName);
            }

            return Ok();
        }


        [HttpGet("log/imaging/getproductlist")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<object>), 200)]
        public async Task<IActionResult> GetProductList()
        {
            var response = claimRepository.GetProductList();
            return Ok(response);
        }
    }

    public class ImageDetailRequest
    {
        [Required]
        public string? claimId { get; set; }

        [Required]
        public string? uploadId { get; set; }
    }
    
}