using MostWanted.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SurveyInstanceService.Services
{
    // Dies ist eine sehr einfache Client-Implementierung.
    // Sie verwendet keine Service Discovery, sondern eine feste URL.
    // In einer echten Anwendung würden Sie dies mit Consul-Integration (wie beim RecommendationServiceClient) kombinieren.
    public interface ISurveyDefinitionServiceClient
    {
        Task<Questionnaire?> GetQuestionnaireByIdAsync(Guid questionnaireId);
    }

    public class SurveyDefinitionServiceClient : ISurveyDefinitionServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SurveyDefinitionServiceClient> _logger;

        public SurveyDefinitionServiceClient(HttpClient httpClient, ILogger<SurveyDefinitionServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<Questionnaire?> GetQuestionnaireByIdAsync(Guid questionnaireId)
        {
            try
            {
                _logger.LogInformation("Requesting questionnaire with ID {QuestionnaireId} from SurveyDefinitionService.", questionnaireId);
                return await _httpClient.GetFromJsonAsync<Questionnaire>($"api/surveydefinitions/{questionnaireId}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to retrieve questionnaire with ID {QuestionnaireId}.", questionnaireId);
                return null;
            }
        }
    }
}