using Microsoft.AspNetCore.Mvc;
using MostWanted.Models;
using ResponseCollectionService.Controllers;

namespace SurveyAnalysisService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly ILogger<AnalysisController> _logger;

        public AnalysisController(ILogger<AnalysisController> logger)
        {
            _logger = logger;
        }

        // GET /api/analysis/{questionnaireId}
        [HttpGet("{questionnaireId}")]
        public IActionResult GetAnalysis(Guid questionnaireId)
        {
            _logger.LogInformation("Analysis requested for Questionnaire ID: {QuestionnaireId}", questionnaireId);

            // ACHTUNG: Direkter Zugriff auf den Speicher eines anderen Services!
            // Dies ist eine Vereinfachung nur für dieses Beispiel.
            // In einer echten Architektur würde dieser Service auf Events hören und eine
            // eigene, für Analysen optimierte Datenkopie besitzen.
            var relevantSubmissions = ResponseCollectionService.Controllers.SubmissionsController.Submissions
                .Where(s => s.QuestionnaireId == questionnaireId)
                .ToList();

            if (!relevantSubmissions.Any())
            {
                return NotFound($"No submissions found for questionnaire ID {questionnaireId}.");
            }

            // Sehr einfache Analyse: Zählt die Häufigkeit jeder Antwortoption
            var analysisResult = relevantSubmissions
                .SelectMany(s => s.Answers)
                .SelectMany(a => a.SelectedAnswers.Select(sa => new { QuestionId = a.QuestionId, SelectedAnswer = sa }))
                .GroupBy(x => new { x.QuestionId, x.SelectedAnswer })
                .Select(g => new
                {
                    g.Key.QuestionId,
                    g.Key.SelectedAnswer,
                    Count = g.Count()
                })
                .OrderBy(x => x.QuestionId)
                .ThenByDescending(x => x.Count);

            return Ok(new
            {
                questionnaireId = questionnaireId,
                totalSubmissions = relevantSubmissions.Count,
                answerDistribution = analysisResult
            });
        }
    }
}