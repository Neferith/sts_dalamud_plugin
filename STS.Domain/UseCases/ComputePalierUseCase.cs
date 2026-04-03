using System;

namespace Sts.Domain.UseCases;

/// <summary>
/// Calcule le palier effectif d'un rang après application du modificateur MJ.
/// Un modificateur positif facilite le jet en abaissant le palier.
/// Un modificateur négatif le durcit en le remontant.
/// Le palier effectif est toujours compris entre 1 et 10.
/// </summary>
public interface IComputePalierUseCase
{
    /// <summary>
    /// Calcule le palier effectif.
    /// </summary>
    /// <param name="rank">Le rang du personnage définissant le palier de base.</param>
    /// <param name="modifier">
    /// Le modificateur MJ appliqué au palier de base.
    /// Positif = facilite (palier plus bas), négatif = durcit (palier plus haut).
    /// </param>
    /// <returns>Le palier effectif, compris entre 1 et 10 inclus.</returns>
    int Execute(Rank rank, int modifier);
}

/// <summary>
/// Implémentation par défaut de <see cref="IComputePalierUseCase"/>.
/// </summary>
public class DefaultComputePalierUseCase : IComputePalierUseCase
{
    /// <inheritdoc/>
    public int Execute(Rank rank, int modifier)
        => Math.Clamp(rank.Palier - modifier, 1, 10);
}
