namespace MostWanted.Models
{
    public class Questionnaire
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<Question> Questions { get; set; } = new List<Question>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Question
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; }
        public List<string> Options { get; set; } = new List<string>(); // Nur relevant für Single/MultipleChoice
    }

    public enum QuestionType
    {
        SingleChoice,
        MultipleChoice,
        FreeText
    }
}