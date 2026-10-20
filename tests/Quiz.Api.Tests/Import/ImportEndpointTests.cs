using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quiz.Api.Import;
using Quiz.Api.Questions;
using Quiz.Api.Tests.Infrastructure;
using Quiz.Data.Entities;

namespace Quiz.Api.Tests.Import;

/// <summary>Chaque test utilise des noms de thèmes uniques, car la base est partagée.</summary>
[Collection(ApiCollection.Name)]
public class ImportEndpointTests(QuizApiFactory factory)
{
    private const string Route = "/api/admin/import";

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Import_WithoutToken_ReturnsUnauthorizedAsync()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(Route, File(Theme(UniqueName(), Question("Q1"))), _json);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Import_CreatesThemesQuestionsAndAnswersAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var cinema = UniqueName();
        var sport = UniqueName();
        var request = File(
            Theme(cinema, Question("  Qui a réalisé Alien ?  ", Difficulty.Medium), Question("Q2")) with { Icon = "🎬" },
            Theme(sport, Question("Q3")));

        var summary = await ImportAsync(client, request);

        Assert.Equal(new ImportResponse(ThemesCreated: 2, QuestionsImported: 3, QuestionsSkipped: 0), summary);
        await using var db = factory.CreateDbContext();
        var theme = await db.Themes.Include(t => t.Questions).ThenInclude(q => q.Answers).SingleAsync(t => t.Name == cinema);
        Assert.Equal("🎬", theme.Icon);
        var question = Assert.Single(theme.Questions, q => q.Text == "Qui a réalisé Alien ?");
        Assert.Equal(Difficulty.Medium, question.Difficulty);
        Assert.Equal(["Bonne", "Mauvaise"], question.Answers.OrderBy(a => a.Id).Select(a => a.Text));
        Assert.Equal([true, false], question.Answers.OrderBy(a => a.Id).Select(a => a.IsCorrect));
    }

    [Fact]
    public async Task Import_ReusesExistingThemeIgnoringCase_AndKeepsItsIconAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var name = UniqueName();
        await ImportAsync(client, File(Theme(name, Question("Q1")) with { Icon = "🎬" }));

        var summary = await ImportAsync(client, File(Theme(name.ToUpperInvariant(), Question("Q2")) with { Icon = "🎥" }));

        Assert.Equal(new ImportResponse(ThemesCreated: 0, QuestionsImported: 1, QuestionsSkipped: 0), summary);
        await using var db = factory.CreateDbContext();
        var theme = await db.Themes.Include(t => t.Questions).SingleAsync(t => t.Name == name);
        Assert.Equal("🎬", theme.Icon);
        Assert.Equal(2, theme.Questions.Count);
    }

    [Fact]
    public async Task Import_SameFileTwice_SkipsExistingQuestionsAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var request = File(Theme(UniqueName(), Question("Q1"), Question("Q2")));
        await ImportAsync(client, request);

        var summary = await ImportAsync(client, request);

        Assert.Equal(new ImportResponse(ThemesCreated: 0, QuestionsImported: 0, QuestionsSkipped: 2), summary);
    }

    [Fact]
    public async Task Import_WithDuplicateQuestionInFile_ImportsItOnceAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var name = UniqueName();

        var summary = await ImportAsync(client, File(Theme(name, Question("Même énoncé"), Question(" MÊME ÉNONCÉ "))));

        Assert.Equal(new ImportResponse(ThemesCreated: 1, QuestionsImported: 1, QuestionsSkipped: 1), summary);
    }

    [Fact]
    public async Task Import_WithOneInvalidQuestion_ReturnsValidationProblemAndWritesNothingAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var name = UniqueName();
        var invalid = Question("Sans bonne réponse") with
        {
            Answers = [new() { Text = "A" }, new() { Text = "B" }],
        };

        var response = await client.PostAsJsonAsync(Route, File(Theme(name, Question("Q1"), invalid)), _json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Themes[0].Questions[1].Answers", problem!.Errors.Keys);
        await using var db = factory.CreateDbContext();
        Assert.False(await db.Themes.AnyAsync(t => t.Name == name));
    }

    private static async Task<ImportResponse> ImportAsync(HttpClient client, ImportRequest request)
    {
        var response = await client.PostAsJsonAsync(Route, request, _json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ImportResponse>(_json))!;
    }

    private static ImportRequest File(params ImportThemeRequest[] themes) => new() { Themes = [.. themes] };

    private static ImportThemeRequest Theme(string name, params ImportQuestionRequest[] questions) =>
        new() { Name = name, Questions = [.. questions] };

    private static ImportQuestionRequest Question(string text, Difficulty? difficulty = null) => new()
    {
        Text = text,
        Difficulty = difficulty,
        Answers = [new SaveAnswerRequest { Text = "Bonne", IsCorrect = true }, new SaveAnswerRequest { Text = "Mauvaise" }],
    };

    private static string UniqueName() => $"Thème {Guid.NewGuid():N}";
}
