using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quiz.Api.Questions;
using Quiz.Api.Tests.Infrastructure;
using Quiz.Api.Themes;
using Quiz.Data.Entities;

namespace Quiz.Api.Tests.Questions;

[Collection(ApiCollection.Name)]
public class QuestionEndpointsTests(QuizApiFactory factory)
{
    private const string Route = "/api/admin/questions";

    // Mêmes réglages que l'API : la difficulté circule en texte ("Easy").
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Get_WithoutToken_ReturnsUnauthorizedAsync()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"{Route}/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorizedAsync()
    {
        using var client = factory.CreateClient();
        var themeId = await CreateThemeInDatabaseAsync();

        var response = await client.PostAsJsonAsync(Route, ValidRequest(themeId), _json);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreatedQuestionWithLocationAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var themeId = await CreateThemeInDatabaseAsync();
        var before = DateTimeOffset.UtcNow;
        var request = ValidRequest(themeId) with { Text = "  Quelle est la capitale de l'Australie ?  " };

        var response = await client.PostAsJsonAsync(Route, request, _json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<QuestionResponse>(_json);
        Assert.NotNull(created);
        Assert.Equal("Quelle est la capitale de l'Australie ?", created.Text);
        Assert.Equal(themeId, created.ThemeId);
        Assert.Equal(Difficulty.Medium, created.Difficulty);
        Assert.InRange(created.CreatedAt, before.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.Equal(["Canberra", "Sydney", "Melbourne"], created.Answers.Select(a => a.Text));
        Assert.Equal([true, false, false], created.Answers.Select(a => a.IsCorrect));

        var fetched = await client.GetFromJsonAsync<QuestionResponse>(response.Headers.Location, _json);
        Assert.NotNull(fetched);
        Assert.Equal(created.Answers, fetched.Answers);
        Assert.Equal(created.Text, fetched.Text);
    }

    [Fact]
    public async Task Create_SendsDifficultyAsText_AndAcceptsNoDifficultyAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var themeId = await CreateThemeInDatabaseAsync();

        var response = await client.PostAsJsonAsync(Route, ValidRequest(themeId) with { Difficulty = null }, _json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<QuestionResponse>(_json);
        Assert.Null(created!.Difficulty);

        var withDifficulty = await client.PostAsJsonAsync(Route, ValidRequest(themeId), _json);
        var body = await withDifficulty.Content.ReadAsStringAsync();
        Assert.Contains("\"difficulty\":\"Medium\"", body);
    }

    [Fact]
    public async Task Create_IncreasesThemeQuestionCountAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var themeId = await CreateThemeInDatabaseAsync();

        await client.PostAsJsonAsync(Route, ValidRequest(themeId), _json);

        var theme = await client.GetFromJsonAsync<ThemeResponse>($"/api/admin/themes/{themeId}");
        Assert.Equal(1, theme!.QuestionCount);
    }

    [Fact]
    public async Task Create_WithTwoCorrectAnswers_ReturnsValidationProblemAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var themeId = await CreateThemeInDatabaseAsync();
        var request = ValidRequest(themeId) with
        {
            Answers = [new() { Text = "A", IsCorrect = true }, new() { Text = "B", IsCorrect = true }],
        };

        var response = await client.PostAsJsonAsync(Route, request, _json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(SaveQuestionRequest.Answers), problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithUnknownTheme_ReturnsValidationProblemAsync()
    {
        using var client = await factory.CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(Route, ValidRequest(int.MaxValue), _json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(SaveQuestionRequest.ThemeId), problem!.Errors.Keys);
    }

    [Fact]
    public async Task Get_UnknownQuestion_ReturnsNotFoundAsync()
    {
        using var client = await factory.CreateAdminClientAsync();

        var response = await client.GetAsync($"{Route}/{int.MaxValue}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReplacesFieldsAndAnswersAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var created = await CreateQuestionAsync(client);
        var otherThemeId = await CreateThemeInDatabaseAsync();
        var request = new SaveQuestionRequest
        {
            Text = "Combien de pattes a une araignée ?",
            ThemeId = otherThemeId,
            Difficulty = Difficulty.Easy,
            Answers = [new() { Text = "6" }, new() { Text = "8", IsCorrect = true }],
        };

        var response = await client.PutAsJsonAsync($"{Route}/{created.Id}", request, _json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<QuestionResponse>(_json);
        Assert.NotNull(updated);
        Assert.Equal(request.Text, updated.Text);
        Assert.Equal(otherThemeId, updated.ThemeId);
        Assert.Equal(Difficulty.Easy, updated.Difficulty);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.Equal(["6", "8"], updated.Answers.Select(a => a.Text));
        Assert.Equal([false, true], updated.Answers.Select(a => a.IsCorrect));

        // Les anciennes réponses ont bien disparu de la base, pas seulement de la réponse HTTP.
        await using var db = factory.CreateDbContext();
        Assert.Equal(2, await db.Answers.CountAsync(a => a.QuestionId == created.Id));
    }

    [Fact]
    public async Task Update_WithInvalidBody_ReturnsValidationProblemAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var created = await CreateQuestionAsync(client);
        var request = ValidRequest(created.ThemeId) with { Answers = [new() { Text = "Seule", IsCorrect = true }] };

        var response = await client.PutAsJsonAsync($"{Route}/{created.Id}", request, _json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithUnknownTheme_ReturnsValidationProblemAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var created = await CreateQuestionAsync(client);

        var response = await client.PutAsJsonAsync($"{Route}/{created.Id}", ValidRequest(int.MaxValue), _json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownQuestion_ReturnsNotFoundAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var themeId = await CreateThemeInDatabaseAsync();

        var response = await client.PutAsJsonAsync($"{Route}/{int.MaxValue}", ValidRequest(themeId), _json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesQuestionAndItsAnswersAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var created = await CreateQuestionAsync(client);

        var response = await client.DeleteAsync($"{Route}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Route}/{created.Id}")).StatusCode);
        await using var db = factory.CreateDbContext();
        Assert.False(await db.Answers.AnyAsync(a => a.QuestionId == created.Id));
    }

    [Fact]
    public async Task Delete_UnknownQuestion_ReturnsNotFoundAsync()
    {
        using var client = await factory.CreateAdminClientAsync();

        var response = await client.DeleteAsync($"{Route}/{int.MaxValue}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static SaveQuestionRequest ValidRequest(int themeId) => new()
    {
        Text = "Quelle est la capitale de l'Australie ?",
        ThemeId = themeId,
        Difficulty = Difficulty.Medium,
        Answers =
        [
            new() { Text = "Canberra", IsCorrect = true },
            new() { Text = "Sydney" },
            new() { Text = "Melbourne" },
        ],
    };

    private async Task<QuestionResponse> CreateQuestionAsync(HttpClient client)
    {
        var themeId = await CreateThemeInDatabaseAsync();
        var response = await client.PostAsJsonAsync(Route, ValidRequest(themeId), _json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<QuestionResponse>(_json))!;
    }

    private async Task<int> CreateThemeInDatabaseAsync()
    {
        await using var db = factory.CreateDbContext();
        var theme = new Theme { Name = $"Thème {Guid.NewGuid():N}" };
        db.Themes.Add(theme);
        await db.SaveChangesAsync();
        return theme.Id;
    }
}
