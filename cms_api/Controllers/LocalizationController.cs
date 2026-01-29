using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core;
using aia_core.Repository.Cms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using CsvHelper;
using System.Globalization;
using System.Collections;
using Microsoft.IdentityModel.Tokens;
using ClosedXML.Excel;
using System.IO;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/localization")]
    public class LocalizationController : ControllerBase
    {
        private readonly ILocalizationRepository localizationRepository;
        public LocalizationController(ILocalizationRepository localizationRepository)
        {
            this.localizationRepository = localizationRepository;
        }


        [HttpPost("upload")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(ResponseModel), 200)]
        public async Task<IActionResult> Upload([Required] IFormFile? file)
        {
            if (file == null || file?.Length == 0 || file?.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                return BadRequest("Content type must be excel file type with .xlsx Or file must have data content!");
            }

            var response = await localizationRepository.Upload(file);

            return Ok(response);
        }

        [HttpGet("download")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public async Task<IActionResult> Download()
        {
            try
            {
                var response = await localizationRepository.Download();


                if (!response.IsNullOrEmpty())
                {
                    #region #Excel

                    // Prepare the Excel content
                    byte[] excelContent;

                    

                    using (var workbook = new XLWorkbook())
                    {
                        IXLWorksheet worksheet =
                        workbook.Worksheets.Add("Localization");



                        worksheet.Cell(1, 1).Value = "Key";
                        worksheet.Cell(1, 2).Value = "English";
                        worksheet.Cell(1, 3).Value = "Burmese";


                        worksheet.Cell(1, 1).Style.Font.Bold = true;
                        worksheet.Cell(1, 2).Style.Font.Bold = true;
                        worksheet.Cell(1, 3).Style.Font.Bold = true;

                        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
                        worksheet.Cell(1, 2).Style.Font.FontColor = XLColor.White;
                        worksheet.Cell(1, 3).Style.Font.FontColor = XLColor.White;

                        worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.Gray;
                        worksheet.Cell(1, 2).Style.Fill.BackgroundColor = XLColor.Gray;
                        worksheet.Cell(1, 3).Style.Fill.BackgroundColor = XLColor.Gray;

                        var dataRowAt = 2;

                        foreach (var dataRow in response)
                        {
                            worksheet.Cell(dataRowAt, 1).Value = dataRow.Key;
                            worksheet.Cell(dataRowAt, 2).Value = dataRow.English;
                            worksheet.Cell(dataRowAt, 3).Value = dataRow.Burmese;


                            dataRowAt++;
                        }

                        worksheet.Column(1).AdjustToContents();
                        worksheet.Column(2).AdjustToContents();
                        worksheet.Column(3).AdjustToContents();

                        //required using System.IO;  
                        using (var stream = new MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            excelContent = stream.ToArray();
                        }



                    }
                    

                    // Return the Excel file
                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    string fileName = "Localization.xlsx";
                    return File(excelContent, contentType, fileName);

                    #endregion
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("/localization/download exception thrown " + ex.Message);
            }

            return Ok("Error in downloading.");

        }
    }
}
