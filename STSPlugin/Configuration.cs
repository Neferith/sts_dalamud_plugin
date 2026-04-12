using Dalamud.Configuration;
using Sts.Domain;
using System;
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
    public RollSource RollSource { get; set; } = RollSource.GameRandom;

    /// <summary>
    /// Mode de chargement des données de référence.
    /// Local = data.json embarqué, Remote = API distante.
    /// </summary>
    public DataSourceMode DataSourceMode { get; set; } = DataSourceMode.Remote;

    /// <summary>
    /// URL de l'endpoint data de l'API STS.
    /// Utilisée par le RemoteJsonDataSource en mode Remote.
    /// </summary>
    public string BackendUrl { get; set; } = "https://api.nlrp.fr/api/data";

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
