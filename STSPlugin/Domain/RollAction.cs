using System.Collections.Generic;

namespace STSPlugin.Domain;

/// <summary>
/// Type de prérequis matériel pour une action de jet.
/// Extensible — chaque valeur peut modifier les conditions du jet si non remplie.
/// </summary>
public enum ActionRequirementType
{
    /// <summary>
    /// Nécessite une arme équipée.
    /// Si aucune arme équipée, ou si toutes les armes équipées sont non maîtrisées
    /// (compétence niveau 0), le palier d'attaque passe à 8.
    /// </summary>
    Weapon,
}

/// <summary>
/// Représente une action de jet prête à l'emploi.
/// </summary>
public class RollAction
{
    /// <summary>Identifiant unique (ex : "attaque_magique").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nom affiché dans la quickbar.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Contextes du jet — l'engine filtre les effets de traits dont le context correspond.
    /// </summary>
    public List<string> Contexts { get; set; } = [];

    /// <summary>
    /// Prérequis matériels pour ce jet.
    /// Évalués avant le lancer — peuvent modifier le palier ou d'autres paramètres.
    /// </summary>
    public List<ActionRequirementType> Requirements { get; set; } = [];

    /// <summary>Action prédéfinie (data.json) — non supprimable par le joueur.</summary>
    public bool IsPredefined { get; set; } = false;
}
