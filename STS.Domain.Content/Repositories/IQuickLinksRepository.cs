using Sts.Domain.Content.Models;
using Sts.Domain.Content.UseCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sts.Domain.Content.Repositories
{
    /// <summary>Accès aux données des <see cref="QuickLink"/>.</summary>
    public interface IQuickLinksRepository
    {
        /// <summary>Retourne tous les liens rapides.</summary>
        Task<IEnumerable<QuickLink>> GetAllAsync();

        /// <summary>Persiste un nouveau lien.</summary>
        Task<QuickLink> AddAsync(CreateQuickLinkParameters parameters);

        /// <summary>Met à jour un lien existant.</summary>
        Task<QuickLink?> UpdateAsync(Guid id, UpdateQuickLinkParameters parameters);

        /// <summary>Supprime un lien par son identifiant.</summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
