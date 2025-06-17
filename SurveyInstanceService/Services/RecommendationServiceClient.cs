using Consul;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SurveyInstanceService.Services
{
    public class RecommendationApiResponse
    {
        public List<string>? Recommendations { get; set; }
        public string? ServedBy { get; set; }
        public string? Host { get; set; }
        public string? Message { get; set; }
    }

    public interface IRecommendationServiceClient
    {
        Task<RecommendationApiResponse?> GetRecommendationsForSegmentAsync(string segmentId);
    }

    public class RecommendationServiceClient : IRecommendationServiceClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConsulClient _consulClient;
        private readonly ILogger<RecommendationServiceClient> _logger;
        private readonly string _recommendationServiceName = "recommendation-service";

        public RecommendationServiceClient(
            IHttpClientFactory httpClientFactory,
            IConsulClient consulClient,
            ILogger<RecommendationServiceClient> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _consulClient = consulClient ?? throw new ArgumentNullException(nameof(consulClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RecommendationApiResponse?> GetRecommendationsForSegmentAsync(string segmentId)
        {
            string? selectedServiceAddress = null;
            try
            {
                _logger.LogInformation("Attempting to discover service: {ServiceName}", _recommendationServiceName);
                var queryResult = await _consulClient.Health.Service(_recommendationServiceName, tag: null, passingOnly: true);

                if (queryResult.Response == null || !queryResult.Response.Any())
                {
                    _logger.LogWarning("No healthy instances found for service '{ServiceName}' in Consul.", _recommendationServiceName);
                    return null;
                }

                var serviceEntry = queryResult.Response[new Random().Next(queryResult.Response.Length)]; // Einfaches zufälliges Load Balancing
                var serviceHost = !string.IsNullOrEmpty(serviceEntry.Service.Address) ? serviceEntry.Service.Address : serviceEntry.Node.Address;
                selectedServiceAddress = $"http://{serviceHost}:{serviceEntry.Service.Port}";

                _logger.LogInformation("Discovered {ServiceName} instance at {ServiceAddress} for segment {SegmentId}", _recommendationServiceName, selectedServiceAddress, segmentId);

                var httpClient = _httpClientFactory.CreateClient("RecommendationServiceHttpClient");
                var requestUri = $"{selectedServiceAddress}/api/recommendations/{segmentId}";

                _logger.LogDebug("Sending GET request to: {RequestUri}", requestUri);
                HttpResponseMessage response = await httpClient.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RecommendationApiResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _logger.LogInformation("Successfully received {Count} recommendations for segment {SegmentId} from instance {ServedBy} on host {Host}. Message from service: {ServiceMessage}",
                                           result?.Recommendations?.Count ?? 0, segmentId, result?.ServedBy, result?.Host, result?.Message);

                    return result;
                }
                else
                {
                    _logger.LogError("Failed to get recommendations from '{ServiceName}' at {ServiceAddress}. Status: {StatusCode}, URI: {RequestUri}",
                                     _recommendationServiceName, selectedServiceAddress, response.StatusCode, requestUri);
                    string errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error content: {ErrorContent}", errorContent);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while calling {ServiceName} (target: {ServiceAddress}) for segment {SegmentId}", _recommendationServiceName, selectedServiceAddress ?? "N/A", segmentId);
                return null;
            }
        }
    }
}