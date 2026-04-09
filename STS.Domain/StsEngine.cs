using Sts.Domain.UseCases;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Collections.Specialized.BitVector32;

namespace Sts.Domain;

/// <summary>
/// Moteur principal du STS. Orchestre les use cases et maintient l'état de la session.
/// Supporte deux modes de jet : interne (Roll) et GameRandom (BeginRoll/ReceiveRandom).
/// Calcule les effets des traits équipés selon les contextes de l'action en cours.
/// </summary>
public class StsEngine
{
    private readonly IComputePalierUseCase _computePalier;
    private readonly IResolveDiceSetUseCase _resolveDiceSet;
    private readonly IPickDiceSetUseCase _pickDiceSet;
    private readonly ICheckRerollUseCase _checkReroll;

    private readonly RollSession _session = new();
    private IReadOnlyList<Trait> _equippedTraits = [];

    private DiceSet? _pendingRejected;
    private RollAction? _pendingAction;
    private bool _isPendingReroll;

    private static readonly Random Rng = new();
    private static int Roll1() => Rng.Next(1, 11);
    private static DiceSet Roll3() => new([Roll1(), Roll1(), Roll1()]);

    // --- propriétés publiques ---

    public Rank CurrentRank => _session.Rank;

    public RollMode Mode
    {
        get => _session.Mode;
        set => _session.Mode = value;
    }

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

    public int EffectivePalier => PalierOverride ?? _computePalier.Execute(_session.Rank, _session.Modifier);

    public bool HasRolled => _session.HasRolled;
    public RollResult? LastResult => _session.LastResult;

    public int RerollsLeft
    {
        get
        {
            var baseRerolls = _session.Rank.Rerolls + PermanentBonusRerolls();
            return _checkReroll.Execute(baseRerolls, _session.RerollsUsed).Remaining;
        }
    }

    public EngineState State { get; private set; } = EngineState.Idle;

    public List<RollEntry> History { get; } = [];

    public StsEngine(
        IComputePalierUseCase computePalier,
        IResolveDiceSetUseCase resolveDiceSet,
        IPickDiceSetUseCase pickDiceSet,
        ICheckRerollUseCase checkReroll)
    {
        _computePalier = computePalier;
        _resolveDiceSet = resolveDiceSet;
        _pickDiceSet = pickDiceSet;
        _checkReroll = checkReroll;
    }

    /// <summary>Factory method pratique avec les implémentations par défaut.</summary>
    public static StsEngine CreateDefault() => new(
        new DefaultComputePalierUseCase(),
        new DefaultResolveDiceSetUseCase(),
        new DefaultPickDiceSetUseCase(),
        new DefaultCheckRerollUseCase());

    // --- configuration ---

    public void SetEquippedTraits(IReadOnlyList<Trait> traits)
        => _equippedTraits = traits;

    public void ChangeRank(RankKey rankKey)
    {
        _session.Rank = Rank.Get(rankKey);
        _session.Reset();
        State = EngineState.Idle;
    }

    // --- mode Internal ---

    /// <summary>[Mode Internal] Jet manuel sans action.</summary>
    public void Roll() => RollWithAction(null);

    /// <summary>[Mode Internal] Jet avec une action — effets de traits calculés.</summary>
    public void Roll(RollAction action) => RollWithAction(action);

    /// <summary>[Mode Internal] Relance les 3 dés si un reroll est disponible.</summary>
    public bool Reroll()
    {
        var baseRerolls = _session.Rank.Rerolls + PermanentBonusRerolls();
        var check = _checkReroll.Execute(baseRerolls, _session.RerollsUsed);
        if (!HasRolled || !check.Allowed) return false;

        _session.RerollsUsed++;
        var action = _session.LastResult?.Action;
        var effects = ComputeTraitEffects(action, isReroll: true);
        var result = ResolveWithSet(Roll3(), _session.LastResult?.Rejected, action, effects);

        var (rerollBonus, rerollNames) = ComputeRerollBonuses(action);
        if (rerollBonus > 0)
            result = result with
            {
                TraitEffects = result.TraitEffects with
                {
                    BonusSuccesses = result.TraitEffects.BonusSuccesses + rerollBonus,
                    BonusTraitNames = [.. result.TraitEffects.BonusTraitNames, .. rerollNames]
                }
            };

        _session.LastResult = result;
        PushHistory(_session.LastResult);
        return true;
    }

    // --- mode GameRandom ---

    /// <summary>[Mode GameRandom] Prépare l'engine pour un jet manuel.</summary>
    public void BeginRoll() => BeginRollWithAction(null);

    /// <summary>[Mode GameRandom] Prépare l'engine pour un jet avec action.</summary>
    public void BeginRoll(RollAction action) => BeginRollWithAction(action);

    /// <summary>[Mode GameRandom] Prépare l'engine pour un reroll.</summary>
    public bool BeginReroll()
    {
        var baseRerolls = _session.Rank.Rerolls + PermanentBonusRerolls();
        var check = _checkReroll.Execute(baseRerolls, _session.RerollsUsed);
        if (!HasRolled || !check.Allowed) return false;

        _session.RerollsUsed++;
        _pendingRejected = _session.LastResult?.Rejected;
        _pendingAction = _session.LastResult?.Action;
        _isPendingReroll = true;
        State = EngineState.WaitingDice;
        return true;
    }

    /// <summary>
    /// [Mode GameRandom] Reçoit la valeur brute du /random (0–999) et résout le jet.
    /// </summary>
    public bool ReceiveRandom(int randomValue)
    {
        if (State != EngineState.WaitingDice) return false;

        var isReroll = _isPendingReroll;
        var effects = ComputeTraitEffects(_pendingAction, isReroll: isReroll);
        var diceSet = ParseRandomToDiceSet(randomValue);
        var result = ResolveWithSet(diceSet, _pendingRejected, _pendingAction, effects);

        if (isReroll)
        {
            var (rerollBonus, rerollNames) = ComputeRerollBonuses(_pendingAction);
            if (rerollBonus > 0)
                result = result with
                {
                    TraitEffects = result.TraitEffects with
                    {
                        BonusSuccesses = result.TraitEffects.BonusSuccesses + rerollBonus,
                        BonusTraitNames = [.. result.TraitEffects.BonusTraitNames, .. rerollNames]
                    }
                };
        }

        _session.LastResult = result;
        PushHistory(_session.LastResult);
        State = EngineState.Idle;
        _pendingRejected = null;
        _pendingAction = null;
        _isPendingReroll = false;
        return true;
    }

    public void ResetEvent()
    {
        _session.Reset();
        State = EngineState.Idle;
        _pendingRejected = null;
        _pendingAction = null;
        _isPendingReroll = false;
        PalierOverride = null;
    }

    // --- calcul des effets de traits ---

    private (int Bonus, List<string> Names) ComputeRerollBonuses(RollAction? action)
    {
        if (_equippedTraits.Count == 0) return (0, []);
        var contexts = action?.Contexts ?? [];

        var bonus = 0;
        var names = new List<string>();
        foreach (var trait in _equippedTraits.Where(t => t.Effects != null))
            foreach (var e in trait.Effects!)
                if (e.Type == TraitEffectType.BonusSuccessOnReroll &&
                    (e.Context == null || contexts.Contains(e.Context)))
                {
                    bonus += e.Value;
                    names.Add(trait.Name);
                }

        return (bonus, names);
    }

    private AppliedTraitEffects ComputeTraitEffects(RollAction? action, bool isReroll = false)
    {
        if (_equippedTraits.Count == 0)
            return AppliedTraitEffects.None;

        var contexts = action?.Contexts ?? [];
        var bonusRerolls = 0;
        var bonusSuccess = 0;
        var malusSuccess = 0;
        RollMode? forcedMode = null;
        var bonusTraitNames = new List<string>();
        var malusTraitNames = new List<string>();

        foreach (var trait in _equippedTraits)
        {
            if (trait.Effects is null) continue;

            foreach (var effect in trait.Effects)
            {
                var matches = effect.Context == null || contexts.Contains(effect.Context);
                if (!matches) continue;

                switch (effect.Type)
                {
                    case TraitEffectType.BonusRerolls:
                        bonusRerolls += effect.Value;
                        break;
                    case TraitEffectType.ForceRollMode:
                        forcedMode = effect.ForcedMode;
                        break;
                    case TraitEffectType.BonusSuccess:
                        bonusSuccess += effect.Value;
                        bonusTraitNames.Add(trait.Name);
                        break;
                    case TraitEffectType.MalusSuccess:
                        malusSuccess += effect.Value;
                        malusTraitNames.Add(trait.Name);
                        break;
                    case TraitEffectType.BonusPalier when isReroll:
                    case TraitEffectType.BonusSuccessOnZero:
                    case TraitEffectType.BonusSuccessOnReroll:
                    case TraitEffectType.Manual:
                        break;
                }
            }
        }

        return new AppliedTraitEffects
            (
                bonusSuccess, 
                malusSuccess, 
                bonusRerolls, 
                forcedMode, 
                bonusTraitNames, 
                malusTraitNames
            );
    }

    private (int Bonus, List<string> Names) ComputeZeroBonuses(DiceSet dice, RollAction? action)
    {
        if (_equippedTraits.Count == 0) return (0, []);
        var contexts = action?.Contexts ?? [];
        if (!dice.Values.Any(v => v == 10)) return (0, []);

        var bonus = 0;
        var names = new List<string>();
        foreach (var trait in _equippedTraits.Where(t => t.Effects != null))
            foreach (var e in trait.Effects!)
                if (e.Type == TraitEffectType.BonusSuccessOnZero &&
                    (e.Context == null || contexts.Contains(e.Context)))
                {
                    bonus += e.Value;
                    names.Add(trait.Name);
                }

        return (bonus, names);
    }

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
        _isPendingReroll = false;
        State = EngineState.WaitingDice;
    }

    private RollResult ResolveNewRoll(DiceSet? previousRejected, RollAction? action, AppliedTraitEffects effects)
        => ResolveWithSet(Roll3(), previousRejected, action, effects);

    private RollResult ResolveWithSet(DiceSet set, DiceSet? previousRejected, RollAction? action, AppliedTraitEffects effects)
    {
        var palier = EffectivePalier;
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
        var (zeroBonuses, zeroNames) = ComputeZeroBonuses(chosen, action);
        var finalEffects = zeroBonuses > 0
            ? effects with
            {
                BonusSuccesses = effects.BonusSuccesses + zeroBonuses,
                BonusTraitNames = [.. effects.BonusTraitNames, .. zeroNames]
            }
            : effects;

        return new RollResult(
            chosen,
            effectiveMode == RollMode.Normal ? null : rejected,
            resolution.Successes,
            palier,
            finalEffects,
            action
        );
    }

    public static DiceSet ParseRandomToDiceSet(int value)
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
