
using Microsoft.AspNetCore.Http;
using nClam;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace aia_core.Extension
{
    public class AllowedFileExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;
        private string clamAvServerUrl;
        private int clamAvServerPort;

        public AllowedFileExtensionsAttribute(params string[] extensions)
        {
            _extensions = extensions;

            //try
            //{
            //    clamAvServerUrl = AppSettingsHelper.GetSetting("ClamAVServer:URL");
            //    clamAvServerPort = Convert.ToInt32(AppSettingsHelper.GetSetting("ClamAVServer:Port"));
            //}
            //catch { }
                        
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            clamAvServerUrl = AppSettingsHelper.GetSetting("ClamAVServer:URL");
            clamAvServerPort = Convert.ToInt32(AppSettingsHelper.GetSetting("ClamAVServer:Port"));

            if (value is IFormFile file)
            {
                string[] parts = file.FileName.Split('.');
                if (parts.Length == 3)
                {
                    return new ValidationResult($"Not allowed to upload file with double extension");
                }

                var fileExtension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();

                if (_extensions.Any(ext => ext.ToLowerInvariant() == fileExtension) == false)
                {
                    return new ValidationResult($"Only the following file types are allowed: {string.Join(", ", _extensions)}");
                }

                string reason = "";

                // Check for malicious content

                //temporay bypassed ContainsMaliciousContent 17/06/2024

                if (ContainsMaliciousContent(file, out reason))
                {
                    return new ValidationResult(reason);
                }


                //Test

                // Check for malicious Javascript
                (bool IsMalicious, string _reason) = ContainsMaliciousJavaScript(file).Result;
                if (IsMalicious)
                {
                    return new ValidationResult($"{_reason} {file.FileName}");
                }

            }

            return ValidationResult.Success;
        }

        private async Task<(bool IsMalicious, string Reason)> ContainsMaliciousJavaScript(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return (true, "File not selected");
                }

                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8))
                    {
                        string fileContent = await streamReader.ReadToEndAsync();

                        // Define regular expressions to detect common JavaScript patterns
                        Regex[] maliciousPatterns = new Regex[]
                        {

                            new Regex(@"<script\b[^>]*>", RegexOptions.IgnoreCase),
                            new Regex(@"\b(?:eval|setTimeout|setInterval|execCommand|document\.write)\s*\(", RegexOptions.IgnoreCase),

                            new Regex(@"(?:\b\w+\s*=\s*|\.)(?:String\.fromCharCode|unescape)\s*\(\s*(?:\""[^\""]*\"")+\s*\)", RegexOptions.IgnoreCase),
                            new Regex(@"(?:\b\w+\s*=\s*)?(?:String\.fromCharCode|unescape)\s*\(\s*(?:0x[0-9a-fA-F]+,?\s*)+\s*\)", RegexOptions.IgnoreCase),
                            new Regex(@"(?:\b\w+\s*=\s*)?(?:String\.fromCharCode|unescape)\s*\(\s*(?:\\x[0-9a-fA-F]{2,})+\s*\)", RegexOptions.IgnoreCase),
                            new Regex(@"\b(?:document\.write)\s*\(", RegexOptions.IgnoreCase),
                            new Regex(@"\b(?:\w+\s*=\s*)?(?:String\.fromCharCode|unescape)\s*\(", RegexOptions.IgnoreCase),
                            new Regex(@"(?:\b\w+\s*=\s*|\.)(?:eval|setTimeout|setInterval|execCommand)\s*\(", RegexOptions.IgnoreCase),
                            new Regex(@"\b(?:window\.location)\s*=", RegexOptions.IgnoreCase),
                            new Regex(@"\b(?:new\s+Function|Function\()", RegexOptions.IgnoreCase),

                            //new Regex(@"\b(?:alert|XSS)\b", RegexOptions.IgnoreCase),


                            new Regex(@"\b(?:XSS)\b", RegexOptions.IgnoreCase)

                        };



                        //if(fileContent.Contains("XSS") || fileContent.ToLower().Contains("xss"))
                        //{
                        //    return (true, "Found malicious JavaScript code");
                        //}

                        //Test



                        return (false, "No malicious JavaScript code detected");
                    }
                }
            }
            catch (Exception ex)
            {
                return (true, $"Exception: {ex.Message}");
            }
        }


        private bool ContainsMaliciousContent(IFormFile file, out string reason)
        {
            reason = "";
            var scanFinalResult = true;

            

            try
            {

                if (file == null || file.Length == 0)
                {
                    reason = "file not selected";
                    return scanFinalResult;

                }

                var ms = new MemoryStream();
                file.OpenReadStream().CopyTo(ms);
                byte[] fileBytes = ms.ToArray();

                var clam = new ClamClient(clamAvServerUrl, clamAvServerPort);

                Console.WriteLine($"ClamAv server > {clamAvServerUrl} {clamAvServerPort}");

                var scanResult = clam.SendAndScanFileAsync(fileBytes).Result;

                Console.WriteLine($"ClamAv scanResult.Result > {scanResult.Result}");
                switch (scanResult.Result)
                {
                    case ClamScanResults.Clean:
                        reason = $"The file is clean! ScanResult:{scanResult.RawResult}";
                        scanFinalResult = false;
                        break;

                    case ClamScanResults.VirusDetected:
                        reason = $"Virus Found! Virus name: {scanResult.InfectedFiles?.FirstOrDefault()?.VirusName}";
                        scanFinalResult = true;
                        break;

                    case ClamScanResults.Error:
                        reason = $"An error occured while scaning the file! ScanResult: {scanResult.RawResult}";
                        scanFinalResult = true;
                        break;

                    case ClamScanResults.Unknown:
                        reason = $"Unknown scan result while scaning the file! ScanResult: {scanResult.RawResult}";
                        scanFinalResult = true;
                        break;
                }
            }
            catch (Exception ex)
            {

                reason = $"Exception : {JsonConvert.SerializeObject(ex)}";
                scanFinalResult = true;

                Console.WriteLine($"ClamAv Exception > {reason}");

                ////scanFinalResult = true; // return true, in case server is not yet started or any ClamAV server issue
            }

            return scanFinalResult;
        }
    }
}
