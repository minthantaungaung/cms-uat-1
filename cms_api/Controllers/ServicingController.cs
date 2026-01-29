using aia_core.Model.Cms.Response;
using aia_core;
using aia_core.Repository.Cms;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using aia_core.Model.Cms.Response.Servicing;
using aia_core.Model.Cms.Request.Servicing;
using System.ComponentModel.DataAnnotations;
using aia_core.Model.Cms.Response.ServicingDetail;
using Hangfire;
using ClosedXML.Excel;
using mobile_api.Helper;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/servicing")]
    public class ServicingController : BaseController
    {
        private readonly IServicingRepository servicingRepository;
        public ServicingController(IServicingRepository servicingRepository)
        {
            this.servicingRepository = servicingRepository;
        }


        #region #list
        [HttpGet]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<ServicingListResponse>>), 200)]
        public async Task<IActionResult> List([FromQuery] ServicingListRequest model)
        {
            var response = servicingRepository.List(model);
            return Ok(response);
        }
        #endregion

        #region #list
        [HttpGet("{serviceId}")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<ServiceDetailResponse>), 200)]
        public async Task<IActionResult> Get([Required][FromRoute] Guid serviceId)
        {
            var response = servicingRepository.Get(serviceId);
            return Ok(response);
        }
        #endregion


        

        [HttpPost("update-servicestatus")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<string>), 200)]
        public async Task<IActionResult> UpdateServiceStatus(ServiceStatusUpdateRequest model)
        {
            var response = servicingRepository.UpdateServiceStatus(model);
            return Ok(response);
        }


        #region #list
        [HttpGet("log/imaging")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<ServiceImagingLogResponse>>), 200)]
        public async Task<IActionResult> ImagingLog([FromQuery] ServiceImagingLogRequest model)
        {
            var response = servicingRepository.ImagingLog(model);
            return Ok(response);
        }
        #endregion


        

        #region #list
        [HttpGet("log/failed")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<PagedList<ServiceFailedLogResponse>>), 200)]
        public async Task<IActionResult> FailedLog([FromQuery] ServiceFailedLogRequest model)
        {
            var response = servicingRepository.FailedLog(model);
            return Ok(response);
        }
        #endregion

        #region #list
        [HttpGet("log/failed/{id}")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel<FailedLogDetailResponse>), 200)]
        public async Task<IActionResult> GetFailedLogDetail([FromRoute] [Required] Guid id)
        {
            var response = servicingRepository.GetFailedLogDetail(id);
            return Ok(response);
        }
        #endregion


        //[HttpGet("log/export")]
        //[Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        //[ProducesResponseType(typeof(FileStreamResult), 200)]
        //public async Task<IActionResult> Export([FromQuery][Required] DateTime fromDate, [FromQuery][Required] DateTime toDate)
        //{
        //    try
        //    {
        //        var response = servicingRepository.Export(fromDate, toDate);

        //        if (response != null && response.Code == 0 && response.Data != null)
        //        {
        //            #region #Excel

        //            // Prepare the Excel content
        //            byte[] excelContent;

        //            using (var workbook = new XLWorkbook())
        //            {
        //                IXLWorksheet worksheet =
        //                workbook.Worksheets.Add("ServicingRequestList");

        //                worksheet.Cell(1, 1).Value = "Main ID";
        //                worksheet.Cell(1, 2).Value = "Id";
        //                worksheet.Cell(1, 3).Value = "Policy Number";
        //                worksheet.Cell(1, 4).Value = "Policy Status";
        //                worksheet.Cell(1, 5).Value = "Member Name";
        //                worksheet.Cell(1, 6).Value = "Member ID";
        //                worksheet.Cell(1, 7).Value = "Group Member ID";
        //                worksheet.Cell(1, 8).Value = "Member Type";
        //                worksheet.Cell(1, 9).Value = "Member Phone";
        //                worksheet.Cell(1, 10).Value = "Service Type";
        //                worksheet.Cell(1, 11).Value = "Status";
        //                worksheet.Cell(1, 12).Value = "Remaining Time";
        //                worksheet.Cell(1, 13).Value = "Submission Date";
        //                worksheet.Cell(1, 14).Value = "Status Updated By";
        //                worksheet.Cell(1, 15).Value = "Status Updated Date";

        //                for (int i = 1; i <= 15; i++)
        //                {
        //                    worksheet.Cell(1, i).Style.Font.Bold = true;
        //                    worksheet.Cell(1, i).Style.Font.FontColor = XLColor.White;
        //                    worksheet.Cell(1, i).Style.Fill.BackgroundColor = XLColor.Gray;
        //                }

        //                var dataRowAt = 2;

        //                foreach (var dataRow in response.Data)
        //                {

        //                    worksheet.Cell(dataRowAt, 1).Value = dataRow.MainId?.ToString();
        //                    worksheet.Cell(dataRowAt, 2).Value = dataRow.ServiceId?.ToString();
        //                    worksheet.Cell(dataRowAt, 3).Value = dataRow.PolicyNumber;
        //                    worksheet.Cell(dataRowAt, 4).Value = dataRow.PolicyStatus;
        //                    worksheet.Cell(dataRowAt, 5).Value = dataRow.MemberName;
        //                    worksheet.Cell(dataRowAt, 6).Value = dataRow.MemberId;
        //                    worksheet.Cell(dataRowAt, 7).Value = dataRow.GroupMemberId;
        //                    worksheet.Cell(dataRowAt, 8).Value = dataRow.MemberType;
        //                    worksheet.Cell(dataRowAt, 9).Value = dataRow.MemberPhone;
        //                    worksheet.Cell(dataRowAt, 10).Value = dataRow.ServiceType;
        //                    worksheet.Cell(dataRowAt, 11).Value = dataRow.ServiceStatus;
        //                    worksheet.Cell(dataRowAt, 12).Value = dataRow.RemainingTime;
        //                    worksheet.Cell(dataRowAt, 13).Value = dataRow.SubmissionDate;
        //                    worksheet.Cell(dataRowAt, 14).Value = dataRow.StatusUpdatedBy;
        //                    worksheet.Cell(dataRowAt, 15).Value = dataRow.StatusUpdatedDate;

        //                    dataRowAt++;
        //                }

        //                for (int i = 1; i <= 15; i++)
        //                {
        //                    worksheet.Column(i).AdjustToContents();
        //                }


        //                //required using System.IO;  
        //                using (var stream = new MemoryStream())
        //                {
        //                    workbook.SaveAs(stream);
        //                    excelContent = stream.ToArray();
        //                }

        //            }


        //            // Return the Excel file
        //            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        //            string fileName = "ServicingRequestList.xlsx";
        //            return File(excelContent, contentType, fileName);

        //            #endregion
        //        }
        //        else
        //        {
        //            return Ok("No data!");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("/servicing/download exception thrown " + ex.Message);
        //        return Ok("Error in downloading.");
        //    }

        //}


        #region #list
        [HttpGet("log/imaging/export")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public async Task<IActionResult> ExportImagingLog([FromQuery] ServiceImagingLogRequest model)
        {
            var response = servicingRepository.ImagingLog(model);

            if (response?.Code == 0 && response?.Data?.DataList != null)
            {
                var excelResult = ExcelGenerator.Generate(response?.Data?.DataList.ToArray(), "ServicingImageLog");

                return File(excelResult.Content, excelResult.ContentType, excelResult.FileName);
            }

            return Ok(response);
        }

        [HttpGet("log/export")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public async Task<IActionResult> Export([FromQuery] ServicingListRequest model)
        {
            var methodStartTime = Utils.GetDefaultDate();


            //if (model.FromDate == null || model.ToDate == null)
            //{
            //    //return BadRequest("Please choose that date range!! Limited maximum date range is 7 days");
                

            //    return BadRequest(new ResponseModel<object> { Code = 400, Message = "Please select a time frame of up to 15 days. Kindly ensure that your selection does not exceed this limit." });

            //}

            //var days = (model.ToDate.Value - model.FromDate.Value).TotalDays;
            //if (days > 15)
            //{
            //    return BadRequest(new ResponseModel<object> { Code = 400, Message = "Please select a time frame of up to 15 days. Kindly ensure that your selection does not exceed this limit." });

            //}

            var queryStartTime = Utils.GetDefaultDate();

            var response = servicingRepository.Export(model);

            Console.WriteLine($"ServiceExportQueryTime => {(Utils.GetDefaultDate() - queryStartTime).TotalMilliseconds}");


            if (response?.Code == 0 && response?.Data?.DataList != null)
            {
                var excelStartTime = Utils.GetDefaultDate();


                #region #Excel

                // Prepare the Excel content
                byte[] excelContent;

                using (var workbook = new XLWorkbook())
                {
                    IXLWorksheet worksheet =
                    workbook.Worksheets.Add("ServicingRequestList");

                    worksheet.Cell(1, 1).Value = "Main ID";
                    worksheet.Cell(1, 2).Value = "Id";
                    worksheet.Cell(1, 3).Value = "Policy Number";
                    worksheet.Cell(1, 4).Value = "Policy Status";
                    worksheet.Cell(1, 5).Value = "Member Name";
                    worksheet.Cell(1, 6).Value = "Member ID";
                    worksheet.Cell(1, 7).Value = "Group Member ID";
                    worksheet.Cell(1, 8).Value = "Member Type";
                    worksheet.Cell(1, 9).Value = "Member Phone";
                    worksheet.Cell(1, 10).Value = "Service Type";
                    worksheet.Cell(1, 11).Value = "Status";
                    //worksheet.Cell(1, 12).Value = "Remaining Time";
                    worksheet.Cell(1, 12).Value = "Submission Date";
                    worksheet.Cell(1, 13).Value = "Status Updated By";
                    worksheet.Cell(1, 14).Value = "Status Updated Date";

                    for (int i = 1; i <= 15; i++)
                    {
                        worksheet.Cell(1, i).Style.Font.Bold = true;
                        worksheet.Cell(1, i).Style.Font.FontColor = XLColor.White;
                        worksheet.Cell(1, i).Style.Fill.BackgroundColor = XLColor.Gray;
                    }

                    var dataRowAt = 2;

                    foreach (var dataRow in response?.Data?.DataList)
                    {

                        worksheet.Cell(dataRowAt, 1).Value = dataRow.MainId?.ToString();
                        worksheet.Cell(dataRowAt, 2).Value = dataRow.ServiceId?.ToString();
                        worksheet.Cell(dataRowAt, 3).Value = dataRow.PolicyNumber;
                        worksheet.Cell(dataRowAt, 4).Value = dataRow.PolicyStatus;
                        worksheet.Cell(dataRowAt, 5).Value = dataRow.MemberName;
                        worksheet.Cell(dataRowAt, 6).Value = dataRow.MemberId;
                        worksheet.Cell(dataRowAt, 7).Value = dataRow.GroupMemberId;
                        worksheet.Cell(dataRowAt, 8).Value = dataRow.MemberType;
                        worksheet.Cell(dataRowAt, 9).Value = dataRow.MemberPhone;
                        worksheet.Cell(dataRowAt, 10).Value = dataRow.ServiceType;
                        worksheet.Cell(dataRowAt, 11).Value = dataRow.ServiceStatus;
                        //worksheet.Cell(dataRowAt, 12).Value = dataRow.RemainingTime;
                        worksheet.Cell(dataRowAt, 12).Value = dataRow.SubmissionDate;
                        worksheet.Cell(dataRowAt, 13).Value = dataRow.StatusUpdatedBy;
                        worksheet.Cell(dataRowAt, 14).Value = dataRow.StatusUpdatedDate;

                        dataRowAt++;
                    }

                    for (int i = 1; i <= 15; i++)
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
                string fileName = "ServicingRequestList.xlsx";

                Console.WriteLine($"ServiceExportExcelTime => {(Utils.GetDefaultDate() - excelStartTime).TotalMilliseconds}");

                Console.WriteLine($"ServiceExportAllProcessTime => {(Utils.GetDefaultDate() - methodStartTime).TotalMilliseconds}{Utils.GetDefaultDate()}");


                return File(excelContent, contentType, fileName);

                #endregion
            }


            return Ok(response);


            
        }


        [HttpGet("log/NormalResponseTest")]        
        public async Task<IActionResult> NormalResponseTest()
        {


            var response = new ResponseModel<string> { Code = 0, Message = "Success", Data = "Normal Response" };
            return Ok(response);

        }

        [HttpGet("log/BadRequestTest")]
        public async Task<IActionResult> BadRequestTest()
        {


            var response = new ResponseModel<string> { Code = 0, Message = "Success", Data = "Bad Request" };
            return BadRequest(response);

        }


        [HttpGet("log/GatewayTimeoutTest")]
        public async Task<IActionResult> GatewayTimeoutTest()
        {

            Thread.Sleep(180000);

            return Ok();

        }

        #endregion
    }
}
