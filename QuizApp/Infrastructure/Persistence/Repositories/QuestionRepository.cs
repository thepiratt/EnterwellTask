using Microsoft.EntityFrameworkCore;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;

namespace QuizApp.Infrastructure.Persistence.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly QuizDbContext _context;

    public QuestionRepository(QuizDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Question question)
    {
        _context.Questions.Add(question);
        await Task.CompletedTask;
    }

    public async Task<Question?> GetByIdAsync(Guid id)
    {
        return await _context.Questions
            .Include(q => q.QuizQuestions)
            .ThenInclude(qq => qq.Quiz)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public IQueryable<Question> Query()
    {
        return _context.Questions.AsQueryable();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
