using Sts.Domain.Character;
using STSPlugin;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public class StoreUpdatingGetAllCharactersUseCase : IGetAllCharactersUseCase
{
    private readonly IGetAllCharactersUseCase _inner;
    private readonly CharacterStore _store;
    private readonly Configuration _configuration;

    public StoreUpdatingGetAllCharactersUseCase(
        IGetAllCharactersUseCase inner,
        CharacterStore store,
    Configuration configuration)
    {
        _inner = inner;
        _store = store;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<Character>> ExecuteAsync()
    {
        var characters = await _inner.ExecuteAsync();
        _store.SetAll(characters);
        _store.SetActive(_configuration.ActiveCharacterId); // resync l'actif avec la liste fraîche
        return characters;
    }
}
