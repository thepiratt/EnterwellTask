using Microsoft.EntityFrameworkCore;
using QuizApp.Domain.Entities;

namespace QuizApp.Infrastructure.Persistence;

public class QuizDbContext(DbContextOptions<QuizDbContext> options) : DbContext(options)
{
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Quiz entity
        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(q => q.Id);

            entity.Property(q => q.Name)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(q => q.CreatedAt)
                .IsRequired();

            entity.Property(q => q.IsDeleted)
                .IsRequired();

            entity.HasMany(q => q.QuizQuestions)
                .WithOne(qq => qq.Quiz)
                .HasForeignKey(qq => qq.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // Global query filter for soft delete
            entity.HasQueryFilter(q => !q.IsDeleted);

            entity.ToTable("Quizzes");
        });

        // Configure Question entity
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(q => q.Id);

            entity.Property(q => q.Text)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(q => q.CorrectAnswer)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(q => q.CreatedAt)
                .IsRequired();

            entity.HasMany(q => q.QuizQuestions)
                .WithOne(qq => qq.Question)
                .HasForeignKey(qq => qq.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index on Text for efficient searching
            entity.HasIndex(q => q.Text)
                .HasDatabaseName("IX_Questions_Text");

            entity.ToTable("Questions");
        });

        // Configure QuizQuestion join table
        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.HasKey(qq => new { qq.QuizId, qq.QuestionId });

            entity.Property(qq => qq.Order)
                .IsRequired();

            entity.Navigation(qq => qq.Quiz)
                .AutoInclude();

            entity.Navigation(qq => qq.Question)
                .AutoInclude();

            // Ensure QuizQuestion respects Quiz soft-delete filter to avoid EF warnings
            entity.HasQueryFilter(qq => !qq.Quiz.IsDeleted);

            entity.ToTable("QuizQuestions");
        });
    }
}
