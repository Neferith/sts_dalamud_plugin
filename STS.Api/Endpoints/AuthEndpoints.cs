using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Sts.Domain.User;

namespace Sts.Api.Endpoints;

/// <summary>Corps de la requête de connexion.</summary>
public record LoginRequest(string Username, string Password);

/// <summary>
/// Endpoint d'authentification unifié.
/// Retourne un JWT avec <c>role=admin</c> ou <c>role=member</c> selon l'utilisateur.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>Enregistre <c>POST /api/auth/login</c>.</summary>
    public static void MapAuthEndpoints(this WebApplication app, IConfiguration configuration)
    {
        app.MapPost("/api/auth/login", async (
            [FromBody] LoginRequest request,
            IAuthenticateUserUseCase authenticate) =>
        {
            var user = await authenticate.ExecuteAsync(request.Username, request.Password);
            if (user is null)
                return Results.Unauthorized();

            var token = GenerateToken(user, configuration);
            return Results.Ok(new { token });
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .WithName("Login")
        .WithSummary("Authentifie un utilisateur et retourne un JWT.");
    }

    // ── JWT ───────────────────────────────────────────────────────────────────

    private static string GenerateToken(User user, IConfiguration configuration)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(
            double.TryParse(configuration["Jwt:ExpirationHours"], out var h) ? h : 8);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name,           user.Username),
            new Claim(ClaimTypes.Role,           user.Role.ToString().ToLowerInvariant()),
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
