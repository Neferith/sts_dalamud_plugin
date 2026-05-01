using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Sts.Domain.User;

namespace Sts.Api.Endpoints;

// ── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>Corps de la requête de création d'un utilisateur.</summary>
public record CreateUserRequest(string Username, string Password, UserRole Role = UserRole.Member);

/// <summary>Corps de la requête de reset du code d'accès.</summary>
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
    /// <summary>Enregistre les endpoints <c>/users</c> sur le <paramref name="app"/> fourni.</summary>
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").RequireAuthorization("admin");

        // GET /users
        group.MapGet("/", async (IGetAllUsersUseCase getAll) =>
        {
            var users = await getAll.ExecuteAsync();
            return Results.Ok(users.Select(ToDto));
        })
        .WithTags("Users");

        // POST /users
        group.MapPost("/", async (
            [FromBody] CreateUserRequest request,
            ICreateUserUseCase create) =>
        {
            var user = await create.ExecuteAsync(request.Username, request.Password, request.Role);
            return user is null
                ? Results.Conflict(new { error = "Ce nom d'utilisateur est déjà pris." })
                : Results.Created($"/users/{user.Id}", ToDto(user));
        })
        .WithTags("Users");

        // PUT /users/{id}/password
        group.MapPut("/{id:guid}/password", async (
            Guid id,
            [FromBody] ResetUserPasswordRequest request,
            IUpdateUserCodeUseCase updateCode) =>
        {
            await updateCode.ExecuteAsync(id, request.NewPassword);
            return Results.NoContent();
        })
        .WithTags("Users");

        // DELETE /users/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IDeleteUserUseCase delete) =>
        {
            await delete.ExecuteAsync(id);
            return Results.NoContent();
        })
        .WithTags("Users");

        return app;
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Username, u.Role, u.CreatedAt);
}
