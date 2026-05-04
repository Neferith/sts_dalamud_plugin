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
    // ── Palette — thème site (bleu-nuit + teal) ───────────────────────────
    private const string ColorTeal = "#4ec3c4";
    private const string ColorTealLight = "#1a3535";   // teal-dim approx
    private const string ColorTealBorder = "#2a5555";   // border-strong approx
    private const string ColorAmber = "#e8a94a";
    private const string ColorAmberLight = "#2a2010";   // amber-dim approx
    private const string ColorAmberBorder = "#5a4010";
    private const string ColorPurple = "#9b6fd4";
    private const string ColorPurpleLight = "#1e1535";   // purple-dim approx
    private const string ColorPurpleBorder = "#3a2560";
    private const string ColorSuccess = "#4ec9a0";
    private const string ColorBg = "#141a24";   // bg-deep
    private const string ColorBgCard = "#202d3f";   // bg-card
    private const string ColorBgSurface = "#1c2535";   // bg-surface
    private const string ColorBorder = "#1e3d3d";   // border approx
    private const string ColorText = "#e8f0f8";   // text-primary
    private const string ColorTextSecond = "#8ca5be";   // text-secondary
    private const string ColorTextMuted = "#4d6680";   // text-muted

    /// <inheritdoc/>
    public Task<byte[]> ExecuteAsync(Character character)
    {
        var rank = Rank.Get(character.RankKey);
        var job = character.JobId != null ? jobs.GetById(character.JobId) : null;

        var certByTrait = character.Certifications
            .Where(c => c.LinkedOriginTraitId != null)
            .ToDictionary(c => c.LinkedOriginTraitId!, c => c);
        var certByAbility = character.Certifications
            .Where(c => c.LinkedAbilityId != null)
            .ToDictionary(c => c.LinkedAbilityId!, c => c);

        var imageFile = Directory.GetFiles(uploadDir, $"{character.Id}.*").FirstOrDefault();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.Background(ColorBg);
                page.DefaultTextStyle(t => t
                    .FontSize(9.5f)
                    .FontFamily("Arial")
                    .FontColor(ColorText));

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    // ── En-tête ───────────────────────────────────────────
                    col.Item().ShowEntire()
                        .Background(ColorBgCard)
                        .Border(0.5f).BorderColor(ColorBorder)
                        .Padding(12)
                        .Row(row =>
                        {
                            if (imageFile is not null)
                            {
                                row.ConstantItem(72)
                                    .Border(1).BorderColor(ColorTealBorder)
                                    .Image(imageFile).FitArea();
                                row.ConstantItem(14);
                            }

                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().Text(character.Name)
                                    .FontSize(18).Bold().FontColor(ColorText);

                                inner.Item().PaddingTop(2).Text(txt =>
                                {
                                    txt.Span(character.Race.Label()).FontSize(10).FontColor(ColorTextSecond);
                                    txt.Span("  ·  ").FontColor(ColorTextMuted);
                                    txt.Span(rank.Label).FontSize(10).FontColor(ColorTextSecond);
                                    txt.Span("  ·  ").FontColor(ColorTextMuted);
                                    txt.Span($"Palier {rank.Palier}+").FontSize(10).FontColor(ColorTextSecond);
                                    txt.Span("  ·  ").FontColor(ColorTextMuted);
                                    txt.Span($"{rank.Rerolls} reroll(s)").FontSize(10).FontColor(ColorTextSecond);
                                    if (job != null)
                                    {
                                        txt.Span("  ·  ").FontColor(ColorTextMuted);
                                        txt.Span(job.Name).FontSize(10).FontColor(ColorTextSecond);
                                    }
                                });

                                inner.Item().PaddingTop(8).Row(sr =>
                                {
                                    sr.RelativeItem().Element(e => StatBox(e,
                                        "POINTS DE COMPÉTENCE",
                                        $"{character.SpentSkillPoints} / {character.SkillPoints} pts"));
                                    sr.ConstantItem(6);
                                    sr.RelativeItem().Element(e => StatBox(e,
                                        "RÉPUTATION",
                                        $"{Reputation.GetLabel(character.ReputationLevel)} ({(character.ReputationLevel >= 0 ? "+" : "")}{character.ReputationLevel})"));
                                    if (character.Inventory.Count > 0)
                                    {
                                        var wc = character.Inventory.Count(i => i.Category == ItemCategory.Weapon);
                                        var ic = character.Inventory.Count(i => i.Category == ItemCategory.Item);
                                        sr.ConstantItem(6);
                                        sr.RelativeItem().Element(e => StatBox(e,
                                            "INVENTAIRE",
                                            $"{wc} arme(s) · {ic} objet(s)",
                                            ColorTeal));
                                    }
                                });
                            });
                        });

                    // ── Histoire ──────────────────────────────────────────
                    if (!string.IsNullOrWhiteSpace(character.Histoire))
                    {
                        col.Item().ShowEntire()
                            .Background(ColorBgCard)
                            .BorderLeft(2.5f).BorderColor(ColorTealBorder)
                            .Padding(10)
                            .Text(character.Histoire.Trim())
                            .FontSize(9).FontColor(ColorTextSecond).Italic();
                    }

                    // ── Grille 2 colonnes ─────────────────────────────────
                    col.Item().Row(mainRow =>
                    {
                        // ── Colonne gauche : certifications + compétences ──
                        mainRow.RelativeItem().Column(left =>
                        {
                            left.Spacing(6);

                            // Certifications
                            if (character.Certifications.Count > 0)
                            {
                                left.Item().Element(e => SectionTitle(e, $"Certifications ({character.Certifications.Count})"));

                                foreach (var pair in character.Certifications.Chunk(2))
                                {
                                    left.Item().ShowEntire().Row(pr =>
                                    {
                                        pr.RelativeItem()
                                            .Background(ColorAmberLight)
                                            .Border(0.5f).BorderColor(ColorAmberBorder)
                                            .Padding(6)
                                            .Element(e => CertContent(e, pair[0], traits, abilities));
                                        if (pair.Length > 1)
                                        {
                                            pr.ConstantItem(5);
                                            pr.RelativeItem()
                                                .Background(ColorAmberLight)
                                                .Border(0.5f).BorderColor(ColorAmberBorder)
                                                .Padding(6)
                                                .Element(e => CertContent(e, pair[1], traits, abilities));
                                        }
                                        else
                                        {
                                            pr.ConstantItem(5);
                                            pr.RelativeItem();
                                        }
                                    });
                                }
                            }

                            // Compétences
                            if (character.EquippedAbilities.Count > 0)
                            {
                                left.Item().PaddingTop(4)
                                    .Element(e => SectionTitle(e, $"Compétences ({character.SpentSkillPoints} / {character.SkillPoints} pts)"));

                                foreach (var rang in new[] { 1, 2, 3 })
                                {
                                    var groupe = character.EquippedAbilities
                                        .Where(eq => eq.Level == rang).ToList();
                                    if (groupe.Count == 0) continue;

                                    var (rc, rb, rbo) = rang switch
                                    {
                                        2 => (ColorPurple, ColorPurpleLight, ColorPurpleBorder),
                                        3 => (ColorAmber, ColorAmberLight, ColorAmberBorder),
                                        _ => (ColorTeal, ColorTealLight, ColorTealBorder),
                                    };

                                    left.Item().Text($"RANG {rang}")
                                        .FontSize(7.5f).Bold().FontColor(rc).LetterSpacing(0.08f);

                                    foreach (var eq in groupe)
                                    {
                                        var ab = abilities.GetById(eq.AbilityId);
                                        if (ab is null) continue;

                                        var levelDesc = ab.Levels
                                            .Where(l => l.Level <= eq.Level)
                                            .OrderBy(l => l.Level).ToList();

                                        left.Item().ShowEntire()
                                            .Background(rb)
                                            .BorderLeft(2).BorderColor(rc)
                                            .PaddingLeft(8).PaddingRight(6).PaddingVertical(5)
                                            .Column(card =>
                                            {
                                                card.Item().Text(txt =>
                                                {
                                                    txt.Span(ab.Name).Bold().FontSize(9.5f);
                                                    if (ab.UsageLimit != UsageLimit.None)
                                                    {
                                                        txt.Span("  ");
                                                        txt.Span(UsageLimitLabel(ab.UsageLimit))
                                                            .FontSize(7.5f).FontColor(ColorAmber);
                                                    }
                                                });

                                                if (certByAbility.TryGetValue(eq.AbilityId, out var abCert) && abCert.FreePoints > 0)
                                                    card.Item().PaddingTop(1)
                                                        .Text($"★ {abCert.FreePoints} pt(s) certif. — {abCert.Name}")
                                                        .FontSize(7.5f).FontColor(ColorSuccess);

                                                foreach (var ld in levelDesc)
                                                {
                                                    if (string.IsNullOrWhiteSpace(ld.Description)) continue;
                                                    card.Item().PaddingTop(2).Text(txt =>
                                                    {
                                                        if (eq.Level > 1)
                                                            txt.Span($"Lv{ld.Level} — ").FontSize(8).FontColor(ColorTeal);
                                                        txt.Span(ld.Description.Trim()).FontSize(8.5f).FontColor(ColorTextSecond);
                                                    });
                                                }
                                            });
                                    }
                                }
                            }
                        });

                        mainRow.ConstantItem(12);

                        // ── Colonne droite : trait d'origine + traits ──────
                        mainRow.RelativeItem().Column(right =>
                        {
                            right.Spacing(6);

                            // Trait d'origine
                            if (character.OriginTraitId != null)
                            {
                                var originTrait = traits.GetById(character.OriginTraitId);
                                var hasCert = certByTrait.ContainsKey(character.OriginTraitId);

                                right.Item().Element(e => SectionTitle(e, "Trait d'origine"));
                                right.Item().ShowEntire()
                                    .Background(ColorAmberLight)
                                    .BorderLeft(2).BorderColor(ColorAmber)
                                    .PaddingLeft(8).PaddingRight(6).PaddingVertical(5)
                                    .Column(card =>
                                    {
                                        card.Item().Text(txt =>
                                        {
                                            txt.Span(originTrait?.Name ?? character.OriginTraitId).Bold().FontSize(9.5f);
                                            if (hasCert)
                                                txt.Span("  ★ Certifié").FontSize(7.5f).FontColor(ColorAmber);
                                        });
                                        if (!string.IsNullOrWhiteSpace(originTrait?.Description))
                                            card.Item().PaddingTop(2)
                                                .Text(originTrait.Description.Trim())
                                                .FontSize(8.5f).FontColor(ColorTextSecond);
                                    });
                            }

                            // Traits équipés
                            if (character.EquippedTraitIds.Count > 0)
                            {
                                right.Item().PaddingTop(4)
                                    .Element(e => SectionTitle(e, $"Traits ({character.EquippedTraitIds.Count} / {rank.Traits})"));

                                foreach (var tid in character.EquippedTraitIds)
                                {
                                    var t = traits.GetById(tid);
                                    right.Item().ShowEntire()
                                        .Background(ColorBgCard)
                                        .Border(0.5f).BorderColor(ColorBorder)
                                        .Padding(6)
                                        .Row(tr =>
                                        {
                                            tr.ConstantItem(14).PaddingTop(3).Column(dc =>
                                                dc.Item().Width(6).Height(6).Background(ColorTeal));
                                            tr.RelativeItem().Column(tc =>
                                            {
                                                tc.Item().Text(txt =>
                                                {
                                                    txt.Span(t?.Name ?? tid).Bold().FontSize(9.5f);
                                                    if (t?.Category is { } cat)
                                                        txt.Span($"  {cat}").FontSize(7.5f).FontColor(ColorTextMuted);
                                                });
                                                if (!string.IsNullOrWhiteSpace(t?.Description))
                                                    tc.Item().PaddingTop(2)
                                                        .Text(t.Description.Trim())
                                                        .FontSize(8.5f).FontColor(ColorTextSecond);
                                            });
                                        });
                                }
                            }
                        });
                    });

                    // ── Inventaire ────────────────────────────────────────
                    if (character.Inventory.Count > 0)
                    {
                        var weapons = character.Inventory
                            .Where(i => i.Category == ItemCategory.Weapon)
                            .OrderBy(i => i.SortIndex).ToList();
                        var invItems = character.Inventory
                            .Where(i => i.Category == ItemCategory.Item)
                            .OrderBy(i => i.SortIndex).ToList();

                        col.Item().Element(e => SectionTitle(e, "Inventaire"));

                        if (weapons.Count > 0)
                        {
                            col.Item().Text("ARMES")
                                .FontSize(7.5f).Bold().FontColor(ColorTeal).LetterSpacing(0.08f);

                            foreach (var group in weapons.Chunk(4))
                            {
                                col.Item().ShowEntire().Row(row =>
                                {
                                    for (int i = 0; i < 4; i++)
                                    {
                                        if (i > 0) row.ConstantItem(5);
                                        if (i < group.Length)
                                        {
                                            var w = group[i];
                                            var isMain = character.MainHandItemId == w.Id;
                                            var isOff = character.OffHandItemId == w.Id;
                                            var unm = character.IsWeaponUnmastered(w);

                                            row.RelativeItem()
                                                .Background(isMain || isOff ? ColorTealLight : ColorBgCard)
                                                .Border(isMain || isOff ? 1f : 0.5f)
                                                .BorderColor(isMain || isOff ? ColorTealBorder : ColorBorder)
                                                .Padding(7)
                                                .Column(card =>
                                                {
                                                    card.Item().AlignCenter().Text("⚔").FontSize(13);
                                                    card.Item().PaddingTop(3).AlignCenter()
                                                        .Text(w.Name).FontSize(8).Bold();
                                                    if (isMain)
                                                        card.Item().AlignCenter()
                                                            .Text("Main princ.").FontSize(7).FontColor(ColorTeal);
                                                    else if (isOff)
                                                        card.Item().AlignCenter()
                                                            .Text("Main sec.").FontSize(7).FontColor(ColorTextMuted);
                                                    if (unm)
                                                        card.Item().AlignCenter()
                                                            .Text("⚠ Palier 8").FontSize(7).FontColor(ColorAmber);
                                                });
                                        }
                                        else
                                        {
                                            row.RelativeItem();
                                        }
                                    }
                                });
                            }
                        }

                        if (invItems.Count > 0)
                        {
                            col.Item().PaddingTop(6).Text("OBJETS DIVERS")
                                .FontSize(7.5f).Bold().FontColor(ColorTextMuted).LetterSpacing(0.08f);

                            foreach (var group in invItems.Chunk(4))
                            {
                                col.Item().ShowEntire().Row(row =>
                                {
                                    for (int i = 0; i < 4; i++)
                                    {
                                        if (i > 0) row.ConstantItem(5);
                                        if (i < group.Length)
                                        {
                                            var item = group[i];
                                            row.RelativeItem()
                                                .Background(ColorBgCard)
                                                .Border(0.5f).BorderColor(ColorBorder)
                                                .Padding(7)
                                                .Column(card =>
                                                {
                                                    card.Item().AlignCenter()
                                                        .Text("◈").FontSize(13).FontColor(ColorTextMuted);
                                                    card.Item().PaddingTop(3).AlignCenter()
                                                        .Text(item.Name).FontSize(8).Bold();
                                                });
                                        }
                                        else
                                        {
                                            row.RelativeItem();
                                        }
                                    }
                                });
                            }
                        }
                    }
                });

                page.Footer()
                    .PaddingTop(8)
                    .BorderTop(0.5f).BorderColor(ColorBorder)
                    .Row(row =>
                    {
                        row.RelativeItem().AlignLeft()
                            .Text("Nouvelle Lune — STS")
                            .FontSize(7.5f).FontColor(ColorTextMuted);
                        row.RelativeItem().AlignRight().Text(txt =>
                        {
                            txt.CurrentPageNumber().FontSize(7.5f).FontColor(ColorTextMuted);
                            txt.Span(" / ").FontSize(7.5f).FontColor(ColorTextMuted);
                            txt.TotalPages().FontSize(7.5f).FontColor(ColorTextMuted);
                        });
                    });
            });
        });

        return Task.FromResult(doc.GeneratePdf());
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void SectionTitle(IContainer container, string text) =>
        container
            .PaddingBottom(4)
            .Text(text.ToUpperInvariant())
            .FontSize(8).Bold()
            .FontColor(ColorTextMuted)
            .LetterSpacing(0.08f);

    private static void StatBox(IContainer container, string label, string value, string? valueColor = null) =>
        container
            .Background(ColorBgSurface)
            .Border(0.5f).BorderColor(ColorBorder)
            .Padding(7)
            .Column(c =>
            {
                c.Item().Text(label).FontSize(7).FontColor(ColorTextMuted).LetterSpacing(0.06f);
                c.Item().PaddingTop(2).Text(value)
                    .FontSize(10).Bold()
                    .FontColor(valueColor ?? ColorText);
            });

    private static void CertContent(IContainer container, Certification cert, TraitRepository traits, AbilityRepository abilities) =>
        container.Column(c =>
        {
            c.Item().Text($"★ {cert.Name}").FontSize(8.5f).Bold().FontColor(ColorAmber);
            if (cert.LinkedOriginTraitId != null)
            {
                var t = traits.GetById(cert.LinkedOriginTraitId);
                c.Item().Text($"Trait : {t?.Name ?? cert.LinkedOriginTraitId}")
                    .FontSize(7.5f).FontColor(ColorTextMuted);
            }
            if (cert.LinkedAbilityId != null && cert.FreePoints > 0)
            {
                var a = abilities.GetById(cert.LinkedAbilityId);
                c.Item().Text($"{cert.FreePoints} pt(s) — {a?.Name ?? cert.LinkedAbilityId}")
                    .FontSize(7.5f).FontColor(ColorTextMuted);
            }
        });

    private static string UsageLimitLabel(UsageLimit limit) => limit switch
    {
        UsageLimit.OncePerCombat => "1× / combat",
        UsageLimit.TwicePerCombat => "2× / combat",
        UsageLimit.OncePerEvent => "1× / event",
        UsageLimit.TwicePerEvent => "2× / event",
        UsageLimit.ThreeTimesPerEvent => "3× / event",
        _ => string.Empty,
    };
}
