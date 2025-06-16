using Microsoft.AspNetCore.Server.Kestrel.Core;
//using GrpcLoggingService.Services;
using EasyCreditPaymentService; // From your payment.proto

var builder = WebApplication.CreateBuilder(args);

// Add gRPC services
builder.Services.AddGrpc(options => {
    options.EnableDetailedErrors = true;
    options.MaxReceiveMessageSize = 2 * 1024 * 1024; // 2MB
});

// Configure HTTP/2
builder.WebHost.ConfigureKestrel(options => {
    options.ListenLocalhost(6002, listenOptions => {
        listenOptions.Protocols = HttpProtocols.Http2;
        Console.WriteLine("Payment Service 2: HTTP/2 enabled on port 6002");
    });
});

var app = builder.Build();
// Register the payment service
/*app.MapGrpcService<EasyCreditPaymentServiceImpl>();*/

///app.MapGrpcService<GrpcLoggingService>();

// Configure middleware
app.UseRouting();

// Map gRPC service
app.MapGrpcService<EasyCreditPaymentServiceImpl>();
app.MapGet("/", () => "Payment Service 2 (gRPC/HTTP2) is running");

app.Run();