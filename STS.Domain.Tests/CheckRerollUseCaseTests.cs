using FluentAssertions;
using Sts.Domain.UseCases;
using Xunit;

namespace STS.Domain.Tests;

public class CheckRerollUseCaseTests
{
    private readonly ICheckRerollUseCase _sut = new DefaultCheckRerollUseCase();

    [Fact]
    public void Execute_AucunUtilise_AllowedEtRemainingEgalMax()
    {
        var result = _sut.Execute(rerollsMax: 2, rerollsUsed: 0);

        result.Allowed.Should().BeTrue();
        result.Remaining.Should().Be(2);
    }

    [Fact]
    public void Execute_TousUtilises_NotAllowedEt0Remaining()
    {
        var result = _sut.Execute(rerollsMax: 1, rerollsUsed: 1);

        result.Allowed.Should().BeFalse();
        result.Remaining.Should().Be(0);
    }

    [Fact]
    public void Execute_UnRestant_AllowedEt1Remaining()
    {
        var result = _sut.Execute(rerollsMax: 2, rerollsUsed: 1);

        result.Allowed.Should().BeTrue();
        result.Remaining.Should().Be(1);
    }

    [Fact]
    public void Execute_UtilisesDepasse_RemainingNeJamaisNegatif()
    {
        // cas défensif — ne devrait pas arriver en pratique
        var result = _sut.Execute(rerollsMax: 1, rerollsUsed: 5);

        result.Allowed.Should().BeFalse();
        result.Remaining.Should().Be(0);
    }

    [Fact]
    public void Execute_Max0_JamaisAllowed()
    {
        var result = _sut.Execute(rerollsMax: 0, rerollsUsed: 0);

        result.Allowed.Should().BeFalse();
        result.Remaining.Should().Be(0);
    }
}
