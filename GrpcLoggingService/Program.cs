using GrpcLoggingService.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;


var builder = WebApplication.CreateBuilder(args);

// Force Kestrel server to allow HTTP/2 without TLS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5280, o => o.Protocols = HttpProtocols.Http2);
});

builder.Services.AddGrpc();

var app = builder.Build();
app.MapGrpcService<LogServiceImpl>();
app.UseRouting();

//app.MapGrpcService<GrpcLoggingService.Services.LogServiceImpl>();
app.MapGet("/", () => "gRPC Logging Service is running...");

app.Run();