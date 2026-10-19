using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Quiz.Api.Validation;
using Quiz.Data;
using Quiz.Data.Entities;

namespace Quiz.Api.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", LoginAsync)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

        app.MapGet("/api/admin/me", GetCurrentAdmin)
            .RequireAuthorization();
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> LoginAsync(
        LoginRequest request,
        QuizDbContext db,
        IPasswordHasher<AdminUser> passwordHasher,
        AccessTokenIssuer tokenIssuer,
        CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        var admin = await db.AdminUsers.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Même réponse que l'e-mail soit inconnu ou le mot de passe faux :
        // on ne révèle pas quelles adresses ont un compte.
        var verification = admin is null
            ? PasswordVerificationResult.Failed
            : passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password);

        if (admin is null || verification == PasswordVerificationResult.Failed)
        {
            return TypedResults.Problem(
                title: "Identifiants invalides.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            admin.PasswordHash = passwordHasher.HashPassword(admin, request.Password);
            await db.SaveChangesAsync(cancellationToken);
        }

        var token = tokenIssuer.Issue(admin);
        return TypedResults.Ok(new LoginResponse(token.Value, token.ExpiresAt));
    }

    private static Ok<CurrentAdminResponse> GetCurrentAdmin(ClaimsPrincipal user) =>
        TypedResults.Ok(new CurrentAdminResponse(user.FindFirstValue(JwtRegisteredClaimNames.Email) ?? ""));
}
