using Sts.Domain.Character;

namespace STS.Export;

/// <summary>Génère l'export PDF d'une fiche personnage.</summary>
public interface IExportCharacterPdfUseCase
{
    /// <summary>Génère le PDF de la fiche personnage.</summary>
    /// <param name="character">La fiche personnage.</param>
    /// <returns>Le contenu binaire du PDF.</returns>
    Task<byte[]> ExecuteAsync(Character character);
}
