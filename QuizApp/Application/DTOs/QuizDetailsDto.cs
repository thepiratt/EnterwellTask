namespace QuizApp.Application.DTOs;

public class QuizDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
}

public class QuestionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? CorrectAnswer { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
}
