using Microsoft.AspNetCore.Mvc;

namespace IEGEasyCreditCardService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CreditPaymentController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Credit payment processed by " + HttpContext.Request.Host);
        }
    }
}