namespace QuizApp.Domain.Entities;

public class Question
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Text { get; private set; } = string.Empty;
    public string CorrectAnswer { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public ICollection<QuizQuestion> QuizQuestions { get; private set; } = new List<QuizQuestion>();

    // Factory method
    public static Question Create(string text, string correctAnswer)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Question text cannot be empty", nameof(text));
        if (string.IsNullOrWhiteSpace(correctAnswer))
            throw new ArgumentException("Correct answer cannot be empty", nameof(correctAnswer));

        return new Question
        {
            Text = text.Trim(),
            CorrectAnswer = correctAnswer.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    // Rehydrate for DTO mapping and exporters
    public static Question Rehydrate(Guid id, string text, string correctAnswer, DateTime createdAt)
    {
        return new Question
        {
            Id = id,
            Text = text,
            CorrectAnswer = correctAnswer,
            CreatedAt = createdAt
        };
    }
}
