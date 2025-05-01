using IEGEasyCreditcardService.Models;
using IEGEasyCreditcardService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IEGEasyCreditcardService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreditcardTransactionsController : ControllerBase
    {
        private readonly ILogger<CreditcardTransactionsController> _logger;

        private ICreditcardValidator _creditcardValidator;

        public CreditcardTransactionsController(ILogger<CreditcardTransactionsController> logger, ICreditcardValidator creditcardValidator)
        {
            _logger = logger;
            _creditcardValidator = creditcardValidator;
        }
        [HttpGet]
        public string Get(int id)
        {
            return "value" + id;
        }

        [HttpPost]
        public IActionResult Post([FromBody] CreditcardTransaction creditcardTransaction)
        {
            _logger.LogError($"TransactionInfo Number: {creditcardTransaction.CreditcardNumber} Amount:{creditcardTransaction.Amount} Receiver: {creditcardTransaction.ReceiverName}");
           
            var isValid = _creditcardValidator.IsValid(creditcardTransaction);
            if (isValid)
            {
                return CreatedAtAction("Get", new { id = Guid.NewGuid() });
            }
            else
            {
                return BadRequest();
            }
          
        }
    }
}
