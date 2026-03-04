using Microsoft.EntityFrameworkCore;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;

namespace QuizApp.Infrastructure.Persistence.Repositories;

public class QuizRepository : IQuizRepository
{
    private readonly QuizDbContext _context;

    public QuizRepository(QuizDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Quiz quiz)
    {
        _context.Quizzes.Add(quiz);
        await Task.CompletedTask;
    }

    public IQueryable<Quiz> Query()
    {
        return _context.Quizzes.AsQueryable();
    }

    public async Task<Quiz?> GetByIdAsync(Guid id)
    {
        return await _context.Quizzes
            .Include(q => q.QuizQuestions)
            .ThenInclude(qq => qq.Question)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public Task<int> CountAsync()
    {
        return _context.Quizzes.CountAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Quiz quiz)
    {
        _context.Quizzes.Update(quiz);
        await Task.CompletedTask;
    }
}
