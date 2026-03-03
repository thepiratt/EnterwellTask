using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using QuizApp.Exporters.Abstractions;

namespace QuizApp.Infrastructure.Export;

// Loads and manages MEF exporters dynamically
public class ExporterLoader
{
    private readonly ILogger<ExporterLoader> _logger;
    private CompositionContainer? _container;
    private IEnumerable<IQuizExporter>? _exporters;

    public ExporterLoader(ILogger<ExporterLoader> logger)
    {
        _logger = logger;
    }
    
    // Loads all available exporters from the plugins folder
    public void LoadExporters(string pluginsPath)
    {
        try
        {
            if (!Directory.Exists(pluginsPath))
            {
                _logger.LogWarning("Plugins directory not found: {PluginsPath}", pluginsPath);
                _exporters = new List<IQuizExporter>();
                return;
            }

            var catalog = new DirectoryCatalog(pluginsPath, "*.dll");
            _container = new CompositionContainer(catalog);

            _exporters = _container.GetExportedValues<IQuizExporter>().ToList();

            _logger.LogInformation("Loaded {ExporterCount} exporters from {PluginsPath}",
                _exporters.Count(), pluginsPath);

            foreach (var exporter in _exporters)
            {
                _logger.LogInformation("Loaded exporter: {ExporterName}", exporter.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading exporters from {PluginsPath}", pluginsPath);
            _exporters = new List<IQuizExporter>();
        }
    }

    // Gets all available exporters
    public IEnumerable<IQuizExporter> GetExporters()
    {
        return _exporters ?? new List<IQuizExporter>();
    }

    public IQuizExporter? GetExporter(string name)
    {
        return GetExporters().FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    // Disposes the MEF container
    public void Dispose()
    {
        _container?.Dispose();
    }
}
