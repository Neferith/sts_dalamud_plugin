using FluentAssertions;
using Sts.Domain.Content.UseCases;
using Sts.Domain.Tests.Content.Fakes;
using Xunit;

namespace Sts.Domain.Tests.Content.UseCases;

public sealed class UpdateSectionUseCaseTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_ForwardsResultFromRepository(bool repoResult)
    {
        var repo = new FakeRulesRepository { UpdateSectionResult = repoResult };
        var uc = new UpdateSectionUseCase(repo);

        var result = await uc.ExecuteAsync("s1", "Titre", 2);

        result.Should().Be(repoResult);
        repo.CapturedSectionId.Should().Be("s1");
        repo.CapturedTitle.Should().Be("Titre");
        repo.CapturedOrder.Should().Be(2);
    }
}
