using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApp.Domain.Entities;
using QuizApp.Infrastructure.Persistence;

namespace QuizApp.Controllers;

/// <summary>
/// API endpoints for creating and managing test data
/// </summary>
[ApiController]
[Route("[controller]/[action]")]
public class TestDataController : ControllerBase
{
    private readonly QuizDbContext _dbContext;

    public TestDataController(QuizDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IDictionary<string, int>>> Count()
    {
        var data = new Dictionary<string, int>
        {
            ["Quizzes"] = await _dbContext.Quizzes.CountAsync(),
            ["Questions"] = await _dbContext.Questions.CountAsync(),
            ["QuizQuestions"] = await _dbContext.QuizQuestions.CountAsync()
        };

        return Ok(data);
    }

    [HttpPost]
    public async Task<ActionResult<IDictionary<string, int>>> Generate()
    {
        // Create sample questions
        var questions = new List<Question>
        {
            new Question { Text = "Koji je glavni grad Francuske?", CorrectAnswer = "Paris", CreatedAt = DateTime.UtcNow },
            new Question { Text = "Koliko je 2 + 2?", CorrectAnswer = "4", CreatedAt = DateTime.UtcNow },
            new Question { Text = "Ko je napisao 'Hamleta'?", CorrectAnswer = "William Shakespeare", CreatedAt = DateTime.UtcNow },
            new Question { Text = "Koja je tacka kljucanja vode (°C)?", CorrectAnswer = "100", CreatedAt = DateTime.UtcNow },
            new Question { Text = "Koja je najveca planeta u nasem Suncevom sistemu?", CorrectAnswer = "Jupiter", CreatedAt = DateTime.UtcNow }
        };

        await _dbContext.Questions.AddRangeAsync(questions);

        // Create a quiz that uses some of the above questions
        var quiz1 = new Quiz
        {
            Name = "Opste znanje",
            CreatedAt = DateTime.UtcNow
        };

        quiz1.QuizQuestions.Add(new QuizQuestion { Quiz = quiz1, Question = questions[0], Order = 1 });
        quiz1.QuizQuestions.Add(new QuizQuestion { Quiz = quiz1, Question = questions[1], Order = 2 });
        quiz1.QuizQuestions.Add(new QuizQuestion { Quiz = quiz1, Question = questions[2], Order = 3 });

        // Create another quiz
        var quiz2 = new Quiz
        {
            Name = "Nauka i matematika",
            CreatedAt = DateTime.UtcNow
        };
        quiz2.QuizQuestions.Add(new QuizQuestion { Quiz = quiz2, Question = questions[1], Order = 1 });
        quiz2.QuizQuestions.Add(new QuizQuestion { Quiz = quiz2, Question = questions[3], Order = 2 });

        // Add a quiz that will be soft-deleted to demonstrate soft delete behavior
        var quiz3 = new Quiz
        {
            Name = "To be deleted",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = true
        };

        quiz3.QuizQuestions.Add(new QuizQuestion { Quiz = quiz3, Question = questions[4], Order = 1 });

        await _dbContext.Quizzes.AddRangeAsync(quiz1, quiz2, quiz3);

        await _dbContext.SaveChangesAsync();

        return await Count();
    }
}
