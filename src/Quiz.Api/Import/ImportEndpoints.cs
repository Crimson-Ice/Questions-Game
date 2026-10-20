using Microsoft.AspNetCore.Http.HttpResults;
using Quiz.Api.Validation;

namespace Quiz.Api.Import;

/// <summary>Import JSON de la banque de questions, réservé à l'administrateur.</summary>
public static class ImportEndpoints
{
    public static void MapImportEndpoints(this WebApplication app)
    {
        app.MapPost("/api/admin/import", ImportAsync)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<ImportRequest>>();
    }

    private static async Task<Ok<ImportResponse>> ImportAsync(
        ImportRequest request, QuestionBankImporter importer, CancellationToken cancellationToken) =>
        TypedResults.Ok(await importer.ImportAsync(request, cancellationToken));
}
