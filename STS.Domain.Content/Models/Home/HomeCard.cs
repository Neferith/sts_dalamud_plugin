// STS.Domain.Content/Models/HomeCard.cs

namespace Sts.Domain.Content.Models;

/// <summary>Carte de présentation affichée sur la home.</summary>
public record HomeCard
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Titre court affiché en en-tête de carte.</summary>
    public required string Title { get; init; }

    /// <summary>Description courte (1-2 lignes max).</summary>
    public required string Description { get; init; }

    /// <summary>Icône unicode ou emoji.</summary>
    public string? Icon { get; init; }

    /// <summary>URL de navigation (null = carte non cliquable).</summary>
    public string? LinkUrl { get; init; }

    /// <summary>Libellé du CTA.</summary>
    public string? LinkLabel { get; init; }

    /// <summary>Accent coloré : "teal" | "ice" | "amber" | "purple".</summary>
    public string Accent { get; init; } = "teal";

    /// <summary>Ordre d'affichage.</summary>
    public int Order { get; init; }

    public bool IsVisible { get; init; } = true;
}
