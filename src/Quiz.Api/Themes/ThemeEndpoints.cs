using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quiz.Api.Validation;
using Quiz.Data;
using Quiz.Data.Entities;

namespace Quiz.Api.Themes;

/// <summary>CRUD des thèmes du back-office, réservé à l'administrateur.</summary>
public static class ThemeEndpoints
{
    private const string GetThemeRoute = "GetTheme";

    public static void MapThemeEndpoints(this WebApplication app)
    {
        var themes = app.MapGroup("/api/admin/themes").RequireAuthorization();

        themes.MapGet("/", ListAsync);
        themes.MapGet("/{id:int}", GetAsync).WithName(GetThemeRoute);
        themes.MapPost("/", CreateAsync).AddEndpointFilter<ValidationFilter<SaveThemeRequest>>();
        themes.MapPut("/{id:int}", UpdateAsync).AddEndpointFilter<ValidationFilter<SaveThemeRequest>>();
        themes.MapDelete("/{id:int}", DeleteAsync);
    }


    private static async Task<Ok<List<ThemeResponse>>> ListAsync(QuizDbContext db, CancellationToken cancellationToken)
    {
        var themes = await db.Themes
            .OrderBy(t => t.Name)
            .Select(t => new ThemeResponse(t.Id, t.Name, t.Icon, t.Questions.Count))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(themes);
    }

    private static async Task<Results<Ok<ThemeResponse>, NotFound>> GetAsync(
        int id, QuizDbContext db, CancellationToken cancellationToken)
    {
        var theme = await FindResponseAsync(db, id, cancellationToken);
        return theme is null ? TypedResults.NotFound() : TypedResults.Ok(theme);
    }

    private static async Task<Results<CreatedAtRoute<ThemeResponse>, ProblemHttpResult>> CreateAsync(
        SaveThemeRequest request, QuizDbContext db, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await IsNameTakenAsync(db, name, excludedId: null, cancellationToken))
        {
            return NameTaken(name);
        }

        var theme = new Theme { Name = name, Icon = NormalizeIcon(request.Icon) };
        db.Themes.Add(theme);
        if (!await TrySaveAsync(db, cancellationToken))
        {
            return NameTaken(name);
        }

        var response = new ThemeResponse(theme.Id, theme.Name, theme.Icon, QuestionCount: 0);
        return TypedResults.CreatedAtRoute(response, GetThemeRoute, new { id = theme.Id });
    }

    private static async Task<Results<Ok<ThemeResponse>, NotFound, ProblemHttpResult>> UpdateAsync(
        int id, SaveThemeRequest request, QuizDbContext db, CancellationToken cancellationToken)
    {
        var theme = await db.Themes.FindAsync([id], cancellationToken);
        if (theme is null)
        {
            return TypedResults.NotFound();
        }

        var name = request.Name.Trim();
        if (await IsNameTakenAsync(db, name, excludedId: id, cancellationToken))
        {
            return NameTaken(name);
        }

        theme.Name = name;
        theme.Icon = NormalizeIcon(request.Icon);
        if (!await TrySaveAsync(db, cancellationToken))
        {
            return NameTaken(name);
        }

        return TypedResults.Ok((await FindResponseAsync(db, id, cancellationToken))!);
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeleteAsync(
        int id, QuizDbContext db, CancellationToken cancellationToken)
    {
        var theme = await db.Themes.FindAsync([id], cancellationToken);
        if (theme is null)
        {
            return TypedResults.NotFound();
        }

        // Règle du cadrage : un thème qui contient des questions ne peut pas être supprimé.
        if (await db.Questions.AnyAsync(q => q.ThemeId == id, cancellationToken))
        {
            return TypedResults.Problem(
                title: "Ce thème contient des questions.",
                detail: "Supprimez ou déplacez ses questions avant de supprimer le thème.",
                statusCode: StatusCodes.Status409Conflict);
        }

        db.Themes.Remove(theme);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static Task<ThemeResponse?> FindResponseAsync(QuizDbContext db, int id, CancellationToken cancellationToken) =>
        db.Themes
            .Where(t => t.Id == id)
            .Select(t => new ThemeResponse(t.Id, t.Name, t.Icon, t.Questions.Count))
            .SingleOrDefaultAsync(cancellationToken);

    /// <summary>Deux thèmes ne peuvent pas porter le même nom, sans tenir compte de la casse.</summary>
    private static Task<bool> IsNameTakenAsync(
        QuizDbContext db, string name, int? excludedId, CancellationToken cancellationToken)
    {
        var lowerName = name.ToLower();
        return db.Themes.AnyAsync(t => t.Name.ToLower() == lowerName && t.Id != excludedId, cancellationToken);
    }

    /// <summary>
    /// Enregistre, et renvoie false si l'index unique sur le nom a refusé l'écriture
    /// (cas de deux requêtes simultanées avec le même nom).
    /// </summary>
    private static async Task<bool> TrySaveAsync(QuizDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;
        }
    }

    private static string? NormalizeIcon(string? icon) => string.IsNullOrWhiteSpace(icon) ? null : icon.Trim();

    private static ProblemHttpResult NameTaken(string name) =>
        TypedResults.Problem(
            title: "Ce nom de thème est déjà utilisé.",
            detail: $"Un thème nommé « {name} » existe déjà.",
            statusCode: StatusCodes.Status409Conflict);
}
