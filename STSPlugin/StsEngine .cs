using System;
using System.Collections.Generic;
using STSPlugin.Domain;
using STSPlugin.UseCases;

namespace STSPlugin;

/// <summary>
/// Moteur principal du STS. Orchestre les use cases et maintient l'état de la session.
/// Ne contient aucune logique métier propre — tout est délégué aux use cases.
/// </summary>
public class StsEngine
{
    // --- dépendances injectées ---
    private readonly ComputePalierUseCase _computePalier;
    private readonly ResolveDiceSetUseCase _resolveDiceSet;
    private readonly PickDiceSetUseCase _pickDiceSet;
    private readonly CheckRerollUseCase _checkReroll;

    // --- état courant ---
    private readonly RollSession _session = new();

    // --- random interne ---
    private static readonly Random Rng = new();
    private static int Roll1() => Rng.Next(1, 11);
    private static DiceSet Roll3() => new([Roll1(), Roll1(), Roll1()]);

    /// <summary>Rang courant du personnage.</summary>
    public Rank CurrentRank => _session.Rank;

    /// <summary>Mode de jet actif (Normal, Avantage, Désavantage).</summary>
    public RollMode Mode
    {
        get => _session.Mode;
        set => _session.Mode = value;
    }

    /// <summary>Modificateur MJ appliqué au palier. Positif = facilite.</summary>
    public int Modifier
    {
        get => _session.Modifier;
        set => _session.Modifier = value;
    }

    /// <summary>Palier effectif après application du modificateur.</summary>
    public int EffectivePalier => _computePalier.Execute(_session.Rank, _session.Modifier);

    /// <summary>Indique si un jet a déjà été effectué.</summary>
    public bool HasRolled => _session.HasRolled;

    /// <summary>Résultat du dernier jet résolu. Null si aucun jet en cours.</summary>
    public RollResult? LastResult => _session.LastResult;

    /// <summary>Nombre de rerolls restants pour l'event en cours.</summary>
    public int RerollsLeft => _checkReroll.Execute(_session.Rank.Rerolls, _session.RerollsUsed).Remaining;

    /// <summary>Historique des 8 derniers jets.</summary>
    public List<RollEntry> History { get; } = [];

    /// <summary>
    /// Initialise l'engine avec les implémentations des use cases.
    /// </summary>
    /// <param name="computePalier">Calcul du palier effectif.</param>
    /// <param name="resolveDiceSet">Évaluation d'un set de dés contre un palier.</param>
    /// <param name="pickDiceSet">Sélection du meilleur ou pire set.</param>
    /// <param name="checkReroll">Vérification de la disponibilité d'un reroll.</param>
    public StsEngine(
        ComputePalierUseCase computePalier,
        ResolveDiceSetUseCase resolveDiceSet,
        PickDiceSetUseCase pickDiceSet,
        CheckRerollUseCase checkReroll)
    {
        _computePalier = computePalier;
        _resolveDiceSet = resolveDiceSet;
        _pickDiceSet = pickDiceSet;
        _checkReroll = checkReroll;
    }

    // --- actions ---

    /// <summary>
    /// Change le rang du personnage et réinitialise la session en cours.
    /// </summary>
    /// <param name="rankKey">Clé enum du rang cible.</param>
    public void ChangeRank(RankKey rankKey)
    {
        _session.Rank = Rank.Get(rankKey);
        _session.Reset();
    }

    /// <summary>
    /// Effectue un jet de dés selon le mode actif.
    /// Normal : 1 set de 3 dés évalué directement.
    /// Avantage / Désavantage : 2 sets lancés, on retient le meilleur ou le pire.
    /// </summary>
    public void Roll()
    {
        _session.RerollsUsed = 0;
        _session.LastResult = ResolveNewRoll(previousRejected: null);
        PushHistory(_session.LastResult);
    }

    /// <summary>
    /// Relance les 3 dés du set actif si un reroll est disponible.
    /// En mode Avantage/Désavantage, le nouveau set est comparé au set rejeté précédent.
    /// </summary>
    /// <returns>True si le reroll a été effectué, false sinon.</returns>
    public bool Reroll()
    {
        var check = _checkReroll.Execute(_session.Rank.Rerolls, _session.RerollsUsed);
        if (!HasRolled || !check.Allowed) return false;

        _session.RerollsUsed++;
        _session.LastResult = ResolveNewRoll(_session.LastResult?.Rejected);
        PushHistory(_session.LastResult);
        return true;
    }

    /// <summary>
    /// Réinitialise les rerolls et le jet en cours pour un nouvel event.
    /// </summary>
    public void ResetEvent() => _session.Reset();

    // --- privé ---

    /// <summary>
    /// Lance les dés et résout le jet selon le mode actif.
    /// </summary>
    /// <param name="previousRejected">
    /// En mode Avantage/Désavantage lors d'un reroll, le set rejeté précédent
    /// est conservé comme second set à comparer. Null pour un jet initial.
    /// </param>
    private RollResult ResolveNewRoll(DiceSet? previousRejected)
    {
        var palier = EffectivePalier;

        if (Mode == RollMode.Normal)
        {
            var set = Roll3();
            var resolution = _resolveDiceSet.Execute(set, palier);
            return new RollResult(set, null, resolution.Successes, palier);
        }
        else
        {
            var a = Roll3();
            var b = previousRejected ?? Roll3();
            var mode = Mode == RollMode.Avantage ? PickMode.Best : PickMode.Worst;
            var picked = _pickDiceSet.Execute(a, b, palier, mode);
            var resolution = _resolveDiceSet.Execute(picked.Chosen, palier);
            return new RollResult(picked.Chosen, picked.Rejected, resolution.Successes, palier);
        }
    }

    private void PushHistory(RollResult result)
    {
        History.Insert(0, new RollEntry(
            _session.Rank.Label,
            result.Chosen,
            result.Palier,
            result.Successes));

        if (History.Count > 8) History.RemoveAt(8);
    }
}
