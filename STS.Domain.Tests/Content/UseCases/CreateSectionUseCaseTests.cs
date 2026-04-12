using FluentAssertions;
using Sts.Domain.Content;
using Sts.Domain.Content.UseCases;
using Sts.Domain.Tests.Content.Fakes;
using Xunit;

namespace Sts.Domain.Tests.Content.UseCases;

public sealed class CreateSectionUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsToRepository_ReturnsTrue()
    {
        var repo = new FakeRulesRepository { AddSectionResult = true };
        var uc = new CreateSectionUseCase(repo);
        var section = new RulesSection { Id = "s1", Title = "S1", Order = 1 };

        var result = await uc.ExecuteAsync(section);

        result.Should().BeTrue();
        repo.CapturedSection.Should().Be(section);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsToRepository_ReturnsFalseOnConflict()
    {
        var repo = new FakeRulesRepository { AddSectionResult = false };
        var uc = new CreateSectionUseCase(repo);

        var result = await uc.ExecuteAsync(new RulesSection { Id = "s1" });

        result.Should().BeFalse();
    }
}
