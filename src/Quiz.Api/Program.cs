using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Quiz.Api.Auth;
using Quiz.Api.Import;
using Quiz.Api.Questions;
using Quiz.Api.Themes;
using Quiz.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<QuizDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Quiz")));

// Les énumérations (la difficulté) circulent en texte dans le JSON : "Easy" plutôt que 0.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Horloge partagée (jetons JWT, date de création des questions), remplaçable dans les tests.
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddAdminAuthentication(builder.Configuration);
builder.Services.AddScoped<QuestionBankImporter>();
builder.Services.AddScoped<QuestionBankSeeder>();
builder.Services.AddHealthChecks();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        var db = scope.ServiceProvider.GetRequiredService<QuizDbContext>();
        await db.Database.MigrateAsync();
    }

    await scope.ServiceProvider.GetRequiredService<AdminAccountSeeder>().SeedAsync();

    // Chemin relatif au dossier du projet (dotnet run) ou absolu (Docker).
    var seedDirectory = app.Configuration["Seed:Directory"];
    if (!string.IsNullOrWhiteSpace(seedDirectory))
    {
        var path = Path.GetFullPath(seedDirectory, app.Environment.ContentRootPath);
        await scope.ServiceProvider.GetRequiredService<QuestionBankSeeder>().SeedAsync(path);
    }
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapThemeEndpoints();
app.MapQuestionEndpoints();
app.MapImportEndpoints();

app.Run();
