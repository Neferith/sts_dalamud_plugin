using Sts.Domain.DataSource;
using Sts.Api.Services;

namespace Sts.Api.Endpoints;

/// <summary>
/// Endpoints CRUD pour les actions de jet STS.
/// Tous les endpoints nécessitent une authentification JWT.
/// </summary>
public static class ActionEndpoints
{
    public static void MapActionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/actions")
            .WithTags("Actions")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllActions")
            .WithSummary("Retourne la liste de toutes les actions.")
            .Produces<List<ActionData>>();

        group.MapGet("/{id}", GetById)
            .WithName("GetAction")
            .WithSummary("Retourne une action par son identifiant.")
            .Produces<ActionData>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateAction")
            .WithSummary("Crée une nouvelle action.")
            .Produces<ActionData>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id}", Update)
            .WithName("UpdateAction")
            .WithSummary("Met à jour une action existante.")
            .Produces<ActionData>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", Delete)
            .WithName("DeleteAction")
            .WithSummary("Supprime une action.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static IResult GetAll(DataService dataService)
        => Results.Ok(dataService.GetActions());

    private static IResult GetById(string id, DataService dataService)
    {
        var action = dataService.GetAction(id);
        return action is null
            ? Results.NotFound($"Action '{id}' introuvable.")
            : Results.Ok(action);
    }

    private static IResult Create(ActionData action, DataService dataService)
    {
        if (string.IsNullOrWhiteSpace(action.Id))
            return Results.BadRequest("L'identifiant de l'action est requis.");

        if (string.IsNullOrWhiteSpace(action.Name))
            return Results.BadRequest("Le nom de l'action est requis.");

        var added = dataService.AddAction(action);
        return added
            ? Results.Created($"/api/actions/{action.Id}", action)
            : Results.Conflict($"Une action avec l'identifiant '{action.Id}' existe déjà.");
    }

    private static IResult Update(string id, ActionData updated, DataService dataService)
    {
        if (string.IsNullOrWhiteSpace(updated.Name))
            return Results.BadRequest("Le nom de l'action est requis.");

        var ok = dataService.UpdateAction(id, updated);
        return ok
            ? Results.Ok(dataService.GetAction(id))
            : Results.NotFound($"Action '{id}' introuvable.");
    }

    private static IResult Delete(string id, DataService dataService)
    {
        var ok = dataService.DeleteAction(id);
        return ok
            ? Results.NoContent()
            : Results.NotFound($"Action '{id}' introuvable.");
    }
}
