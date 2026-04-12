using Sts.Api.Services;

namespace Sts.Api.Endpoints;

public static class DataEndpoints
{
    public static void MapDataEndpoints(this WebApplication app)
    {
        app.MapGet("/api/data", (HttpContext ctx) =>
        {
            var dataService = ctx.RequestServices.GetRequiredService<DataService>();
            var json = dataService.GetRawJson();

            if (json == "{}")
                return Results.NotFound("Le fichier data.json est introuvable ou vide.");

            return Results.Content(json, "application/json");
        });
    }
}
