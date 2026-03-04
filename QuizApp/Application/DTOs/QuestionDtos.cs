namespace QuizApp.Application.DTOs;

public class QuizListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QuestionSearchResultDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int UsageCount { get; set; }
}
