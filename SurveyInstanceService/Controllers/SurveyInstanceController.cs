using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SurveyInstanceService.Services;
using System;
using System.Threading.Tasks;
using MostWanted.Models;

namespace SurveyInstanceService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SurveyInstanceController : ControllerBase
    {
        private readonly IRecommendationServiceClient _recommendationClient;
        private readonly ISurveyDefinitionServiceClient _surveyDefinitionClient;
        private readonly ILogger<SurveyInstanceController> _logger;
        private readonly IConfiguration _configuration;

        public SurveyInstanceController(
            IRecommendationServiceClient recommendationClient,
            ISurveyDefinitionServiceClient surveyDefinitionClient,
            ILogger<SurveyInstanceController> logger,
            IConfiguration configuration)
        {
            _recommendationClient = recommendationClient;
            _surveyDefinitionClient = surveyDefinitionClient;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSurveyInstance([FromBody] SurveyInstanceCreationRequest request)
        {
            if (request == null || request.QuestionnaireId == Guid.Empty)
            {
                _logger.LogWarning("Invalid request: QuestionnaireId is required.");
                return BadRequest("QuestionnaireId is required.");
            }

            _logger.LogInformation("Processing request to create instance for Questionnaire ID: {QuestionnaireId} and Segment: {SegmentId}",
                request.QuestionnaireId, request.SegmentId);

            var questionnaire = await _surveyDefinitionClient.GetQuestionnaireByIdAsync(request.QuestionnaireId);
            if (questionnaire == null)
            {
                _logger.LogWarning("Questionnaire with ID {QuestionnaireId} not found via SurveyDefinitionService.", request.QuestionnaireId);
                return NotFound($"The questionnaire with ID {request.QuestionnaireId} could not be found.");
            }

            RecommendationApiResponse? recommendationData = null;
            if (!string.IsNullOrEmpty(request.SegmentId))
            {
                recommendationData = await _recommendationClient.GetRecommendationsForSegmentAsync(request.SegmentId);
                if (recommendationData != null && recommendationData.Recommendations != null)
                {
                    _logger.LogInformation("Obtained {Count} recommendations for the new survey instance.", recommendationData.Recommendations.Count);
                }
                else
                {
                    _logger.LogWarning("No recommendations were obtained for segment {SegmentId}.", request.SegmentId);
                }
            }

            var serviceMessage = _configuration.GetValue<string>("SurveyInstanceService:Greeting", "SurveyInstanceService is running smoothly!");

            return Ok(new
            {
                message = $"Instance creation process initiated for questionnaire '{questionnaire.Title}'.",
                instanceData = new
                {
                    questionnaireId = request.QuestionnaireId,
                    segmentId = request.SegmentId,
                    totalQuestions = questionnaire.Questions.Count
                },
                obtainedRecommendations = recommendationData,
                serviceMessage = serviceMessage
            });
        }
    }

    public class SurveyInstanceCreationRequest
    {
        public Guid QuestionnaireId { get; set; }
        public string? SegmentId { get; set; }
    }
}