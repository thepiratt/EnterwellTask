namespace QuizApp.Domain.Entities;

public class Quiz
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public bool IsDeleted { get; private set; }
    public ICollection<QuizQuestion> QuizQuestions { get; private set; } = new List<QuizQuestion>();

    // Factory method to create a new Quiz with validation
    public static Quiz Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Quiz name cannot be empty", nameof(name));

        return new Quiz
        {
            Name = name.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    // Rehydrate existing quiz (for exporting or mapping from DTO)
    public static Quiz Rehydrate(Guid id, string name, DateTime createdAt, bool isDeleted = false, ICollection<QuizQuestion>? quizQuestions = null)
    {
        return new Quiz
        {
            Id = id,
            Name = name,
            CreatedAt = createdAt,
            IsDeleted = isDeleted,
            QuizQuestions = quizQuestions ?? new List<QuizQuestion>()
        };
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Quiz name cannot be empty", nameof(name));

        Name = name.Trim();
    }

    public void SoftDelete()
    {
        IsDeleted = true;
    }

    public void AddQuestion(Question question, int order)
    {
        if (question == null) throw new ArgumentNullException(nameof(question));

        QuizQuestions.Add(new QuizQuestion
        {
            Quiz = this,
            Question = question,
            Order = order
        });
    }
}
