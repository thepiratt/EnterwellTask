using Microsoft.AspNetCore.Mvc;
using QuizApp.Application.Services;
using QuizApp.Infrastructure.Export;
using QuizApp.Domain.Entities;

namespace QuizApp.Controllers;

/// <summary>
/// API endpoints for exporting quizzes
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExportersController : ControllerBase
{
    private readonly ExporterLoader _exporterLoader;
    private readonly QuizService _quizService;
    private readonly ILogger<ExportersController> _logger;

    public ExportersController(
        ExporterLoader exporterLoader,
        QuizService quizService,
        ILogger<ExportersController> logger)
    {
        _exporterLoader = exporterLoader;
        _quizService = quizService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available exporters
    /// </summary>
    /// <returns>List of exporter names</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExporterInfo>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ExporterInfo>> GetExporters()
    {
        var exporters = _exporterLoader.GetExporters()
            .Select(e => new ExporterInfo { Name = e.Name })
            .ToList();

        return Ok(exporters);
    }

    /// <summary>
    /// Exports a quiz using the specified exporter
    /// </summary>
    /// <param name="quizId">Quiz ID to export</param>
    /// <param name="exporter">Exporter name (e.g., "CSV")</param>
    /// <returns>Exported file</returns>
    [HttpGet("quizzes/{quizId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportQuiz(Guid quizId, [FromQuery] string exporter = "CSV")
    {
        try
        {
            var quiz = await _quizService.GetQuizByIdAsync(quizId);
            if (quiz == null)
            {
                return NotFound(new { message = $"Quiz with ID {quizId} not found" });
            }

            var exporterInstance = _exporterLoader.GetExporter(exporter);
            if (exporterInstance == null)
            {
                return BadRequest(new { message = $"Exporter '{exporter}' not found" });
            }

            var quizEntity = Quiz.Rehydrate(quiz.Id, quiz.Name, quiz.CreatedAt, false, quiz.Questions
                .Select(q => new QuizQuestion
                {
                    Order = q.Order,
                    Question = Question.Rehydrate(q.Id, q.Text, q.CorrectAnswer ?? string.Empty, q.CreatedAt)
                })
                .ToList());

            var fileContent = exporterInstance.Export(quizEntity);

            var fileName = $"{quiz.Name.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{exporterInstance.FileExtension}";

            _logger.LogInformation("Quiz {QuizId} exported as {ExporterName}", quizId, exporter);

            return File(fileContent, exporterInstance.ContentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting quiz {QuizId}", quizId);
            return StatusCode(500, new { message = "Error exporting quiz" });
        }
    }

    /// <summary>
    /// DTO for exporter information
    /// </summary>
    public class ExporterInfo
    {
        public string Name { get; set; } = string.Empty;
    }
}
