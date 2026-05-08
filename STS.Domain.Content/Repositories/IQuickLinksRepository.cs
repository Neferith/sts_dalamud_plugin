using Sts.Domain.Content.Models;
using Sts.Domain.Content.UseCases;

namespace Sts.Domain.Content.Repositories
{

    /// <summary>Accès en lecture seule aux <see cref="QuickLink"/>.</summary>
    public interface IQuickLinksReadRepository
    {
        /// <summary>Retourne tous les liens rapides.</summary>
        Task<IEnumerable<QuickLink>> GetAllAsync();
    }


    /// <summary>Accès complet aux <see cref="QuickLink"/>.</summary>
    public interface IQuickLinksRepository : IQuickLinksReadRepository
    {
        /// <summary>Crée un lien rapide.</summary>
        Task<QuickLink> AddAsync(CreateQuickLinkParameters parameters);

        /// <summary>Met à jour un lien rapide.</summary>
        Task<QuickLink?> UpdateAsync(Guid id, UpdateQuickLinkParameters parameters);

        /// <summary>Supprime un lien rapide.</summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
