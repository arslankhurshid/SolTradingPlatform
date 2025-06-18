using Microsoft.AspNetCore.Mvc;
using MostWanted.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SurveyDefinitionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SurveyDefinitionsController : ControllerBase
    {
        // === In-Memory-Speicher (ersetzen Sie dies später durch eine echte Datenbank) ===
        private static readonly List<Questionnaire> _questionnaires = new List<Questionnaire>();
        private readonly ILogger<SurveyDefinitionsController> _logger;

        public SurveyDefinitionsController(ILogger<SurveyDefinitionsController> logger)
        {
            _logger = logger;
            // Beispiel-Fragebogen hinzufügen, wenn die Liste leer ist
            if (!_questionnaires.Any())
            {
                _questionnaires.Add(new Questionnaire
                {
                    Id = Guid.Parse("f4d4a8e2-7c1d-4b7a-9a0e-5b9c0f3d1e1a"),
                    Title = "Standard-Fragebogen zu Bezahlmethoden",
                    Description = "Helfen Sie uns, Ihr Einkaufserlebnis zu verbessern.",
                    Questions = new List<Question>
                    {
                        new Question { Id = 1, Text = "Welche Bezahlmethode bevorzugen Sie online?", Type = QuestionType.SingleChoice, Options = new List<string> { "Kreditkarte", "PayPal", "Campus02Coins" } },
                        new Question { Id = 2, Text = "Was ist Ihnen bei einer Bezahlmethode am wichtigsten?", Type = QuestionType.MultipleChoice, Options = new List<string> { "Sicherheit", "Geschwindigkeit", "Anonymität", "Einfachheit" } },
                        new Question { Id = 3, Text = "Haben Sie weitere Anmerkungen?", Type = QuestionType.FreeText }
                    }
                });
            }
        }

        // POST /api/surveydefinitions
        [HttpPost]
        public IActionResult CreateQuestionnaire([FromBody] Questionnaire questionnaire)
        {
            if (questionnaire == null)
            {
                return BadRequest("Questionnaire data is missing.");
            }
            // In einer echten Anwendung: Validierung hier
            _questionnaires.Add(questionnaire);
            _logger.LogInformation("New questionnaire created with ID: {Id}", questionnaire.Id);
            return CreatedAtAction(nameof(GetQuestionnaireById), new { id = questionnaire.Id }, questionnaire);
        }

        // GET /api/surveydefinitions
        [HttpGet]
        public ActionResult<IEnumerable<Questionnaire>> GetAllQuestionnaires()
        {
            return Ok(_questionnaires);
        }

        // GET /api/surveydefinitions/{id}
        [HttpGet("{id}")]
        public ActionResult<Questionnaire> GetQuestionnaireById(Guid id)
        {
            var questionnaire = _questionnaires.FirstOrDefault(q => q.Id == id);
            if (questionnaire == null)
            {
                _logger.LogWarning("Questionnaire with ID: {Id} not found.", id);
                return NotFound();
            }
            return Ok(questionnaire);
        }
    }
}