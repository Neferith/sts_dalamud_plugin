using FluentAssertions;
using Sts.Domain;
using Xunit;

namespace STS.Domain.Tests;

public class DiceSetTests
{
    [Fact]
    public void Constructor_Valide_CreeLeDiceSet()
    {
        var dice = new DiceSet([1, 5, 10]);
        dice.Values.Should().BeEquivalentTo([1, 5, 10], o => o.WithStrictOrdering());
    }

    [Fact]
    public void Constructor_PasTroisDes_LanceException()
    {
        var act = () => new DiceSet([1, 2]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ValeurHorsLimite_LanceException()
    {
        var act = () => new DiceSet([0, 5, 5]);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_ValeurSuperieure10_LanceException()
    {
        var act = () => new DiceSet([5, 11, 5]);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_ClonerLesValeurs_ModificationExterieureSansEffect()
    {
        var values = new[] { 1, 2, 3 };
        var dice = new DiceSet(values);
        values[0] = 9;

        dice.Values[0].Should().Be(1);
    }

    [Fact]
    public void Max_ReturneLaValeurMaximale()
    {
        var dice = new DiceSet([3, 9, 5]);
        dice.Max().Should().Be(9);
    }

    [Fact]
    public void Display_10_Retourne0()
        => DiceSet.Display(10).Should().Be("0");

    [Fact]
    public void Display_AutreValeur_RetourneValeur()
        => DiceSet.Display(7).Should().Be("7");

    [Fact]
    public void ToDisplayString_FormatCorrect()
    {
        var dice = new DiceSet([1, 10, 5]);
        dice.ToDisplayString().Should().Be("1 · 0 · 5");
    }
}

public class RankTests
{
    [Theory]
    [InlineData(RankKey.Novice,     7, 1, 2)]
    [InlineData(RankKey.Aventurier, 6, 1, 3)]
    [InlineData(RankKey.Veteran,    5, 2, 4)]
    [InlineData(RankKey.Mentor,     4, 2, 5)]
    public void Get_RetourneLesBonnesValeurs(RankKey key, int palier, int rerolls, int traits)
    {
        var rank = Rank.Get(key);
        rank.Palier.Should().Be(palier);
        rank.Rerolls.Should().Be(rerolls);
        rank.Traits.Should().Be(traits);
    }

    [Theory]
    [InlineData(RankKey.Novice,     1, true)]
    [InlineData(RankKey.Novice,     2, true)]   // MaxAbilityLv2=1 → >0
    [InlineData(RankKey.Novice,     3, false)]  // MaxAbilityLv3=0 → interdit
    [InlineData(RankKey.Aventurier, 3, false)]
    [InlineData(RankKey.Veteran,    3, true)]   // MaxAbilityLv3=1
    [InlineData(RankKey.Mentor,     3, true)]   // MaxAbilityLv3=-1 → illimité
    public void AllowsAbilityLevel_RetourneCorrectement(RankKey key, int level, bool expected)
    {
        Rank.Get(key).AllowsAbilityLevel(level).Should().Be(expected);
    }
}

public class RollResultTests
{
    [Fact]
    public void Successes_SommeRawEtBonus()
    {
        var dice = new DiceSet([6, 6, 6]);
        var effects = new AppliedTraitEffects(
            BonusSuccesses: 2, 
            MalusSuccesses: 0, 
            BonusRerolls: 0, 
            ForcedMode: null, 
            BonusTraitNames: Array.Empty<string>(), 
            MalusTraitNames: Array.Empty<string>()
            );
        var result = new RollResult(dice, null, RawSuccesses: 2, Palier: 5, effects);

        result.Successes.Should().Be(4);
    }

    [Fact]
    public void Successes_JamaisNegatif()
    {
        var dice = new DiceSet([1, 1, 1]);
        var effects = new AppliedTraitEffects(
            BonusSuccesses: 0, 
            MalusSuccesses: 5, 
            BonusRerolls: 0, 
            ForcedMode: null, 
            BonusTraitNames: Array.Empty<string>(), 
            MalusTraitNames: Array.Empty<string>()
            );
        var result = new RollResult(dice, null, RawSuccesses: 0, Palier: 7, effects);

        result.Successes.Should().Be(0);
    }
}

public class ReputationTests
{
    [Theory]
    [InlineData(-5, "Criminel notoire")]
    [InlineData(0,  "Inexistant")]
    [InlineData(10, "Légendaire")]
    public void GetLabel_RetourneLeLabel(int level, string expected)
        => Reputation.GetLabel(level).Should().Be(expected);

    [Fact]
    public void Clamp_ValeurInferieure_RetourneMin()
        => Reputation.Clamp(-99).Should().Be(Reputation.Min);

    [Fact]
    public void Clamp_ValeurSuperieure_RetourneMax()
        => Reputation.Clamp(99).Should().Be(Reputation.Max);
}
