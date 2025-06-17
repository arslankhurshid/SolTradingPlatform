// RecommendationService/Controllers/RecommendationsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;

namespace RecommendationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationsController : ControllerBase
    {
        private readonly ILogger<RecommendationsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _instanceId; // Eindeutige ID für diese Instanz

        public RecommendationsController(ILogger<RecommendationsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            // Eindeutige ID für diese Instanz generieren oder aus Konfiguration lesen
            _instanceId = configuration.GetValue<string>("InstanceId", Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        // GET /api/recommendations/segment123
        [HttpGet("{segmentId}")]
        public IActionResult GetRecommendations(string segmentId)
        {
            var numberOfRecs = _configuration.GetValue<int>("RecommendationService:NumberOfRecommendations", 5);
            var messageFromConfig = _configuration.GetValue<string>("RecommendationService:WelcomeMessage", "Default Welcome!");

            _logger.LogInformation("Instance '{InstanceId}' ({Host}) generating {NumberOfRecs} recommendations for segment {SegmentId}. Config Message: {ConfigMessage}",
                _instanceId,
                HttpContext.Request.Host.ToString(),
                numberOfRecs,
                segmentId,
                messageFromConfig);

            var recommendations = Enumerable.Range(1, numberOfRecs)
                                            .Select(i => $"PROD-{segmentId}-{_instanceId}-{i}")
                                            .ToList();
            return Ok(new { recommendations, servedBy = _instanceId, host = HttpContext.Request.Host.ToString(), message = messageFromConfig });
        }

        // GET /health (für Consul Health Check)
        [HttpGet("/health")]
        public IActionResult HealthCheck()
        {
            // Hier könnten komplexere Health Checks stattfinden
            _logger.LogDebug("Health check called for RecommendationService instance '{InstanceId}'. Status: Healthy.", _instanceId);
            return Ok(new { status = "Healthy", instance = _instanceId, timestamp = DateTime.UtcNow });
        }
    }
}