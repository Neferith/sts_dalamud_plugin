using System.Collections.Generic;

namespace STSPlugin.Domain;

/// <summary>
/// Représente une action de jet prête à l'emploi.
/// Une action encapsule un nom et une liste de contextes qui seront
/// utilisés par l'engine pour calculer les effets des traits applicables.
/// </summary>
public class RollAction
{
    /// <summary>Identifiant unique de l'action (ex : "attaque_magique").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nom affiché dans la barre de raccourcis (ex : "Attaque magique").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Contextes du jet.
    /// L'engine filtre les effets des traits dont le context correspond à l'un de ces contextes.
    /// Ex : ["attaque", "attaque_magique"] activera Polyviolence (context "attaque")
    /// et Spécialiste de la magie (context "attaque_magique").
    /// </summary>
    public List<string> Contexts { get; set; } = [];

    /// <summary>
    /// Indique si cette action est prédéfinie (chargée depuis data.json).
    /// Les actions prédéfinies ne peuvent pas être supprimées par le joueur.
    /// </summary>
    public bool IsPredefined { get; set; } = false;
}
