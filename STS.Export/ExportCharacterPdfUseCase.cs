using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sts.Domain;
using Sts.Domain.Character;
using Sts.Domain.Repository;

namespace STS.Export;

/// <inheritdoc/>
public sealed class ExportCharacterPdfUseCase(
    TraitRepository traits,
    JobRepository jobs,
    AbilityRepository abilities,
    string uploadDir) : IExportCharacterPdfUseCase
{
    /// <inheritdoc/>
    public Task<byte[]> ExecuteAsync(Character character)
    {
        var rank = Rank.Get(character.RankKey);
        var job  = character.JobId != null ? jobs.GetById(character.JobId) : null;

        var certByTrait   = character.Certifications.Where(c => c.LinkedOriginTraitId != null).ToDictionary(c => c.LinkedOriginTraitId!, c => c);
        var certByAbility = character.Certifications.Where(c => c.LinkedAbilityId    != null).ToDictionary(c => c.LinkedAbilityId!,    c => c);

        var imageFile = Directory.GetFiles(uploadDir, $"{character.Id}.*").FirstOrDefault();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    // ── En-tête ───────────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        if (imageFile is not null)
                        {
                            row.ConstantItem(80).Image(imageFile).FitArea();
                            row.ConstantItem(12);
                        }

                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text(character.Name).FontSize(20).Bold();
                            inner.Item().Text($"{character.Race.Label()} — {job?.Name ?? "Sans classe"}")
                                .FontSize(11).FontColor(Colors.Grey.Darken1);
                            inner.Item().PaddingTop(4).Text(txt =>
                            {
                                txt.Span("Rang : ").Bold();
                                txt.Span($"{rank.Label}   ");
                                txt.Span("Palier : ").Bold();
                                txt.Span($"{rank.Palier}+   ");
                                txt.Span("Rerolls : ").Bold();
                                txt.Span($"{rank.Rerolls}   ");
                                txt.Span("Réputation : ").Bold();
                                txt.Span($"{Reputation.GetLabel(character.ReputationLevel)} ({(character.ReputationLevel >= 0 ? "+" : "")}{character.ReputationLevel})");
                            });
                        });
                    });

                    // ── Histoire ──────────────────────────────────────────────
                    if (!string.IsNullOrWhiteSpace(character.Histoire))
                    {
                        col.Item().Column(inner =>
                        {
                            inner.Item().Text("Histoire").FontSize(12).Bold();
                            inner.Item().PaddingTop(4)
                                .Background(Colors.Grey.Lighten3)
                                .Padding(8)
                                .Text(character.Histoire.Trim()).FontSize(9);
                        });
                    }

                    // ── Capacités ─────────────────────────────────────────────
                    if (character.EquippedAbilities.Count > 0)
                    {
                        col.Item().Text("Capacités").FontSize(12).Bold();
                        foreach (var eq in character.EquippedAbilities)
                        {
                            var ab = abilities.GetById(eq.AbilityId);
                            if (ab is null) continue;

                            col.Item().PaddingLeft(8).Column(inner =>
                            {
                                inner.Item().Text(txt =>
                                {
                                    txt.Span($"{ab.Name} Lv{eq.Level}").Bold();
                                    if (certByAbility.TryGetValue(eq.AbilityId, out var abCert) && abCert.FreePoints > 0)
                                        txt.Span($"  ★ {abCert.Name} — {abCert.FreePoints} pt(s) gratuit(s)")
                                           .FontSize(8).FontColor(Colors.Amber.Darken2);
                                });
                                if (ab.UsageLimit != UsageLimit.None)
                                    inner.Item().Text(UsageLimitLabel(ab.UsageLimit))
                                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                                foreach (var ld in ab.Levels.Where(l => l.Level <= eq.Level).OrderBy(l => l.Level))
                                {
                                    if (string.IsNullOrWhiteSpace(ld.Description)) continue;
                                    inner.Item().Text(txt =>
                                    {
                                        if (eq.Level > 1)
                                            txt.Span($"Lv{ld.Level} — ").FontSize(8).FontColor(Colors.Teal.Medium);
                                        txt.Span(ld.Description.Trim()).FontSize(9);
                                    });
                                }
                            });
                        }
                    }

                    // ── Traits ────────────────────────────────────────────────
                    var allTraits = character.EquippedTraitIds.ToList();
                    if (character.OriginTraitId != null) allTraits.Insert(0, character.OriginTraitId);
                    if (allTraits.Count > 0)
                    {
                        col.Item().Text("Traits").FontSize(12).Bold();
                        foreach (var tid in allTraits)
                        {
                            var t = traits.GetById(tid);
                            col.Item().PaddingLeft(8).Column(inner =>
                            {
                                inner.Item().Text(txt =>
                                {
                                    txt.Span(t?.Name ?? tid).Bold();
                                    if (tid == character.OriginTraitId)
                                        txt.Span("  (origine)").FontSize(8).FontColor(Colors.Amber.Darken2);
                                    if (certByTrait.TryGetValue(tid, out var traitCert))
                                        txt.Span($"  ★ {traitCert.Name} — gratuit")
                                           .FontSize(8).FontColor(Colors.Amber.Darken2);
                                });
                                if (!string.IsNullOrWhiteSpace(t?.Description))
                                    inner.Item().Text(t.Description.Trim()).FontSize(9);
                            });
                        }
                    }

                    // ── Certifications ────────────────────────────────────────
                    if (character.Certifications.Count > 0)
                    {
                        col.Item().Text("Certifications").FontSize(12).Bold();
                        foreach (var cert in character.Certifications)
                        {
                            col.Item().PaddingLeft(8).Column(inner =>
                            {
                                inner.Item().Text(cert.Name).Bold();
                                if (cert.LinkedOriginTraitId != null)
                                {
                                    var t = traits.GetById(cert.LinkedOriginTraitId);
                                    inner.Item().Text($"Trait d'origine gratuit : {t?.Name ?? cert.LinkedOriginTraitId}")
                                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                                }
                                if (cert.LinkedAbilityId != null && cert.FreePoints > 0)
                                {
                                    var a = abilities.GetById(cert.LinkedAbilityId);
                                    inner.Item().Text($"{cert.FreePoints} pt(s) gratuit(s) sur : {a?.Name ?? cert.LinkedAbilityId}")
                                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                                }
                            });
                        }
                    }

                    // ── Inventaire ────────────────────────────────────────────
                    if (character.Inventory.Count > 0)
                    {
                        col.Item().Text("Inventaire").FontSize(12).Bold();
                        var weapons = character.Inventory.Where(i => i.Category == ItemCategory.Weapon).OrderBy(i => i.SortIndex).ToList();
                        var items   = character.Inventory.Where(i => i.Category == ItemCategory.Item).OrderBy(i => i.SortIndex).ToList();

                        foreach (var w in weapons)
                        {
                            col.Item().PaddingLeft(8).Column(inner =>
                            {
                                inner.Item().Text(txt =>
                                {
                                    txt.Span(w.Name).Bold();
                                    txt.Span("  Arme").FontSize(8).FontColor(Colors.Blue.Medium);
                                    if (character.MainHandItemId == w.Id) txt.Span("  Main principale").FontSize(8).FontColor(Colors.Green.Medium);
                                    if (character.OffHandItemId  == w.Id) txt.Span("  Main secondaire").FontSize(8).FontColor(Colors.Green.Medium);
                                    if (character.IsWeaponUnmastered(w))  txt.Span("  ⚠ Palier 8").FontSize(8).FontColor(Colors.Red.Medium);
                                });
                                if (!string.IsNullOrWhiteSpace(w.Description))
                                    inner.Item().Text(w.Description.Trim()).FontSize(9);
                            });
                        }

                        foreach (var item in items)
                        {
                            col.Item().PaddingLeft(8).Column(inner =>
                            {
                                inner.Item().Text(item.Name).Bold();
                                if (!string.IsNullOrWhiteSpace(item.Description))
                                    inner.Item().Text(item.Description.Trim()).FontSize(9);
                            });
                        }
                    }
                });

                page.Footer().AlignRight().Text(txt =>
                {
                    txt.Span("Nouvelle Lune — STS · ").FontSize(8).FontColor(Colors.Grey.Medium);
                    txt.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    txt.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                    txt.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return Task.FromResult(doc.GeneratePdf());
    }

    private static string UsageLimitLabel(UsageLimit limit) => limit switch
    {
        UsageLimit.OncePerCombat      => "⏱ 1× par combat",
        UsageLimit.TwicePerCombat     => "⏱ 2× par combat",
        UsageLimit.OncePerEvent       => "⏱ 1× par event",
        UsageLimit.TwicePerEvent      => "⏱ 2× par event",
        UsageLimit.ThreeTimesPerEvent => "⏱ 3× par event",
        _                             => string.Empty,
    };
}
