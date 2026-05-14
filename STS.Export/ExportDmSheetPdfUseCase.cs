using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sts.Domain;
using Sts.Domain.Character;
using Sts.Domain.Repository;
using System.Text.RegularExpressions;

namespace STS.Export;

/// <inheritdoc/>
public sealed class ExportDmSheetPdfUseCase(
    TraitRepository traits,
    JobRepository jobs,
    AbilityRepository abilities,
    string uploadDir,
    string imageStoragePath) : IExportDmSheetPdfUseCase
{
    // ── Palette parchemin ─────────────────────────────────────────────────
    private const string Parchment = "#f7f0e0";
    private const string Ink = "#3a2a1a";
    private const string InkLight = "#5a3a1a";
    private const string LineColor = "#c8b89a";
    private const string AmberInk = "#8a6a1a";

    private const string FontSerif = "IM Fell English";

    // ── Contexte interne ──────────────────────────────────────────────────
    private sealed record DmContext(
        Job? Job,
        Character Character,
        Trait? OriginTrait,
        List<Trait> EquippedRoleDpsTraits,
        List<Trait> EquippedRoleTankTraits,
        List<Trait> EquippedRoleSoigneurTraits,
        List<Trait> EquippedConnaissanceTraits,
        List<Trait> EquippedJobTraits,
        List<(Ability Ability, int Level)> EquippedWeaponAbilities,
        List<(Ability Ability, int Level)> EquippedJobAbilities,
        List<(Ability Ability, int Level)> EquippedRoleDpsAbilities,
        List<(Ability Ability, int Level)> EquippedRoleTankAbilities,
        List<(Ability Ability, int Level)> EquippedRoleSoigneurAbilities,
        string? ImagePath,
        string? JobIconPath
    );

    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<byte[]> ExecuteAsync(Character character)
    {
        var job = character.JobId != null ? jobs.GetById(character.JobId) : null;
        var ctx = BuildContext(job, character);
        return Task.FromResult(BuildDocument(ctx).GeneratePdf());
    }

    // ── Construction du contexte ──────────────────────────────────────────

    private DmContext BuildContext(Job? job, Character character)
    {
        var jobId = job?.Id ?? character.JobId ?? "";
        var equippedSet = character.EquippedTraitIds?.ToHashSet() ?? [];
        var equippedMap = character.EquippedAbilities
                            .ToDictionary(e => e.AbilityId, e => e.Level);

        List<Trait> FilterTraits(TraitCategory cat, Func<Trait, bool>? extra = null) =>
            traits.GetByCategory(cat)
                  .Where(t => equippedSet.Contains(t.Id) && (extra == null || extra(t)))
                  .ToList();

        List<(Ability, int)> FilterAbilities(AbilityCategory cat, Func<Ability, bool>? extra = null) =>
            abilities.GetByCategory(cat)
                     .Where(a => equippedMap.ContainsKey(a.Id) && (extra == null || extra(a)))
                     .Select(a => (a, equippedMap[a.Id]))
                     .ToList();

        return new DmContext(
            Job: job,
            Character: character,
            OriginTrait: character.OriginTraitId != null
                ? traits.GetById(character.OriginTraitId)
                : null,
            EquippedRoleDpsTraits: FilterTraits(TraitCategory.RoleDps),
            EquippedRoleTankTraits: FilterTraits(TraitCategory.RoleTank),
            EquippedRoleSoigneurTraits: FilterTraits(TraitCategory.RoleSoigneur),
            EquippedConnaissanceTraits: FilterTraits(TraitCategory.Connaissance),
            EquippedJobTraits: FilterTraits(TraitCategory.Job,
                                            t => t.RequiredJobIds?.Contains(jobId) == true),
            EquippedWeaponAbilities: abilities.GetWeapons()
                .Where(a => equippedMap.ContainsKey(a.Id))
                .Select(a => (a, equippedMap[a.Id]))
                .OrderBy(x => x.a.Name)
                .ToList(),
            EquippedJobAbilities: FilterAbilities(AbilityCategory.Job,
                                      a => a.RequiredJobIds?.Contains(jobId) == true)
                                      .OrderBy(x => x.Item1.StartLevel).ToList(),
            EquippedRoleDpsAbilities: FilterAbilities(AbilityCategory.RoleDps),
            EquippedRoleTankAbilities: FilterAbilities(AbilityCategory.RoleTank),
            EquippedRoleSoigneurAbilities: FilterAbilities(AbilityCategory.RoleSoigneur),
            ImagePath: Directory.GetFiles(uploadDir, $"{character.Id}.*").FirstOrDefault(),
            JobIconPath: ResolveIconPath(job?.IconUrl)
        );
    }

    private string? ResolveIconPath(string? iconUrl)
    {
        if (iconUrl is null) return null;
        try
        {
            var fileName = Path.GetFileName(new Uri(iconUrl).LocalPath);
            var path = Path.Combine(imageStoragePath, fileName);
            return File.Exists(path) ? path : null;
        }
        catch { return null; }
    }

    // ── Document ──────────────────────────────────────────────────────────

    private Document BuildDocument(DmContext ctx) =>
        Document.Create(container =>
        {
            container.Page(page => BuildPage1(page, ctx));
            container.Page(page => BuildPage2(page, ctx));
            container.Page(page => BuildPage3(page, ctx));
        });

    // ══════════════════════════════════════════════════════════════════════
    // PAGE 1 : Header + Traits
    // ══════════════════════════════════════════════════════════════════════

    private static void BuildPage1(PageDescriptor page, DmContext ctx)
    {
        ApplyPageDefaults(page);
        page.Content().Border(2).BorderColor(Ink).Column(col =>
        {
            col.Spacing(0);
            GuildBar(col.Item(), ctx.Character.Name);
            NameBar(col.Item(), ctx);
            JobDescriptionSection(col.Item(), ctx);
            FieldsGrid(col.Item(), ctx);
            HistoireSection(col.Item(), ctx);
            TraitsSection(col.Item(), ctx);
        });
        PageFooter(page, ctx);
    }

    // ══════════════════════════════════════════════════════════════════════
    // PAGE 2 : Capacités acquises avec descriptions
    // ══════════════════════════════════════════════════════════════════════

    private static void BuildPage2(PageDescriptor page, DmContext ctx)
    {
        ApplyPageDefaults(page);
        page.Content().Border(2).BorderColor(Ink).Column(col =>
        {
            col.Spacing(0);
            GuildBarSection(col.Item(), ctx.Character.Name, "Capacités");
            col.Item().Element(e => SectionBanner(e, "Capacités"));

            RenderAbilityGroup(col, "MAÎTRISES D'ARMES", ctx.EquippedWeaponAbilities);
            RenderAbilityGroup(col, (ctx.Job?.Name ?? "Métier").ToUpperInvariant(),
                                                                  ctx.EquippedJobAbilities);
            RenderAbilityGroup(col, "RÔLE DPS", ctx.EquippedRoleDpsAbilities);
            RenderAbilityGroup(col, "RÔLE TANK", ctx.EquippedRoleTankAbilities);
            RenderAbilityGroup(col, "RÔLE SOIGNEUR", ctx.EquippedRoleSoigneurAbilities);
        });
        PageFooter(page, ctx);
    }

    private static void RenderAbilityGroup(
        ColumnDescriptor col,
        string label,
        List<(Ability Ability, int Level)> list)
    {
        if (list.Count == 0) return;

        col.Item()
            .Background("#ede6d4")
            .BorderBottom(0.5f).BorderColor(LineColor)
            .PaddingHorizontal(8).PaddingVertical(3)
            .Text(label)
            .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.13f);

        col.Item().BorderBottom(1).BorderColor(Ink).Padding(8).Column(c =>
        {
            foreach (var (ability, level) in list)
                AbilityDmRow(c.Item(), ability, level);
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // PAGE 3 : Certifications + Inventaire (données réelles uniquement)
    // ══════════════════════════════════════════════════════════════════════

    private void BuildPage3(PageDescriptor page, DmContext ctx)
    {
        ApplyPageDefaults(page);
        page.Content().Border(2).BorderColor(Ink).Column(col =>
        {
            col.Spacing(0);
            GuildBarSection(col.Item(), ctx.Character.Name, "Certifications & Inventaire");
            CertificationsSection(col.Item(), ctx);
            InventaireSection(col.Item(), ctx);
        });
        PageFooter(page, ctx);
    }

    // ══════════════════════════════════════════════════════════════════════
    // SECTIONS — PAGE 1
    // ══════════════════════════════════════════════════════════════════════

    private static void GuildBar(IContainer container, string charName) =>
        container
            .BorderBottom(1).BorderColor(Ink)
            .PaddingHorizontal(12).PaddingVertical(3)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text($"La Nouvelle Lune · STS — {charName}")
                    .FontSize(7.5f).Italic().FontColor(InkLight);
                row.AutoItem()
                    .Text("✦ Version MJ ✦")
                    .FontSize(7.5f).Italic().FontColor(AmberInk);
            });

    private static void GuildBarSection(IContainer container, string charName, string section) =>
        container
            .BorderBottom(1).BorderColor(Ink)
            .PaddingHorizontal(12).PaddingVertical(3)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text($"La Nouvelle Lune · STS — {charName}")
                    .FontSize(7.5f).Italic().FontColor(InkLight);
                row.AutoItem()
                    .Text($"{section} · MJ")
                    .FontSize(7.5f).Bold().FontColor(AmberInk).LetterSpacing(0.08f);
            });

    private static void NameBar(IContainer container, DmContext ctx) =>
        container
            .BorderBottom(2).BorderColor(Ink)
            .PaddingHorizontal(14).PaddingTop(7).PaddingBottom(5)
            .Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(ctx.Character.Name).FontSize(26).Bold();
                    col.Item().PaddingTop(2)
                        .Text("Nom du personnage")
                        .FontSize(7.5f).FontColor(InkLight).LetterSpacing(0.12f);

                    if (ctx.Job is not null)
                    {
                        col.Item().PaddingTop(5).Row(jobRow =>
                        {
                            if (ctx.JobIconPath is not null)
                            {
                                jobRow.ConstantItem(16).Height(16)
                                    .Image(ctx.JobIconPath).FitArea();
                                jobRow.ConstantItem(5);
                            }
                            jobRow.AutoItem()
                                .Text(ctx.Job.Name)
                                .FontSize(10).FontColor(Ink);
                        });
                    }
                });

                if (ctx.ImagePath is not null)
                {
                    row.ConstantItem(10);
                    row.ConstantItem(72).Height(72)
                        .Border(1).BorderColor(Ink)
                        .Image(ctx.ImagePath).FitArea();
                }
                else if (ctx.JobIconPath is not null)
                {
                    row.ConstantItem(10);
                    row.ConstantItem(60).Height(60)
                        .Image(ctx.JobIconPath).FitArea();
                }
            });

    private static void JobDescriptionSection(IContainer container, DmContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ctx.Job?.Description)) return;
        container
            .BorderBottom(1).BorderColor(LineColor)
            .PaddingHorizontal(14).PaddingVertical(6)
            .Text(StripMarkdown(ctx.Job.Description))
            .FontSize(8.5f).Italic().FontColor(InkLight).LineHeight(1.5f);
    }

    private static void FieldsGrid(IContainer container, DmContext ctx)
    {
        var c = ctx.Character;
        var rank = Rank.Get(c.RankKey);

        container.BorderBottom(2).BorderColor(Ink).Column(rows =>
        {
            rows.Item().Row(row =>
            {
                FieldCell(row.RelativeItem(2).BorderRight(1).BorderColor(Ink),
                    "RACE", c.Race.Label());
                FieldCell(row.RelativeItem(2).BorderRight(1).BorderColor(Ink),
                    "RANG", rank?.Label ?? "—");
                FieldCell(row.RelativeItem(2),
                    "PALIER", rank?.Palier.ToString() ?? "—");
            });
            rows.Item().BorderTop(1).BorderColor(LineColor).Row(row =>
            {
                FieldCell(row.RelativeItem(2).BorderRight(1).BorderColor(Ink),
                    "REROLLS", rank?.Rerolls.ToString() ?? "—");
                FieldCell(row.RelativeItem(2).BorderRight(1).BorderColor(Ink),
                    "RÉPUTATION", c.ReputationLevel.ToString("+#;-#;0"));
                FieldCell(row.RelativeItem(2),
                    "POINTS DE COMPÉTENCE", $"{c.SpentSkillPoints} / {c.SkillPoints}",
                    sub: $"{c.RemainingSkillPoints} restant(s)");
            });
        });
    }

    private static void HistoireSection(IContainer container, DmContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ctx.Character.Histoire)) return;
        container
            .BorderBottom(2).BorderColor(Ink)
            .PaddingHorizontal(14).PaddingTop(5).PaddingBottom(6)
            .Column(col =>
            {
                col.Item().Text("HISTOIRE")
                    .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.14f);
                col.Item().PaddingTop(4)
                    .Text(ctx.Character.Histoire.Trim())
                    .FontSize(8.5f).Italic().FontColor(Ink).LineHeight(1.5f);
            });
    }

    private static void TraitsSection(IContainer container, DmContext ctx)
    {
        var roleTraits = ctx.EquippedRoleDpsTraits
            .Concat(ctx.EquippedRoleTankTraits)
            .Concat(ctx.EquippedRoleSoigneurTraits)
            .ToList();
        var connMetierTraits = ctx.EquippedConnaissanceTraits
            .Concat(ctx.EquippedJobTraits)
            .ToList();

        bool hasAny = ctx.OriginTrait is not null
                   || roleTraits.Count > 0
                   || connMetierTraits.Count > 0;
        if (!hasAny) return;

        container.Column(col =>
        {
            col.Item().Element(e => SectionBanner(e, "Traits", topBorder: true));

            if (ctx.OriginTrait is not null)
                TraitDmRow(col.Item(), ctx.OriginTrait, isOrigin: true);

            if (roleTraits.Count > 0)
            {
                col.Item()
                    .Background("#ede6d4")
                    .BorderBottom(0.5f).BorderColor(LineColor)
                    .PaddingHorizontal(12).PaddingVertical(3)
                    .Text("RÔLES")
                    .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.13f);
                foreach (var t in roleTraits)
                    TraitDmRow(col.Item(), t);
            }

            if (connMetierTraits.Count > 0)
            {
                col.Item()
                    .Background("#ede6d4")
                    .BorderBottom(0.5f).BorderColor(LineColor)
                    .PaddingHorizontal(12).PaddingVertical(3)
                    .Text("CONNAISSANCE & MÉTIER")
                    .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.13f);
                foreach (var t in connMetierTraits)
                    TraitDmRow(col.Item(), t);
            }
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // SECTIONS — PAGE 3
    // ══════════════════════════════════════════════════════════════════════

    private void CertificationsSection(IContainer container, DmContext ctx)
    {
        var certs = ctx.Character.Certifications;
        if (certs.Count == 0) return;

        container
            .BorderBottom(2).BorderColor(Ink)
            .Column(col =>
            {
                col.Item().Element(e => SectionBanner(e, "Certifications"));

                col.Item().PaddingHorizontal(12).PaddingTop(6).PaddingBottom(4).Column(inner =>
                {
                    inner.Item().BorderBottom(1.5f).BorderColor(Ink)
                        .PaddingBottom(3).Row(hdr =>
                        {
                            hdr.RelativeItem(5).Text("Nom")
                                .FontSize(7.5f).FontColor(InkLight).LetterSpacing(0.12f);
                            hdr.RelativeItem(6).Text("Capacité / Trait lié")
                                .FontSize(7.5f).FontColor(InkLight).LetterSpacing(0.12f);
                            hdr.ConstantItem(50).AlignRight().Text("Pts libres")
                                .FontSize(7.5f).FontColor(InkLight).LetterSpacing(0.12f);
                        });

                    foreach (var cert in certs)
                    {
                        inner.Item().BorderBottom(0.5f).BorderColor(LineColor)
                            .PaddingVertical(3).Row(r =>
                            {
                                r.RelativeItem(5).Text(txt =>
                                {
                                    txt.Span("★ ").FontSize(9).FontColor(AmberInk);
                                    txt.Span(cert.Name).FontSize(10);
                                });
                                r.RelativeItem(6).Text(txt =>
                                {
                                    txt.Span("→ ").FontSize(9).FontColor(AmberInk);
                                    if (cert.LinkedOriginTraitId != null)
                                    {
                                        var t = traits.GetById(cert.LinkedOriginTraitId);
                                        txt.Span(t?.Name ?? cert.LinkedOriginTraitId).FontSize(9.5f);
                                    }
                                    else if (cert.LinkedAbilityId != null)
                                    {
                                        var a = abilities.GetById(cert.LinkedAbilityId);
                                        txt.Span(a?.Name ?? cert.LinkedAbilityId).FontSize(9.5f);
                                    }
                                });
                                r.ConstantItem(50).AlignRight()
                                    .Text(cert.FreePoints > 0 ? $"+{cert.FreePoints}" : "—")
                                    .FontSize(10).Bold()
                                    .FontColor(cert.FreePoints > 0 ? AmberInk : InkLight);
                            });
                    }
                });
            });
    }

    private static void InventaireSection(IContainer container, DmContext ctx)
    {
        var items = ctx.Character.Inventory.OrderBy(i => i.SortIndex).ToList();
        if (items.Count == 0) return;

        container.Column(col =>
        {
            col.Item().Element(e => SectionBanner(e, "Inventaire", topBorder: false));

            col.Item().PaddingHorizontal(12).PaddingTop(6).PaddingBottom(8).Column(inner =>
            {
                inner.Item().BorderBottom(1.5f).BorderColor(Ink)
                    .PaddingBottom(3).Row(hdr =>
                    {
                        hdr.RelativeItem().Text("Objet")
                            .FontSize(7.5f).FontColor(InkLight).LetterSpacing(0.12f);
                        hdr.ConstantItem(60).Text("Type")
                            .FontSize(7.5f).FontColor(InkLight).LetterSpacing(0.12f);
                    });

                foreach (var item in items)
                {
                    bool equipped = ctx.Character.MainHandItemId == item.Id
                                  || ctx.Character.OffHandItemId == item.Id;
                    var typeLabel = item.Category == ItemCategory.Weapon ? "Arme" : "Équipement";

                    inner.Item().BorderBottom(0.5f).BorderColor(LineColor)
                        .PaddingVertical(3).Row(r =>
                        {
                            r.RelativeItem().Row(nr =>
                            {
                                if (equipped)
                                {
                                    nr.ConstantItem(8).Height(8).Svg(_ => EquipDotSvg());
                                    nr.ConstantItem(4);
                                }
                                nr.RelativeItem().Text(item.Name).FontSize(10);
                            });
                            r.ConstantItem(60).Text(typeLabel)
                                .FontSize(8.5f).FontColor(InkLight);
                        });
                }

                inner.Item().PaddingTop(4).Row(leg =>
                {
                    leg.ConstantItem(8).Height(8).Svg(_ => EquipDotSvg());
                    leg.ConstantItem(5);
                    leg.AutoItem().Text("= objet équipé en main")
                        .FontSize(7.5f).Italic().FontColor(InkLight);
                });
            });
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // COMPOSANTS SPÉCIFIQUES VERSION MJ
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Ligne de trait : nom + badge usageLimit + description.</summary>
    private static void TraitDmRow(IContainer container, Trait trait, bool isOrigin = false) =>
        container
            .BorderBottom(0.5f).BorderColor(LineColor)
            .PaddingHorizontal(12).PaddingVertical(5)
            .Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(txt =>
                    {
                        if (isOrigin) txt.Span("✦ ").FontSize(9).FontColor(AmberInk);
                        txt.Span(trait.Name).FontSize(10).Bold();
                    });
                    if (trait.UsageLimit != UsageLimit.None)
                    {
                        row.AutoItem()
                            .Border(0.5f).BorderColor(AmberInk)
                            .PaddingHorizontal(5).PaddingVertical(1)
                            .Text(UsageLimitLabel(trait.UsageLimit))
                            .FontSize(7.5f).FontColor(AmberInk);
                    }
                });

                if (!string.IsNullOrWhiteSpace(trait.Description))
                {
                    col.Item().PaddingTop(3)
                        .Text(StripMarkdown(trait.Description))
                        .FontSize(8.5f).Italic().FontColor(InkLight).LineHeight(1.4f);
                }
            });

    /// <summary>
    /// Bloc capacité : dots de progression + nom + badge + description globale
    /// + descriptions de chaque niveau acquis.
    /// </summary>
    private static void AbilityDmRow(IContainer container, Ability ability, int acquiredLevel)
    {
        const float dotSize = 13f;
        const float gap = 3f;

        container
            .BorderBottom(0.5f).BorderColor(LineColor)
            .PaddingVertical(5)
            .Column(col =>
            {
                // En-tête : dots + nom + badge usageLimit
                col.Item().Row(row =>
                {
                    for (int lvl = 1; lvl <= ability.MaxLevel; lvl++)
                    {
                        if (lvl < ability.StartLevel)
                        {
                            row.ConstantItem(dotSize);
                        }
                        else
                        {
                            bool filled = lvl <= acquiredLevel;
                            int lvlCopy = lvl;
                            // Layers : cercle SVG + chiffre natif QuestPDF (cross-platform)
                            row.ConstantItem(dotSize).Height(dotSize)
                                .Layers(layers =>
                                {
                                    layers.Layer().Svg(_ => CircleOnlySvg(dotSize, filled));
                                    layers.PrimaryLayer().AlignCenter().AlignMiddle()
                                        .Text(lvlCopy.ToString()).FontSize(7).Bold()
                                        .FontColor(filled ? Parchment : Ink);
                                });
                        }
                        if (lvl < ability.MaxLevel) row.ConstantItem(gap);
                    }

                    row.ConstantItem(6);
                    row.RelativeItem().Text(ability.Name).FontSize(10).Bold();

                    if (ability.UsageLimit != UsageLimit.None)
                    {
                        row.ConstantItem(4);
                        row.AutoItem()
                            .Border(0.5f).BorderColor(AmberInk)
                            .PaddingHorizontal(5).PaddingVertical(1)
                            .Text(UsageLimitLabel(ability.UsageLimit))
                            .FontSize(7.5f).FontColor(AmberInk);
                    }
                });

                // Description globale
                if (!string.IsNullOrWhiteSpace(ability.Description))
                {
                    col.Item().PaddingTop(3)
                        .Text(StripMarkdown(ability.Description))
                        .FontSize(8.5f).Italic().FontColor(InkLight).LineHeight(1.4f);
                }

                // Description par niveau acquis
                foreach (var lvlData in ability.Levels
                    .Where(l => l.Level <= acquiredLevel && !string.IsNullOrWhiteSpace(l.Description)))
                {
                    int lvlCopy = lvlData.Level;
                    col.Item().PaddingTop(4).Row(r =>
                    {
                        // Layers : cercle SVG + chiffre natif QuestPDF (cross-platform)
                        r.ConstantItem(dotSize).Height(dotSize)
                            .Layers(layers =>
                            {
                                layers.Layer().Svg(_ => CircleOnlySvg(dotSize, filled: true));
                                layers.PrimaryLayer().AlignCenter().AlignMiddle()
                                    .Text(lvlCopy.ToString()).FontSize(7).Bold()
                                    .FontColor(Parchment);
                            });
                        r.ConstantItem(6);
                        r.RelativeItem()
                            .Text(StripMarkdown(lvlData.Description))
                            .FontSize(8.5f).FontColor(Ink).LineHeight(1.4f);
                    });
                }
            });
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS COMMUNS
    // ══════════════════════════════════════════════════════════════════════

    private static void ApplyPageDefaults(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(1.6f, Unit.Centimetre);
        page.Background(Parchment);
        page.DefaultTextStyle(t => t.FontFamily(FontSerif).FontSize(9.5f).FontColor(Ink));
    }

    private static void SectionBanner(IContainer container, string text, bool topBorder = false)
    {
        var c = container;
        if (topBorder) c = c.BorderTop(2).BorderColor(Ink);
        c.Background(Ink)
            .PaddingHorizontal(12).PaddingVertical(3)
            .Text(text.ToUpperInvariant())
            .FontSize(8).Bold().FontColor(Parchment).LetterSpacing(0.16f);
    }

    private static void FieldCell(IContainer container, string label, string value, string? sub = null) =>
        container
            .PaddingHorizontal(10).PaddingVertical(4)
            .Column(c =>
            {
                c.Item().Text(label).FontSize(7.5f).FontColor(InkLight).LetterSpacing(0.12f);
                c.Item().PaddingTop(2).Text(value).FontSize(13).Bold();
                if (sub != null)
                    c.Item().Text(sub).FontSize(8).Italic().FontColor(InkLight);
            });

    private static void PageFooter(PageDescriptor page, DmContext ctx) =>
        page.Footer()
            .PaddingTop(6)
            .BorderTop(0.5f).BorderColor(LineColor)
            .Row(row =>
            {
                row.RelativeItem().AlignLeft()
                    .Text($"La Nouvelle Lune · STS — {ctx.Character.Name} (MJ)")
                    .FontSize(7.5f).Italic().FontColor(InkLight);
                row.RelativeItem().AlignRight().Text(txt =>
                {
                    txt.CurrentPageNumber().FontSize(7.5f).FontColor(InkLight);
                    txt.Span(" / ").FontSize(7.5f).FontColor(InkLight);
                    txt.TotalPages().FontSize(7.5f).FontColor(InkLight);
                });
            });

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS — SVG
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cercle SVG sans texte. Le chiffre est rendu en natif QuestPDF via Layers
    /// pour garantir le bon rendu cross-platform (Linux / Docker).
    /// </summary>
    private static string CircleOnlySvg(float size, bool filled)
    {
        float cx = size / 2f, cy = size / 2f, r = size / 2f - 1.5f;
        var fill = filled ? Ink : "none";
        return $"""
            <svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}'>
              <circle cx='{cx:F1}' cy='{cy:F1}' r='{r:F1}'
                      fill='{fill}' stroke='{Ink}' stroke-width='1.5'/>
            </svg>
            """;
    }

    private static string EquipDotSvg() =>
        $"""
        <svg xmlns='http://www.w3.org/2000/svg' width='8' height='8'>
          <circle cx='4' cy='4' r='3' fill='{Ink}'/>
        </svg>
        """;

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS — LABELS & TEXTE
    // ══════════════════════════════════════════════════════════════════════

    private static string UsageLimitLabel(UsageLimit limit) => limit switch
    {
        UsageLimit.OncePerCombat => "1× par combat",
        UsageLimit.TwicePerCombat => "2× par combat",
        UsageLimit.OncePerEvent => "1× par évén.",
        UsageLimit.TwicePerEvent => "2× par évén.",
        UsageLimit.ThreeTimesPerEvent => "3× par évén.",
        _ => string.Empty,
    };

    private static string StripMarkdown(string? md)
    {
        if (string.IsNullOrWhiteSpace(md)) return string.Empty;
        md = Regex.Replace(md, @"^#{1,6}\s+", "", RegexOptions.Multiline);
        md = Regex.Replace(md, @"\*{1,3}(.+?)\*{1,3}", "$1", RegexOptions.Singleline);
        md = Regex.Replace(md, @"_{1,3}(.+?)_{1,3}", "$1", RegexOptions.Singleline);
        md = Regex.Replace(md, @"^---+\s*$", "", RegexOptions.Multiline);
        md = Regex.Replace(md, @"\n{3,}", "\n\n");
        return md.Trim();
    }
}
