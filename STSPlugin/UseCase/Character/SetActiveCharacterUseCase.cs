using Sts.Domain;
using Sts.Domain.Character;

namespace STSPlugin.CharacterUseCases;

public interface SetActiveCharacterUseCase
{
    void Execute(Character? character);
}

public class DefaultSetActiveCharacterUseCase : SetActiveCharacterUseCase
{
    private readonly Configuration _configuration;
    private readonly StsEngine _engine;
    private readonly ActiveCharacterState _activeCharacterState;

    public DefaultSetActiveCharacterUseCase(
        Configuration configuration,
        StsEngine engine,
        ActiveCharacterState activeCharacterState)
    {
        _configuration = configuration;
        _engine = engine;
        _activeCharacterState = activeCharacterState;
    }

    public void Execute(Character? character)
    {
        _configuration.ActiveCharacterId = character?.Id;
        _configuration.Save();

        if (character != null)
            _engine.ChangeRank(character.RankKey);

        _activeCharacterState.Set(character); // notifie OnChanged
    }
}
