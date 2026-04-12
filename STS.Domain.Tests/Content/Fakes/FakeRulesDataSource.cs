using Sts.Domain.Content;
using Sts.Domain.Content.DataSources;

namespace Sts.Domain.Tests.Content.Fakes;

/// <summary>
/// Implémentation en mémoire de <see cref="IRulesDataSource"/> pour les tests.
/// Permet de contrôler les données initiales et d'observer les appels à <see cref="SaveAsync"/>.
/// </summary>
internal sealed class FakeRulesDataSource : IRulesDataSource
{
    private List<RulesSection> _data;

    public int LoadCallCount { get; private set; }
    public int SaveCallCount { get; private set; }
    public List<RulesSection>? LastSaved { get; private set; }

    public FakeRulesDataSource(IEnumerable<RulesSection>? initial = null)
    {
        _data = initial?.ToList() ?? [];
    }

    public Task<List<RulesSection>> LoadAsync()
    {
        LoadCallCount++;
        return Task.FromResult(_data.ToList());
    }

    public Task SaveAsync(List<RulesSection> sections)
    {
        SaveCallCount++;
        LastSaved = sections.ToList();
        _data = sections.ToList();
        return Task.CompletedTask;
    }
}
