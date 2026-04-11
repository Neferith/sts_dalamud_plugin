using FluentAssertions;
using Sts.Api.Repositories;
using Sts.Domain.Content;
using Sts.Domain.Content.Repositories;
using Sts.Domain.Tests.Content.Fakes;
using Xunit;

namespace Sts.Domain.Tests.Content;

public sealed class RulesRepositoryTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RulesSection MakeSection(string id, int order = 0, List<RulesPost>? posts = null) =>
        new() { Id = id, Title = $"Section {id}", Order = order, Posts = posts ?? [] };

    private static RulesPost MakePost(string id) =>
        new() { Id = id, Title = $"Post {id}", Content = $"Content {id}" };

    private static RulesRepository Repo(params RulesSection[] initial)
    {
        var ds = new FakeRulesDataSource(initial);
        return new RulesRepository(ds);
    }

    private static (RulesRepository repo, FakeRulesDataSource ds) RepoWithDs(params RulesSection[] initial)
    {
        var ds = new FakeRulesDataSource(initial);
        return (new RulesRepository(ds), ds);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_EmptySource_ReturnsEmptyList()
    {
        var repo = Repo();

        var result = await repo.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSectionsSortedByOrder()
    {
        var repo = Repo(MakeSection("c", order: 3), MakeSection("a", order: 1), MakeSection("b", order: 2));

        var result = await repo.GetAllAsync();

        result.Select(s => s.Id).Should().Equal("a", "b", "c");
    }

    [Fact]
    public async Task GetAllAsync_CalledMultipleTimes_LoadsDataSourceOnlyOnce()
    {
        var (repo, ds) = RepoWithDs(MakeSection("s1"));

        await repo.GetAllAsync();
        await repo.GetAllAsync();
        await repo.GetAllAsync();

        ds.LoadCallCount.Should().Be(1);
    }

    // ── AddSectionAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddSectionAsync_NewId_ReturnsTrue()
    {
        var repo = Repo();

        var result = await repo.AddSectionAsync(MakeSection("new"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddSectionAsync_NewSection_AppearsInGetAll()
    {
        var repo = Repo();
        var section = MakeSection("new");

        await repo.AddSectionAsync(section);
        var all = await repo.GetAllAsync();

        all.Should().ContainSingle(s => s.Id == "new");
    }

    [Fact]
    public async Task AddSectionAsync_DuplicateId_ReturnsFalse()
    {
        var repo = Repo(MakeSection("existing"));

        var result = await repo.AddSectionAsync(MakeSection("existing"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddSectionAsync_DuplicateId_DoesNotPersist()
    {
        var (repo, ds) = RepoWithDs(MakeSection("existing"));

        await repo.AddSectionAsync(MakeSection("existing"));

        ds.SaveCallCount.Should().Be(0);
    }

    [Fact]
    public async Task AddSectionAsync_MaintainsSortOrder()
    {
        var repo = Repo(MakeSection("b", order: 2));
        await repo.AddSectionAsync(MakeSection("a", order: 1));

        var all = await repo.GetAllAsync();

        all.Select(s => s.Id).Should().Equal("a", "b");
    }

    [Fact]
    public async Task AddSectionAsync_Persists()
    {
        var (repo, ds) = RepoWithDs();

        await repo.AddSectionAsync(MakeSection("new"));

        ds.SaveCallCount.Should().Be(1);
        ds.LastSaved.Should().ContainSingle(s => s.Id == "new");
    }

    // ── UpdateSectionAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSectionAsync_ExistingSection_ReturnsTrue()
    {
        var repo = Repo(MakeSection("s1", order: 1));

        var result = await repo.UpdateSectionAsync("s1", "Nouveau titre", 2);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSectionAsync_UpdatesTitleAndOrder()
    {
        var repo = Repo(MakeSection("s1", order: 1));

        await repo.UpdateSectionAsync("s1", "Nouveau titre", 5);
        var all = await repo.GetAllAsync();

        var updated = all.Single(s => s.Id == "s1");
        updated.Title.Should().Be("Nouveau titre");
        updated.Order.Should().Be(5);
    }

    [Fact]
    public async Task UpdateSectionAsync_PreservesExistingPosts()
    {
        var post = MakePost("p1");
        var repo = Repo(MakeSection("s1", posts: [post]));

        await repo.UpdateSectionAsync("s1", "Nouveau titre", 1);
        var all = await repo.GetAllAsync();

        all.Single(s => s.Id == "s1").Posts.Should().ContainSingle(p => p.Id == "p1");
    }

    [Fact]
    public async Task UpdateSectionAsync_ReordersSectionsAfterUpdate()
    {
        var repo = Repo(MakeSection("a", order: 1), MakeSection("b", order: 2));

        await repo.UpdateSectionAsync("b", "B", order: 0);
        var all = await repo.GetAllAsync();

        all.Select(s => s.Id).Should().Equal("b", "a");
    }

    [Fact]
    public async Task UpdateSectionAsync_UnknownId_ReturnsFalse()
    {
        var repo = Repo();

        var result = await repo.UpdateSectionAsync("unknown", "X", 1);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateSectionAsync_UnknownId_DoesNotPersist()
    {
        var (repo, ds) = RepoWithDs();

        await repo.UpdateSectionAsync("unknown", "X", 1);

        ds.SaveCallCount.Should().Be(0);
    }

    // ── DeleteSectionAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteSectionAsync_ExistingSection_ReturnsTrue()
    {
        var repo = Repo(MakeSection("s1"));

        var result = await repo.DeleteSectionAsync("s1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteSectionAsync_RemovesSectionFromGetAll()
    {
        var repo = Repo(MakeSection("s1"), MakeSection("s2"));

        await repo.DeleteSectionAsync("s1");
        var all = await repo.GetAllAsync();

        all.Should().NotContain(s => s.Id == "s1");
        all.Should().ContainSingle(s => s.Id == "s2");
    }

    [Fact]
    public async Task DeleteSectionAsync_UnknownId_ReturnsFalse()
    {
        var repo = Repo();

        var result = await repo.DeleteSectionAsync("unknown");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSectionAsync_UnknownId_DoesNotPersist()
    {
        var (repo, ds) = RepoWithDs();

        await repo.DeleteSectionAsync("unknown");

        ds.SaveCallCount.Should().Be(0);
    }

    // ── AddPostAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddPostAsync_ExistingSection_NewPost_ReturnsTrue()
    {
        var repo = Repo(MakeSection("s1"));

        var result = await repo.AddPostAsync("s1", MakePost("p1"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddPostAsync_NewPost_AppearsInSection()
    {
        var repo = Repo(MakeSection("s1"));

        await repo.AddPostAsync("s1", MakePost("p1"));
        var all = await repo.GetAllAsync();

        all.Single(s => s.Id == "s1").Posts.Should().ContainSingle(p => p.Id == "p1");
    }

    [Fact]
    public async Task AddPostAsync_UnknownSection_ReturnsNull()
    {
        var repo = Repo();

        var result = await repo.AddPostAsync("unknown", MakePost("p1"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddPostAsync_DuplicatePostId_ReturnsFalse()
    {
        var repo = Repo(MakeSection("s1", posts: [MakePost("p1")]));

        var result = await repo.AddPostAsync("s1", MakePost("p1"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddPostAsync_DuplicatePostId_DoesNotPersist()
    {
        var (repo, ds) = RepoWithDs(MakeSection("s1", posts: [MakePost("p1")]));

        await repo.AddPostAsync("s1", MakePost("p1"));

        ds.SaveCallCount.Should().Be(0);
    }

    [Fact]
    public async Task AddPostAsync_UnknownSection_DoesNotPersist()
    {
        var (repo, ds) = RepoWithDs();

        await repo.AddPostAsync("unknown", MakePost("p1"));

        ds.SaveCallCount.Should().Be(0);
    }

    // ── UpdatePostAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePostAsync_ExistingPost_ReturnsTrue()
    {
        var repo = Repo(MakeSection("s1", posts: [MakePost("p1")]));

        var result = await repo.UpdatePostAsync("s1", "p1", "Nouveau", "Contenu");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePostAsync_UpdatesTitleAndContent()
    {
        var repo = Repo(MakeSection("s1", posts: [MakePost("p1")]));

        await repo.UpdatePostAsync("s1", "p1", "Nouveau titre", "Nouveau contenu");
        var all = await repo.GetAllAsync();

        var post = all.Single(s => s.Id == "s1").Posts.Single(p => p.Id == "p1");
        post.Title.Should().Be("Nouveau titre");
        post.Content.Should().Be("Nouveau contenu");
    }

    [Fact]
    public async Task UpdatePostAsync_PreservesOtherPosts()
    {
        var repo = Repo(MakeSection("s1", posts: [MakePost("p1"), MakePost("p2")]));

        await repo.UpdatePostAsync("s1", "p1", "X", "Y");
        var all = await repo.GetAllAsync();

        all.Single(s => s.Id == "s1").Posts.Should().Contain(p => p.Id == "p2");
    }

    [Fact]
    public async Task UpdatePostAsync_UnknownSection_ReturnsFalse()
    {
        var repo = Repo();

        var result = await repo.UpdatePostAsync("unknown", "p1", "X", "Y");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePostAsync_UnknownPost_ReturnsFalse()
    {
        var repo = Repo(MakeSection("s1"));

        var result = await repo.UpdatePostAsync("s1", "unknown", "X", "Y");

        result.Should().BeFalse();
    }

    // ── DeletePostAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePostAsync_ExistingPost_ReturnsTrue()
    {
        var repo = Repo(MakeSection("s1", posts: [MakePost("p1")]));

        var result = await repo.DeletePostAsync("s1", "p1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePostAsync_RemovesPostFromSection()
    {
        var repo = Repo(MakeSection("s1", posts: [MakePost("p1"), MakePost("p2")]));

        await repo.DeletePostAsync("s1", "p1");
        var all = await repo.GetAllAsync();

        all.Single(s => s.Id == "s1").Posts.Should().NotContain(p => p.Id == "p1");
        all.Single(s => s.Id == "s1").Posts.Should().ContainSingle(p => p.Id == "p2");
    }

    [Fact]
    public async Task DeletePostAsync_UnknownSection_ReturnsFalse()
    {
        var repo = Repo();

        var result = await repo.DeletePostAsync("unknown", "p1");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePostAsync_UnknownPost_ReturnsFalse()
    {
        var repo = Repo(MakeSection("s1"));

        var result = await repo.DeletePostAsync("s1", "unknown");

        result.Should().BeFalse();
    }
}
