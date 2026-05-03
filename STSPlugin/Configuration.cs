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
    /// URL de base de l'API STS (sans slash final).
    /// Exemple : https://api.nlrp.fr
    /// Toutes les URLs sont dérivées de cette base.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.nlrp.fr";

    /// <summary>Nom d'utilisateur du joueur pour l'authentification API.</summary>
    public string PlayerUsername { get; set; } = string.Empty;

    /// <summary>
    /// Mot de passe du joueur (stocké en clair dans la config locale Dalamud).
    /// Utilisé pour obtenir et renouveler le JWT automatiquement.
    /// </summary>
    public string PlayerPassword { get; set; } = string.Empty;

    /// <summary>URL de l'endpoint data — dérivée de ApiBaseUrl.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string DataUrl => $"{ApiBaseUrl.TrimEnd('/')}/api/data";

    /// <summary>URL de l'endpoint auth — dérivée de ApiBaseUrl.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string AuthUrl => $"{ApiBaseUrl.TrimEnd('/')}/api/auth/login";

    /// <summary>URL de l'endpoint characters — dérivée de ApiBaseUrl.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string CharactersUrl => $"{ApiBaseUrl.TrimEnd('/')}/api/characters";

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
