using System.Text;
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
using aia_core.Repository;
using Newtonsoft.Json;
using System.Web;
using System.Xml.Linq;

namespace cms_api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/saml")]
    public class OktaController : BaseController
    {
        private readonly IDevRepository devRepository;
        private readonly IAuthRepository authRepository;
        private readonly IConfiguration config;
        public OktaController(IDevRepository devRepository,IAuthRepository authRepository, IConfiguration config)
        {
            this.devRepository = devRepository;
            this.authRepository = authRepository;
            this.config = config;
        }

        [HttpPost("acs")]
        [AllowAnonymous]
        public async Task<IActionResult> OktaCallBack()
        {
            try
            {
                devRepository.ErrorLog("inside OktaCallBack");
                Console.WriteLine($"inside OktaCallBack");

                string samlResponse = "";
                var context = HttpContext;
                using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
                {
                    samlResponse = await reader.ReadToEndAsync();
                    devRepository.ErrorLog($"requestBody: {samlResponse}");
                }

                // Parse and process the SAML response here
                string decodedString = HttpUtility.UrlDecode(samlResponse);
                var samlResponseBase64 = decodedString.Replace("SAMLResponse=", "").Replace("&RelayState=", ""); // Assuming you have a model for the request

                // Decode the SAML response from base64
                byte[] samlBytes = Convert.FromBase64String(samlResponseBase64);
                string samlXml = Encoding.UTF8.GetString(samlBytes);
                devRepository.ErrorLog($"samlXml: {samlXml}");

                XDocument xmlDoc = XDocument.Parse(samlXml);
                XNamespace saml2p = "urn:oasis:names:tc:SAML:2.0:protocol";
                XNamespace saml2 = "urn:oasis:names:tc:SAML:2.0:assertion";

                string email = xmlDoc.Root
                   .Descendants(saml2 + "Attribute")
                   .Where(attr => attr.Attribute("Name")?.Value == "email")
                   .Elements(saml2 + "AttributeValue")
                   .Select(element => element.Value)
                   .FirstOrDefault();

                devRepository.ErrorLog($"OktaCallBack Email: {email}");

                string redirectUrl = "";
                ResponseModel<string> response = authRepository.ADLogin(email);
                devRepository.ErrorLog($"OktaCallBack authRepository response: {JsonConvert.SerializeObject(response)}");
                if (response.Code==0)
                {    
                    redirectUrl = config["SamlUrl:Success"] + "?token=" + response.Data;
                }
                else
                {
                    redirectUrl = config["SamlUrl:Fail"] + "?message=" + response.Message;
                }
                devRepository.ErrorLog(redirectUrl);
                return Redirect(redirectUrl);
            }
            catch (System.Exception ex)
            {
                

                devRepository.ErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), "saml/acs");
                string failUrl = config["SamlUrl:Fail"];

                Console.WriteLine($"inside OktaCallBack Exception > {ex.Message} {JsonConvert.SerializeObject(ex)}");
                Console.WriteLine($"inside OktaCallBack failUrl > {failUrl}");
                return Redirect(failUrl);
            }

        }
    }
}