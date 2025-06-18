namespace MostWanted.Models
{
    public class Submission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid QuestionnaireId { get; set; }
        public string RespondentId { get; set; } = "anonymous"; // z.B. User-ID
        public List<Answer> Answers { get; set; } = new List<Answer>();
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }

    public class Answer
    {
        public int QuestionId { get; set; }
        public List<string> SelectedAnswers { get; set; } = new List<string>();
    }
}