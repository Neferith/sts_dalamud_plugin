using Sts.Domain.Character;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STSPlugin;

/// <summary>
/// Source de vérité unique pour les personnages.
/// Toute lecture ou mutation passe par ici.
/// </summary>
public class CharacterStore
{
    private IReadOnlyList<Character> _all = [];
    private Guid? _active;

    // ── Lecture ───────────────────────────────────────────────────────────────

    public IReadOnlyList<Character> All => _all;
    public Character? Active => _active.HasValue ? _all.FirstOrDefault(c => c.Id == _active.Value) : null;

    // ── Événements ────────────────────────────────────────────────────────────

    /// <summary>Déclenché quand la liste complète change (après un fetch API).</summary>
    public event Action? OnListChanged;

    /// <summary>Déclenché quand le personnage actif change.</summary>
    public event Action? OnActiveChanged;

    // ── Mutations ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Remplace la liste complète.
    /// Si le personnage actif est présent dans la nouvelle liste, sa référence est mise à jour.
    /// </summary>
    public void SetAll(IReadOnlyList<Character> characters)
    {
        _all = characters;

        // Synchroniser l'actif avec la nouvelle liste
        if (_active != null)
        {
            var fresh = characters.FirstOrDefault(c => c.Id == _active);
            if (fresh != null)
                _active = fresh.Id;
            // Si disparu de la liste, on garde l'ancienne référence —
            // c'est SetActive(null) qui doit être appelé explicitement si besoin
        }

        OnListChanged?.Invoke();
    }

    /// <summary>
    /// Définit le personnage actif.
    /// </summary>
    public void SetActive(Guid? id)
    {
        _active = id;
        OnActiveChanged?.Invoke();
    }

    /// <summary>
    /// Met à jour une fiche dans la liste ET dans Active si c'est la même.
    /// À appeler après toute mutation persistée (UpdateCharacter, EquipTrait, etc.).
    /// </summary>
    public void Replace(Character updated)
    {
        var list = _all.ToList();
        var index = list.FindIndex(c => c.Id == updated.Id);
        if (index >= 0)
            list[index] = updated;
        _all = list;

        if (_active == updated.Id)
        {
            _active = updated.Id;
            OnActiveChanged?.Invoke();
        }

        OnListChanged?.Invoke();
    }
}
