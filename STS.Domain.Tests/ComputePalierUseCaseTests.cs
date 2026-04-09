using FluentAssertions;
using Sts.Domain;
using Sts.Domain.UseCases;
using Xunit;

namespace STS.Domain.Tests;

public class ComputePalierUseCaseTests
{
    private readonly IComputePalierUseCase _sut = new DefaultComputePalierUseCase();

    [Theory]
    [InlineData(RankKey.Novice,     0,  7)]
    [InlineData(RankKey.Aventurier, 0,  6)]
    [InlineData(RankKey.Veteran,    0,  5)]
    [InlineData(RankKey.Mentor,     0,  4)]
    public void Execute_SansModificateur_RetournePalierDuRang(RankKey key, int modifier, int expected)
    {
        var rank = Rank.Get(key);
        _sut.Execute(rank, modifier).Should().Be(expected);
    }

    [Fact]
    public void Execute_ModificateurPositif_AbaissePalier()
    {
        var rank = Rank.Get(RankKey.Aventurier); // palier 6
        _sut.Execute(rank, modifier: 2).Should().Be(4);
    }

    [Fact]
    public void Execute_ModificateurNegatif_HaussePalier()
    {
        var rank = Rank.Get(RankKey.Aventurier); // palier 6
        _sut.Execute(rank, modifier: -2).Should().Be(8);
    }

    [Fact]
    public void Execute_ClampMin_PalierMinimumEst1()
    {
        var rank = Rank.Get(RankKey.Mentor); // palier 4
        _sut.Execute(rank, modifier: 10).Should().Be(1);
    }

    [Fact]
    public void Execute_ClampMax_PalierMaximumEst10()
    {
        var rank = Rank.Get(RankKey.Novice); // palier 7
        _sut.Execute(rank, modifier: -10).Should().Be(10);
    }
}
