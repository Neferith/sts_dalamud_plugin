using Sts.Api.Services;

namespace Sts.Api.Endpoints;

/// <summary>
/// Endpoint public de données brutes — consommé par le plugin Dalamud au démarrage.
/// </summary>
public static class DataEndpoints
{
    public static void MapDataEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/data")
            .WithTags("Data");

        group.MapGet("/", GetData)
            .WithName("GetData")
            .WithSummary("Retourne les données de référence STS complètes (plugin).")
            .AllowAnonymous()
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
