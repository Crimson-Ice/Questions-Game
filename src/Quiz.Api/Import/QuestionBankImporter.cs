using Microsoft.EntityFrameworkCore;
using Quiz.Data;
using Quiz.Data.Entities;

namespace Quiz.Api.Import;

/// <summary>
/// Importe une banque de questions déjà validée. Partagé par l'endpoint d'import et, plus tard,
/// par le chargement de la banque initiale au démarrage.
/// </summary>
public sealed class QuestionBankImporter(QuizDbContext db, TimeProvider timeProvider)
{
    public async Task<ImportResponse> ImportAsync(ImportRequest request, CancellationToken cancellationToken)
    {
        var themes = await LoadExistingThemesAsync(request, cancellationToken);
        var existingTexts = await LoadExistingQuestionTextsAsync(themes.Values, cancellationToken);
        var now = timeProvider.GetUtcNow();
        int themesCreated = 0, questionsImported = 0, questionsSkipped = 0;

        foreach (var themeRequest in request.Themes)
        {
            var name = themeRequest.Name.Trim();
            if (!themes.TryGetValue(name, out var theme))
            {
                theme = new Theme { Name = name, Icon = string.IsNullOrWhiteSpace(themeRequest.Icon) ? null : themeRequest.Icon.Trim() };
                db.Themes.Add(theme);
                themes[name] = theme;
                themesCreated++;
            }

            // Un thème qui vient d'être créé n'a pas encore d'Id (0) ni de questions en base.
            var texts = existingTexts.TryGetValue(theme.Id, out var known)
                ? known
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var questionRequest in themeRequest.Questions)
            {
                var text = questionRequest.Text.Trim();

                // Une question dont l'énoncé existe déjà dans ce thème (en base ou plus haut dans
                // le fichier) est ignorée : réimporter un fichier ne crée pas de doublons.
                if (!texts.Add(text))
                {
                    questionsSkipped++;
                    continue;
                }

                theme.Questions.Add(new Question
                {
                    Text = text,
                    Difficulty = questionRequest.Difficulty,
                    CreatedAt = now,
                    Answers = questionRequest.Answers
                        .Select(a => new Answer { Text = a.Text.Trim(), IsCorrect = a.IsCorrect })
                        .ToList(),
                });
                questionsImported++;
            }
        }

        // Un seul SaveChanges : EF Core l'exécute dans une transaction, donc tout ou rien.
        await db.SaveChangesAsync(cancellationToken);
        return new ImportResponse(themesCreated, questionsImported, questionsSkipped);
    }

    /// <summary>Les thèmes du fichier qui existent déjà, indexés par nom sans tenir compte de la casse.</summary>
    private async Task<Dictionary<string, Theme>> LoadExistingThemesAsync(
        ImportRequest request, CancellationToken cancellationToken)
    {
        var lowerNames = request.Themes.Select(t => t.Name.Trim().ToLower()).ToList();
        var existing = await db.Themes
            .Where(t => lowerNames.Contains(t.Name.ToLower()))
            .ToListAsync(cancellationToken);

        return existing.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<int, HashSet<string>>> LoadExistingQuestionTextsAsync(
        IEnumerable<Theme> themes, CancellationToken cancellationToken)
    {
        var themeIds = themes.Select(t => t.Id).ToList();
        var questions = await db.Questions
            .Where(q => themeIds.Contains(q.ThemeId))
            .Select(q => new { q.ThemeId, q.Text })
            .ToListAsync(cancellationToken);

        return questions
            .GroupBy(q => q.ThemeId)
            .ToDictionary(g => g.Key, g => g.Select(q => q.Text).ToHashSet(StringComparer.OrdinalIgnoreCase));
    }
}
