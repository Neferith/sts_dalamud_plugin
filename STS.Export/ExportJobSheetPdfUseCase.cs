using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sts.Domain;
using Sts.Domain.Character;
using Sts.Domain.Repository;

namespace STS.Export;

/// <inheritdoc/>
public sealed class ExportJobSheetPdfUseCase(
    TraitRepository traits,
    JobRepository jobs,
    AbilityRepository abilities,
    string uploadDir,
    string imageStoragePath) : IExportJobSheetPdfUseCase
{
    // ── Palette parchemin ─────────────────────────────────────────────────
    private const string Parchment = "#f7f0e0";
    private const string Ink = "#3a2a1a";
    private const string InkLight = "#5a3a1a";
    private const string LineColor = "#c8b89a";
    private const string AmberInk = "#8a6a1a";

    // ── Typographie ───────────────────────────────────────────────────────
    private const string FontSerif = "IM Fell English";

    /// <summary>
    /// Enregistre les polices embarquées dans l'assembly.
    /// À appeler une fois au démarrage de l'application (Program.cs).
    /// </summary>
    public static void RegisterFonts()
    {
        var assembly = typeof(ExportJobSheetPdfUseCase).Assembly;

        // Mappe le nom de fichier (sans extension) vers le nom de famille QuestPDF
        var fontMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["IMFellEnglish-Regular"] = "IM Fell English",
            ["IMFellEnglish-Italic"] = "IM Fell English",
        };

        foreach (var resourceName in assembly.GetManifestResourceNames()
                     .Where(n => n.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)))
        {
            // "STS.Export.Fonts.IMFellEnglish-Regular.ttf" → segments[^2] = "IMFellEnglish-Regular"
            var segments = resourceName.Split('.');
            var baseName = segments.Length >= 2 ? segments[^2] : resourceName;

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            if (fontMap.TryGetValue(baseName, out var familyName))
                FontManager.RegisterFontWithCustomName(familyName, stream);
            else
                FontManager.RegisterFont(stream);
        }
    }

    // ── Contexte interne ──────────────────────────────────────────────────
    private sealed record SheetContext(
        Job? Job,
        Character? Character,
        // Traits par catégorie
        List<Trait> JobTraits,
        List<Trait> ConnaissanceTraits,
        List<Trait> RoleDpsTraits,
        List<Trait> RoleTankTraits,
        List<Trait> RoleSoigneurTraits,
        // Capacités par catégorie
        List<Ability> WeaponAbilities,
        List<Ability> JobAbilities,
        List<Ability> RoleDpsAbilities,
        List<Ability> RoleTankAbilities,
        List<Ability> RoleSoigneurAbilities,
        // Lookups personnage (vides si fiche vierge)
        HashSet<string> EquippedTraitSet,
        string? OriginTraitId,
        string? OriginTraitName,
        Dictionary<string, int> EquippedAbilityMap,
        // Portrait
        string? ImagePath,
        // Icône du job
        string? JobIconPath
    );

    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<byte[]> ExecuteAsync(string jobId)
    {
        var job = jobs.GetById(jobId)
            ?? throw new ArgumentException($"Job '{jobId}' introuvable.");
        var ctx = BuildContext(job, character: null);
        return Task.FromResult(BuildDocument(ctx).GeneratePdf());
    }

    /// <inheritdoc/>
    public Task<byte[]> ExecuteAsync(Character character)
    {
        var job = character.JobId != null ? jobs.GetById(character.JobId) : null;
        var ctx = BuildContext(job, character);
        return Task.FromResult(BuildDocument(ctx).GeneratePdf());
    }

    // ── Construction du contexte ──────────────────────────────────────────

    private SheetContext BuildContext(Job? job, Character? character)
    {
        var jobId = job?.Id ?? character?.JobId ?? "";

        var originTraitName = character?.OriginTraitId != null
            ? traits.GetById(character.OriginTraitId)?.Name ?? character.OriginTraitId
            : null;

        var imagePath = character != null
            ? Directory.GetFiles(uploadDir, $"{character.Id}.*").FirstOrDefault()
            : null;
        var baseUploadDir = Path.GetDirectoryName(uploadDir) ?? uploadDir;
        var jobIconDir = Path.Combine(uploadDir, "jobs");
        Console.WriteLine($"[ExportPdf] uploadDir={uploadDir}");
        Console.WriteLine($"[ExportPdf] jobIconDir={jobIconDir} exists={Directory.Exists(jobIconDir)}");
        Console.WriteLine($"[ExportPdf] job.Id={job?.Id} job.IconUrl={job?.IconUrl}");
        var jobIconPath = ResolveIconPath(job?.IconUrl);
        Console.WriteLine($"[ExportPdf] jobIconPath={jobIconPath ?? "NULL"}");


        return new SheetContext(
            Job: job,
            Character: character,
            JobTraits: [.. traits.GetByCategory(TraitCategory.Job)
                                    .Where(t => t.RequiredJobIds?.Contains(jobId) == true)],
            ConnaissanceTraits: [.. traits.GetByCategory(TraitCategory.Connaissance)],
            RoleDpsTraits: [.. traits.GetByCategory(TraitCategory.RoleDps)],
            RoleTankTraits: [.. traits.GetByCategory(TraitCategory.RoleTank)],
            RoleSoigneurTraits: [.. traits.GetByCategory(TraitCategory.RoleSoigneur)],
            WeaponAbilities: [.. abilities.GetWeapons().OrderBy(a => a.Name)],
            JobAbilities: [.. abilities.GetByCategory(AbilityCategory.Job)
                                    .Where(a => a.RequiredJobIds?.Contains(jobId) == true)
                                    .OrderBy(a => a.StartLevel)],
            RoleDpsAbilities: [.. abilities.GetByCategory(AbilityCategory.RoleDps)],
            RoleTankAbilities: [.. abilities.GetByCategory(AbilityCategory.RoleTank)],
            RoleSoigneurAbilities: [.. abilities.GetByCategory(AbilityCategory.RoleSoigneur)],
            EquippedTraitSet: character?.EquippedTraitIds?.ToHashSet() ?? [],
            OriginTraitId: character?.OriginTraitId,
            OriginTraitName: originTraitName,
            EquippedAbilityMap: character?.EquippedAbilities
                                    .ToDictionary(e => e.AbilityId, e => e.Level) ?? [],
            ImagePath: imagePath,
            JobIconPath: jobIconPath
        );
    }


    /// <summary>
    /// Résout l'URL d'une icône de galerie en chemin fichier local.
    /// Extrait le nom de fichier de l'URL et le cherche dans imageStoragePath.
    /// </summary>
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

    private Document BuildDocument(SheetContext ctx) =>
        Document.Create(container =>
        {
            container.Page(page => BuildPage1(page, ctx));  // Header + Traits
            container.Page(page => BuildPage2(page, ctx));  // Capacités (Armes, Job, Rôles)
            container.Page(page => BuildPage3(page, ctx));  // Certifications + Inventaire
        });

    // ── PAGE 1 : Header + Traits ──────────────────────────────────────────

    private static void BuildPage1(PageDescriptor page, SheetContext ctx)
    {
        page.Size(PageSizes.A4);
        page.Margin(1.6f, Unit.Centimetre);
        page.Background(Parchment);
        page.DefaultTextStyle(t => t.FontFamily(FontSerif).FontSize(9.5f).FontColor(Ink));

        page.Content().Border(2).BorderColor(Ink).Column(col =>
        {
            col.Spacing(0);
            GuildBar(col.Item(), ctx);
            NameBar(col.Item(), ctx);
            FieldsGrid(col.Item(), ctx);
            HistoireSection(col.Item(), ctx);
            TraitsSection(col.Item(), ctx);
        });

        PageFooter(page, ctx);
    }

    // ── PAGE 2 : Capacités ───────────────────────────────────────────────

    private static void BuildPage2(PageDescriptor page, SheetContext ctx)
    {
        page.Size(PageSizes.A4);
        page.Margin(1.6f, Unit.Centimetre);
        page.Background(Parchment);
        page.DefaultTextStyle(t => t.FontFamily(FontSerif).FontSize(9.5f).FontColor(Ink));

        page.Content().Border(2).BorderColor(Ink).Column(col =>
        {
            col.Spacing(0);
            GuildBarSection(col.Item(), ctx, "Capacités");
            col.Item().Element(e => SectionBanner(e, "Capacités"));

            // ── Armes & Métier ────────────────────────────────────────────
            col.Item().Row(banner =>
            {
                banner.RelativeItem()
                    .Background("#ede6d4")
                    .BorderRight(0.5f).BorderColor(LineColor)
                    .BorderBottom(0.5f).BorderColor(LineColor)
                    .PaddingHorizontal(8).PaddingVertical(3)
                    .Text("MAÎTRISES D'ARMES")
                    .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.13f);
                banner.RelativeItem()
                    .Background("#ede6d4")
                    .BorderBottom(0.5f).BorderColor(LineColor)
                    .PaddingHorizontal(8).PaddingVertical(3)
                    .Text((ctx.Job?.Name ?? "Métier").ToUpperInvariant())
                    .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.13f);
            });

            col.Item().BorderBottom(1).BorderColor(Ink).Row(row =>
            {
                row.RelativeItem()
                    .BorderRight(1).BorderColor(Ink)
                    .Padding(8)
                    .Column(left =>
                        AbilityGroup(left, "Maîtrises d'armes", ctx.WeaponAbilities,
                            ctx.EquippedAbilityMap));

                row.RelativeItem()
                    .Padding(8)
                    .Column(right =>
                    {
                        if (ctx.JobAbilities.Count > 0)
                            AbilityGroup(right, ctx.Job?.Name ?? "Job", ctx.JobAbilities,
                                ctx.EquippedAbilityMap);
                    });
            });

            // ── Rôles ─────────────────────────────────────────────────────
            col.Item().Row(banner =>
            {
                banner.RelativeItem()
                    .Background("#ede6d4")
                    .BorderRight(0.5f).BorderColor(LineColor)
                    .BorderBottom(0.5f).BorderColor(LineColor)
                    .PaddingHorizontal(8).PaddingVertical(3)
                    .Text("RÔLE DPS")
                    .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.13f);
                banner.RelativeItem()
                    .Background("#ede6d4")
                    .BorderBottom(0.5f).BorderColor(LineColor)
                    .PaddingHorizontal(8).PaddingVertical(3)
                    .Text("RÔLE TANK & SOIGNEUR")
                    .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.13f);
            });

            col.Item().Row(row =>
            {
                row.RelativeItem()
                    .BorderRight(1).BorderColor(Ink)
                    .Padding(8)
                    .Column(left =>
                    {
                        if (ctx.RoleDpsAbilities.Count > 0)
                            AbilityGroup(left, "Rôle DPS", ctx.RoleDpsAbilities,
                                ctx.EquippedAbilityMap);
                    });

                row.RelativeItem()
                    .Padding(8)
                    .Column(right =>
                    {
                        if (ctx.RoleTankAbilities.Count > 0)
                            AbilityGroup(right, "Rôle Tank", ctx.RoleTankAbilities,
                                ctx.EquippedAbilityMap);
                        if (ctx.RoleSoigneurAbilities.Count > 0)
                            AbilityGroup(right, "Rôle Soigneur", ctx.RoleSoigneurAbilities,
                                ctx.EquippedAbilityMap);
                    });
            });
        });

        PageFooter(page, ctx);
    }

    // ── PAGE 3 : Certifications + Inventaire ─────────────────────────────

    private void BuildPage3(PageDescriptor page, SheetContext ctx)
    {
        page.Size(PageSizes.A4);
        page.Margin(1.6f, Unit.Centimetre);
        page.Background(Parchment);
        page.DefaultTextStyle(t => t.FontFamily(FontSerif).FontSize(9.5f).FontColor(Ink));

        page.Content().Border(2).BorderColor(Ink).Column(col =>
        {
            col.Spacing(0);
            GuildBarSection(col.Item(), ctx, "Certifications & Inventaire");
            CertificationsSection(col.Item(), ctx);
            InventaireSection(col.Item(), ctx);
        });

        PageFooter(page, ctx);
    }

    // ══════════════════════════════════════════════════════════════════════
    // SECTIONS — PAGE 1
    // ══════════════════════════════════════════════════════════════════════

    private static void GuildBar(IContainer container, SheetContext ctx) =>
        container
            .BorderBottom(1).BorderColor(Ink)
            .PaddingHorizontal(12).PaddingVertical(3)
            .Row(row =>
            {
                row.RelativeItem().Text("La Nouvelle Lune · STS")
                    .FontSize(7.5f).Italic().FontColor(InkLight);
                row.AutoItem().Text("✦ nlrp.fr ✦")
                    .FontSize(7.5f).Italic().FontColor(InkLight);
            });

    private static void GuildBarSection(IContainer container, SheetContext ctx, string section)
    {
        var charName = ctx.Character?.Name ?? ctx.Job?.Name ?? "";
        container
            .BorderBottom(1).BorderColor(Ink)
            .PaddingHorizontal(12).PaddingVertical(3)
            .Row(row =>
            {
                row.RelativeItem().Text($"La Nouvelle Lune · STS — {charName}")
                    .FontSize(7.5f).Italic().FontColor(InkLight);
                row.AutoItem().Text(section)
                    .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.08f);
            });
    }

    private static void NameBar(IContainer container, SheetContext ctx)
    {
        var name = ctx.Character?.Name ?? ctx.Job?.Name ?? "—";
        var sublabel = ctx.Character != null ? "Nom du personnage" : $"Job · {ctx.Job?.Name ?? ""}";

        container
            .BorderBottom(2).BorderColor(Ink)
            .PaddingHorizontal(14).PaddingTop(7).PaddingBottom(5)
            .Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(name).FontSize(26).Bold();

                    if (ctx.Character != null)
                    {
                        // Fiche personnage : label fixe + ligne job avec icône
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
                    }
                    else
                    {
                        // Fiche vierge : sublabel standard
                       // col.Item().PaddingTop(2)
                         //   .Text($"Métier · {ctx.Job?.Name ?? ""}")
                           // .FontSize(7.5f).FontColor(InkLight).LetterSpacing(0.12f);
                    }
                });

                // Portrait (fiche personnage) — sans icône job ici
                if (ctx.ImagePath is not null)
                {
                    row.ConstantItem(10);
                    row.ConstantItem(72).Height(72)
                        .Border(1).BorderColor(Ink)
                        .Image(ctx.ImagePath).FitArea();
                }
                // Fiche vierge : icône job dans la zone portrait
                else if (ctx.JobIconPath is not null)
                {
                    row.ConstantItem(10);
                    row.ConstantItem(60).Height(60)
                        .Image(ctx.JobIconPath).FitArea();
                }
            });
    }

    private static void FieldsGrid(IContainer container, SheetContext ctx)
    {
        var c = ctx.Character;
        var rank = c != null ? Rank.Get(c.RankKey) : null;

        string RaceVal() => c?.Race.Label() ?? "—";
        string RangVal() => rank?.Label ?? "—";
        string PalierVal() => rank?.Palier.ToString() ?? "—";
        string RerollVal() => rank?.Rerolls.ToString() ?? "—";
        string RepVal() => c != null ? c.ReputationLevel.ToString("+#;-#;0") : "—";
        string PtsVal() => c != null ? $"{c.SpentSkillPoints} / {c.SkillPoints}" : "—";
        string? PtsSub() => c != null ? $"{c.RemainingSkillPoints} restant(s)" : null;

        container.BorderBottom(2).BorderColor(Ink).Column(rows =>
        {
            // Rangée 1 : Race · Rang · Palier
            rows.Item().Row(row =>
            {
                FieldCell(row.RelativeItem(2)
                    .BorderRight(1).BorderColor(Ink), "RACE", RaceVal());
                FieldCell(row.RelativeItem(2)
                    .BorderRight(1).BorderColor(Ink), "RANG", RangVal());
                FieldCell(row.RelativeItem(2), "PALIER", PalierVal());
            });

            // Rangée 2 : Rerolls · Réputation · Points
            rows.Item().BorderTop(1).BorderColor(LineColor).Row(row =>
            {
                FieldCell(row.RelativeItem(2)
                    .BorderRight(1).BorderColor(Ink), "REROLLS", RerollVal());
                FieldCell(row.RelativeItem(2)
                    .BorderRight(1).BorderColor(Ink), "RÉPUTATION", RepVal());
                FieldCell(row.RelativeItem(2), "POINTS DE COMPÉTENCE", PtsVal(),
                    sub: PtsSub());
            });
        });
    }

    private static void HistoireSection(IContainer container, SheetContext ctx)
    {
        container
            .BorderBottom(2).BorderColor(Ink)
            .PaddingHorizontal(14).PaddingTop(5).PaddingBottom(6)
            .Column(col =>
            {
                col.Item().Text("HISTOIRE")
                    .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.14f);

                if (ctx.Character != null && !string.IsNullOrWhiteSpace(ctx.Character.Histoire))
                {
                    // Fiche remplie : texte compact, auto-dimensionné, pas de lignes vides
                    col.Item().PaddingTop(4)
                        .Text(ctx.Character.Histoire.Trim())
                        .FontSize(8.5f).Italic().FontColor(Ink).LineHeight(1.5f);
                }
                else
                {
                    // Fiche vierge : 4 lignes à remplir
                    col.Item().PaddingTop(4).Column(lines =>
                    {
                        for (int i = 0; i < 4; i++)
                            lines.Item().PaddingBottom(9)
                                .BorderBottom(0.5f).BorderColor(LineColor)
                                .Height(1);
                    });
                }
            });
    }

    private static void TraitsSection(IContainer container, SheetContext ctx)
    {
        container.Column(col =>
        {
            col.Item().Element(e => SectionBanner(e, "Traits", topBorder: true));

            // Champ unique Trait d'origine — pleine largeur
            col.Item()
                .BorderBottom(1).BorderColor(LineColor)
                .PaddingHorizontal(12).PaddingVertical(5)
                .Row(row =>
                {
                    row.AutoItem()
                        .Text("TRAIT D'ORIGINE")
                        .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.13f);
                    row.ConstantItem(12);
                    row.RelativeItem()
                        .BorderBottom(0.5f).BorderColor(Ink)
                        .PaddingBottom(1)
                        .Text(ctx.OriginTraitName ?? string.Empty)
                        .FontSize(10).FontColor(Ink);
                });

            // ── Sous-section Rôles (2 colonnes) ──────────────────────────
            col.Item()
                .Background("#ede6d4")
                .BorderBottom(0.5f).BorderColor(LineColor)
                .PaddingHorizontal(12).PaddingVertical(3)
                .Text("RÔLES")
                .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.13f);

            col.Item().BorderBottom(1).BorderColor(Ink).Row(row =>
            {
                row.RelativeItem()
                    .BorderRight(1).BorderColor(LineColor)
                    .Padding(7)
                    .Column(left =>
                        TraitGroup(left, "DPS", ctx.RoleDpsTraits,
                            t => ctx.EquippedTraitSet.Contains(t.Id)));

                row.RelativeItem()
                    .Padding(7)
                    .Column(right =>
                    {
                        if (ctx.RoleTankTraits.Count > 0)
                            TraitGroup(right, "Tank", ctx.RoleTankTraits,
                                t => ctx.EquippedTraitSet.Contains(t.Id));
                        if (ctx.RoleSoigneurTraits.Count > 0)
                            TraitGroup(right, "Soigneur", ctx.RoleSoigneurTraits,
                                t => ctx.EquippedTraitSet.Contains(t.Id));
                    });
            });

            // ── Sous-section Connaissance & Métier (2 colonnes) ──────────
            col.Item()
                .Background("#ede6d4")
                .BorderBottom(0.5f).BorderColor(LineColor)
                .PaddingHorizontal(12).PaddingVertical(3)
                .Text("CONNAISSANCE & MÉTIER")
                .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.13f);

            col.Item().Row(row =>
            {
                row.RelativeItem()
                    .BorderRight(1).BorderColor(LineColor)
                    .Padding(7)
                    .Column(left =>
                    {
                        if (ctx.ConnaissanceTraits.Count > 0)
                            TraitGroup(left, "Connaissance", ctx.ConnaissanceTraits,
                                t => ctx.EquippedTraitSet.Contains(t.Id));
                    });

                row.RelativeItem()
                    .Padding(7)
                    .Column(right =>
                    {
                        if (ctx.JobTraits.Count > 0)
                            TraitGroup(right, ctx.Job?.Name ?? "Métier", ctx.JobTraits,
                                t => ctx.EquippedTraitSet.Contains(t.Id));
                    });
            });
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // SECTIONS — PAGE 4
    // ══════════════════════════════════════════════════════════════════════

    private void CertificationsSection(IContainer container, SheetContext ctx)
    {
        const int emptySlots = 10;

        container
            .BorderBottom(2).BorderColor(Ink)
            .Column(col =>
            {
                col.Item().Element(e => SectionBanner(e, "Certifications"));

                col.Item().PaddingHorizontal(12).PaddingTop(6).PaddingBottom(4).Column(inner =>
                {
                    // En-tête colonnes
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

                    // Certifications existantes
                    foreach (var cert in ctx.Character?.Certifications ?? [])
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
                                    .FontSize(10).Bold().FontColor(cert.FreePoints > 0 ? AmberInk : InkLight);
                            });
                    }

                    // Lignes vides
                    for (int i = 0; i < emptySlots; i++)
                        inner.Item().BorderBottom(0.5f).BorderColor(LineColor)
                            .PaddingVertical(3).Height(20);
                });
            });
    }

    private static void InventaireSection(IContainer container, SheetContext ctx)
    {
        const int emptySlots = 6;

        container.Column(col =>
        {
            col.Item().Element(e => SectionBanner(e, "Inventaire", topBorder: false));

            col.Item().PaddingHorizontal(12).PaddingTop(6).PaddingBottom(8).Column(inner =>
            {
                // En-tête
                inner.Item().BorderBottom(1.5f).BorderColor(Ink)
                    .PaddingBottom(3).Row(hdr =>
                    {
                        hdr.RelativeItem().Text("Objet")
                            .FontSize(7.5f).FontColor(InkLight).LetterSpacing(0.12f);
                        hdr.ConstantItem(60).Text("Type")
                            .FontSize(7.5f).FontColor(InkLight).LetterSpacing(0.12f);
                    });

                var items = ctx.Character?.Inventory
                    .OrderBy(i => i.SortIndex).ToList() ?? [];

                foreach (var item in items)
                {
                    bool equipped = ctx.Character?.MainHandItemId == item.Id
                                 || ctx.Character?.OffHandItemId == item.Id;
                    var typeLabel = item.Category == ItemCategory.Weapon ? "Arme" : "Équipement";

                    inner.Item().BorderBottom(0.5f).BorderColor(LineColor)
                        .PaddingVertical(3).Row(r =>
                        {
                            r.RelativeItem().Row(nr =>
                            {
                                if (equipped)
                                {
                                    nr.ConstantItem(8).Height(8)
                                        .Svg(_ => EquipDotSvg());
                                    nr.ConstantItem(4);
                                }
                                nr.RelativeItem().Text(item.Name).FontSize(10);
                            });
                            r.ConstantItem(60).Text(typeLabel)
                                .FontSize(8.5f).FontColor(InkLight);
                        });
                }

                // Lignes vides
                for (int i = 0; i < emptySlots; i++)
                    inner.Item().BorderBottom(0.5f).BorderColor(LineColor)
                        .PaddingVertical(3).Height(20);

                // Légende
                inner.Item().PaddingTop(4).Row(leg =>
                {
                    leg.ConstantItem(8).Height(8)
                        .Svg(_ => EquipDotSvg());
                    leg.ConstantItem(5);
                    leg.AutoItem().Text("= objet équipé en main")
                        .FontSize(7.5f).Italic().FontColor(InkLight);
                });
            });
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS — GROUPES
    // ══════════════════════════════════════════════════════════════════════

    private static void TraitGroup(
        ColumnDescriptor col,
        string title,
        List<Trait> traitList,
        Func<Trait, bool> isChecked)
    {
        if (traitList.Count == 0) return;

        col.Item().PaddingBottom(8).Column(inner =>
        {
            SubSectionTitle(inner.Item(), title);
            foreach (var t in traitList)
                TraitRow(inner.Item(), t.Name, isChecked(t));
        });
    }

    private static void AbilityGroup(
        ColumnDescriptor col,
        string title,
        List<Ability> abilityList,
        Dictionary<string, int> equippedMap)
    {
        if (abilityList.Count == 0) return;

        col.Item().PaddingBottom(8).Column(inner =>
        {
            SubSectionTitle(inner.Item(), title);
            foreach (var a in abilityList)
            {
                int maxLevel = a.Levels.Max(l => l.Level);
                int acquired = equippedMap.TryGetValue(a.Id, out var lv) ? lv : 0;
                AbilityRow(inner.Item(), a.Name, a.StartLevel, maxLevel, acquired, a.UsageLimit);
            }
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS — COMPOSANTS UNITAIRES
    // ══════════════════════════════════════════════════════════════════════

    private static void SectionBanner(IContainer container, string text, bool topBorder = false)
    {
        var c = container;
        if (topBorder) c = c.BorderTop(2).BorderColor(Ink);

        c.Background(Ink)
            .PaddingHorizontal(12).PaddingVertical(3)
            .Text(text.ToUpperInvariant())
            .FontSize(8).Bold().FontColor(Parchment).LetterSpacing(0.16f);
    }

    private static void SubSectionTitle(IContainer container, string text) =>
        container
            .BorderBottom(1).BorderColor(Ink)
            .PaddingBottom(2).PaddingTop(0)
            .Text(text.ToUpperInvariant())
            .FontSize(7.5f).Bold().FontColor(InkLight).LetterSpacing(0.13f);

    private static void FieldCell(IContainer container, string label, string value, string? sub = null) =>
        container
            .PaddingHorizontal(10).PaddingVertical(4)
            .Column(c =>
            {
                c.Item().Text(label)
                    .FontSize(7.5f).FontColor(InkLight).LetterSpacing(0.12f);
                c.Item().PaddingTop(2).Text(value)
                    .FontSize(13).Bold();
                if (sub != null)
                    c.Item().Text(sub)
                        .FontSize(8).Italic().FontColor(InkLight);
            });

    private static void TraitRow(IContainer container, string name, bool isChecked) =>
        container
            .BorderBottom(0.5f).BorderColor(LineColor)
            .PaddingVertical(2)
            .Row(row =>
            {
                row.ConstantItem(11).Height(11)
                    .Svg(_ => CheckboxSvg(isChecked));
                row.ConstantItem(6);
                row.RelativeItem()
                    .Text(name)
                    .FontSize(10)
                    .FontColor(isChecked ? Ink : InkLight);
            });

    private static void AbilityRow(
        IContainer container,
        string name,
        int startLevel,
        int maxLevel,
        int acquired,
        UsageLimit usageLimit)
    {
        const float dotSize = 13f;
        const float gap = 3f;

        container
            .BorderBottom(0.5f).BorderColor(LineColor)
            .PaddingVertical(2.5f)
            .Row(row =>
            {
                // Ronds numérotés (SVG, pas de dépendance SkiaSharp)
                for (int lvl = 1; lvl <= maxLevel; lvl++)
                {
                    if (lvl < startLevel)
                    {
                        row.ConstantItem(dotSize); // espace vide
                    }
                    else
                    {
                        bool filled = lvl <= acquired;
                        int lvlCopy = lvl;
                        row.ConstantItem(dotSize).Height(dotSize)
                            .Svg(_ => DotSvg(dotSize, lvlCopy, filled));
                    }
                    if (lvl < maxLevel) row.ConstantItem(gap);
                }

                row.ConstantItem(6);

                row.RelativeItem()
                    .Text(name)
                    .FontSize(10)
                    .FontColor(acquired > 0 ? Ink : InkLight);

                if (usageLimit != UsageLimit.None)
                {
                    row.ConstantItem(4);
                    row.AutoItem()
                        .Border(0.5f).BorderColor(AmberInk)
                        .PaddingHorizontal(5).PaddingVertical(1)
                        .Text(UsageLimitLabel(usageLimit))
                        .FontSize(7.5f).FontColor(AmberInk);
                }
            });
    }

    private static void PageFooter(PageDescriptor page, SheetContext ctx) =>
        page.Footer()
            .PaddingTop(6)
            .BorderTop(0.5f).BorderColor(LineColor)
            .Row(row =>
            {
                row.RelativeItem().AlignLeft()
                    .Text("La Nouvelle Lune · STS")
                    .FontSize(7.5f).Italic().FontColor(InkLight);
                row.RelativeItem().AlignRight().Text(txt =>
                {
                    txt.CurrentPageNumber().FontSize(7.5f).FontColor(InkLight);
                    txt.Span(" / ").FontSize(7.5f).FontColor(InkLight);
                    txt.TotalPages().FontSize(7.5f).FontColor(InkLight);
                });
            });

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS — SVG (pas de dépendance SkiaSharp)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Cercle numéroté pour la progression d'une capacité.</summary>
    private static string DotSvg(float size, int level, bool filled)
    {
        float cx = size / 2f;
        float cy = size / 2f;
        float r = size / 2f - 1.5f;
        var fill = filled ? Ink : "none";
        var text = filled ? Parchment : Ink;
        return $"""
            <svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}'>
              <circle cx='{cx:F1}' cy='{cy:F1}' r='{r:F1}'
                      fill='{fill}' stroke='{Ink}' stroke-width='1.5'/>
              <text x='{cx:F1}' y='{cy + 2.5f:F1}'
                    text-anchor='middle' font-size='7' font-weight='bold' fill='{text}'>{level}</text>
            </svg>
            """;
    }

    /// <summary>Case à cocher carrée.</summary>
    private static string CheckboxSvg(bool isChecked)
    {
        var fill = isChecked ? Ink : "none";
        return $"""
            <svg xmlns='http://www.w3.org/2000/svg' width='11' height='11'>
              <rect x='1' y='1' width='9' height='9'
                    fill='{fill}' stroke='{Ink}' stroke-width='1.5'/>
            </svg>
            """;
    }

    /// <summary>Point plein pour indiquer un objet équipé.</summary>
    private static string EquipDotSvg() =>
        $"""
        <svg xmlns='http://www.w3.org/2000/svg' width='8' height='8'>
          <circle cx='4' cy='4' r='3' fill='{Ink}'/>
        </svg>
        """;

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS — LABELS
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
}
