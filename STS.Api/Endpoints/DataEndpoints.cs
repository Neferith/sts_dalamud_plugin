using Microsoft.AspNetCore.Mvc;
using Sts.Api.Services;

namespace Sts.Api.Endpoints;

/// <summary>
/// Endpoints relatifs aux données de référence STS (jobs, traits, abilities, actions).
/// </summary>
public static class DataEndpoints
{
    public static void MapDataEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/data")
            .WithTags("Data");

        /// <summary>
        /// Retourne l'intégralité du data.json — jobs, traits, abilities, actions.
        /// Le plugin l'utilise au démarrage à la place du fichier local.
        /// </summary>
        group.MapGet("/", GetData)
            .WithName("GetData")
            .WithSummary("Retourne les données de référence STS complètes.")
            .Produces<string>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status404NotFound);
    }

    private static IResult GetData(DataService dataService)
    {
        var json = dataService.GetRawJson();

        if (json == "{}")
            return Results.NotFound("Le fichier data.json est introuvable ou vide.");

        return Results.Content(json, "application/json");
    }
}
