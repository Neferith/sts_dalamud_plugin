using FluentAssertions;
using Sts.Domain.Content;
using Sts.Domain.Content.UseCases;
using Sts.Domain.Tests.Content.Fakes;
using Xunit;

namespace Sts.Domain.Tests.Content.UseCases;

public sealed class CreatePostUseCaseTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public async Task ExecuteAsync_ForwardsTriStateResultFromRepository(bool? repoResult)
    {
        var repo = new FakeRulesRepository { AddPostResult = repoResult };
        var uc = new CreatePostUseCase(repo);
        var post = new RulesPost { Id = "p1", Title = "P1", Content = "..." };

        var result = await uc.ExecuteAsync("s1", post);

        result.Should().Be(repoResult);
        repo.CapturedSectionId.Should().Be("s1");
        repo.CapturedPost.Should().Be(post);
    }
}
