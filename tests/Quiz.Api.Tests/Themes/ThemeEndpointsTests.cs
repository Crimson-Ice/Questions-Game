using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quiz.Api.Tests.Infrastructure;
using Quiz.Api.Themes;
using Quiz.Data.Entities;

namespace Quiz.Api.Tests.Themes;

[Collection(ApiCollection.Name)]
public class ThemeEndpointsTests(QuizApiFactory factory)
{
    private const string Route = "/api/admin/themes";

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorizedAsync()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreatedThemeWithLocationAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var name = UniqueName();

        var response = await client.PostAsJsonAsync(Route, new SaveThemeRequest { Name = $"  {name}  ", Icon = "🎬" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ThemeResponse>();
        Assert.NotNull(created);
        Assert.Equal(name, created.Name);
        Assert.Equal("🎬", created.Icon);
        Assert.Equal(0, created.QuestionCount);

        var fetched = await client.GetFromJsonAsync<ThemeResponse>(response.Headers.Location);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Create_WithExistingNameInAnotherCase_ReturnsConflictAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var name = UniqueName();
        await client.PostAsJsonAsync(Route, new SaveThemeRequest { Name = name });

        var response = await client.PostAsJsonAsync(Route, new SaveThemeRequest { Name = name.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithoutName_ReturnsValidationProblemAsync(string name)
    {
        using var client = await factory.CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(Route, new SaveThemeRequest { Name = name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(SaveThemeRequest.Name), problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithTooLongName_ReturnsValidationProblemAsync()
    {
        using var client = await factory.CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(Route, new SaveThemeRequest { Name = new string('a', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsThemesWithQuestionCountAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var themeId = await CreateThemeWithQuestionsInDatabaseAsync(questionCount: 2);

        var themes = await client.GetFromJsonAsync<List<ThemeResponse>>(Route);

        var theme = Assert.Single(themes!, t => t.Id == themeId);
        Assert.Equal(2, theme.QuestionCount);
    }

    [Fact]
    public async Task Update_RenamesThemeAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var created = await CreateThemeAsync(client);
        var newName = UniqueName();

        var response = await client.PutAsJsonAsync($"{Route}/{created.Id}", new SaveThemeRequest { Name = newName });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ThemeResponse>();
        Assert.Equal(newName, updated!.Name);
    }

    [Fact]
    public async Task Update_KeepingItsOwnName_IsAllowedAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var created = await CreateThemeAsync(client);

        var response = await client.PutAsJsonAsync($"{Route}/{created.Id}",
            new SaveThemeRequest { Name = created.Name, Icon = "🎵" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithNameOfAnotherTheme_ReturnsConflictAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var first = await CreateThemeAsync(client);
        var second = await CreateThemeAsync(client);

        var response = await client.PutAsJsonAsync($"{Route}/{second.Id}", new SaveThemeRequest { Name = first.Name });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownTheme_ReturnsNotFoundAsync()
    {
        using var client = await factory.CreateAdminClientAsync();

        var response = await client.PutAsJsonAsync($"{Route}/{int.MaxValue}", new SaveThemeRequest { Name = UniqueName() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_EmptyTheme_RemovesItAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var created = await CreateThemeAsync(client);

        var response = await client.DeleteAsync($"{Route}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var fetch = await client.GetAsync($"{Route}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task Delete_ThemeWithQuestions_ReturnsConflictAndKeepsThemeAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var themeId = await CreateThemeWithQuestionsInDatabaseAsync(questionCount: 1);

        var response = await client.DeleteAsync($"{Route}/{themeId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Ce thème contient des questions.", problem!.Title);

        await using var db = factory.CreateDbContext();
        Assert.True(await db.Themes.AnyAsync(t => t.Id == themeId));
    }

    [Fact]
    public async Task Delete_UnknownTheme_ReturnsNotFoundAsync()
    {
        using var client = await factory.CreateAdminClientAsync();

        var response = await client.DeleteAsync($"{Route}/{int.MaxValue}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<ThemeResponse> CreateThemeAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(Route, new SaveThemeRequest { Name = UniqueName() });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ThemeResponse>())!;
    }

    private async Task<int> CreateThemeWithQuestionsInDatabaseAsync(int questionCount)
    {
        await using var db = factory.CreateDbContext();
        var theme = new Theme { Name = UniqueName() };
        for (var i = 1; i <= questionCount; i++)
        {
            theme.Questions.Add(new Question
            {
                Text = $"Question {i}",
                CreatedAt = DateTimeOffset.UtcNow,
                Answers =
                [
                    new Answer { Text = "Bonne", IsCorrect = true },
                    new Answer { Text = "Mauvaise", IsCorrect = false },
                ],
            });
        }

        db.Themes.Add(theme);
        await db.SaveChangesAsync();
        return theme.Id;
    }

    private static string UniqueName() => $"Thème {Guid.NewGuid():N}";
}
