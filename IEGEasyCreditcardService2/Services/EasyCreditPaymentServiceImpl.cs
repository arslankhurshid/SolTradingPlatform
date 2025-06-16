// Services/EasyCreditPaymentServiceImpl.cs

using Grpc.Core;
using EasyCreditPaymentService;

public class EasyCreditPaymentServiceImpl : EasyCreditPaymentService.EasyCreditPaymentService.EasyCreditPaymentServiceBase
{
    public override Task<CreditCardPaymentResponse> ProcessPayment(
        CreditCardPaymentRequest request,
        ServerCallContext context)
    {
        bool isApproved = request.CardNumber.StartsWith("4");
        return Task.FromResult(new CreditCardPaymentResponse { Success = isApproved });
    }
}