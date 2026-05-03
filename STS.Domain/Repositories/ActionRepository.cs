using System.Collections.Generic;
using System.Linq;
using Sts.Domain.DataSource;
using Sts.Domain;

namespace Sts.Domain.Repository;

/// <summary>
/// Contrat d'accès aux actions de jet prédéfinies.
/// </summary>
public interface ActionRepository
{
    /// <summary>Retourne toutes les actions prédéfinies.</summary>
    IReadOnlyList<RollAction> GetAll();

    /// <summary>Retourne une action prédéfinie par son identifiant, ou null.</summary>
    RollAction? GetById(string id);
}

/// <summary>
/// Implémentation par défaut de <see cref="ActionRepository"/>.
/// Charge les actions prédéfinies depuis data.json et les conserve en cache mémoire.
/// </summary>
public class DefaultActionRepository : ActionRepository
{
    private readonly IReadOnlyList<RollAction> _cache;

    public DefaultActionRepository(IDataSource dataSource)
    {
        var data = dataSource.Load();
        _cache = data.Actions
            .Select(a => new RollAction
            {
                Id = a.Id,
                Name = a.Name,
                Contexts = a.Contexts,
                Requirements = a.Requirements.Select(ParseRequirement).ToList(),
                IsPredefined = true,
            })
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<RollAction> GetAll() => _cache;

    /// <inheritdoc/>
    public RollAction? GetById(string id)
        => _cache.FirstOrDefault(a => a.Id == id);

    private static ActionRequirementType ParseRequirement(string value) => value switch
    {
        "Weapon" => ActionRequirementType.Weapon,
        _ => ActionRequirementType.Weapon,
    };
}
