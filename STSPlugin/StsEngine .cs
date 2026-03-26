using System;
using System.Collections.Generic;
using System.Linq;

namespace STSPlugin;

public enum RollMode { Normal, Avantage, Desavantage }

public record RankData(string Label, int Palier, int Rerolls, int Traits);

public record RollEntry(string RankLabel, int[] Dice, int Palier, int Successes);

public class StsEngine
{
    public static readonly Dictionary<string, RankData> Ranks = new()
    {
        ["novice"] = new("Novice", 7, 1, 2),
        ["aventurier"] = new("Aventurier", 6, 1, 3),
        ["veteran"] = new("Vétéran", 5, 2, 4),
        ["mentor"] = new("Mentor", 4, 2, 5),
    };

    public static readonly string[] RankKeys = ["novice", "aventurier", "veteran", "mentor"];

    private static readonly Random Rng = new();

    // --- état courant ---
    public string CurrentRank { get; private set; } = "aventurier";
    public RollMode Mode { get; set; } = RollMode.Normal;
    public int Modifier { get; set; } = 0; // positif = facilite (baisse le palier)
    public int RerollsUsed { get; private set; } = 0;
    public int[] CurrentDice { get; private set; } = [];
    public int[] OtherDice { get; private set; } = []; // set rejeté (avantage/désavantage)
    public bool HasRolled { get; private set; } = false;

    public List<RollEntry> History { get; } = [];

    // --- propriétés calculées ---
    public RankData Rank => Ranks[CurrentRank];
    public int EffectivePalier => Math.Max(1, Rank.Palier - Modifier);
    public int RerollsLeft => Math.Max(0, Rank.Rerolls - RerollsUsed);
    public int Successes => HasRolled ? CountSuc(CurrentDice, EffectivePalier) : 0;

    // --- helpers ---
    private static int Roll1() => Rng.Next(1, 11);
    private static int[] Roll3() => [Roll1(), Roll1(), Roll1()];
    public static int CountSuc(int[] d, int p) => d.Count(v => v >= p);
    public static string DispDie(int v) => v == 10 ? "0" : v.ToString();

    // --- actions ---
    public void ChangeRank(string rank)
    {
        if (!Ranks.ContainsKey(rank)) return;
        CurrentRank = rank;
        ResetEvent();
    }

    public void Roll()
    {
        RerollsUsed = 0;
        var p = EffectivePalier;

        if (Mode == RollMode.Normal)
        {
            CurrentDice = Roll3();
            OtherDice = [];
        }
        else
        {
            var a = Roll3();
            var b = Roll3();
            var (keep, discard) = PickBest(a, b, p, Mode == RollMode.Avantage);
            CurrentDice = keep;
            OtherDice = discard;
        }

        HasRolled = true;
        PushHistory();
    }

    public bool Reroll()
    {
        if (!HasRolled || RerollsLeft <= 0) return false;
        RerollsUsed++;
        var p = EffectivePalier;

        if (Mode == RollMode.Normal)
        {
            CurrentDice = Roll3();
        }
        else
        {
            // On relance le set courant et on re-compare avec l'autre
            var fresh = Roll3();
            var (keep, discard) = PickBest(fresh, OtherDice, p, Mode == RollMode.Avantage);
            CurrentDice = keep;
            OtherDice = discard;
        }

        PushHistory();
        return true;
    }

    public void ResetEvent()
    {
        RerollsUsed = 0;
        HasRolled = false;
        CurrentDice = [];
        OtherDice = [];
    }

    // Résumé texte pour /sts roll → chat
    public string ChatSummary()
    {
        if (!HasRolled) return "[STS] Aucun jet en cours.";
        var dice = string.Join(" · ", CurrentDice.Select(DispDie));
        var s = Successes;
        var res = s == 0 ? "Échec total" : s == 1 ? "1 succès" : $"{s} succès";
        var mod = Modifier != 0 ? $" (modif {(Modifier > 0 ? "+" : "")}{Modifier})" : "";
        return $"[STS] {Rank.Label}{mod} · {dice} · palier {EffectivePalier}+ → {res}";
    }

    // --- privé ---
    private static (int[] keep, int[] discard) PickBest(int[] a, int[] b, int palier, bool wantBest)
    {
        var sA = CountSuc(a, palier);
        var sB = CountSuc(b, palier);
        bool pickA;
        if (sA != sB) pickA = wantBest ? sA > sB : sA < sB;
        else pickA = wantBest ? a.Max() >= b.Max() : a.Max() <= b.Max();
        return pickA ? (a, b) : (b, a);
    }

    private void PushHistory()
    {
        History.Insert(0, new RollEntry(Rank.Label, (int[])CurrentDice.Clone(), EffectivePalier, Successes));
        if (History.Count > 8) History.RemoveAt(8);
    }
}
