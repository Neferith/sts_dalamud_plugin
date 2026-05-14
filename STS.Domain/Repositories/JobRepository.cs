using System.Collections.Generic;
using System.Linq;
using Sts.Domain.DataSource;
using Sts.Domain;

namespace Sts.Domain.Repository;

/// <summary>
/// Contrat d'accès aux jobs du système.
/// </summary>
public interface JobRepository
{
    /// <summary>Retourne tous les jobs disponibles.</summary>
    IReadOnlyList<Job> GetAll();

    /// <summary>Retourne un job par son identifiant, ou null s'il n'existe pas.</summary>
    Job? GetById(string id);
}

/// <summary>
/// Implémentation par défaut de <see cref="JobRepository"/>.
/// Charge les données depuis la source et les conserve en cache mémoire.
/// </summary>
public class DefaultJobRepository : JobRepository
{
    private readonly IReadOnlyDictionary<string, Job> _cache;

    public DefaultJobRepository(IDataSource dataSource)
    {
        var data = dataSource.Load();
        _cache = data.Jobs
            .Select(j => new Job(j.Id, j.Name, j.Description, j.IconUrl))
            .ToDictionary(j => j.Id);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Job> GetAll()
        => [.. _cache.Values];

    /// <inheritdoc/>
    public Job? GetById(string id)
        => _cache.TryGetValue(id, out var job) ? job : null;
}
