using System;

namespace STSPlugin.UseCases;

/// <summary>
/// Cas d'usage : vérifier si un reroll est disponible pour le jet en cours.
/// Un reroll permet de relancer l'intégralité des 3 dés du set actif.
/// Le nombre de rerolls disponibles dépend du rang du personnage.
/// </summary>
public interface CheckRerollUseCase
{
    /// <summary>
    /// Résultat de la vérification de disponibilité d'un reroll.
    /// </summary>
    /// <param name="Allowed">Indique si au moins un reroll est encore disponible.</param>
    /// <param name="Remaining">Nombre de rerolls restants pour l'event en cours.</param>
    public record Result(bool Allowed, int Remaining);

    /// <summary>
    /// Vérifie si un reroll est possible et calcule le nombre de rerolls restants.
    /// </summary>
    /// <param name="rerollsMax">Nombre maximum de rerolls accordés par le rang.</param>
    /// <param name="rerollsUsed">Nombre de rerolls déjà consommés lors de l'event.</param>
    /// <returns>La disponibilité et le nombre de rerolls restants.</returns>
    Result Execute(int rerollsMax, int rerollsUsed);
}

/// <summary>
/// Implémentation par défaut de <see cref="CheckRerollUseCase"/>.
/// </summary>
public class DefaultCheckRerollUseCase : CheckRerollUseCase
{
    /// <inheritdoc/>
    public CheckRerollUseCase.Result Execute(int rerollsMax, int rerollsUsed)
    {
        var left = Math.Max(0, rerollsMax - rerollsUsed);
        return new CheckRerollUseCase.Result(left > 0, left);
    }
}
