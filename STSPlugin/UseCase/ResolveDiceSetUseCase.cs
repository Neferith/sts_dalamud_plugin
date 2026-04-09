using System.Linq;
using Sts.Domain;

namespace STSPlugin.UseCases;

/// <summary>
/// Résultat de l'évaluation d'un set de dés contre un palier.
/// </summary>
/// <param name="Hits">Tableau de 3 booléens indiquant si chaque dé est un succès.</param>
/// <param name="Successes">Nombre total de dés ayant atteint ou dépassé le palier.</param>
public record DiceResolution(bool[] Hits, int Successes);

/// <summary>
/// Cas d'usage : évaluer un set de 3 dés contre un palier effectif.
/// Retourne les succès individuels et le total.
/// </summary>
public interface ResolveDiceSetUseCase
{
    /// <summary>
    /// Évalue chaque dé du set contre le palier fourni.
    /// Un dé est un succès s'il est supérieur ou égal au palier.
    /// </summary>
    /// <param name="diceSet">Le set de 3 dés à évaluer.</param>
    /// <param name="palier">Le palier effectif (après modificateur MJ).</param>
    /// <returns>Le détail des succès et leur total.</returns>
    DiceResolution Execute(DiceSet diceSet, int palier);
}

/// <summary>
/// Implémentation par défaut de <see cref="ResolveDiceSetUseCase"/>.
/// </summary>
public class DefaultResolveDiceSetUseCase : ResolveDiceSetUseCase
{
    /// <inheritdoc/>
    public DiceResolution Execute(DiceSet diceSet, int palier)
    {
        var hits = diceSet.Values.Select(d => d >= palier).ToArray();
        return new DiceResolution(hits, hits.Count(h => h));
    }
}
