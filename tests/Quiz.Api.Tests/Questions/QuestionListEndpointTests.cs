using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Quiz.Api.Paging;
using Quiz.Api.Questions;
using Quiz.Api.Tests.Infrastructure;
using Quiz.Data.Entities;

namespace Quiz.Api.Tests.Questions;

/// <summary>
/// La base est partagée entre les tests : chaque test crée son propre thème et filtre dessus,
/// pour ne voir que ses propres questions.
/// </summary>
[Collection(ApiCollection.Name)]
public class QuestionListEndpointTests(QuizApiFactory factory)
{
    private const string Route = "/api/admin/questions";

    private static readonly DateTimeOffset _baseDate = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorizedAsync()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_FilteredByTheme_ReturnsSummariesNewestFirstAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var themeId = await CreateThemeWithQuestionsAsync("Plus ancienne", "Plus récente");
        await CreateThemeWithQuestionsAsync("Autre thème");

        var page = await ListAsync(client, $"?themeId={themeId}");

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(["Plus récente", "Plus ancienne"], page.Items.Select(q => q.Text));
        var first = page.Items[0];
        Assert.Equal(themeId, first.ThemeId);
        Assert.StartsWith("Thème", first.ThemeName);
        Assert.Equal(Difficulty.Hard, first.Difficulty);
        Assert.Equal(2, first.AnswerCount);
        Assert.Equal(_baseDate.AddMinutes(1), first.CreatedAt);
    }

    [Fact]
    public async Task List_WithPageSize_ReturnsRequestedPageAndTotalCountAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var themeId = await CreateThemeWithQuestionsAsync("Q1", "Q2", "Q3");

        var page = await ListAsync(client, $"?themeId={themeId}&page=2&pageSize=2");

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(["Q1"], page.Items.Select(q => q.Text));
    }

    [Fact]
    public async Task List_WithSearch_MatchesTextIgnoringCaseAsync()
    {
        using var client = await factory.CreateAdminClientAsync();
        var themeId = await CreateThemeWithQuestionsAsync(
            "Quelle est la capitale de l'Australie ?", "Qui a peint la Joconde ?");

        var page = await ListAsync(client, $"?themeId={themeId}&search=%20CAPITALE%20");

        Assert.Equal(["Quelle est la capitale de l'Australie ?"], page.Items.Select(q => q.Text));
        Assert.Equal(1, page.TotalCount);
    }

    [Theory]
    [InlineData("100%25", "Vrai à 100% ?")]
    [InlineData("a_b", "Que vaut a_b ?")]
    public async Task List_WithLikeWildcardsInSearch_SearchesThemLiterallyAsync(string search, string expected)
    {
        using var client = await factory.CreateAdminClientAsync();
        var themeId = await CreateThemeWithQuestionsAsync("Vrai à 100% ?", "Vrai à 1000 ?", "Que vaut a_b ?", "Que vaut axb ?");

        var page = await ListAsync(client, $"?themeId={themeId}&search={search}");

        Assert.Equal([expected], page.Items.Select(q => q.Text));
    }

    [Fact]
    public async Task List_WithUnknownTheme_ReturnsEmptyPageAsync()
    {
        using var client = await factory.CreateAdminClientAsync();

        var page = await ListAsync(client, $"?themeId={int.MaxValue}");

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Theory]
    [InlineData("?page=0", nameof(QuestionListQuery.Page))]
    [InlineData("?pageSize=101", nameof(QuestionListQuery.PageSize))]
    public async Task List_WithInvalidPaging_ReturnsValidationProblemAsync(string queryString, string field)
    {
        using var client = await factory.CreateAdminClientAsync();

        var response = await client.GetAsync(Route + queryString);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(field, problem!.Errors.Keys);
    }

    private static async Task<PagedResponse<QuestionSummaryResponse>> ListAsync(HttpClient client, string queryString)
    {
        var response = await client.GetAsync(Route + queryString);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResponse<QuestionSummaryResponse>>(_json))!;
    }

    /// <summary>Crée un thème et ses questions, la première étant la plus ancienne (une minute d'écart).</summary>
    private async Task<int> CreateThemeWithQuestionsAsync(params string[] texts)
    {
        await using var db = factory.CreateDbContext();
        var theme = new Theme { Name = $"Thème {Guid.NewGuid():N}" };
        for (var i = 0; i < texts.Length; i++)
        {
            theme.Questions.Add(new Question
            {
                Text = texts[i],
                Difficulty = Difficulty.Hard,
                CreatedAt = _baseDate.AddMinutes(i),
                Answers = [new Answer { Text = "Oui", IsCorrect = true }, new Answer { Text = "Non" }],
            });
        }

        db.Themes.Add(theme);
        await db.SaveChangesAsync();
        return theme.Id;
    }
}
