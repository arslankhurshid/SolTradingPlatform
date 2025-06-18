// SurveyInstanceService/Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Consul; // F�r IConsulClient und ConsulClient
using SurveyInstanceService.Services; // Namespace f�r IRecommendationServiceClient und seine Implementierung
using System; // F�r Uri
using Winton.Extensions.Configuration.Consul;

var builder = WebApplication.CreateBuilder(args);

// 1. Logging konfigurieren
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

// 2. Controller-Dienste hinzuf�gen
builder.Services.AddControllers();

// 3. ConsulClient f�r Service Discovery registrieren
builder.Services.AddSingleton<IConsulClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>(); // Logger f�r diese Konfiguration
    var configuration = sp.GetRequiredService<IConfiguration>();
    var consulAddress = configuration.GetValue<string>("Consul:HttpAddress");

    if (string.IsNullOrEmpty(consulAddress))
    {
        consulAddress = "http://localhost:8500"; // Fallback-Wert
        logger.LogWarning("Consul:HttpAddress not found in configuration. Using fallback: {FallbackConsulAddress}", consulAddress);
    }
    logger.LogInformation("Initializing ConsulClient with address: {ConsulAddress}", consulAddress);

    return new ConsulClient(config =>
    {
        config.Address = new Uri(consulAddress);
    });
});

// 4. HttpClientFactory und unseren RecommendationServiceClient registrieren
// Definiert einen benannten HttpClient, der von RecommendationServiceClient verwendet werden kann.
// Hier k�nnten auch spezifische Handler oder Policies (z.B. Polly f�r Retries) konfiguriert werden.
builder.Services.AddHttpClient("RecommendationServiceHttpClient");
builder.Services.AddScoped<IRecommendationServiceClient, RecommendationServiceClient>();

// Registrierung f�r den neuen SurveyDefinitionServiceClient
builder.Services.AddHttpClient<ISurveyDefinitionServiceClient, SurveyDefinitionServiceClient>(client =>
{
    // HIER EINE FESTE URL F�R DAS LOKALE BEISPIEL.
    // IN EINER ECHTEN ANWENDUNG W�RDE HIER SERVICE DISCOVERY ZUM EINSATZ KOMMEN!
    client.BaseAddress = new Uri("http://localhost:5013"); // Port des SurveyDefinitionService
});

var centralConsulConfigAddress = builder.Configuration.GetValue<string>("Consul:HttpAddress"); // K�nnte derselbe sein
var centralConsulKeyPath = "config/mostwanted/surveyinstanceservice"; // Eigener Pfad f�r diesen Service

if (!string.IsNullOrEmpty(centralConsulConfigAddress))
{
    builder.Configuration.AddConsul(
        centralConsulKeyPath,
        options =>
        {
            options.ConsulConfigurationOptions = cco => { cco.Address = new Uri(centralConsulConfigAddress); };
            options.Optional = true;
            options.ReloadOnChange = true;
        });
}

var app = builder.Build();

// HTTP-Request-Pipeline konfigurieren
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// app.UseHttpsRedirection(); // F�r lokale Tests oft auskommentiert

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();