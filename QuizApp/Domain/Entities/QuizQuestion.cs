namespace QuizApp.Domain.Entities;

public class QuizQuestion
{
    public Guid QuizId { get; set; }
    public Guid QuestionId { get; set; }
    public int Order { get; set; }
    public Quiz? Quiz { get; set; }
    public Question? Question { get; set; }
}
