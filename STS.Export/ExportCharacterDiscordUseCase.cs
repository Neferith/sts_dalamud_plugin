using System.Text;
using Sts.Domain;
using Sts.Domain.Character;
using Sts.Domain.Repository;

namespace STS.Export;

/// <inheritdoc/>
public sealed class ExportCharacterDiscordUseCase(
    TraitRepository traits,
    JobRepository jobs,
    AbilityRepository abilities) : IExportCharacterDiscordUseCase
{
    /// <inheritdoc/>
    public string Execute(Character character)
    {
        var rank = Rank.Get(character.RankKey);
        var job  = character.JobId != null ? jobs.GetById(character.JobId) : null;
        var sb   = new StringBuilder();

        sb.AppendLine("```");
        sb.AppendLine($"{character.Race.Label()} - {job?.Name ?? "Sans classe"}");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("```");
        sb.AppendLine($"**Rang :** {rank.Label}");
        sb.AppendLine($"**Réussite :** {rank.Palier}+");
        sb.AppendLine($"**Rerolls :** {rank.Rerolls}");
        sb.AppendLine($"**Réputation :** {Reputation.GetLabel(character.ReputationLevel)} ({(character.ReputationLevel >= 0 ? "+" : "")}{character.ReputationLevel})");
        sb.AppendLine();
        sb.AppendLine($"**Histoire :** {(string.IsNullOrWhiteSpace(character.Histoire) ? "..." : character.Histoire.Trim())}");
        sb.AppendLine("```");
        sb.AppendLine();

        var certByTrait   = character.Certifications.Where(c => c.LinkedOriginTraitId != null).ToDictionary(c => c.LinkedOriginTraitId!, c => c);
        var certByAbility = character.Certifications.Where(c => c.LinkedAbilityId    != null).ToDictionary(c => c.LinkedAbilityId!,    c => c);

        sb.AppendLine("## Capacités :");
        if (character.EquippedAbilities.Count == 0)
        {
            sb.AppendLine("- Aucune");
        }
        else
        {
            foreach (var eq in character.EquippedAbilities)
            {
                var ab = abilities.GetById(eq.AbilityId);
                if (ab is null) continue;

                sb.Append($"- **{ab.Name} Lv{eq.Level}**");
                if (certByAbility.TryGetValue(eq.AbilityId, out var abCert) && abCert.FreePoints > 0)
                    sb.Append($" *(★ {abCert.Name} — {abCert.FreePoints} pt(s) gratuit(s))*");
                sb.AppendLine(" :");

                if (ab.UsageLimit != UsageLimit.None)
                    sb.AppendLine($"> {UsageLimitLabel(ab.UsageLimit)}");

                foreach (var ld in ab.Levels.Where(l => l.Level <= eq.Level).OrderBy(l => l.Level))
                {
                    if (string.IsNullOrWhiteSpace(ld.Description)) continue;
                    sb.AppendLine($"> Rang {ld.Level} : {ld.Description.Trim()}");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Traits :");
        var allTraits = character.EquippedTraitIds.ToList();
        if (character.OriginTraitId != null) allTraits.Insert(0, character.OriginTraitId);
        if (allTraits.Count == 0)
        {
            sb.AppendLine("- Aucun");
        }
        else
        {
            foreach (var tid in allTraits)
            {
                var t = traits.GetById(tid);
                if (t is null) { sb.AppendLine($"- **{tid}**"); continue; }

                sb.Append($"- **{t.Name}**");
                if (certByTrait.TryGetValue(tid, out var traitCert))
                    sb.Append($" *(★ {traitCert.Name} — gratuit)*");
                sb.AppendLine(" :");
                if (!string.IsNullOrWhiteSpace(t.Description))
                    sb.AppendLine($"> {t.Description.Trim()}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Certifications :");
        if (character.Certifications.Count == 0)
        {
            sb.AppendLine("- Aucune");
        }
        else
        {
            foreach (var cert in character.Certifications)
            {
                sb.AppendLine($"- **{cert.Name}**");
                if (cert.LinkedOriginTraitId != null)
                {
                    var t = traits.GetById(cert.LinkedOriginTraitId);
                    sb.AppendLine($"> Trait d'origine gratuit : {t?.Name ?? cert.LinkedOriginTraitId}");
                }
                if (cert.LinkedAbilityId != null && cert.FreePoints > 0)
                {
                    var a = abilities.GetById(cert.LinkedAbilityId);
                    sb.AppendLine($"> {cert.FreePoints} pt(s) gratuit(s) sur : {a?.Name ?? cert.LinkedAbilityId}");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Inventaire :");
        if (character.Inventory.Count == 0)
        {
            sb.AppendLine("- Aucun objet.");
        }
        else
        {
            var weapons = character.Inventory.Where(i => i.Category == ItemCategory.Weapon).OrderBy(i => i.SortIndex).ToList();
            var items   = character.Inventory.Where(i => i.Category == ItemCategory.Item).OrderBy(i => i.SortIndex).ToList();

            if (weapons.Count > 0)
            {
                sb.AppendLine("**Armes :**");
                foreach (var w in weapons)
                {
                    var slots = new List<string>();
                    if (character.MainHandItemId == w.Id) slots.Add("main principale");
                    if (character.OffHandItemId  == w.Id) slots.Add("main secondaire");
                    var equippedNote  = slots.Count > 0 ? $" *(équipée — {string.Join(", ", slots)})*" : "";
                    var masteredNote  = character.IsWeaponUnmastered(w) ? " *(non maîtrisée — palier 8)*" : "";
                    var linkedAbility = w.LinkedAbilityId != null ? abilities.GetById(w.LinkedAbilityId) : null;
                    var linkedNote    = linkedAbility != null ? $" — compétence : {linkedAbility.Name}" : "";

                    sb.AppendLine($"- **{w.Name}**{equippedNote}{masteredNote}{linkedNote}");
                    if (!string.IsNullOrWhiteSpace(w.Description))
                        sb.AppendLine($"> {w.Description.Trim()}");
                }
            }

            if (items.Count > 0)
            {
                if (weapons.Count > 0) sb.AppendLine();
                sb.AppendLine("**Objets :**");
                foreach (var item in items)
                {
                    sb.AppendLine($"- **{item.Name}**");
                    if (!string.IsNullOrWhiteSpace(item.Description))
                        sb.AppendLine($"> {item.Description.Trim()}");
                }
            }
        }

        return sb.ToString().TrimEnd();
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
