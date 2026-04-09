using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Sts.Api.Endpoints;

/// <summary>
/// Endpoint d'authentification.
/// Retourne un token JWT à partir des credentials admin.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Authentifie l'administrateur et retourne un token JWT.")
            .AllowAnonymous()
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static IResult Login(LoginRequest request, IConfiguration configuration)
    {
        var expectedUsername = configuration["Admin:Username"] ?? string.Empty;
        var expectedPassword = configuration["Admin:Password"] ?? string.Empty;

        // Comparaison en temps constant pour éviter les timing attacks
        var usernameOk = CryptographicEquals(request.Username, expectedUsername);
        var passwordOk = CryptographicEquals(request.Password, expectedPassword);

        if (!usernameOk || !passwordOk)
            return Results.Unauthorized();

        var token = GenerateToken(configuration);
        return Results.Ok(new LoginResponse(token));
    }

    private static string GenerateToken(IConfiguration configuration)
    {
        var secret       = configuration["Jwt:Secret"]       ?? throw new InvalidOperationException("Jwt:Secret manquant.");
        var issuer       = configuration["Jwt:Issuer"]       ?? "sts-api";
        var audience     = configuration["Jwt:Audience"]     ?? "sts-admin";
        var expirationHours = int.TryParse(configuration["Jwt:ExpirationHours"], out var h) ? h : 8;

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "Admin"),
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Comparaison en temps constant pour résister aux timing attacks.</summary>
    private static bool CryptographicEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}

/// <summary>Corps de la requête de login.</summary>
public record LoginRequest(string Username, string Password);

/// <summary>Réponse contenant le token JWT.</summary>
public record LoginResponse(string Token);
