using QuizApp.Application.Common;
using QuizApp.Application.DTOs;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;

namespace QuizApp.Application.Services;

public class QuizService
{
    private readonly IQuizRepository _repository;
    private readonly ILogger<QuizService> _logger;

    public QuizService(IQuizRepository repository, ILogger<QuizService> logger)
    {
        _repository = repository;
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
                var existing = await _repository.GetByIdAsync(qInput.QuestionId.Value);
                if (existing == null)
                {
                    throw new InvalidOperationException($"Question with ID '{qInput.QuestionId}' not found");
                }
                question = existing.QuizQuestions.Select(qq => qq.Question).FirstOrDefault(q => q.Id == qInput.QuestionId.Value);
                if (question == null)
                {
                    throw new InvalidOperationException($"Question with ID '{qInput.QuestionId}' not found in quizzes");
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

        await _repository.AddAsync(quiz);
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Quiz '{QuizName}' created with ID '{QuizId}'", quiz.Name, quiz.Id);

        return await GetQuizByIdAsync(quiz.Id) ?? throw new InvalidOperationException("Failed to retrieve created quiz");
    }

    public async Task<QuizDetailsDto> UpdateQuizAsync(Guid quizId, UpdateQuizRequest request)
    {
        var quiz = await _repository.GetByIdAsync(quizId);

        if (quiz == null)
        {
            throw new InvalidOperationException($"Quiz with ID '{quizId}' not found");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Quiz name cannot be empty", nameof(request.Name));
        }

        quiz.Name = request.Name.Trim();

        quiz.QuizQuestions.Clear();

        foreach (var qInput in request.Questions.OrderBy(q => q.Order))
        {
            Question? question = null;

            if (qInput.QuestionId.HasValue)
            {
                var existing = await _repository.GetByIdAsync(qInput.QuestionId.Value);
                if (existing == null)
                {
                    throw new InvalidOperationException($"Question with ID '{qInput.QuestionId}' not found");
                }
                question = existing.QuizQuestions.Select(qq => qq.Question).FirstOrDefault(q => q.Id == qInput.QuestionId.Value);
                if (question == null)
                {
                    throw new InvalidOperationException($"Question with ID '{qInput.QuestionId}' not found in quizzes");
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

        await _repository.UpdateAsync(quiz);
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Quiz '{QuizName}' (ID: '{QuizId}') updated", quiz.Name, quiz.Id);

        return await GetQuizByIdAsync(quizId) ?? throw new InvalidOperationException("Failed to retrieve updated quiz");
    }

    public async Task SoftDeleteQuizAsync(Guid quizId)
    {
        var quiz = await _repository.GetByIdAsync(quizId);

        if (quiz == null)
        {
            throw new InvalidOperationException($"Quiz with ID '{quizId}' not found");
        }

        quiz.IsDeleted = true;
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

        var quizzes = query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new Quiz
            {
                Id = q.Id,
                Name = q.Name,
                CreatedAt = q.CreatedAt,
                QuizQuestions = q.QuizQuestions
            })
            .ToList();

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
