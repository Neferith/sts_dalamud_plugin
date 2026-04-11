namespace Sts.Domain.Content.Repositories;

/// <summary>Contrat métier de gestion des sections et posts de règles.</summary>
public interface IRulesRepository
{
    /// <summary>Retourne toutes les sections triées par ordre croissant.</summary>
    Task<IReadOnlyList<RulesSection>> GetAllAsync();

    /// <summary>Ajoute une section. Retourne <c>false</c> si l'ID existe déjà.</summary>
    Task<bool> AddSectionAsync(RulesSection section);

    /// <summary>Met à jour titre et ordre d'une section. Retourne <c>false</c> si introuvable.</summary>
    Task<bool> UpdateSectionAsync(string sectionId, string title, int order);

    /// <summary>Supprime une section et ses posts. Retourne <c>false</c> si introuvable.</summary>
    Task<bool> DeleteSectionAsync(string sectionId);

    /// <summary>
    /// Ajoute un post dans une section.
    /// <c>true</c> = créé ; <c>false</c> = ID post en conflit ; <c>null</c> = section introuvable.
    /// </summary>
    Task<bool?> AddPostAsync(string sectionId, RulesPost post);

    /// <summary>Met à jour un post. Retourne <c>false</c> si section ou post introuvable.</summary>
    Task<bool> UpdatePostAsync(string sectionId, string postId, string title, string content);

    /// <summary>Supprime un post. Retourne <c>false</c> si section ou post introuvable.</summary>
    Task<bool> DeletePostAsync(string sectionId, string postId);
}
