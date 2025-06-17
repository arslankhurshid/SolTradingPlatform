using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SurveyInstanceService.Services; // Stellt sicher, dass dieser Namespace IRecommendationServiceClient und RecommendationApiResponse enthält
using System.Threading.Tasks;

namespace SurveyInstanceService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SurveyInstanceController : ControllerBase
    {
        private readonly IRecommendationServiceClient _recommendationClient;
        private readonly ILogger<SurveyInstanceController> _logger;
        private readonly IConfiguration _configuration;

        public SurveyInstanceController(
            IRecommendationServiceClient recommendationClient,
            ILogger<SurveyInstanceController> logger,
            IConfiguration configuration)
        {
            _recommendationClient = recommendationClient;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSurveyInstance([FromBody] SurveyInstanceCreationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.SegmentId))
            {
                _logger.LogWarning("Invalid request received for CreateSurveyInstance: SegmentId is missing.");
                return BadRequest("SegmentId is required.");
            }

            var centralConfigValue = _configuration.GetValue<string>("SurveyInstanceService:Greeting", "Default Greeting for SurveyInstance");
            _logger.LogInformation("Creating survey instance for segment: {SegmentId}. Central Config Greeting: {Greeting}", request.SegmentId, centralConfigValue);

            RecommendationApiResponse? recommendationData = // Verwende hier den korrekten Typ
                await _recommendationClient.GetRecommendationsForSegmentAsync(request.SegmentId);

            if (recommendationData != null && recommendationData.Recommendations != null)
            {
                _logger.LogInformation("Obtained {Count} recommendations for the new survey instance for segment {SegmentId}.", recommendationData.Recommendations.Count, request.SegmentId);
            }
            else
            {
                _logger.LogWarning("No recommendations obtained for segment {SegmentId}.", request.SegmentId);
            }

            return Ok(new
            {
                message = "Survey instance creation process initiated.",
                segmentId = request.SegmentId,
                obtainedRecommendations = recommendationData, // Hier das GANZE Objekt verwenden!
                serviceMessage = centralConfigValue
            });
        }
    }

    public class SurveyInstanceCreationRequest
    {
        public string? SegmentId { get; set; }
        // Weitere Eigenschaften für die Erstellung einer Survey-Instanz
    }
}