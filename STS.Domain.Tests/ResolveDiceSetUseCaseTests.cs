using FluentAssertions;
using Sts.Domain;
using Sts.Domain.UseCases;
using Xunit;

namespace STS.Domain.Tests;

public class ResolveDiceSetUseCaseTests
{
    private readonly IResolveDiceSetUseCase _sut = new DefaultResolveDiceSetUseCase();

    [Fact]
    public void Execute_TousDessus_Retourne3Succes()
    {
        var dice = new DiceSet([7, 8, 9]);
        var result = _sut.Execute(dice, palier: 6);

        result.Successes.Should().Be(3);
        result.Hits.Should().AllBeEquivalentTo(true);
    }

    [Fact]
    public void Execute_TousEn_Dessous_Retourne0Succes()
    {
        var dice = new DiceSet([1, 2, 3]);
        var result = _sut.Execute(dice, palier: 6);

        result.Successes.Should().Be(0);
        result.Hits.Should().AllBeEquivalentTo(false);
    }

    [Fact]
    public void Execute_EgalAuPalier_CompteCommeSucces()
    {
        var dice = new DiceSet([6, 6, 6]);
        var result = _sut.Execute(dice, palier: 6);

        result.Successes.Should().Be(3);
    }

    [Fact]
    public void Execute_Mixte_CompteCorrectement()
    {
        var dice = new DiceSet([5, 6, 7]); // palier 6 → 5 fail, 6 hit, 7 hit
        var result = _sut.Execute(dice, palier: 6);

        result.Successes.Should().Be(2);
        result.Hits.Should().BeEquivalentTo([false, true, true]);
    }

    [Fact]
    public void Execute_ValeurMaximale10_EstSucces()
    {
        // 10 s'affiche "0" mais vaut 10 mécaniquement
        var dice = new DiceSet([10, 10, 10]);
        var result = _sut.Execute(dice, palier: 7);

        result.Successes.Should().Be(3);
    }

    [Fact]
    public void Execute_Palier1_ToutEstSucces()
    {
        var dice = new DiceSet([1, 1, 1]);
        var result = _sut.Execute(dice, palier: 1);

        result.Successes.Should().Be(3);
    }

    [Fact]
    public void Execute_RetourneExactement3Hits()
    {
        var dice = new DiceSet([4, 5, 6]);
        var result = _sut.Execute(dice, palier: 5);

        result.Hits.Should().HaveCount(3);
    }
}
