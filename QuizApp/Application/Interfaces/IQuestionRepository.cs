using QuizApp.Domain.Entities;

namespace QuizApp.Application.Interfaces;

public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(Guid id);
    Task AddAsync(Question question);
    IQueryable<Question> Query();
    Task SaveChangesAsync();
}
