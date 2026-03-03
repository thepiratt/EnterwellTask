using Microsoft.EntityFrameworkCore;
using QuizApp.Application.Common;
using QuizApp.Application.DTOs;
using QuizApp.Domain.Entities;
using QuizApp.Infrastructure.Persistence;

namespace QuizApp.Application.Services;

public class QuizService
{
    private readonly QuizDbContext _context;
    private readonly ILogger<QuizService> _logger;

    public QuizService(QuizDbContext context, ILogger<QuizService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<QuizDetailsDto> CreateQuizAsync(CreateQuizRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Quiz name cannot be empty", nameof(request.Name));
        }

        if (!request.Questions.Any())
        {
            throw new ArgumentException("Quiz must contain at least one question", nameof(request.Questions));
        }

        var quiz = new Quiz
        {
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        foreach (var qInput in request.Questions.OrderBy(q => q.Order))
        {
            Question? question = null;

            if (qInput.QuestionId.HasValue)
            {
                question = await _context.Questions.FirstOrDefaultAsync(q => q.Id == qInput.QuestionId.Value);
                if (question == null)
                {
                    throw new InvalidOperationException($"Question with ID '{qInput.QuestionId}' not found");
                }
            }
            else if (!string.IsNullOrWhiteSpace(qInput.Text) && !string.IsNullOrWhiteSpace(qInput.CorrectAnswer))
            {
                question = new Question
                {
                    Text = qInput.Text.Trim(),
                    CorrectAnswer = qInput.CorrectAnswer.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.Questions.Add(question);
            }
            else
            {
                throw new ArgumentException("Each question must either have a QuestionId or both Text and CorrectAnswer");
            }

            quiz.QuizQuestions.Add(new QuizQuestion
            {
                Quiz = quiz,
                Question = question,
                Order = qInput.Order
            });
        }

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Quiz '{QuizName}' created with ID '{QuizId}'", quiz.Name, quiz.Id);

        return await GetQuizByIdAsync(quiz.Id) ?? throw new InvalidOperationException("Failed to retrieve created quiz");
    }

    public async Task<QuizDetailsDto> UpdateQuizAsync(Guid quizId, UpdateQuizRequest request)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.QuizQuestions)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz == null)
        {
            throw new InvalidOperationException($"Quiz with ID '{quizId}' not found");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Quiz name cannot be empty", nameof(request.Name));
        }

        quiz.Name = request.Name.Trim();

        _context.QuizQuestions.RemoveRange(quiz.QuizQuestions);

        foreach (var qInput in request.Questions.OrderBy(q => q.Order))
        {
            Question? question = null;

            if (qInput.QuestionId.HasValue)
            {
                question = await _context.Questions.FirstOrDefaultAsync(q => q.Id == qInput.QuestionId.Value);
                if (question == null)
                {
                    throw new InvalidOperationException($"Question with ID '{qInput.QuestionId}' not found");
                }
            }
            else if (!string.IsNullOrWhiteSpace(qInput.Text) && !string.IsNullOrWhiteSpace(qInput.CorrectAnswer))
            {
                question = new Question
                {
                    Text = qInput.Text.Trim(),
                    CorrectAnswer = qInput.CorrectAnswer.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.Questions.Add(question);
            }
            else
            {
                throw new ArgumentException("Each question must either have a QuestionId or both Text and CorrectAnswer");
            }

            quiz.QuizQuestions.Add(new QuizQuestion
            {
                Quiz = quiz,
                Question = question,
                Order = qInput.Order
            });
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Quiz '{QuizName}' (ID: '{QuizId}') updated", quiz.Name, quiz.Id);

        return await GetQuizByIdAsync(quizId) ?? throw new InvalidOperationException("Failed to retrieve updated quiz");
    }

    public async Task SoftDeleteQuizAsync(Guid quizId)
    {
        var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz == null)
        {
            throw new InvalidOperationException($"Quiz with ID '{quizId}' not found");
        }

        quiz.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Quiz '{QuizName}' (ID: '{QuizId}') soft-deleted", quiz.Name, quiz.Id);
    }

    public async Task<QuizDetailsDto?> GetQuizByIdAsync(Guid quizId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.QuizQuestions)
            .ThenInclude(qq => qq.Question)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz == null)
        {
            return null;
        }

        return MapToQuizDetailsDto(quiz);
    }

    public async Task<PagedResult<QuizListItemDto>> GetPagedQuizzesAsync(int pageNumber = 1, int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100; // Max page size

        var query = _context.Quizzes.AsQueryable();
        var totalCount = await query.CountAsync();

        var quizzes = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(q => q.QuizQuestions)
            .ToListAsync();

        var items = quizzes.Select(q => new QuizListItemDto
        {
            Id = q.Id,
            Name = q.Name,
            QuestionCount = q.QuizQuestions.Count,
            CreatedAt = q.CreatedAt
        }).ToList();

        return new PagedResult<QuizListItemDto>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<PagedResult<QuestionSearchResultDto>> SearchQuestionsAsync(
        string searchTerm, int pageNumber = 1, int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Questions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(q => q.Text.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var questions = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(q => q.QuizQuestions)
            .ToListAsync();

        var items = questions.Select(q => new QuestionSearchResultDto
        {
            Id = q.Id,
            Text = q.Text,
            CreatedAt = q.CreatedAt,
            UsageCount = q.QuizQuestions.Count
        }).ToList();

        return new PagedResult<QuestionSearchResultDto>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    private static QuizDetailsDto MapToQuizDetailsDto(Quiz quiz)
    {
        return new QuizDetailsDto
        {
            Id = quiz.Id,
            Name = quiz.Name,
            CreatedAt = quiz.CreatedAt,
            Questions = quiz.QuizQuestions
                .OrderBy(qq => qq.Order)
                .Select(qq => new QuestionDto
                {
                    Id = qq.Question?.Id ?? Guid.Empty,
                    Text = qq.Question?.Text ?? string.Empty,
                    CorrectAnswer = qq.Question?.CorrectAnswer,
                    Order = qq.Order,
                    CreatedAt = qq.Question?.CreatedAt ?? DateTime.MinValue
                })
                .ToList()
        };
    }
}
