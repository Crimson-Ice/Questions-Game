using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Quiz.Api.Paging;
using Quiz.Api.Validation;
using Quiz.Data;
using Quiz.Data.Entities;

namespace Quiz.Api.Questions;

/// <summary>Création, lecture, modification et suppression des questions du back-office.</summary>
public static class QuestionEndpoints
{
    private const string GetQuestionRoute = "GetQuestion";

    private const string LikeEscapeCharacter = "\\";

    public static void MapQuestionEndpoints(this WebApplication app)
    {
        var questions = app.MapGroup("/api/admin/questions").RequireAuthorization();

        questions.MapGet("/", ListAsync).AddEndpointFilter<ValidationFilter<QuestionListQuery>>();
        questions.MapGet("/{id:int}", GetAsync).WithName(GetQuestionRoute);
        questions.MapPost("/", CreateAsync).AddEndpointFilter<ValidationFilter<SaveQuestionRequest>>();
        questions.MapPut("/{id:int}", UpdateAsync).AddEndpointFilter<ValidationFilter<SaveQuestionRequest>>();
        questions.MapDelete("/{id:int}", DeleteAsync);
    }

    private static async Task<Ok<PagedResponse<QuestionSummaryResponse>>> ListAsync(
        [AsParameters] QuestionListQuery query, QuizDbContext db, CancellationToken cancellationToken)
    {
        var questions = db.Questions.AsQueryable();
        if (query.ThemeId is { } themeId)
        {
            questions = questions.Where(q => q.ThemeId == themeId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ILIKE : recherche sans tenir compte de la casse. Les jokers % et _ tapés par
            // l'utilisateur sont échappés pour être cherchés tels quels.
            var pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";
            questions = questions.Where(q => EF.Functions.ILike(q.Text, pattern, LikeEscapeCharacter));
        }

        var totalCount = await questions.CountAsync(cancellationToken);
        var items = await questions
            .OrderByDescending(q => q.CreatedAt)
            .ThenByDescending(q => q.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(q => new QuestionSummaryResponse(
                q.Id, q.Text, q.ThemeId, q.Theme.Name, q.Difficulty, q.Answers.Count, q.CreatedAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PagedResponse<QuestionSummaryResponse>(items, query.Page, query.PageSize, totalCount));
    }

    private static async Task<Results<Ok<QuestionResponse>, NotFound>> GetAsync(
        int id, QuizDbContext db, CancellationToken cancellationToken)
    {
        var question = await FindResponseAsync(db, id, cancellationToken);
        return question is null ? TypedResults.NotFound() : TypedResults.Ok(question);
    }

    private static async Task<Results<CreatedAtRoute<QuestionResponse>, ValidationProblem>> CreateAsync(
        SaveQuestionRequest request, QuizDbContext db, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        if (!await db.Themes.AnyAsync(t => t.Id == request.ThemeId, cancellationToken))
        {
            return UnknownTheme();
        }

        var question = new Question { Text = request.Text.Trim(), CreatedAt = timeProvider.GetUtcNow() };
        Apply(request, question);
        db.Questions.Add(question);
        await db.SaveChangesAsync(cancellationToken);

        var response = (await FindResponseAsync(db, question.Id, cancellationToken))!;
        return TypedResults.CreatedAtRoute(response, GetQuestionRoute, new { id = question.Id });
    }

    private static async Task<Results<Ok<QuestionResponse>, NotFound, ValidationProblem>> UpdateAsync(
        int id, SaveQuestionRequest request, QuizDbContext db, CancellationToken cancellationToken)
    {
        var question = await db.Questions
            .Include(q => q.Answers)
            .SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (question is null)
        {
            return TypedResults.NotFound();
        }

        if (!await db.Themes.AnyAsync(t => t.Id == request.ThemeId, cancellationToken))
        {
            return UnknownTheme();
        }

        // Les réponses sont remplacées en bloc : EF Core supprime celles retirées de la liste
        // (relation obligatoire, donc suppression en base) et insère les nouvelles.
        question.Answers.Clear();
        Apply(request, question);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok((await FindResponseAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        int id, QuizDbContext db, CancellationToken cancellationToken)
    {
        // Les réponses sont supprimées en cascade par la base.
        var deleted = await db.Questions.Where(q => q.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    private static void Apply(SaveQuestionRequest request, Question question)
    {
        question.Text = request.Text.Trim();
        question.ThemeId = request.ThemeId;
        question.Difficulty = request.Difficulty;
        question.Answers.AddRange(request.Answers.Select(a => new Answer { Text = a.Text.Trim(), IsCorrect = a.IsCorrect }));
    }

    private static Task<QuestionResponse?> FindResponseAsync(QuizDbContext db, int id, CancellationToken cancellationToken) =>
        db.Questions
            .Where(q => q.Id == id)
            .Select(q => new QuestionResponse(
                q.Id,
                q.Text,
                q.ThemeId,
                q.Theme.Name,
                q.Difficulty,
                q.CreatedAt,
                q.Answers.OrderBy(a => a.Id).Select(a => new AnswerResponse(a.Id, a.Text, a.IsCorrect)).ToList()))
            .SingleOrDefaultAsync(cancellationToken);

    private static string EscapeLikePattern(string value) =>
        value.Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter)
            .Replace("%", LikeEscapeCharacter + "%")
            .Replace("_", LikeEscapeCharacter + "_");

    private static ValidationProblem UnknownTheme() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(SaveQuestionRequest.ThemeId)] = ["Ce thème n'existe pas."],
        });
}
