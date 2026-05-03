using Sts.Api.Models;
using Sts.Api.Services;
using Sts.Domain.DataSource;

namespace Sts.Api.Endpoints;

/// <summary>
/// Endpoints CRUD pour les compétences STS.
/// Tous les endpoints nécessitent une authentification JWT.
/// </summary>
public static class AbilityEndpoints
{
    public static void MapAbilityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/abilities")
            .WithTags("Abilities")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllAbilities")
            .WithSummary("Retourne la liste de toutes les compétences.")
            .Produces<List<AbilityData>>();

        group.MapGet("/{id}", GetById)
            .WithName("GetAbility")
            .WithSummary("Retourne une compétence par son identifiant.")
            .Produces<AbilityData>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateAbility")
            .WithSummary("Crée une nouvelle compétence.")
            .Produces<AbilityData>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id}", Update)
            .WithName("UpdateAbility")
            .WithSummary("Met à jour une compétence existante.")
            .Produces<AbilityData>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", Delete)
            .WithName("DeleteAbility")
            .WithSummary("Supprime une compétence.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static IResult GetAll(DataService dataService)
        => Results.Ok(dataService.GetAbilities());

    private static IResult GetById(string id, DataService dataService)
    {
        var ability = dataService.GetAbility(id);
        return ability is null
            ? Results.NotFound($"Compétence '{id}' introuvable.")
            : Results.Ok(ability);
    }

    private static IResult Create(AbilityData ability, DataService dataService)
    {
        if (string.IsNullOrWhiteSpace(ability.Id))
            return Results.BadRequest("L'identifiant de la compétence est requis.");

        if (string.IsNullOrWhiteSpace(ability.Name))
            return Results.BadRequest("Le nom de la compétence est requis.");

        if (string.IsNullOrWhiteSpace(ability.Category))
            return Results.BadRequest("La catégorie de la compétence est requise.");

        if (ability.Levels is null || ability.Levels.Count == 0)
            return Results.BadRequest("La compétence doit avoir au moins un niveau.");

        var added = dataService.AddAbility(ability);
        return added
            ? Results.Created($"/api/abilities/{ability.Id}", ability)
            : Results.Conflict($"Une compétence avec l'identifiant '{ability.Id}' existe déjà.");
    }

    private static IResult Update(string id, AbilityData updated, DataService dataService)
    {
        if (string.IsNullOrWhiteSpace(updated.Name))
            return Results.BadRequest("Le nom de la compétence est requis.");

        if (string.IsNullOrWhiteSpace(updated.Category))
            return Results.BadRequest("La catégorie de la compétence est requise.");

        if (updated.Levels is null || updated.Levels.Count == 0)
            return Results.BadRequest("La compétence doit avoir au moins un niveau.");

        var ok = dataService.UpdateAbility(id, updated);
        return ok
            ? Results.Ok(dataService.GetAbility(id))
            : Results.NotFound($"Compétence '{id}' introuvable.");
    }

    private static IResult Delete(string id, DataService dataService)
    {
        var ok = dataService.DeleteAbility(id);
        return ok
            ? Results.NoContent()
            : Results.NotFound($"Compétence '{id}' introuvable.");
    }
}
