using System;
using System.Collections.Generic;
using System.Linq;

namespace STSPlugin.Domain;

/// <summary>
/// Identifiant typé d'un rang STS.
/// Ensemble fermé — toute valeur invalide est rejetée à la compilation.
/// </summary>
public enum RankKey
{
    Novice,
    Aventurier,
    Veteran,
    Mentor
}

/// <summary>
/// Mode de jet déterminant combien de sets sont lancés et lequel est retenu.
/// </summary>
public enum RollMode
{
    /// <summary>Un seul set de 3 dés, évalué directement.</summary>
    Normal,

    /// <summary>Deux sets lancés, on retient celui avec le plus de succès.</summary>
    Avantage,

    /// <summary>Deux sets lancés, on retient celui avec le moins de succès.</summary>
    Desavantage
}

/// <summary>
/// Données immuables associées à un rang.
/// </summary>
/// <param name="Label">Nom affiché du rang.</param>
/// <param name="Palier">Valeur minimale pour qu'un dé soit un succès.</param>
/// <param name="Rerolls">Nombre de rerolls accordés par event.</param>
/// <param name="Traits">Nombre de traits disponibles.</param>
public record Rank(string Label, int Palier, int Rerolls, int Traits)
{
    /// <summary>
    /// Catalogue des rangs indexés par leur clé enum.
    /// Source de vérité unique pour toutes les valeurs du système.
    /// </summary>
    public static readonly IReadOnlyDictionary<RankKey, Rank> All = new Dictionary<RankKey, Rank>
    {
        [RankKey.Novice] = new("Novice", 7, 1, 2),
        [RankKey.Aventurier] = new("Aventurier", 6, 1, 3),
        [RankKey.Veteran] = new("Vétéran", 5, 2, 4),
        [RankKey.Mentor] = new("Mentor", 4, 2, 5),
    };

    /// <summary>Récupère le rang associé à une clé.</summary>
    public static Rank Get(RankKey key) => All[key];
}

/// <summary>
/// Un set de 3 dés STS. Valeur immuable.
/// Chaque dé est entre 1 et 10, le 10 s'affiche "0" par convention FFXIV.
/// </summary>
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

    /// <summary>Affiche une valeur de dé : 10 → "0", autres → chiffre.</summary>
    public static string Display(int value) => value == 10 ? "0" : value.ToString();

    /// <summary>Affichage lisible du set, ex : "0 · 4 · 2".</summary>
    public string ToDisplayString() => string.Join(" · ", Values.Select(Display));
}

/// <summary>Résultat d'un jet résolu.</summary>
public record RollResult(DiceSet Chosen, DiceSet? Rejected, int Successes, int Palier);

/// <summary>Entrée d'historique.</summary>
public record RollEntry(string RankLabel, DiceSet Dice, int Palier, int Successes);

/// <summary>
/// État mutable de la session de jet en cours.
/// Portée par l'engine, jamais manipulée directement par les use cases.
/// </summary>
public class RollSession
{
    /// <summary>Rang actif du personnage.</summary>
    public Rank Rank { get; set; } = Rank.Get(RankKey.Aventurier);

    /// <summary>Mode de jet actif.</summary>
    public RollMode Mode { get; set; } = RollMode.Normal;

    /// <summary>Modificateur MJ sur le palier. Positif = facilite.</summary>
    public int Modifier { get; set; } = 0;

    /// <summary>Nombre de rerolls consommés depuis le dernier reset event.</summary>
    public int RerollsUsed { get; set; } = 0;

    /// <summary>Résultat du dernier jet. Null si aucun jet effectué.</summary>
    public RollResult? LastResult { get; set; } = null;

    /// <summary>Indique si un jet a déjà été effectué.</summary>
    public bool HasRolled => LastResult != null;

    /// <summary>Réinitialise la session pour un nouvel event.</summary>
    public void Reset()
    {
        RerollsUsed = 0;
        LastResult = null;
    }
}
