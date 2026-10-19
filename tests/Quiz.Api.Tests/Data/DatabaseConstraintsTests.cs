using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quiz.Api.Tests.Infrastructure;
using Quiz.Data.Entities;

namespace Quiz.Api.Tests.Data;

/// <summary>
/// Vérifie les contraintes posées par la configuration EF Core, directement contre PostgreSQL.
/// </summary>
[Collection(ApiCollection.Name)]
public class DatabaseConstraintsTests(QuizApiFactory factory)
{
    [Fact]
    public async Task DeleteTheme_WithQuestions_IsRejectedByDatabaseAsync()
    {
        var themeId = await CreateThemeWithQuestionAsync();

        await using var db = factory.CreateDbContext();
        var theme = await db.Themes.SingleAsync(t => t.Id == themeId);
        db.Themes.Remove(theme);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgresError = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.RestrictViolation, postgresError.SqlState);
    }

    [Fact]
    public async Task DeleteQuestion_DeletesItsAnswersAsync()
    {
        var themeId = await CreateThemeWithQuestionAsync();

        await using (var db = factory.CreateDbContext())
        {
            var question = await db.Questions.SingleAsync(q => q.ThemeId == themeId);
            db.Questions.Remove(question);
            await db.SaveChangesAsync();
        }

        await using var verification = factory.CreateDbContext();
        var remainingAnswers = await verification.Answers
            .CountAsync(a => a.Question.ThemeId == themeId);
        Assert.Equal(0, remainingAnswers);
    }

    [Fact]
    public async Task CreateTheme_WithExistingName_IsRejectedByDatabaseAsync()
    {
        var name = UniqueName();

        await using (var db = factory.CreateDbContext())
        {
            db.Themes.Add(new Theme { Name = name });
            await db.SaveChangesAsync();
        }

        await using var duplicate = factory.CreateDbContext();
        duplicate.Themes.Add(new Theme { Name = name });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
        var postgresError = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresError.SqlState);
    }

    /// <summary>
    /// Crée un thème contenant une question à deux réponses, et renvoie l'identifiant du thème.
    /// Chaque test utilise un nom unique, car la base est partagée par toute la collection.
    /// </summary>
    private async Task<int> CreateThemeWithQuestionAsync()
    {
        await using var db = factory.CreateDbContext();
        var theme = new Theme
        {
            Name = UniqueName(),
            Questions =
            [
                new Question
                {
                    Text = "Quelle est la capitale de la France ?",
                    CreatedAt = DateTimeOffset.UtcNow,
                    Answers =
                    [
                        new Answer { Text = "Paris", IsCorrect = true },
                        new Answer { Text = "Lyon", IsCorrect = false },
                    ],
                },
            ],
        };
        db.Themes.Add(theme);
        await db.SaveChangesAsync();
        return theme.Id;
    }

    private static string UniqueName() => $"Thème {Guid.NewGuid():N}";
}
