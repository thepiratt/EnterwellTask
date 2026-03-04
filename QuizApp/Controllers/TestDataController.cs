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
        // Create sample questions using domain factory
        var questions = new List<Question>
        {
            Question.Create("Koji je glavni grad Francuske?", "Paris"),
            Question.Create("Koliko je 2 + 2?", "4"),
            Question.Create("Ko je napisao 'Hamleta'?", "William Shakespeare"),
            Question.Create("Koja je tacka kljucanja vode (°C)?", "100"),
            Question.Create("Koja je najveca planeta u nasem Suncevom sistemu?", "Jupiter")
        };

        await _dbContext.Questions.AddRangeAsync(questions);

        // Create a quiz that uses some of the above questions
        var quiz1 = Quiz.Create("Opste znanje");
        quiz1.AddQuestion(questions[0], 1);
        quiz1.AddQuestion(questions[1], 2);
        quiz1.AddQuestion(questions[2], 3);

        // Create another quiz
        var quiz2 = Quiz.Create("Nauka i matematika");
        quiz2.AddQuestion(questions[1], 1);
        quiz2.AddQuestion(questions[3], 2);

        // Add a quiz that will be soft-deleted to demonstrate soft delete behavior
        var quiz3 = Quiz.Create("To be deleted");
        quiz3.AddQuestion(questions[4], 1);
        quiz3.SoftDelete();

        await _dbContext.Quizzes.AddRangeAsync(quiz1, quiz2, quiz3);

        await _dbContext.SaveChangesAsync();

        return await Count();
    }
}
