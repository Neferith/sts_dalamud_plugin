using FluentAssertions;
using Sts.Domain.Content;
using Sts.Domain.Content.UseCases;
using Sts.Domain.Tests.Content.Fakes;
using Xunit;

namespace Sts.Domain.Tests.Content.UseCases;

public sealed class GetRulesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsSectionsFromRepository()
    {
        var sections = new List<RulesSection>
        {
            new() { Id = "s1", Title = "Section 1", Order = 1 },
        };
        var repo = new FakeRulesRepository { GetAllResult = sections };
        var uc = new GetRulesUseCase(repo);

        var result = await uc.ExecuteAsync();

        result.Should().BeEquivalentTo(sections);
    }
}
