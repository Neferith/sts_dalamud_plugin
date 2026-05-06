using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using Sts.Domain;
using Sts.Domain.Character;
using Sts.Domain.Repository;

namespace STS.Export;


/// <summary>
/// Génère une fiche de job au format PDF (style parchemin JDR).
/// Deux variantes : fiche vierge depuis un job, fiche remplie depuis un personnage.
/// </summary>
public interface IExportJobSheetPdfUseCase
{
    /// <summary>
    /// Génère une fiche vierge pour le job donné.
    /// Toutes les cases/ronds sont vides — destinée à l'impression.
    /// </summary>
    Task<byte[]> ExecuteAsync(string jobId);

    /// <summary>
    /// Génère une fiche remplie depuis la fiche du personnage.
    /// Cases et ronds reflètent les traits et capacités acquis.
    /// </summary>
    Task<byte[]> ExecuteAsync(Character character);
}
