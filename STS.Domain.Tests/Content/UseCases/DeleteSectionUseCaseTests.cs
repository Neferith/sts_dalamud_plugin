using FluentAssertions;
using Sts.Domain.Content.UseCases;
using Sts.Domain.Tests.Content.Fakes;
using Xunit;

namespace Sts.Domain.Tests.Content.UseCases;

public sealed class DeleteSectionUseCaseTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_ForwardsResultFromRepository(bool repoResult)
    {
        var repo = new FakeRulesRepository { DeleteSectionResult = repoResult };
        var uc = new DeleteSectionUseCase(repo);

        var result = await uc.ExecuteAsync("s1");

        result.Should().Be(repoResult);
        repo.CapturedSectionId.Should().Be("s1");
    }
}
