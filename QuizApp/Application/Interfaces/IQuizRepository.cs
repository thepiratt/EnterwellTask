using QuizApp.Domain.Entities;

namespace QuizApp.Application.Interfaces;

public interface IQuizRepository
{
    Task<Quiz?> GetByIdAsync(Guid id);
    Task AddAsync(Quiz quiz);
    Task UpdateAsync(Quiz quiz);
    Task<int> CountAsync();
    IQueryable<Quiz> Query();
    Task SaveChangesAsync();
}
