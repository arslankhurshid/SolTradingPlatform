// Services/EasyCreditPaymentServiceImpl.cs

using Grpc.Core;
using EasyCreditPaymentService;

public class EasyCreditPaymentServiceImpl : EasyCreditPaymentService.EasyCreditPaymentService.EasyCreditPaymentServiceBase
{
    private readonly ILogger<EasyCreditPaymentServiceImpl> _logger;

    public EasyCreditPaymentServiceImpl(ILogger<EasyCreditPaymentServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<CreditCardPaymentResponse> ProcessPayment(
        CreditCardPaymentRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation($"Received payment for: {request.CardHolder}");
        
        // Basic validation
        if (string.IsNullOrEmpty(request.CardNumber) || string.IsNullOrEmpty(request.CardHolder) || string.IsNullOrEmpty(request.Expiry))
        {
            _logger.LogInformation($"Payment rejected for {request.CardHolder}: Invalid card details");
            return Task.FromResult(new CreditCardPaymentResponse 
            { 
                Success = false,
                TransactionId = string.Empty
            });
        }

        // Simple validation - approve if card number starts with 4
        bool isApproved = request.CardNumber.StartsWith("4");
        
        if (isApproved)
        {
            _logger.LogInformation($"Payment approved for {request.CardHolder} with card ending in {request.CardNumber.Substring(request.CardNumber.Length - 4)}");
        }
        else
        {
            _logger.LogInformation($"Payment rejected for {request.CardHolder}: Invalid card number");
        }
        
        return Task.FromResult(new CreditCardPaymentResponse 
        { 
            Success = isApproved,
            TransactionId = isApproved ? $"TX-{DateTime.UtcNow.Ticks}" : string.Empty
        });
    }
}

