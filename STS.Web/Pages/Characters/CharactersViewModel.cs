using Sts.Domain.Character;
using STS.Web.Services;

namespace STS.Web.ViewModels;

/// <summary>ViewModel de la page liste des personnages.</summary>
public sealed class CharactersViewModel(CharacterApiService api, AuthService auth)
{
    public IReadOnlyList<Character> Characters { get; private set; } = [];
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
