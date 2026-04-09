using Sts.Api.Services;

namespace Sts.Api.Endpoints;

public static class RulesEndpoints
{
    public static void MapRulesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rules").WithTags("Rules");

        /// <summary>
        /// Retourne la liste complète des sections de règles avec leurs articles (contenu Markdown inclus).
        /// Endpoint public — aucune authentification requise.
        /// </summary>
        group.MapGet("/", (RulesService rules) => Results.Ok(rules.GetAll()))
             .WithName("GetRules")
             .AllowAnonymous()
             .Produces(StatusCodes.Status200OK);
    }
}
