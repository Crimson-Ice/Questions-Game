using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quiz.Data;

namespace Quiz.Api.Import;

/// <summary>
/// Charge la banque initiale (<c>seed/*.json</c>) au démarrage, uniquement si la base ne contient
/// encore aucune question. Utilise le même validateur et le même import que l'endpoint.
/// </summary>
public sealed class QuestionBankSeeder(
    QuizDbContext db,
    QuestionBankImporter importer,
    IValidator<ImportRequest> validator,
    IOptions<JsonOptions> jsonOptions,
    ILogger<QuestionBankSeeder> logger)
{
    public async Task SeedAsync(string directory, CancellationToken cancellationToken = default)
    {
        if (await db.Questions.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Banque initiale non chargée : la base contient déjà des questions.");
            return;
        }

        if (!Directory.Exists(directory))
        {
            throw new InvalidOperationException($"Dossier de la banque initiale introuvable : {directory}");
        }

        var request = await ReadFilesAsync(directory, cancellationToken);

        // Un fichier invalide arrête le démarrage : mieux vaut le voir tout de suite qu'importer à moitié.
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = string.Join(Environment.NewLine, validation.Errors.Select(e => $"{e.PropertyName} : {e.ErrorMessage}"));
            throw new InvalidOperationException($"Banque initiale invalide ({directory}) :{Environment.NewLine}{errors}");
        }

        var result = await importer.ImportAsync(request, cancellationToken);
        logger.LogInformation(
            "Banque initiale chargée : {ThemesCreated} thèmes, {QuestionsImported} questions.",
            result.ThemesCreated, result.QuestionsImported);
    }

    /// <summary>Réunit les thèmes de tous les fichiers, lus par ordre alphabétique, en un seul import.</summary>
    private async Task<ImportRequest> ReadFilesAsync(string directory, CancellationToken cancellationToken)
    {
        var themes = new List<ImportThemeRequest>();
        foreach (var path in Directory.GetFiles(directory, "*.json").Order(StringComparer.Ordinal))
        {
            await using var stream = File.OpenRead(path);
            var file = await JsonSerializer.DeserializeAsync<ImportRequest>(
                stream, jsonOptions.Value.SerializerOptions, cancellationToken)
                ?? throw new InvalidOperationException($"Fichier de banque vide : {path}");
            themes.AddRange(file.Themes);
        }

        return new ImportRequest { Themes = themes };
    }
}
