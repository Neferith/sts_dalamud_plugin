using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sts.Domain.Content.Repositories
{
    /// <summary>Accès aux <see cref="SiteSettings"/> (singleton JSON).</summary>
    public interface ISiteSettingsRepository
    {
        /// <summary>Retourne les paramètres courants.</summary>
        Task<SiteSettings> GetAsync();

        /// <summary>Persiste les paramètres.</summary>
        Task<SiteSettings> SaveAsync(SiteSettings settings);
    }
}
