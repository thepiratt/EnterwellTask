namespace QuizApp.Domain.Entities;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<QuizQuestion> QuizQuestions { get; set; } = [];
}
