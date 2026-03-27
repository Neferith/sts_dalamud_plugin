using System;
using System.Collections.Generic;
using STSPlugin.Domain;
using STSPlugin.UseCases;

namespace STSPlugin;

/// <summary>
/// Moteur principal du STS. Orchestre les use cases et maintient l'état de la session.
/// Supporte deux modes de jet : interne (Roll/Reroll) et GameRandom (BeginRoll/ReceiveRandom).
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

    // --- état GameRandom ---
    private DiceSet? _pendingRejected; // set rejeté à conserver lors d'un reroll en mode avantage

    // --- random interne ---
    private static readonly Random Rng = new();
    private static int Roll1() => Rng.Next(1, 11);
    private static DiceSet Roll3() => new([Roll1(), Roll1(), Roll1()]);

    // --- propriétés publiques ---

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

    /// <summary>
    /// État vis-à-vis du mode GameRandom.
    /// Idle = prêt, WaitingDice = en attente d'un résultat /random depuis le chat.
    /// </summary>
    public EngineState State { get; private set; } = EngineState.Idle;

    /// <summary>Historique des 8 derniers jets.</summary>
    public List<RollEntry> History { get; } = [];

    /// <summary>
    /// Initialise l'engine avec les implémentations des use cases.
    /// </summary>
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

    // --- actions mode Internal ---

    /// <summary>
    /// Change le rang du personnage et réinitialise la session en cours.
    /// </summary>
    public void ChangeRank(RankKey rankKey)
    {
        _session.Rank = Rank.Get(rankKey);
        _session.Reset();
        State = EngineState.Idle;
    }

    /// <summary>
    /// [Mode Internal] Effectue un jet de dés complet immédiatement.
    /// </summary>
    public void Roll()
    {
        _session.LastResult = ResolveNewRoll(previousRejected: null);
        PushHistory(_session.LastResult);
    }

    /// <summary>
    /// [Mode Internal] Relance les 3 dés si un reroll est disponible.
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

    // --- actions mode GameRandom ---

    /// <summary>
    /// [Mode GameRandom] Prépare l'engine à recevoir un résultat /random.
    /// À appeler avant d'envoyer /random dans le chat.
    /// </summary>
    public void BeginRoll()
    {
        _pendingRejected = null;
        State = EngineState.WaitingDice;
    }

    /// <summary>
    /// [Mode GameRandom] Prépare l'engine à recevoir un résultat /random pour un reroll.
    /// Conserve le set rejeté précédent pour le mode Avantage/Désavantage.
    /// </summary>
    /// <returns>True si le reroll peut être effectué, false sinon.</returns>
    public bool BeginReroll()
    {
        var check = _checkReroll.Execute(_session.Rank.Rerolls, _session.RerollsUsed);
        if (!HasRolled || !check.Allowed) return false;

        _session.RerollsUsed++;
        _pendingRejected = _session.LastResult?.Rejected; // conserver pour la comparaison
        State = EngineState.WaitingDice;
        return true;
    }

    /// <summary>
    /// [Mode GameRandom] Reçoit la valeur brute du /random du jeu (0–999) et résout le jet.
    /// Chaque chiffre du résultat correspond à un dé : 0 → 10, 1–9 → 1–9.
    /// </summary>
    /// <param name="randomValue">Valeur reçue du /random (0 à 999).</param>
    /// <returns>True si le jet est résolu, false si l'état n'était pas WaitingDice.</returns>
    public bool ReceiveRandom(int randomValue)
    {
        if (State != EngineState.WaitingDice) return false;

        var diceSet = ParseRandomToDiceSet(randomValue);
        _session.LastResult = ResolveWithSet(diceSet, _pendingRejected);
        PushHistory(_session.LastResult);
        State = EngineState.Idle;
        _pendingRejected = null;
        return true;
    }

    // --- reset ---

    /// <summary>
    /// Réinitialise les rerolls et le jet en cours pour un nouvel event.
    /// </summary>
    public void ResetEvent()
    {
        _session.Reset();
        State = EngineState.Idle;
        _pendingRejected = null;
    }

    // --- privé ---

    /// <summary>
    /// [Mode Internal] Lance les dés et résout selon le mode actif.
    /// </summary>
    private RollResult ResolveNewRoll(DiceSet? previousRejected)
    {
        var set = Roll3();
        return ResolveWithSet(set, previousRejected);
    }

    /// <summary>
    /// Résout un jet à partir d'un set de dés fourni (interne ou GameRandom).
    /// En mode Normal : évalue directement le set.
    /// En mode Avantage/Désavantage : compare avec le set rejeté précédent ou en lance un second.
    /// </summary>
    private RollResult ResolveWithSet(DiceSet set, DiceSet? previousRejected)
    {
        var palier = EffectivePalier;

        if (Mode == RollMode.Normal)
        {
            var resolution = _resolveDiceSet.Execute(set, palier);
            return new RollResult(set, null, resolution.Successes, palier);
        }
        else
        {
            var other = previousRejected ?? Roll3();
            var mode = Mode == RollMode.Avantage ? PickMode.Best : PickMode.Worst;
            var picked = _pickDiceSet.Execute(set, other, palier, mode);
            var resolution = _resolveDiceSet.Execute(picked.Chosen, palier);
            return new RollResult(picked.Chosen, picked.Rejected, resolution.Successes, palier);
        }
    }

    /// <summary>
    /// Parse un résultat /random (0–999) en DiceSet STS.
    /// Chaque chiffre : 0 → 10, 1–9 → 1–9.
    /// Ex : 042 → [10, 4, 2] | 000 → [10, 10, 10]
    /// </summary>
    private static DiceSet ParseRandomToDiceSet(int value)
    {
        var s = value.ToString("D3");
        var dice = new int[3];
        for (var i = 0; i < 3; i++)
        {
            var d = s[i] - '0';
            dice[i] = d == 0 ? 10 : d;
        }
        return new DiceSet(dice);
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
