using Sts.Domain.Character;
using System;

namespace STSPlugin;

/// <summary>
/// État partagé du personnage actif.
/// Notifie les abonnés via <see cref="OnChanged"/> à chaque changement.
/// </summary>
public class ActiveCharacterState
{
    private Character? _current;

    /// <summary>Personnage actif, ou null si aucun.</summary>
    public Character? Current => _current;

    /// <summary>Déclenché après chaque appel à <see cref="Set"/>.</summary>
    public event Action? OnChanged;

    /// <summary>Définit le personnage actif et notifie les abonnés.</summary>
    public void Set(Character? character)
    {
        _current = character;
        OnChanged?.Invoke();
    }
}
