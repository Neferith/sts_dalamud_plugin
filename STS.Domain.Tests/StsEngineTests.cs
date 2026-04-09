using FluentAssertions;
using Sts.Domain;
using Sts.Domain.UseCases;
using Xunit;

namespace STS.Domain.Tests;

/// <summary>
/// Tests du StsEngine.
/// On utilise CreateDefault() pour l'essentiel ; les tests de traits injectent
/// directement les traits via SetEquippedTraits().
/// </summary>
public class StsEngineTests
{
    private static StsEngine Engine() => StsEngine.CreateDefault();

    // ── état initial ──────────────────────────────────────────────────────────

    [Fact]
    public void Initial_HasRolled_EstFalse()
        => Engine().HasRolled.Should().BeFalse();

    [Fact]
    public void Initial_LastResult_EstNull()
        => Engine().LastResult.Should().BeNull();

    [Fact]
    public void Initial_State_EstIdle()
        => Engine().State.Should().Be(EngineState.Idle);

    [Fact]
    public void Initial_RangParDefaut_EstAventurier()
        => Engine().CurrentRank.Should().Be(Rank.Get(RankKey.Aventurier));

    // ── Roll ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Roll_ProduitsUnResultat()
    {
        var engine = Engine();
        engine.Roll();

        engine.HasRolled.Should().BeTrue();
        engine.LastResult.Should().NotBeNull();
    }

    [Fact]
    public void Roll_SuccesBornesEntre0Et3()
    {
        var engine = Engine();
        engine.Roll();

        engine.LastResult!.Successes.Should().BeInRange(0, 3);
    }

    [Fact]
    public void Roll_PalierCourantUtilise()
    {
        var engine = Engine();
        engine.ChangeRank(RankKey.Mentor); // palier 4
        engine.Roll();

        engine.LastResult!.Palier.Should().Be(4);
    }

    [Fact]
    public void Roll_AvecModificateur_PalierAjuste()
    {
        var engine = Engine();
        engine.ChangeRank(RankKey.Aventurier); // palier 6
        engine.Modifier = 2;
        engine.Roll();

        engine.LastResult!.Palier.Should().Be(4);
    }

    [Fact]
    public void Roll_AvecPalierOverride_UtiliseOverride()
    {
        var engine = Engine();
        engine.PalierOverride = 8;
        engine.Roll();

        engine.LastResult!.Palier.Should().Be(8);
    }

    [Fact]
    public void Roll_ReinitialisePalierOverride()
    {
        var engine = Engine();
        engine.PalierOverride = 8;
        engine.Roll();

        engine.PalierOverride.Should().BeNull();
    }

    [Fact]
    public void Roll_AvecAction_ActionDansLeResultat()
    {
        var engine = Engine();
        var action = new RollAction { Id = "attaque", Name = "Attaque", Contexts = ["combat"] };
        engine.Roll(action);

        engine.LastResult!.Action.Should().Be(action);
    }

    [Fact]
    public void Roll_SansAction_ActionNullDansLeResultat()
    {
        var engine = Engine();
        engine.Roll();

        engine.LastResult!.Action.Should().BeNull();
    }

    // ── Reroll ────────────────────────────────────────────────────────────────

    [Fact]
    public void Reroll_SansJetPrecedent_RetourneFalse()
    {
        var engine = Engine();
        engine.Reroll().Should().BeFalse();
    }

    [Fact]
    public void Reroll_ApresUnJet_RetourneTrue()
    {
        var engine = Engine();
        engine.Roll();
        engine.Reroll().Should().BeTrue();
    }

    [Fact]
    public void Reroll_ProduitsUnNouveauResultat()
    {
        var engine = Engine();
        engine.Roll();
        var before = engine.LastResult;
        engine.Reroll();

        // Le résultat peut être identique par chance, mais l'objet est reconstruit
        engine.LastResult.Should().NotBeNull();
    }

    [Fact]
    public void Reroll_DecrementeRerollsLeft()
    {
        var engine = Engine();
        engine.ChangeRank(RankKey.Aventurier); // 1 reroll
        engine.Roll();
        var before = engine.RerollsLeft;

        engine.Reroll();

        engine.RerollsLeft.Should().Be(before - 1);
    }

    [Fact]
    public void Reroll_QuandPlusDeRerolls_RetourneFalse()
    {
        var engine = Engine();
        engine.ChangeRank(RankKey.Novice); // 1 reroll
        engine.Roll();
        engine.Reroll(); // consomme le seul reroll

        engine.Reroll().Should().BeFalse();
    }

    // ── ChangeRank ────────────────────────────────────────────────────────────

    [Fact]
    public void ChangeRank_MetAJourLeRang()
    {
        var engine = Engine();
        engine.ChangeRank(RankKey.Veteran);
        engine.CurrentRank.Should().Be(Rank.Get(RankKey.Veteran));
    }

    [Fact]
    public void ChangeRank_ResetLaSession()
    {
        var engine = Engine();
        engine.Roll();
        engine.ChangeRank(RankKey.Mentor);

        engine.HasRolled.Should().BeFalse();
        engine.RerollsLeft.Should().Be(Rank.Get(RankKey.Mentor).Rerolls);
    }

    // ── ResetEvent ────────────────────────────────────────────────────────────

    [Fact]
    public void ResetEvent_EffaceLeJet()
    {
        var engine = Engine();
        engine.Roll();
        engine.ResetEvent();

        engine.HasRolled.Should().BeFalse();
        engine.LastResult.Should().BeNull();
    }

    [Fact]
    public void ResetEvent_ReinitialiseLesRerolls()
    {
        var engine = Engine();
        engine.ChangeRank(RankKey.Veteran); // 2 rerolls
        engine.Roll();
        engine.Reroll();
        engine.ResetEvent();

        engine.RerollsLeft.Should().Be(2);
    }

    [Fact]
    public void ResetEvent_EffacePalierOverride()
    {
        var engine = Engine();
        engine.PalierOverride = 8;
        engine.ResetEvent();

        engine.PalierOverride.Should().BeNull();
    }

    // ── Historique ────────────────────────────────────────────────────────────

    [Fact]
    public void History_AjouteEntreeApresRoll()
    {
        var engine = Engine();
        engine.Roll();

        engine.History.Should().HaveCount(1);
    }

    [Fact]
    public void History_LimitéA8Entrees()
    {
        var engine = Engine();
        for (var i = 0; i < 10; i++)
            engine.Roll();

        engine.History.Should().HaveCount(8);
    }

    [Fact]
    public void History_LaDerniereEntreeEstEnPremier()
    {
        var engine = Engine();
        engine.Roll();
        var firstResult = engine.LastResult!.Palier;
        engine.ResetEvent();
        engine.ChangeRank(RankKey.Mentor); // palier 4
        engine.Roll();

        engine.History[0].Palier.Should().Be(4);
    }

    // ── Mode GameRandom ───────────────────────────────────────────────────────

    [Fact]
    public void BeginRoll_PasseEnWaitingDice()
    {
        var engine = Engine();
        engine.BeginRoll();

        engine.State.Should().Be(EngineState.WaitingDice);
    }

    [Fact]
    public void ReceiveRandom_QuandIdle_RetourneFalse()
    {
        var engine = Engine();
        engine.ReceiveRandom(500).Should().BeFalse();
    }

    [Fact]
    public void ReceiveRandom_ApresBeginRoll_RetourneTrue()
    {
        var engine = Engine();
        engine.BeginRoll();
        engine.ReceiveRandom(500).Should().BeTrue();
    }

    [Fact]
    public void ReceiveRandom_ProduitsUnResultat()
    {
        var engine = Engine();
        engine.BeginRoll();
        engine.ReceiveRandom(789);

        engine.HasRolled.Should().BeTrue();
        engine.LastResult.Should().NotBeNull();
    }

    [Fact]
    public void ReceiveRandom_RepasseEnIdle()
    {
        var engine = Engine();
        engine.BeginRoll();
        engine.ReceiveRandom(123);

        engine.State.Should().Be(EngineState.Idle);
    }

    [Theory]
    [InlineData(789, new[] { 7, 8, 9 })]
    [InlineData(100, new[] { 1, 10, 10 })] // 0 → 10
    [InlineData(  0, new[] { 10, 10, 10 })] // 000 → tous 10
    [InlineData(  5, new[] { 10, 10, 5 })] // 005
    public void ParseRandomToDiceSet_ConvertitCorrectement(int input, int[] expectedDice)
    {
        var result = StsEngine.ParseRandomToDiceSet(input);
        result.Values.Should().BeEquivalentTo(expectedDice, opts => opts.WithStrictOrdering());
    }

    // ── GameRandom + Reroll ───────────────────────────────────────────────────

    [Fact]
    public void BeginReroll_SansJetPrecedent_RetourneFalse()
    {
        var engine = Engine();
        engine.BeginReroll().Should().BeFalse();
    }

    [Fact]
    public void BeginReroll_ApresUnJet_RetourneTrue()
    {
        var engine = Engine();
        engine.BeginRoll();
        engine.ReceiveRandom(500);

        engine.BeginReroll().Should().BeTrue();
        engine.State.Should().Be(EngineState.WaitingDice);
    }

    // ── Traits — BonusRerolls ─────────────────────────────────────────────────

    [Fact]
    public void Trait_BonusRerolls_Permanent_AugmenteRerollsLeft()
    {
        var engine = Engine();
        engine.ChangeRank(RankKey.Aventurier); // 1 reroll de base
        engine.SetEquippedTraits([
            new Trait("t1", "Test", "desc", TraitCategory.Connaissance,
                Effects: [new TraitEffect(TraitEffectType.BonusRerolls, Value: 1, Context: null)])
        ]);

        engine.RerollsLeft.Should().Be(2);
    }

    [Fact]
    public void Trait_BonusRerolls_Contextuel_NAugmentePasLeCompteurGlobal()
    {
        // Un BonusRerolls avec context n'est PAS un bonus permanent
        var engine = Engine();
        engine.ChangeRank(RankKey.Aventurier); // 1 reroll de base
        engine.SetEquippedTraits([
            new Trait("t1", "Test", "desc", TraitCategory.Connaissance,
                Effects: [new TraitEffect(TraitEffectType.BonusRerolls, Value: 1, Context: "combat")])
        ]);

        // Context non null → ne compte pas dans PermanentBonusRerolls
        engine.RerollsLeft.Should().Be(1);
    }

    // ── Traits — BonusSuccess ─────────────────────────────────────────────────

    [Fact]
    public void Trait_BonusSuccess_Contextuel_AppliqueSiContextMatch()
    {
        var engine = Engine();
        var action = new RollAction { Id = "a", Name = "A", Contexts = ["combat"] };

        engine.SetEquippedTraits([
            new Trait("t1", "Test", "desc", TraitCategory.Job,
                Effects: [new TraitEffect(TraitEffectType.BonusSuccess, Value: 1, Context: "combat")])
        ]);

        engine.Roll(action);

        engine.LastResult!.TraitEffects.BonusSuccesses.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Trait_BonusSuccess_Contextuel_NonAppliqueSiContextDifferent()
    {
        var engine = Engine();
        var action = new RollAction { Id = "a", Name = "A", Contexts = ["social"] };

        engine.SetEquippedTraits([
            new Trait("t1", "Test", "desc", TraitCategory.Job,
                Effects: [new TraitEffect(TraitEffectType.BonusSuccess, Value: 2, Context: "combat")])
        ]);

        engine.Roll(action);

        engine.LastResult!.TraitEffects.BonusSuccesses.Should().Be(0);
    }

    // ── Traits — MalusSuccess ─────────────────────────────────────────────────

    [Fact]
    public void Trait_MalusSuccess_AppliqueSiContextMatch()
    {
        var engine = Engine();
        var action = new RollAction { Id = "a", Name = "A", Contexts = ["combat"] };

        engine.SetEquippedTraits([
            new Trait("t1", "Test", "desc", TraitCategory.Job,
                Effects: [new TraitEffect(TraitEffectType.MalusSuccess, Value: 1, Context: "combat")])
        ]);

        engine.Roll(action);

        engine.LastResult!.TraitEffects.MalusSuccesses.Should().Be(1);
    }

    [Fact]
    public void Trait_MalusSuccess_SuccesTotalJamaisNegatif()
    {
        // Même avec beaucoup de malus, Successes >= 0
        var engine = Engine();
        var action = new RollAction { Id = "a", Name = "A", Contexts = ["combat"] };

        engine.SetEquippedTraits([
            new Trait("t1", "Test", "desc", TraitCategory.Job,
                Effects: [new TraitEffect(TraitEffectType.MalusSuccess, Value: 99, Context: "combat")])
        ]);

        engine.Roll(action);

        engine.LastResult!.Successes.Should().Be(0);
    }

    // ── Traits — ForceRollMode ────────────────────────────────────────────────

    [Fact]
    public void Trait_ForceRollMode_Avantage_PresenceDeDeuxSets()
    {
        var engine = Engine();
        engine.Mode = RollMode.Normal;
        var action = new RollAction { Id = "a", Name = "A", Contexts = ["combat"] };

        engine.SetEquippedTraits([
            new Trait("t1", "Test", "desc", TraitCategory.Job,
                Effects: [new TraitEffect(TraitEffectType.ForceRollMode, ForcedMode: RollMode.Avantage, Context: "combat")])
        ]);

        engine.Roll(action);

        // En avantage, Rejected est non null
        engine.LastResult!.Rejected.Should().NotBeNull();
    }

    // ── Traits — BonusSuccessOnZero ───────────────────────────────────────────

    [Fact]
    public void Trait_BonusSuccessOnZero_SansZero_PasDeBonus()
    {
        // On injecte un DiceSet sans zéro via GameRandom : 111
        var engine = Engine();
        engine.SetEquippedTraits([
            new Trait("t1", "Test", "desc", TraitCategory.Connaissance,
                Effects: [new TraitEffect(TraitEffectType.BonusSuccessOnZero, Value: 2)])
        ]);

        engine.BeginRoll();
        engine.ReceiveRandom(111); // dés 1,1,1 → pas de 10

        engine.LastResult!.TraitEffects.BonusSuccesses.Should().Be(0);
    }

    [Fact]
    public void Trait_BonusSuccessOnZero_AvecZero_AppliqueBonus()
    {
        // 900 → dés 9,10,10 → deux zéros
        var engine = Engine();
        engine.SetEquippedTraits([
            new Trait("t1", "Test", "desc", TraitCategory.Connaissance,
                Effects: [new TraitEffect(TraitEffectType.BonusSuccessOnZero, Value: 2)])
        ]);

        engine.BeginRoll();
        engine.ReceiveRandom(900); // dés 9,10,10 → hasZero = true

        engine.LastResult!.TraitEffects.BonusSuccesses.Should().Be(2);
    }

    // ── Traits — BonusSuccessOnReroll ─────────────────────────────────────────

    [Fact]
    public void Trait_BonusSuccessOnReroll_AppliqueApresReroll()
    {
        var engine = Engine();
        var action = new RollAction { Id = "a", Name = "A", Contexts = ["combat"] };

        engine.SetEquippedTraits([
            new Trait("t1", "Acharnement", "desc", TraitCategory.Job,
                Effects: [new TraitEffect(TraitEffectType.BonusSuccessOnReroll, Value: 1, Context: "combat")])
        ]);

        engine.BeginRoll(action);
        engine.ReceiveRandom(500); // premier jet

        engine.BeginReroll();
        engine.ReceiveRandom(500); // reroll

        engine.LastResult!.TraitEffects.BonusSuccesses.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Trait_BonusSuccessOnReroll_NonAppliqueSurJetInitial()
    {
        var engine = Engine();
        var action = new RollAction { Id = "a", Name = "A", Contexts = ["combat"] };

        engine.SetEquippedTraits([
            new Trait("t1", "Acharnement", "desc", TraitCategory.Job,
                Effects: [new TraitEffect(TraitEffectType.BonusSuccessOnReroll, Value: 1, Context: "combat")])
        ]);

        engine.BeginRoll(action);
        engine.ReceiveRandom(500); // premier jet seulement

        // Le bonus OnReroll ne s'applique pas lors du jet initial
        engine.LastResult!.TraitEffects.BonusSuccesses.Should().Be(0);
    }

    // ── PalierOverride ────────────────────────────────────────────────────────

    [Fact]
    public void PalierOverride_ForceLesPalierIndependammentDuRang()
    {
        var engine = Engine();
        engine.ChangeRank(RankKey.Mentor); // palier normal = 4
        engine.PalierOverride = 8;

        engine.EffectivePalier.Should().Be(8);
    }

    [Fact]
    public void PalierOverride_NullAprésResetEvent()
    {
        var engine = Engine();
        engine.PalierOverride = 8;
        engine.ResetEvent();

        engine.PalierOverride.Should().BeNull();
    }

    // ── Mode Avantage / Désavantage ───────────────────────────────────────────

    [Fact]
    public void ModeAvantage_RejectedNonNull()
    {
        var engine = Engine();
        engine.Mode = RollMode.Avantage;
        engine.Roll();

        engine.LastResult!.Rejected.Should().NotBeNull();
    }

    [Fact]
    public void ModeDesavantage_RejectedNonNull()
    {
        var engine = Engine();
        engine.Mode = RollMode.Desavantage;
        engine.Roll();

        engine.LastResult!.Rejected.Should().NotBeNull();
    }

    [Fact]
    public void ModeNormal_RejectedNull()
    {
        var engine = Engine();
        engine.Mode = RollMode.Normal;
        engine.Roll();

        engine.LastResult!.Rejected.Should().BeNull();
    }
}
