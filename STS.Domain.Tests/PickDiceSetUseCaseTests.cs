using FluentAssertions;
using Sts.Domain;
using Sts.Domain.UseCases;
using Xunit;

namespace STS.Domain.Tests;

public class PickDiceSetUseCaseTests
{
    private readonly IPickDiceSetUseCase _sut = new DefaultPickDiceSetUseCase();

    // --- Avantage ---

    [Fact]
    public void Avantage_SetAMeilleur_ChoisitA()
    {
        var a = new DiceSet([7, 8, 9]); // 3 succès (palier 6)
        var b = new DiceSet([1, 2, 3]); // 0 succès

        var result = _sut.Execute(a, b, palier: 6, PickMode.Best);

        result.Chosen.Should().Be(a);
        result.Rejected.Should().Be(b);
    }

    [Fact]
    public void Avantage_SetBMeilleur_ChoisitB()
    {
        var a = new DiceSet([1, 2, 3]); // 0 succès
        var b = new DiceSet([7, 8, 9]); // 3 succès (palier 6)

        var result = _sut.Execute(a, b, palier: 6, PickMode.Best);

        result.Chosen.Should().Be(b);
        result.Rejected.Should().Be(a);
    }

    [Fact]
    public void Avantage_EgaliteSucces_TiebreakMaxDe_ChoisitPlusGrand()
    {
        var a = new DiceSet([6, 1, 1]); // 1 succès, max=6
        var b = new DiceSet([6, 1, 1]); // 1 succès, max=6 — égalité parfaite → A gagné (>=)

        // Les deux sont égaux → tiebreak max → a.Max() >= b.Max() → A
        var result = _sut.Execute(a, b, palier: 6, PickMode.Best);
        result.Chosen.Should().Be(a);
    }

    [Fact]
    public void Avantage_EgaliteSucces_TiebreakMaxDe_ChoisitSetAvecPlusGrandDe()
    {
        var a = new DiceSet([6, 1, 1]); // 1 succès, max=6
        var b = new DiceSet([9, 1, 1]); // 1 succès, max=9

        var result = _sut.Execute(a, b, palier: 6, PickMode.Best);

        result.Chosen.Should().Be(b);
        result.Rejected.Should().Be(a);
    }

    // --- Désavantage ---

    [Fact]
    public void Desavantage_SetAMoinsSucces_ChoisitA()
    {
        var a = new DiceSet([1, 2, 3]); // 0 succès
        var b = new DiceSet([7, 8, 9]); // 3 succès

        var result = _sut.Execute(a, b, palier: 6, PickMode.Worst);

        result.Chosen.Should().Be(a);
        result.Rejected.Should().Be(b);
    }

    [Fact]
    public void Desavantage_SetBMoinsSucces_ChoisitB()
    {
        var a = new DiceSet([7, 8, 9]); // 3 succès
        var b = new DiceSet([1, 2, 3]); // 0 succès

        var result = _sut.Execute(a, b, palier: 6, PickMode.Worst);

        result.Chosen.Should().Be(b);
        result.Rejected.Should().Be(a);
    }

    [Fact]
    public void Desavantage_EgaliteSucces_TiebreakMinDe_ChoisitPlusPetit()
    {
        var a = new DiceSet([6, 1, 1]); // 1 succès, max=6
        var b = new DiceSet([9, 1, 1]); // 1 succès, max=9

        var result = _sut.Execute(a, b, palier: 6, PickMode.Worst);

        // Désavantage + égalité → on choisit celui avec le plus petit max
        result.Chosen.Should().Be(a);
        result.Rejected.Should().Be(b);
    }

    // --- Invariant ---

    [Fact]
    public void Result_ContientToujoursChosenEtRejected()
    {
        var a = new DiceSet([5, 5, 5]);
        var b = new DiceSet([6, 6, 6]);

        var result = _sut.Execute(a, b, palier: 6, PickMode.Best);

        new[] { result.Chosen, result.Rejected }.Should().Contain([a, b]);
    }
}
