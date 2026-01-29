using System.ComponentModel.DataAnnotations;
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
    [Route("/v{version:apiVersion}/file")]
    public class FileController : BaseController
    {
        #region "const"
        private IDevRepository devRepository;
        private IAiaCrmApiService aiaCrmApiService;
        private IRecurringJobRunner recurringJobRunner;

        public FileController(IDevRepository devRepository, IAiaCrmApiService aiaCrmApiService, IRecurringJobRunner recurringJobRunner)
        {
            this.devRepository = devRepository;
            this.aiaCrmApiService = aiaCrmApiService;
            this.recurringJobRunner = recurringJobRunner;
        }
        #endregion

        [HttpGet("{fileName}")]
        [Authorize(AuthenticationSchemes = DefaultConstants.AccessTokenBearer)]
        public IActionResult GetFile(string fileName)
        {
            // Provide the path to the file on the server
            //string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Files", fileName);

            string filePath = Path.Combine("/app/wwwroot/images", fileName);

            // Check if the file exists
            if (System.IO.File.Exists(filePath))
            {
                // Read the file content into a byte array
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

                // Determine the file's content type based on its extension
                string contentType = GetContentType(fileName);

                // Return the file as a FileResult
                return File(fileBytes, contentType, fileName);
            }
            else
            {
                // If the file does not exist, return a NotFound result
                return NotFound();
            }
        }

        private string GetContentType(string fileName)
        {
            // Determine the content type based on the file extension
            string contentType;
            switch (Path.GetExtension(fileName).ToLowerInvariant())
            {
                case ".txt":
                    contentType = "text/plain";
                    break;
                case ".pdf":
                    contentType = "application/pdf";
                    break;
                case ".jpg":
                case ".jpeg":
                    contentType = "image/jpeg";
                    break;
                case ".png":
                    contentType = "image/png";
                    break;
                default:
                    contentType = "application/octet-stream";
                    break;
            }

            return contentType;
        }

    }
}
