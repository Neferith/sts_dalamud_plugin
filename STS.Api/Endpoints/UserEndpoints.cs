using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sts.Domain.User;

namespace Sts.Api.Endpoints;

// ── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>Corps de la requête de création d'un utilisateur.</summary>
public record CreateUserRequest(string Username, string Password, UserRole Role = UserRole.Member);

/// <summary>Corps de la requête de reset du mot de passe.</summary>
public record ResetUserPasswordRequest(string NewPassword);

/// <summary>Vue publique d'un utilisateur (sans hash).</summary>
public record UserDto(Guid Id, string Username, UserRole Role, DateTime CreatedAt);

// ── Endpoints ─────────────────────────────────────────────────────────────────

/// <summary>
/// Endpoints de gestion des comptes utilisateurs.
/// Toutes les routes requièrent le rôle <c>admin</c>.
/// </summary>
public static class UserEndpoints
{
    /// <summary>Enregistre les endpoints <c>/api/users</c>.</summary>
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization("admin");

        // GET /api/users
        group.MapGet("/", async (IGetAllUsersUseCase getAll) =>
        {
            var users = await getAll.ExecuteAsync();
            return Results.Ok(users.Select(ToDto));
        })
        .WithName("GetAllUsers")
        .WithSummary("Retourne la liste de tous les utilisateurs.");

        // POST /api/users
        group.MapPost("/", async (
            [FromBody] CreateUserRequest request,
            ICreateUserUseCase create) =>
        {
            var user = await create.ExecuteAsync(request.Username, request.Password, request.Role);
            return user is null
                ? Results.Conflict(new { error = "Ce nom d'utilisateur est déjà pris." })
                : Results.Created($"/api/users/{user.Id}", ToDto(user));
        })
        .WithName("CreateUser")
        .WithSummary("Crée un nouveau compte utilisateur.");

        // PUT /api/users/{id}/password
        group.MapPut("/{id:guid}/password", async (
            Guid id,
            [FromBody] ResetUserPasswordRequest request,
            IUpdateUserCodeUseCase updateCode) =>
        {
            await updateCode.ExecuteAsync(id, request.NewPassword);
            return Results.NoContent();
        })
        .WithName("ResetUserPassword")
        .WithSummary("Réinitialise le mot de passe d'un utilisateur.");

        // DELETE /api/users/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IDeleteUserUseCase delete) =>
        {
            await delete.ExecuteAsync(id);
            return Results.NoContent();
        })
        .WithName("DeleteUser")
        .WithSummary("Supprime un compte utilisateur.");
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Username, u.Role, u.CreatedAt);
}
