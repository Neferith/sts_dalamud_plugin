using Sts.Domain.Content.Models;

namespace Sts.Domain.Content.UseCases;

/// <summary>Paramètres pour la mise à jour d'un <see cref="QuickLink"/>.</summary>
/// <param name="Label">Libellé affiché.</param>
/// <param name="Url">URL cible.</param>
/// <param name="Icon">Icône : emoji ou slug CSS. Peut être <see langword="null"/>.</param>
/// <param name="Category">Catégorie d'affichage.</param>
/// <param name="Order">Ordre au sein de la catégorie.</param>
/// <param name="IsVisible">Si le lien est visible sur la home.</param>
public record UpdateQuickLinkParameters(
    string Label,
    string Url,
    string? Icon,
    QuickLinkCategory Category,
    int Order,
    bool IsVisible);
