using Microsoft.AspNetCore.Server.Kestrel.Core;
using EasyCreditPaymentService; // From your payment.proto

var builder = WebApplication.CreateBuilder(args);

// Add gRPC services
builder.Services.AddGrpc(options => {
    options.EnableDetailedErrors = true;
    options.MaxReceiveMessageSize = 2 * 1024 * 1024; // 2MB
});

builder.Services.AddLogging(logging => {
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

// Configure HTTP/2
builder.WebHost.ConfigureKestrel(options => {
    options.ListenLocalhost(6001, listenOptions => {
        listenOptions.Protocols = HttpProtocols.Http2;
        Console.WriteLine("Payment Service 1: HTTP/2 enabled on port 6001");
    });
});

var app = builder.Build();

// Configure middleware
app.UseRouting();

// Map gRPC service
app.MapGrpcService<EasyCreditPaymentServiceImpl>();
app.MapGet("/", () => "Payment Service 1 (gRPC/HTTP2) is running");

app.Run();