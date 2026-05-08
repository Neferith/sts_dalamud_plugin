using Sts.Domain.Content.Models;

namespace Sts.Domain.Content.Repositories
{
    /// <summary>Accès en lecture seule aux <see cref="SiteSettings"/>.</summary>
    public interface ISiteSettingsReadRepository
    {
        /// <summary>Retourne les paramètres courants.</summary>
        Task<SiteSettings> GetAsync();
    }

    /// <summary>Accès complet aux <see cref="SiteSettings"/>.</summary>
    public interface ISiteSettingsRepository : ISiteSettingsReadRepository
    {
        /// <summary>Persiste les paramètres et les retourne.</summary>
        Task<SiteSettings> SaveAsync(SiteSettings settings);
    }
}
