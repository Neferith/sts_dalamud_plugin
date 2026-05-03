using Sts.Domain.Character;

namespace STS.Export;

/// <summary>Génère l'export Discord d'une fiche personnage.</summary>
public interface IExportCharacterDiscordUseCase
{
    /// <summary>Construit le contenu Markdown formaté pour Discord.</summary>
    /// <param name="character">La fiche personnage.</param>
    /// <returns>Le contenu Markdown prêt à l'emploi.</returns>
    string Execute(Character character);
}
