using Sts.Domain.Character;
using Sts.Domain.Repository;
using STS.Web.Services;

namespace STS.Web.ViewModels;

/// <summary>ViewModel de la page liste des personnages.</summary>
public sealed class CharactersViewModel(
    CharacterApiService api,
    AuthService auth,
    JobRepository jobs)
{
    public IReadOnlyList<Character> Characters { get; private set; } = [];

    /// <summary>URL absolue de l'image d'un personnage, ou null si aucune image.</summary>
    public string? ImageUrl(Character character) =>
        character.ImageUrl is null ? null : api.AbsoluteImageUrl(character.ImageUrl);

    /// <summary>Nom affiché du job, ou l'id brut si introuvable.</summary>
    public string JobName(string? jobId)
        => jobId is null ? string.Empty : jobs.GetById(jobId)?.Name ?? jobId;

    /// <summary>
    /// URL absolue de l'icône du job, ou null si le job n'existe pas
    /// ou n'a pas encore d'icône uploadée.
    /// </summary>
    public string? JobIconUrl(string? jobId)
    {
        if (jobId is null) return null;
        var job = jobs.GetById(jobId);
        return job?.IconUrl is null ? null : api.AbsoluteJobIconUrl(jobId);
    }

    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }

    /// <summary>Indique si l'utilisateur peut créer un nouveau personnage.</summary>
    public bool CanCreate =>
        auth.IsAuthenticated &&
        Characters.Count < auth.MaxCharacters;

    /// <summary>Message expliquant pourquoi la création est bloquée.</summary>
    public string? CreateBlockedReason =>
        !auth.IsAuthenticated ? "Connexion requise." :
        !CanCreate ? $"Limite atteinte ({auth.MaxCharacters} fiche(s) maximum)." :
        null;

    public Action? OnStateChanged { get; set; }

    public async Task LoadAsync()
    {
        IsLoading = true;
        Error = null;
        Notify();

        try
        {
            Characters = await api.GetAllAsync();
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

    private void Notify() => OnStateChanged?.Invoke();
}
