using Sts.Domain.Character;

namespace STS.Export;

/// <summary>Génère la fiche personnage version MJ : champs remplis uniquement, avec descriptions.</summary>
public interface IExportDmSheetPdfUseCase
{
    /// <summary>
    /// Génère le PDF version MJ pour un personnage.
    /// Seuls les traits équipés et les capacités acquises sont affichés,
    /// accompagnés de leurs descriptions.
    /// </summary>
    /// <param name="character">Personnage à exporter.</param>
    /// <returns>Contenu du PDF généré.</returns>
    Task<byte[]> ExecuteAsync(Character character);
}
