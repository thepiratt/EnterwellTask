using System.ComponentModel.Composition;
using System.Text;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using QuizApp.Domain.Entities;
using QuizApp.Exporters.Abstractions;

namespace QuizApp.Exporters.Csv;

[Export(typeof(IQuizExporter))]
public class CsvQuizExporter : IQuizExporter
{
    public string Name => "CSV";
    public string ContentType => "text/csv";
    public string FileExtension => ".csv";

    public byte[] Export(Quiz quiz)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        // Write header
        csv.WriteField("Question Number");
        csv.WriteField("Question Text");
        csv.NextRecord();

        // Write questions (exclude correct answers)
        var orderedQuestions = quiz.QuizQuestions.OrderBy(qq => qq.Order);

        foreach (var quizQuestion in orderedQuestions)
        {
            if (quizQuestion.Question != null)
            {
                csv.WriteField(quizQuestion.Order);
                csv.WriteField(quizQuestion.Question.Text);
                csv.NextRecord();
            }
        }

        writer.Flush();
        return ms.ToArray();
    }
}
