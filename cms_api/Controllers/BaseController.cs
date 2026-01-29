using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace cms_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        public BaseController()
        { }
    }
}
