using Microsoft.AspNetCore.Mvc;
using QuizApp.Application.DTOs;
using QuizApp.Application.Services;

namespace QuizApp.Controllers;

/// <summary>
/// API endpoints for searching and managing questions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class QuestionsController : ControllerBase
{
    private readonly QuizService _quizService;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(QuizService quizService, ILogger<QuestionsController> logger)
    {
        _quizService = quizService;
        _logger = logger;
    }

    /// <summary>
    /// Searches questions by text
    /// </summary>
    /// <param name="search">Search term (optional, searches question text)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paginated list of matching questions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Application.Common.PagedResult<QuestionSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Application.Common.PagedResult<QuestionSearchResultDto>>> SearchQuestions(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _quizService.SearchQuestionsAsync(search ?? string.Empty, page, pageSize);
        return Ok(result);
    }
}
