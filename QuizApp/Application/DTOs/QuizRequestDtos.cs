namespace QuizApp.Application.DTOs;

public class CreateQuizRequest
{
    public string Name { get; set; } = string.Empty;
    public ICollection<QuestionInput> Questions { get; set; } = [];
}

public class QuestionInput
{
    public Guid? QuestionId { get; set; }
    public string? Text { get; set; }
    public string? CorrectAnswer { get; set; }
    public int Order { get; set; }
}

public class UpdateQuizRequest
{
    public string Name { get; set; } = string.Empty;
    public ICollection<QuestionInput> Questions { get; set; } = [];
}
