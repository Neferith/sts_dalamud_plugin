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
    private readonly CharacterStore _store;

    public DefaultSetActiveCharacterUseCase(
        Configuration configuration,
        CharacterStore store)
    {
        _configuration = configuration;
        _store = store;
    }

    public void Execute(Character? character)
    {
        _configuration.ActiveCharacterId = character?.Id;
        _configuration.Save();
        _store.SetActive(character?.Id); // → OnActiveChanged → RefreshEquippedTraits → ChangeRank
    }
}
