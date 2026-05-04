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
    private readonly CharacterStore _store;

    public DefaultSetActiveCharacterUseCase(
        Configuration configuration,
        StsEngine engine,
        CharacterStore store)
    {
        _configuration = configuration;
        _engine = engine;
        _store = store;
    }

    public void Execute(Character? character)
    {
        _configuration.ActiveCharacterId = character?.Id;
        _configuration.Save();
        if (character != null) _engine.ChangeRank(character.RankKey);
        _store.SetActive(character?.Id);
    }
}
