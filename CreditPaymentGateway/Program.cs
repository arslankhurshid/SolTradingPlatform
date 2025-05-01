using Grpc.Net.Client;
using Grpc.Net.ClientFactory;
using GrpcLoggingService;
//using LoggerGrpc;

var builder = WebApplication.CreateBuilder(args);


// All service registrations here
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 👇 Add gRPC client BEFORE builder.Build()
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.AddGrpcClient<LogService.LogServiceClient>(o =>
{
    o.Address = new Uri("http://localhost:5280"); // or the actual gRPC logging service URL
});

var app = builder.Build();

// Use middlewares after building
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();