using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Quiz.Api.Auth;
using Quiz.Api.Themes;
using Quiz.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<QuizDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Quiz")));

builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddAdminAuthentication(builder.Configuration);
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
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapThemeEndpoints();

app.Run();
