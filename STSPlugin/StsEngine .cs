using System;
using System.Collections.Generic;
using System.Linq;
using STSPlugin.Domain;
using STSPlugin.UseCases;

namespace STSPlugin;

/// <summary>
/// Moteur principal du STS. Orchestre les use cases et maintient l'état de la session.
/// Supporte deux modes de jet : interne (Roll) et GameRandom (BeginRoll/ReceiveRandom).
/// Calcule les effets des traits équipés selon les contextes de l'action en cours.
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

    // --- traits équipés du personnage actif ---
    private IReadOnlyList<Trait> _equippedTraits = [];

    // --- état GameRandom ---
    private DiceSet? _pendingRejected;
    private RollAction? _pendingAction;

    // --- random interne ---
    private static readonly Random Rng = new();
    private static int Roll1() => Rng.Next(1, 11);
    private static DiceSet Roll3() => new([Roll1(), Roll1(), Roll1()]);

    // --- propriétés publiques ---

    /// <summary>Rang courant du personnage.</summary>
    public Rank CurrentRank => _session.Rank;

    /// <summary>Mode de jet actif.</summary>
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

    /// <summary>
    /// Palier forcé par une règle externe (ex : arme non maîtrisée → 8).
    /// Null = palier normal calculé depuis le rang et le modificateur MJ.
    /// Automatiquement remis à null à chaque nouveau jet et à ResetEvent().
    /// </summary>
    public int? PalierOverride { get; set; } = null;

    /// <summary>Palier effectif — override prioritaire sur le calcul normal.</summary>
    public int EffectivePalier => PalierOverride ?? _computePalier.Execute(_session.Rank, _session.Modifier);

    /// <summary>Indique si un jet a déjà été effectué.</summary>
    public bool HasRolled => _session.HasRolled;

    /// <summary>Résultat du dernier jet résolu.</summary>
    public RollResult? LastResult => _session.LastResult;

    /// <summary>
    /// Nombre de rerolls restants pour l'event en cours.
    /// Inclut les bonus permanents des traits (context null).
    /// </summary>
    public int RerollsLeft
    {
        get
        {
            var baseRerolls = _session.Rank.Rerolls + PermanentBonusRerolls();
            return _checkReroll.Execute(baseRerolls, _session.RerollsUsed).Remaining;
        }
    }

    /// <summary>État vis-à-vis du mode GameRandom.</summary>
    public EngineState State { get; private set; } = EngineState.Idle;

    /// <summary>Historique des 8 derniers jets.</summary>
    public List<RollEntry> History { get; } = [];

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

    // --- configuration ---

    /// <summary>
    /// Met à jour les traits équipés du personnage actif.
    /// À appeler quand le personnage actif change ou quand ses traits sont modifiés.
    /// </summary>
    public void SetEquippedTraits(IReadOnlyList<Trait> traits)
        => _equippedTraits = traits;

    public void ChangeRank(RankKey rankKey)
    {
        _session.Rank = Rank.Get(rankKey);
        _session.Reset();
        State = EngineState.Idle;
    }

    // --- actions mode Internal ---

    /// <summary>
    /// [Mode Internal] Jet manuel sans action — pas d'effets de traits calculés.
    /// </summary>
    public void Roll()
        => RollWithAction(null);

    /// <summary>
    /// [Mode Internal] Jet avec une action — effets de traits calculés selon les contextes.
    /// </summary>
    public void Roll(RollAction action)
        => RollWithAction(action);

    /// <summary>
    /// [Mode Internal] Relance les 3 dés si un reroll est disponible.
    /// </summary>
    public bool Reroll()
    {
        var baseRerolls = _session.Rank.Rerolls + PermanentBonusRerolls();
        var check = _checkReroll.Execute(baseRerolls, _session.RerollsUsed);
        if (!HasRolled || !check.Allowed) return false;

        _session.RerollsUsed++;
        var action = _session.LastResult?.Action;
        var effects = ComputeTraitEffects(action, isReroll: true);
        _session.LastResult = ResolveWithSet(Roll3(), _session.LastResult?.Rejected, action, effects);
        PushHistory(_session.LastResult);
        return true;
    }

    // --- actions mode GameRandom ---

    /// <summary>[Mode GameRandom] Prépare l'engine pour un jet manuel.</summary>
    public void BeginRoll()
        => BeginRollWithAction(null);

    /// <summary>[Mode GameRandom] Prépare l'engine pour un jet avec action.</summary>
    public void BeginRoll(RollAction action)
        => BeginRollWithAction(action);

    /// <summary>[Mode GameRandom] Prépare l'engine pour un reroll.</summary>
    public bool BeginReroll()
    {
        var baseRerolls = _session.Rank.Rerolls + PermanentBonusRerolls();
        var check = _checkReroll.Execute(baseRerolls, _session.RerollsUsed);
        if (!HasRolled || !check.Allowed) return false;

        _session.RerollsUsed++;
        _pendingRejected = _session.LastResult?.Rejected;
        _pendingAction = _session.LastResult?.Action;
        State = EngineState.WaitingDice;
        return true;
    }

    /// <summary>
    /// [Mode GameRandom] Reçoit la valeur brute du /random (0–999) et résout le jet.
    /// </summary>
    public bool ReceiveRandom(int randomValue)
    {
        if (State != EngineState.WaitingDice) return false;

        var effects = ComputeTraitEffects(_pendingAction, isReroll: _pendingRejected != null);
        var diceSet = ParseRandomToDiceSet(randomValue);
        _session.LastResult = ResolveWithSet(diceSet, _pendingRejected, _pendingAction, effects);
        PushHistory(_session.LastResult);
        State = EngineState.Idle;
        _pendingRejected = null;
        _pendingAction = null;
        return true;
    }

    // --- reset ---

    public void ResetEvent()
    {
        _session.Reset();
        State = EngineState.Idle;
        _pendingRejected = null;
        _pendingAction = null;
        PalierOverride = null;
    }

    // --- calcul des effets de traits ---

    /// <summary>
    /// Calcule les effets de traits applicables pour une action donnée.
    /// </summary>
    private AppliedTraitEffects ComputeTraitEffects(RollAction? action, bool isReroll = false)
    {
        if (_equippedTraits.Count == 0)
            return AppliedTraitEffects.None;

        var contexts = action?.Contexts ?? [];

        var bonusRerolls = 0;
        var bonusSuccess = 0;
        var malusSuccess = 0;
        RollMode? forcedMode = null;

        foreach (var trait in _equippedTraits)
        {
            if (trait.Effects is null) continue;

            foreach (var effect in trait.Effects)
            {
                // Un effet s'applique si son context est null (permanent)
                // ou présent dans les contextes de l'action
                var matches = effect.Context == null
                    || contexts.Contains(effect.Context);

                if (!matches) continue;

                switch (effect.Type)
                {
                    case TraitEffectType.BonusRerolls:
                        bonusRerolls += effect.Value;
                        break;

                    case TraitEffectType.BonusPalier when isReroll:
                        // Géré dans EffectivePalier lors du reroll — stocké séparément
                        break;

                    case TraitEffectType.ForceRollMode:
                        // Dernier trait qui force le mode gagne
                        forcedMode = effect.ForcedMode;
                        break;

                    case TraitEffectType.BonusSuccess:
                        bonusSuccess += effect.Value;
                        break;

                    case TraitEffectType.MalusSuccess:
                        malusSuccess += effect.Value;
                        break;

                    case TraitEffectType.BonusSuccessOnZero:
                        // Sera calculé après résolution des dés
                        break;

                    case TraitEffectType.Manual:
                        break;
                }
            }
        }

        return new AppliedTraitEffects(bonusSuccess, malusSuccess, bonusRerolls, forcedMode);
    }

    /// <summary>
    /// Calcule les bonus de réussites liés aux 0 dans les dés (BonusSuccessOnZero).
    /// Appelé après résolution des dés.
    /// </summary>
    private int ComputeZeroBonuses(DiceSet dice, RollAction? action)
    {
        if (_equippedTraits.Count == 0) return 0;

        var contexts = action?.Contexts ?? [];
        var hasZero = dice.Values.Any(v => v == 10); // 10 s'affiche "0"
        if (!hasZero) return 0;

        return _equippedTraits
            .Where(t => t.Effects != null)
            .SelectMany(t => t.Effects!)
            .Where(e => e.Type == TraitEffectType.BonusSuccessOnZero)
            .Where(e => e.Context == null || contexts.Contains(e.Context))
            .Sum(e => e.Value);
    }

    /// <summary>Rerolls permanents accordés par les traits (context null).</summary>
    private int PermanentBonusRerolls()
        => _equippedTraits
            .Where(t => t.Effects != null)
            .SelectMany(t => t.Effects!)
            .Where(e => e.Type == TraitEffectType.BonusRerolls && e.Context == null)
            .Sum(e => e.Value);

    // --- privé ---

    private void RollWithAction(RollAction? action)
    {
        var effects = ComputeTraitEffects(action);
        _session.LastResult = ResolveNewRoll(null, action, effects);
        PushHistory(_session.LastResult);
        PalierOverride = null;
    }

    private void BeginRollWithAction(RollAction? action)
    {
        _pendingRejected = null;
        _pendingAction = action;
        State = EngineState.WaitingDice;
    }

    private RollResult ResolveNewRoll(DiceSet? previousRejected, RollAction? action, AppliedTraitEffects effects)
        => ResolveWithSet(Roll3(), previousRejected, action, effects);

    private RollResult ResolveWithSet(DiceSet set, DiceSet? previousRejected, RollAction? action, AppliedTraitEffects effects)
    {
        var palier = EffectivePalier;

        // Le mode peut être forcé par un trait
        var effectiveMode = effects.ForcedMode ?? Mode;

        DiceSet chosen, rejected;

        if (effectiveMode == RollMode.Normal)
        {
            chosen = set;
            rejected = null!;
        }
        else
        {
            var other = previousRejected ?? Roll3();
            var pickMode = effectiveMode == RollMode.Avantage ? PickMode.Best : PickMode.Worst;
            var picked = _pickDiceSet.Execute(set, other, palier, pickMode);
            chosen = picked.Chosen;
            rejected = picked.Rejected;
        }

        var resolution = _resolveDiceSet.Execute(chosen, palier);

        // Ajouter les bonus de zéros
        var zeroBonuses = ComputeZeroBonuses(chosen, action);
        var finalEffects = effects with { BonusSuccesses = effects.BonusSuccesses + zeroBonuses };

        return new RollResult(
            chosen,
            effectiveMode == RollMode.Normal ? null : rejected,
            resolution.Successes,
            palier,
            finalEffects,
            action
        );
    }

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
            result.RawSuccesses,
            result.Successes,
            result.Action?.Name));

        if (History.Count > 8) History.RemoveAt(8);
    }
}
