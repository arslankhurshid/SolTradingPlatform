using Grpc.Net.Client;
using Grpc.Net.ClientFactory;
using GrpcLoggingService;
//using IEGEasyCreditcardService;
//using EasyCreditPaymentService;

//using LoggerGrpc;

var builder = WebApplication.CreateBuilder(args);


// All service registrations here
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.AddGrpcClient<LogService.LogServiceClient>(options =>
{
    options.Address = new Uri("http://localhost:5280"); 
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();