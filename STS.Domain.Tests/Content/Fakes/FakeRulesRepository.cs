using Sts.Domain.Content;
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Tests.Content.Fakes;

/// <summary>
/// Implémentation contrôlée de <see cref="IRulesRepository"/> pour tester les use cases.
/// Expose des propriétés permettant de définir les valeurs de retour et d'observer les appels.
/// </summary>
internal sealed class FakeRulesRepository : IRulesRepository
{
    // ── Valeurs de retour configurables ───────────────────────────────────────

    public IReadOnlyList<RulesSection> GetAllResult { get; set; } = [];
    public bool AddSectionResult { get; set; } = true;
    public bool UpdateSectionResult { get; set; } = true;
    public bool DeleteSectionResult { get; set; } = true;
    public bool? AddPostResult { get; set; } = true;
    public bool UpdatePostResult { get; set; } = true;
    public bool DeletePostResult { get; set; } = true;

    // ── Arguments capturés ────────────────────────────────────────────────────

    public RulesSection? CapturedSection { get; private set; }
    public RulesPost? CapturedPost { get; private set; }
    public string? CapturedSectionId { get; private set; }
    public string? CapturedPostId { get; private set; }
    public string? CapturedTitle { get; private set; }
    public string? CapturedContent { get; private set; }
    public int? CapturedOrder { get; private set; }

    // ── Implémentation ────────────────────────────────────────────────────────

    public Task<IReadOnlyList<RulesSection>> GetAllAsync() =>
        Task.FromResult(GetAllResult);

    public Task<bool> AddSectionAsync(RulesSection section)
    {
        CapturedSection = section;
        return Task.FromResult(AddSectionResult);
    }

    public Task<bool> UpdateSectionAsync(string sectionId, string title, int order)
    {
        CapturedSectionId = sectionId;
        CapturedTitle = title;
        CapturedOrder = order;
        return Task.FromResult(UpdateSectionResult);
    }

    public Task<bool> DeleteSectionAsync(string sectionId)
    {
        CapturedSectionId = sectionId;
        return Task.FromResult(DeleteSectionResult);
    }

    public Task<bool?> AddPostAsync(string sectionId, RulesPost post)
    {
        CapturedSectionId = sectionId;
        CapturedPost = post;
        return Task.FromResult(AddPostResult);
    }

    public Task<bool> UpdatePostAsync(string sectionId, string postId, string title, string content)
    {
        CapturedSectionId = sectionId;
        CapturedPostId = postId;
        CapturedTitle = title;
        CapturedContent = content;
        return Task.FromResult(UpdatePostResult);
    }

    public Task<bool> DeletePostAsync(string sectionId, string postId)
    {
        CapturedSectionId = sectionId;
        CapturedPostId = postId;
        return Task.FromResult(DeletePostResult);
    }
}
