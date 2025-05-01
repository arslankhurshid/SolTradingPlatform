using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IEGEasyCreditcardService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AcceptedCreditCardsController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "American", "Diners", "Master", "Visa", "Blue Monday" };
        }
    }
}
