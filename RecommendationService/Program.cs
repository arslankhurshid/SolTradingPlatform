// RecommendationService/Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using Winton.Extensions.Configuration.Consul; // Wichtig für AddConsul

var builder = WebApplication.CreateBuilder(args);

// Dienste konfigurieren
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});
builder.Services.AddControllers();

// Optionale Konfiguration aus Consul KV laden
var consulConfigAddress = builder.Configuration.GetValue<string>("Consul:HttpAddress");
var consulConfigKeyPath = builder.Configuration.GetValue<string>("Consul:ConfigurationKeyPath");

if (!string.IsNullOrEmpty(consulConfigAddress) && !string.IsNullOrEmpty(consulConfigKeyPath))
{
    builder.Configuration.AddConsul(
        consulConfigKeyPath,
        options =>
        {
            options.ConsulConfigurationOptions = cco => { cco.Address = new Uri(consulConfigAddress); };
            options.Optional = true; // Macht Consul-Konfiguration optional
            options.ReloadOnChange = true; // Lädt bei Änderungen in Consul neu
            // options.Parser ist hier nicht explizit gesetzt, der Standard-Parser wird verwendet.
        }
    );
}

var app = builder.Build();

// HTTP-Request-Pipeline konfigurieren
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// app.UseHttpsRedirection(); 

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();