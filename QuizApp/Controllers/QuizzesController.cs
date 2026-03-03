using Microsoft.AspNetCore.Mvc;
using QuizApp.Application.DTOs;
using QuizApp.Application.Services;

namespace QuizApp.Controllers;

/// <summary>
/// API endpoints for managing quizzes
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class QuizzesController : ControllerBase
{
    private readonly QuizService _quizService;
    private readonly ILogger<QuizzesController> _logger;

    public QuizzesController(QuizService quizService, ILogger<QuizzesController> logger)
    {
        _quizService = quizService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of all quizzes
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paginated list of quizzes</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Application.Common.PagedResult<QuizListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Application.Common.PagedResult<QuizListItemDto>>> GetQuizzes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _quizService.GetPagedQuizzesAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific quiz with all its questions
    /// </summary>
    /// <param name="id">Quiz ID</param>
    /// <returns>Quiz details including all questions</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(QuizDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuizDetailsDto>> GetQuiz(Guid id)
    {
        var quiz = await _quizService.GetQuizByIdAsync(id);

        if (quiz == null)
        {
            return NotFound(new { message = $"Quiz with ID {id} not found" });
        }

        return Ok(quiz);
    }

    /// <summary>
    /// Creates a new quiz
    /// </summary>
    /// <param name="request">Quiz creation request</param>
    /// <returns>Created quiz with details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(QuizDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuizDetailsDto>> CreateQuiz([FromBody] CreateQuizRequest request)
    {
        try
        {
            var quiz = await _quizService.CreateQuizAsync(request);
            return CreatedAtAction(nameof(GetQuiz), new { id = quiz.Id }, quiz);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating quiz");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating quiz");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing quiz
    /// </summary>
    /// <param name="id">Quiz ID</param>
    /// <param name="request">Quiz update request</param>
    /// <returns>Updated quiz details</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(QuizDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuizDetailsDto>> UpdateQuiz(Guid id, [FromBody] UpdateQuizRequest request)
    {
        try
        {
            var quiz = await _quizService.UpdateQuizAsync(id, request);
            return Ok(quiz);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Quiz not found or invalid operation");
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating quiz");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Soft deletes a quiz
    /// </summary>
    /// <param name="id">Quiz ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuiz(Guid id)
    {
        try
        {
            await _quizService.SoftDeleteQuizAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Quiz not found for deletion");
            return NotFound(new { message = ex.Message });
        }
    }
}
