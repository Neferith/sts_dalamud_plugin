using System;
using System.Collections.Generic;
using System.Linq;

namespace STSPlugin.Domain;

/// <summary>Identifiant typé d'un rang STS.</summary>
public enum RankKey { Novice, Aventurier, Veteran, Mentor }

/// <summary>Mode de jet déterminant combien de sets sont lancés et lequel est retenu.</summary>
public enum RollMode
{
    /// <summary>Un seul set de 3 dés, évalué directement.</summary>
    Normal,
    /// <summary>Deux sets lancés, on retient celui avec le plus de succès.</summary>
    Avantage,
    /// <summary>Deux sets lancés, on retient celui avec le moins de succès.</summary>
    Desavantage
}

/// <summary>Source des valeurs de dés utilisées lors d'un jet.</summary>
public enum RollSource
{
    /// <summary>RNG interne du plugin.</summary>
    Internal,
    /// <summary>/random du jeu — vérifiable par tous.</summary>
    GameRandom
}

/// <summary>État de l'engine vis-à-vis du mode GameRandom.</summary>
public enum EngineState
{
    /// <summary>Prêt, aucun jet en attente.</summary>
    Idle,
    /// <summary>En attente d'un résultat /random depuis le chat.</summary>
    WaitingDice
}

/// <summary>Données immuables associées à un rang.</summary>
public record Rank(string Label, int Palier, int Rerolls, int Traits)
{
    public static readonly IReadOnlyDictionary<RankKey, Rank> All = new Dictionary<RankKey, Rank>
    {
        [RankKey.Novice] = new("Novice", 7, 1, 2),
        [RankKey.Aventurier] = new("Aventurier", 6, 1, 3),
        [RankKey.Veteran] = new("Vétéran", 5, 2, 4),
        [RankKey.Mentor] = new("Mentor", 4, 2, 5),
    };
    public static Rank Get(RankKey key) => All[key];
}

/// <summary>Un set de 3 dés STS. Valeur immuable.</summary>
public record DiceSet
{
    public int[] Values { get; }

    public DiceSet(int[] values)
    {
        if (values.Length != 3)
            throw new ArgumentException("Un DiceSet doit contenir exactement 3 dés.", nameof(values));
        if (values.Any(v => v < 1 || v > 10))
            throw new ArgumentOutOfRangeException(nameof(values), "Chaque dé doit être entre 1 et 10.");
        Values = (int[])values.Clone();
    }

    public int Max() => Values.Max();
    public static string Display(int value) => value == 10 ? "0" : value.ToString();
    public string ToDisplayString() => string.Join(" · ", Values.Select(Display));
}

/// <summary>
/// Effets de traits appliqués lors d'un jet.
/// Portés par RollResult pour l'affichage.
/// </summary>
public record AppliedTraitEffects(
    /// <summary>Succès ajoutés par les traits (BonusSuccess + BonusSuccessOnZero).</summary>
    int BonusSuccesses,
    /// <summary>Succès requis en plus par les traits (MalusSuccess).</summary>
    int MalusSuccesses,
    /// <summary>Rerolls supplémentaires accordés par les traits pour ce jet.</summary>
    int BonusRerolls,
    /// <summary>Mode forcé par un trait, null si aucun.</summary>
    RollMode? ForcedMode
)
{
    /// <summary>Succès nets après application des bonus et malus.</summary>
    public int NetSuccesses => BonusSuccesses - MalusSuccesses;
    public static AppliedTraitEffects None => new(0, 0, 0, null);
}

/// <summary>Résultat d'un jet résolu.</summary>
public record RollResult(
    DiceSet Chosen,
    DiceSet? Rejected,
    /// <summary>Succès bruts des dés (avant traits).</summary>
    int RawSuccesses,
    int Palier,
    /// <summary>Effets de traits appliqués. None si jet sans action.</summary>
    AppliedTraitEffects TraitEffects,
    /// <summary>Action ayant déclenché ce jet. Null si jet manuel.</summary>
    RollAction? Action = null
)
{
    /// <summary>Succès totaux = dés + bonus traits - malus traits.</summary>
    public int Successes => Math.Max(0, RawSuccesses + TraitEffects.NetSuccesses);
}

/// <summary>Entrée d'historique.</summary>
public record RollEntry(string RankLabel, DiceSet Dice, int Palier, int RawSuccesses, int TotalSuccesses, string? ActionName = null);

/// <summary>État mutable de la session de jet en cours.</summary>
public class RollSession
{
    public Rank Rank { get; set; } = Rank.Get(RankKey.Aventurier);
    public RollMode Mode { get; set; } = RollMode.Normal;
    public int Modifier { get; set; } = 0;
    public int RerollsUsed { get; set; } = 0;
    public RollResult? LastResult { get; set; } = null;
    public bool HasRolled => LastResult != null;

    public void Reset()
    {
        RerollsUsed = 0;
        LastResult = null;
    }
}
