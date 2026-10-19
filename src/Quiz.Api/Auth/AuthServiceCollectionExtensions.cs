using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Quiz.Data.Entities;

namespace Quiz.Api.Auth;

public static class AuthServiceCollectionExtensions
{
    /// <summary>Authentification JWT des administrateurs, hachage des mots de passe et création du compte admin.</summary>
    public static IServiceCollection AddAdminAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AdminAccountOptions>()
            .Bind(configuration.GetSection(AdminAccountOptions.SectionName));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                // Garde les noms de claims du jeton (« sub », « email ») tels quels.
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new()
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = AccessTokenIssuer.CreateSigningKey(jwt.Value.SigningKey),
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });
        services.AddAuthorization();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AccessTokenIssuer>();
        services.AddSingleton<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
        services.AddScoped<AdminAccountSeeder>();

        return services;
    }
}
