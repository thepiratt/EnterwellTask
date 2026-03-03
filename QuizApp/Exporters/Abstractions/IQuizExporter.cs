using QuizApp.Domain.Entities;

namespace QuizApp.Exporters.Abstractions;

public interface IQuizExporter
{
    string Name { get; }
    string ContentType { get; }
    string FileExtension { get; }
    byte[] Export(Quiz quiz);
}
