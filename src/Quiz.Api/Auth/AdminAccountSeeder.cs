using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quiz.Data;
using Quiz.Data.Entities;

namespace Quiz.Api.Auth;

/// <summary>
/// Crée le compte administrateur décrit dans la configuration s'il n'existe pas encore.
/// Ne modifie jamais un compte existant.
/// </summary>
public sealed class AdminAccountSeeder(
    QuizDbContext db,
    IPasswordHasher<AdminUser> passwordHasher,
    IOptions<AdminAccountOptions> options,
    ILogger<AdminAccountSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var account = options.Value;
        if (string.IsNullOrWhiteSpace(account.Email))
        {
            logger.LogInformation("Aucun compte administrateur configuré (Admin:Email vide).");
            return;
        }

        if (string.IsNullOrEmpty(account.Password) || account.Password.Length < AdminAccountOptions.MinimumPasswordLength)
        {
            throw new InvalidOperationException(
                $"Admin:Password doit contenir au moins {AdminAccountOptions.MinimumPasswordLength} caractères.");
        }

        var email = EmailNormalizer.Normalize(account.Email);
        if (await db.AdminUsers.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return;
        }

        var admin = new AdminUser { Email = email, PasswordHash = "" };
        admin.PasswordHash = passwordHasher.HashPassword(admin, account.Password);
        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Compte administrateur {Email} créé.", email);
    }
}
