using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sts.Domain.Content.Models
{
    /// <summary>Lien rapide affiché sur la home, configurable depuis l'admin.</summary>
    public record QuickLink
    {
        /// <summary>Identifiant unique.</summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>Libellé affiché.</summary>
        public required string Label { get; init; }

        /// <summary>URL cible.</summary>
        public required string Url { get; init; }

        /// <summary>Icône : emoji ou slug CSS.</summary>
        public string? Icon { get; init; }

        /// <summary>Catégorie d'affichage sur la home.</summary>
        public QuickLinkCategory Category { get; init; } = QuickLinkCategory.Ressources;

        /// <summary>Ordre d'affichage au sein de la catégorie.</summary>
        public int Order { get; init; }

        /// <summary>Si <see langword="false"/>, le lien n'est pas affiché sur la home.</summary>
        public bool IsVisible { get; init; } = true;
    }

    /// <summary>Catégorie d'un <see cref="QuickLink"/> sur la home.</summary>
    public enum QuickLinkCategory
    {
        /// <summary>Section « Rejoindre la guilde ».</summary>
        Recrutement,

        /// <summary>Section « Ressources membres ».</summary>
        Ressources
    }
}
