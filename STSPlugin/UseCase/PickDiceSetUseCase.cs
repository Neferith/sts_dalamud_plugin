using System.Linq;
using Sts.Domain;

namespace STSPlugin.UseCases;

/// <summary>
/// Détermine si on retient le meilleur ou le pire set lors d'un jet avec avantage ou désavantage.
/// </summary>
public enum PickMode
{
    /// <summary>Avantage : on retient le set avec le plus de succès.</summary>
    Best,
    /// <summary>Désavantage : on retient le set avec le moins de succès.</summary>
    Worst
}

/// <summary>
/// Résultat de la sélection entre deux sets de dés.
/// </summary>
/// <param name="Chosen">Le set retenu selon le mode (avantage ou désavantage).</param>
/// <param name="Rejected">Le set écarté.</param>
public record PickedDiceSet(DiceSet Chosen, DiceSet Rejected);

/// <summary>
/// Cas d'usage : choisir le meilleur ou le pire set parmi deux, selon le mode de jet.
/// Tiebreak sur le nombre de succès égal : on compare le plus grand dé de chaque set.
/// </summary>
public interface PickDiceSetUseCase
{
    /// <summary>
    /// Compare deux sets de dés et retourne celui à conserver.
    /// </summary>
    /// <param name="a">Premier set de dés.</param>
    /// <param name="b">Second set de dés.</param>
    /// <param name="palier">Le palier effectif utilisé pour compter les succès.</param>
    /// <param name="mode">
    /// <see cref="PickMode.Best"/> pour l'avantage (plus de succès),
    /// <see cref="PickMode.Worst"/> pour le désavantage (moins de succès).
    /// </param>
    /// <returns>Le set choisi et le set rejeté.</returns>
    PickedDiceSet Execute(DiceSet a, DiceSet b, int palier, PickMode mode);
}

/// <summary>
/// Implémentation par défaut de <see cref="PickDiceSetUseCase"/>.
/// </summary>
public class DefaultPickDiceSetUseCase : PickDiceSetUseCase
{
    /// <inheritdoc/>
    public PickedDiceSet Execute(DiceSet a, DiceSet b, int palier, PickMode mode)
    {
        var sA = a.Values.Count(d => d >= palier);
        var sB = b.Values.Count(d => d >= palier);

        // Tiebreak : plus grand dé si égalité de succès
        bool pickA;
        if (sA != sB)
            pickA = mode == PickMode.Best ? sA > sB : sA < sB;
        else
            pickA = mode == PickMode.Best ? a.Max() >= b.Max() : a.Max() <= b.Max();

        return pickA ? new PickedDiceSet(a, b) : new PickedDiceSet(b, a);
    }
}
