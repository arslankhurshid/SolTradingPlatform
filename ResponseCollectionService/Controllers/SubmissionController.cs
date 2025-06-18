using Microsoft.AspNetCore.Mvc;
using MostWanted.Models;

namespace ResponseCollectionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionsController : ControllerBase
    {
        // === In-Memory-Speicher (ersetzen Sie dies später durch eine echte Datenbank) ===
        public static readonly List<Submission> Submissions = new List<Submission>();
        private readonly ILogger<SubmissionsController> _logger;

        public SubmissionsController(ILogger<SubmissionsController> logger)
        {
            _logger = logger;
        }

        // POST /api/submissions
        [HttpPost]
        public IActionResult PostSubmission([FromBody] Submission submission)
        {
            if (submission == null)
            {
                return BadRequest("Submission data is missing.");
            }

            Submissions.Add(submission);
            _logger.LogInformation("New submission received for Questionnaire ID: {QuestionnaireId} with {AnswerCount} answers.",
                submission.QuestionnaireId, submission.Answers.Count);

            // In einer echten Anwendung würde hier ein Event an einen Message Broker
            // (z.B. RabbitMQ, Azure Service Bus) gesendet werden, um den SurveyAnalysisService
            // über neue Daten zu informieren. Für jetzt simulieren wir das nur mit einem Log.
            _logger.LogInformation("EVENT_SIMULATION: SubmissionReceived event published for ID: {SubmissionId}", submission.Id);

            return Accepted(submission);
        }
    }
}