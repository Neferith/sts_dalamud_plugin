using Dalamud.Configuration;
using Sts.Domain;
using System;
using Sts.Domain;
using STSPlugin.ConfigDomain;

namespace STSPlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>Rang persisté entre sessions (fallback si aucun personnage actif).</summary>
    public string LastRank { get; set; } = RankKey.Aventurier.ToString();

    /// <summary>Identifiant du personnage actif. Null si aucun sélectionné.</summary>
    public Guid? ActiveCharacterId { get; set; } = null;

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
