using FluentAssertions;
using Sts.Domain.Content.UseCases;
using Sts.Domain.Tests.Content.Fakes;
using Xunit;

namespace Sts.Domain.Tests.Content.UseCases;

public sealed class UpdatePostUseCaseTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_ForwardsResultFromRepository(bool repoResult)
    {
        var repo = new FakeRulesRepository { UpdatePostResult = repoResult };
        var uc = new UpdatePostUseCase(repo);

        var result = await uc.ExecuteAsync("s1", "p1", "Titre", "Contenu");

        result.Should().Be(repoResult);
        repo.CapturedSectionId.Should().Be("s1");
        repo.CapturedPostId.Should().Be("p1");
        repo.CapturedTitle.Should().Be("Titre");
        repo.CapturedContent.Should().Be("Contenu");
    }
}
