using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Quiz.Data;
using Testcontainers.PostgreSql;

namespace Quiz.Api.Tests.Infrastructure;

/// <summary>
/// Démarre l'API en mémoire, branchée sur un PostgreSQL jetable lancé dans Docker.
/// Partagée par toutes les classes de la collection <see cref="ApiCollection"/>.
/// </summary>
public sealed class QuizApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@quiz.test";

    public const string AdminPassword = "test-password-1234";

    private readonly PostgreSqlContainer _database =
        new PostgreSqlBuilder("postgres:18-alpine").Build();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // Accéder à Services démarre l'API, qui applique alors les migrations.
        _ = Services;
    }

    /// <summary>
    /// Crée un DbContext indépendant de l'API, pour préparer ou vérifier des données en base.
    /// </summary>
    public QuizDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<QuizDbContext>()
            .UseNpgsql(_database.GetConnectionString())
            .Options;

        return new QuizDbContext(options);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Quiz", _database.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Jwt:SigningKey", "test-signing-key-with-at-least-32-characters");
        builder.UseSetting("Admin:Email", AdminEmail);
        builder.UseSetting("Admin:Password", AdminPassword);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}
