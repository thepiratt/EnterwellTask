namespace QuizApp.Domain.Entities;

public class Quiz
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public ICollection<QuizQuestion> QuizQuestions { get; set; } = [];
}
