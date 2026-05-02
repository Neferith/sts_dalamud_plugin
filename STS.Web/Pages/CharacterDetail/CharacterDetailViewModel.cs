using Sts.Domain;
using Sts.Domain.Character;
using Sts.Domain.Repository;
using STS.Web.Services;

namespace STS.Web.ViewModels;

/// <summary>ViewModel de la page de détail d'un personnage.</summary>
public sealed class CharacterDetailViewModel(
    CharacterApiService api,
    AuthService auth,
    TraitRepository traits,
    JobRepository jobs,
    AbilityRepository abilities)
{
    public Character? Character { get; private set; }
    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }

    /// <summary>Indique si l'utilisateur connecté est le propriétaire de cette fiche.</summary>
    public bool IsOwner =>
        auth.IsAuthenticated &&
        Character is not null &&
        Character.UserId == auth.UserId;

    public Action? OnStateChanged { get; set; }

    // ── Résolution des données de référence ───────────────────────────────────

    public string JobName(string? jobId)
        => jobId is null ? "Aucun" : jobs.GetById(jobId)?.Name ?? jobId;

    public Trait? GetTrait(string traitId)
        => traits.GetById(traitId);

    public Ability? GetAbility(string abilityId)
        => abilities.GetById(abilityId);

    public string AbilityName(string abilityId)
        => abilities.GetById(abilityId)?.Name ?? abilityId;

    public string TraitName(string traitId)
        => traits.GetById(traitId)?.Name ?? traitId;

    public string UsageLimitLabel(UsageLimit limit) => limit switch
    {
        UsageLimit.OncePerCombat => "⏱ 1× par combat",
        UsageLimit.TwicePerCombat => "⏱ 2× par combat",
        UsageLimit.OncePerEvent => "⏱ 1× par event",
        UsageLimit.TwicePerEvent => "⏱ 2× par event",
        UsageLimit.ThreeTimesPerEvent => "⏱ 3× par event",
        _ => string.Empty,
    };

    // ── Chargement ────────────────────────────────────────────────────────────

    public async Task LoadAsync(Guid id)
    {
        IsLoading = true;
        Error = null;
        Notify();

        try
        {
            Character = await api.GetByIdAsync(id);
            if (Character is null)
                Error = "Personnage introuvable.";
        }
        catch (Exception ex)
        {
            Error = $"Erreur lors du chargement : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            Notify();
        }
    }

    public async Task<string?> DeleteAsync()
    {
        if (Character is null) return "Aucun personnage chargé.";
        var error = await api.DeleteAsync(Character.Id);
        if (error is null) Character = null;
        Notify();
        return error;
    }

    private void Notify() => OnStateChanged?.Invoke();
}
