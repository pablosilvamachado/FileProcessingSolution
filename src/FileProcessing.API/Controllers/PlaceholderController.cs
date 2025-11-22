using Microsoft.AspNetCore.Mvc;

namespace FileProcessing.Api.Controllers
{
    [ApiController]
    [Route("api/placeholder")]
    public class PlaceholderController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("placeholder");
    }
}
