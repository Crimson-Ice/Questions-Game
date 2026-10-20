using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Quiz.Api.Import;
using Quiz.Api.Tests.Infrastructure;
using Quiz.Data;

namespace Quiz.Api.Tests.Import;

/// <summary>Chaque test travaille sur une base neuve, et sur un dossier temporaire de fichiers JSON.</summary>
[Collection(ApiCollection.Name)]
public sealed class QuestionBankSeederTests(QuizApiFactory factory) : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("quiz-seed-").FullName;

    [Fact]
    public async Task Seed_OnEmptyDatabase_ImportsAllFilesAsync()
    {
        await using var db = await factory.CreateEmptyDatabaseAsync();
        WriteFile("a.json", ThemeJson("Cinéma", "Q1"));
        WriteFile("b.json", ThemeJson("Sport", "Q2", "Q3"));

        await CreateSeeder(db).SeedAsync(_directory);

        Assert.Equal(["Cinéma", "Sport"], await db.Themes.OrderBy(t => t.Name).Select(t => t.Name).ToListAsync());
        Assert.Equal(3, await db.Questions.CountAsync());
        Assert.Equal(6, await db.Answers.CountAsync());
    }

    [Fact]
    public async Task Seed_WhenDatabaseAlreadyHasQuestions_DoesNothingAsync()
    {
        await using var db = await factory.CreateEmptyDatabaseAsync();
        WriteFile("a.json", ThemeJson("Cinéma", "Q1"));
        await CreateSeeder(db).SeedAsync(_directory);
        WriteFile("b.json", ThemeJson("Sport", "Q2"));

        await CreateSeeder(db).SeedAsync(_directory);

        Assert.Equal(1, await db.Questions.CountAsync());
        Assert.False(await db.Themes.AnyAsync(t => t.Name == "Sport"));
    }

    [Fact]
    public async Task Seed_WithInvalidQuestion_ThrowsAndWritesNothingAsync()
    {
        await using var db = await factory.CreateEmptyDatabaseAsync();
        WriteFile("a.json", ThemeJson("Cinéma", "Q1"));
        WriteFile("b.json", """
            { "themes": [ { "name": "Sport", "questions": [
              { "text": "Sans bonne réponse", "answers": [ { "text": "A" }, { "text": "B" } ] } ] } ] }
            """);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSeeder(db).SeedAsync(_directory));

        Assert.Contains("Themes[1].Questions[0].Answers", exception.Message);
        Assert.False(await db.Themes.AnyAsync());
    }

    [Theory]
    [InlineData("{ \"themes\": [ ")]
    [InlineData("{ \"themes\": null }")]
    public async Task Seed_WithMalformedFile_ThrowsWithFileNameAsync(string content)
    {
        await using var db = await factory.CreateEmptyDatabaseAsync();
        WriteFile("casse.json", content);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSeeder(db).SeedAsync(_directory));

        Assert.Contains("casse.json", exception.Message);
    }

    [Fact]
    public async Task Seed_WithMissingDirectory_ThrowsAsync()
    {
        await using var db = await factory.CreateEmptyDatabaseAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSeeder(db).SeedAsync(Path.Combine(_directory, "inexistant")));
    }

    /// <summary>Garde-fou de la vraie banque initiale : tous les fichiers de <c>seed/</c> se chargent.</summary>
    [Fact]
    public async Task Seed_RepositoryBank_LoadsEveryThemeAsync()
    {
        await using var db = await factory.CreateEmptyDatabaseAsync();

        await CreateSeeder(db).SeedAsync(FindRepositorySeedDirectory());

        var questionCounts = await db.Themes.Select(t => t.Questions.Count).ToListAsync();
        Assert.NotEmpty(questionCounts);
        Assert.All(questionCounts, count => Assert.True(count > 0));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    /// <summary>Le seeder avec les vrais réglages JSON de l'API, branché sur la base du test.</summary>
    private QuestionBankSeeder CreateSeeder(QuizDbContext db) => new(
        db,
        new QuestionBankImporter(db, TimeProvider.System),
        new ImportRequestValidator(),
        factory.Services.GetRequiredService<IOptions<JsonOptions>>(),
        NullLogger<QuestionBankSeeder>.Instance);

    private void WriteFile(string name, string content) => File.WriteAllText(Path.Combine(_directory, name), content);

    private static string ThemeJson(string theme, params string[] questions)
    {
        var items = questions.Select(q =>
            $$"""{ "text": "{{q}}", "difficulty": "Easy", "answers": [ { "text": "Oui", "isCorrect": true }, { "text": "Non" } ] }""");
        return $$"""{ "themes": [ { "name": "{{theme}}", "questions": [ {{string.Join(", ", items)}} ] } ] }""";
    }

    private static string FindRepositorySeedDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Quiz.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Racine du dépôt introuvable."), "seed");
    }
}
