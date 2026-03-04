using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using QuizApp.Application.Interfaces;
using QuizApp.Application.Services;
using QuizApp.Infrastructure.Export;
using QuizApp.Infrastructure.Middleware;
using QuizApp.Infrastructure.Persistence;
using QuizApp.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container
builder.Services.AddControllers();

// Configure Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<QuizDbContext>(options =>
    options.UseSqlServer(connectionString,
        sqlOptions => sqlOptions.MigrationsAssembly("QuizApp")));

// Add Application Services
builder.Services.AddScoped<QuizService>();

// Register repository implementations
builder.Services.AddScoped<IQuizRepository, QuizRepository>();

// Add MEF Exporter Loader
builder.Services.AddSingleton<ExporterLoader>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ExporterLoader>>();
    var loader = new ExporterLoader(logger);

    // Get the plugins directory
    var pluginsPath = Path.Combine(
        AppContext.BaseDirectory,
        "plugins"
    );

    loader.LoadExporters(pluginsPath);
    return loader;
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Quiz API",
        Version = "v1",
        Description = "A ASP.NET Core Web API for managing quizzes with CRUD operations, question recycling, pagination, soft delete, and plugin-based export system.",
        Contact = new OpenApiContact
        {
            Name = "Quiz App Team"
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        policyBuilder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Quiz API v1");
    c.RoutePrefix = string.Empty; // Serve at root
});

// Apply global exception middleware
app.UseGlobalExceptionMiddleware();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<QuizDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
