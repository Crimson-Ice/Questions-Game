using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quiz.Api.Auth;
using Quiz.Api.Tests.Infrastructure;

namespace Quiz.Api.Tests.Auth;

[Collection(ApiCollection.Name)]
public class AuthEndpointsTests(QuizApiFactory factory)
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAsync()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = QuizApiFactory.AdminEmail, Password = QuizApiFactory.AdminPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.AccessToken));
        Assert.True(body.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_IgnoresEmailCaseAndSpacesAsync()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "  ADMIN@Quiz.Test ", Password = QuizApiFactory.AdminPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorizedAsync()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = QuizApiFactory.AdminEmail, Password = "wrong-password-123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsSameUnauthorizedAsWrongPasswordAsync()
    {
        using var client = factory.CreateClient();

        var unknownEmail = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "nobody@quiz.test", Password = QuizApiFactory.AdminPassword });
        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = QuizApiFactory.AdminEmail, Password = "wrong-password-123" });

        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        var unknownEmailProblem = await unknownEmail.Content.ReadFromJsonAsync<ProblemDetails>();
        var wrongPasswordProblem = await wrongPassword.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(wrongPasswordProblem?.Title, unknownEmailProblem?.Title);
    }

    [Fact]
    public async Task Login_WithInvalidBody_ReturnsValidationProblemAsync()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "not-an-email", Password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(nameof(LoginRequest.Email), problem.Errors.Keys);
        Assert.Contains(nameof(LoginRequest.Password), problem.Errors.Keys);
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorizedAsync()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithInvalidToken_ReturnsUnauthorizedAsync()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-token");

        var response = await client.GetAsync("/api/admin/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithToken_ReturnsCurrentAdminAsync()
    {
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = QuizApiFactory.AdminEmail, Password = QuizApiFactory.AdminPassword });
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var admin = await response.Content.ReadFromJsonAsync<CurrentAdminResponse>();
        Assert.Equal(QuizApiFactory.AdminEmail, admin?.Email);
    }

    [Fact]
    public async Task SeedAdmin_Twice_KeepsSingleAccountWithHashedPasswordAsync()
    {
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<AdminAccountSeeder>().SeedAsync();
        }

        await using var db = factory.CreateDbContext();
        var admins = await db.AdminUsers.Where(u => u.Email == QuizApiFactory.AdminEmail).ToListAsync();
        var admin = Assert.Single(admins);
        Assert.NotEqual(QuizApiFactory.AdminPassword, admin.PasswordHash);
    }
}
