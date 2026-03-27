using Dalamud.Configuration;
using STSPlugin.Domain;
using System;


namespace STSPlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>Rang persisté entre sessions.</summary>
    public string LastRank { get; set; } = RankKey.Aventurier.ToString();

    /// <summary>Si true, poste le résultat dans le chat après /sts roll.</summary>
    public bool EchoToChat { get; set; } = true;

    /// <summary>Canal cible pour l'echo (say, party, ls1…).</summary>
    public string ChatChannel { get; set; } = "say";

    /// <summary>
    /// Source des valeurs de dés.
    /// Internal = RNG interne, GameRandom = /random du jeu (vérifiable par tous).
    /// </summary>
    public RollSource RollSource { get; set; } = RollSource.Internal;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}

