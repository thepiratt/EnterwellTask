using System.Linq;
using Microsoft.EntityFrameworkCore;
using QuizApp.Application.Common;
using QuizApp.Application.DTOs;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;

namespace QuizApp.Application.Services;

public class QuizService
{
    private readonly IQuizRepository _repository;
    private readonly IQuestionRepository _questionRepository;
    private readonly ILogger<QuizService> _logger;

    public QuizService(IQuizRepository repository, IQuestionRepository questionRepository, ILogger<QuizService> logger)
    {
        _repository = repository;
        _questionRepository = questionRepository;
        _logger = logger;
    }

    public async Task<QuizDetailsDto> CreateQuizAsync(CreateQuizRequest request)
    {
        var quiz = Quiz.Create(request.Name);

        foreach (var qInput in request.Questions.OrderBy(q => q.Order))
        {
            Question question;

            if (qInput.QuestionId.HasValue)
            {
                question = await _questionRepository.GetByIdAsync(qInput.QuestionId.Value)
                    ?? throw new InvalidOperationException($"Question with ID '{qInput.QuestionId}' not found");
            }
            else
            {
                question = Question.Create(qInput.Text ?? string.Empty, qInput.CorrectAnswer ?? string.Empty);
                await _questionRepository.AddAsync(question);
            }

            quiz.AddQuestion(question, qInput.Order);
        }

        await _repository.AddAsync(quiz);
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Quiz '{QuizName}' created with ID '{QuizId}'", quiz.Name, quiz.Id);

        return await GetQuizByIdAsync(quiz.Id) ?? throw new InvalidOperationException("Failed to retrieve created quiz");
    }

    public async Task<QuizDetailsDto> UpdateQuizAsync(Guid quizId, UpdateQuizRequest request)
    {
        var quiz = await _repository.GetByIdAsync(quizId)
            ?? throw new InvalidOperationException($"Quiz with ID '{quizId}' not found");

        quiz.UpdateName(request.Name);

        // clear and re-add
        quiz.QuizQuestions.Clear();

        foreach (var qInput in request.Questions.OrderBy(q => q.Order))
        {
            Question question;

            if (qInput.QuestionId.HasValue)
            {
                question = await _questionRepository.GetByIdAsync(qInput.QuestionId.Value)
                    ?? throw new InvalidOperationException($"Question with ID '{qInput.QuestionId}' not found");
            }
            else
            {
                question = Question.Create(qInput.Text ?? string.Empty, qInput.CorrectAnswer ?? string.Empty);
                await _questionRepository.AddAsync(question);
            }

            quiz.AddQuestion(question, qInput.Order);
        }

        await _repository.UpdateAsync(quiz);
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Quiz '{QuizName}' (ID: '{QuizId}') updated", quiz.Name, quiz.Id);

        return await GetQuizByIdAsync(quizId) ?? throw new InvalidOperationException("Failed to retrieve updated quiz");
    }

    public async Task SoftDeleteQuizAsync(Guid quizId)
    {
        var quiz = await _repository.GetByIdAsync(quizId)
            ?? throw new InvalidOperationException($"Quiz with ID '{quizId}' not found");

        quiz.SoftDelete();
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Quiz '{QuizName}' (ID: '{QuizId}') soft-deleted", quiz.Name, quiz.Id);
    }

    public async Task<QuizDetailsDto?> GetQuizByIdAsync(Guid quizId)
    {
        var quiz = await _repository.GetByIdAsync(quizId);

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

        var query = _repository.Query();
        var totalCount = await _repository.CountAsync();

        var quizzes = await query
            .Include(q => q.QuizQuestions)
            .ThenInclude(qq => qq.Question)
            .OrderByDescending(q => q.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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

        var query = _repository.Query().SelectMany(q => q.QuizQuestions.Select(qq => qq.Question)).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(q => q.Text.ToLower().Contains(term));
        }

        var totalCount = query.Count();

        var questions = query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

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
