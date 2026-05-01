using Sts.Domain;
using Sts.Domain.Character;
using STS.Web.Services;

namespace STS.Web.ViewModels;

/// <summary>ViewModel de la page de création d'un personnage.</summary>
public sealed class CharacterCreateViewModel(CharacterApiService api)
{
    public string  FormName  { get; set; } = string.Empty;
    public RankKey FormRank  { get; set; } = RankKey.Novice;
    public bool    IsSaving  { get; private set; }
    public string? Error     { get; private set; }

    public Action? OnStateChanged { get; set; }

    /// <summary>
    /// Crée le personnage.
    /// Retourne le personnage créé si succès, null sinon (Error est renseigné).
    /// </summary>
    public async Task<Character?> CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            Error = "Le nom du personnage est requis.";
            Notify();
            return null;
        }

        IsSaving = true;
        Error    = null;
        Notify();

        try
        {
            var (character, error) = await api.CreateAsync(FormName.Trim(), FormRank);
            if (error is not null)
            {
                Error = error;
                return null;
            }
            return character;
        }
        catch (Exception ex)
        {
            Error = $"Erreur lors de la création : {ex.Message}";
            return null;
        }
        finally
        {
            IsSaving = false;
            Notify();
        }
    }

    private void Notify() => OnStateChanged?.Invoke();
}
