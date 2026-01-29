using Azure;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace mobile_api.Helper
{
    public static class ExcelGenerator
    {
        public static ExcelExportResult Generate<T>(System.Collections.Generic.IEnumerable<T> list, string filename)
        {

            using (var workbook = new XLWorkbook())
            {
                IXLWorksheet worksheet = workbook.Worksheets.Add("Sheet1");

                var properties = typeof(T).GetProperties();
                for (int i = 0; i < properties.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = properties[i].Name;
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.Gray;
                }

                var dataRowAt = 2;

                foreach (var dataRow in list)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        var value = properties[i].GetValue(dataRow);
                        if (value != null)
                        {
                            worksheet.Cell(dataRowAt, i + 1).Value = value.ToString();
                        }

                    }

                    dataRowAt++;
                }

                for (int i = 1; i <= properties.Length; i++)
                {
                    worksheet.Column(i).AdjustToContents();
                }

                // Save to a memory stream

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var excelContent = stream.ToArray();

                    return new ExcelExportResult
                    {
                        Content = excelContent,
                        ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        FileName = filename
                    };
                }
            }
        }



        //public static ExcelExportResult Generate<T>(System.Collections.Generic.IEnumerable<T> list, string filename)
        //{
        //    using (var workbook = new XLWorkbook())
        //    {
        //        IXLWorksheet worksheet = workbook.Worksheets.Add("Sheet1");
        //        var properties = typeof(T).GetProperties();

        //        // Header creation and styling
        //        for (int i = 0; i < properties.Length; i++)
        //        {
        //            var headerCell = worksheet.Cell(1, i + 1);
        //            headerCell.Value = properties[i].Name;
        //            headerCell.Style.Font.Bold = true;
        //            headerCell.Style.Font.FontColor = XLColor.White;
        //            headerCell.Style.Fill.BackgroundColor = XLColor.Gray;
        //        }

        //        // Use a range for data insertion
        //        var dataRowAt = 2;
        //        var dataRange = worksheet.Range(dataRowAt, 1, dataRowAt + list.Count() - 1, properties.Length);

        //        int rowIndex = 0;
        //        foreach (var dataRow in list)
        //        {
        //            for (int i = 0; i < properties.Length; i++)
        //            {
        //                var value = properties[i].GetValue(dataRow);
        //                if (value != null)
        //                {
        //                    dataRange.Cell(rowIndex + 1, i + 1).Value = value.ToString();
        //                }
        //            }
        //            rowIndex++;
        //        }

        //        // Comment out or limit column width adjustment for better performance
        //        // Adjust column width only for the first few rows if needed
        //        // for (int i = 1; i <= properties.Length; i++)
        //        // {
        //        //     worksheet.Column(i).AdjustToContents(1, 20); // Adjust based on first 20 rows
        //        // }

        //        // Save to a memory stream
        //        using (var stream = new MemoryStream())
        //        {
        //            workbook.SaveAs(stream);
        //            var excelContent = stream.ToArray();

        //            return new ExcelExportResult
        //            {
        //                Content = excelContent,
        //                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //                FileName = filename
        //            };
        //        }
        //    }
        //}
    }


    public class ExcelExportResult
    {
        public byte[] Content { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }
}

