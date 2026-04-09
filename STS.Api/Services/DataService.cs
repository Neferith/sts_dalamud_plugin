using System.Text.Json;
using Sts.Api.Models;

namespace Sts.Api.Services;

/// <summary>
/// Service singleton qui gère la lecture et l'écriture du data.json.
/// Thread-safe via ReaderWriterLockSlim.
/// </summary>
public class DataService
{
    private readonly string _filePath;
    private readonly ILogger<DataService> _logger;

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private DataModel _model = new();
    private string _rawJsonCache = "{}";
    private readonly ReaderWriterLockSlim _lock = new();

    public DataService(IConfiguration configuration, ILogger<DataService> logger)
    {
        _logger = logger;

        var configured = configuration["Sts:DataFilePath"] ?? "data.json";
        _filePath = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured);

        _logger.LogInformation("DataService initialisé. Fichier : {Path}", _filePath);
        Load();
    }

    // ─── Lecture publique ──────────────────────────────────────────────────────

    /// <summary>
    /// Retourne le contenu JSON brut — utilisé par le plugin via GET /api/data.
    /// </summary>
    public string GetRawJson()
    {
        _lock.EnterReadLock();
        try { return _rawJsonCache; }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Retourne la liste de tous les jobs.</summary>
    public List<JobData> GetJobs()
    {
        _lock.EnterReadLock();
        try { return [.. _model.Jobs]; }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Retourne un job par son identifiant, ou null s'il n'existe pas.</summary>
    public JobData? GetJob(string id)
    {
        _lock.EnterReadLock();
        try { return _model.Jobs.FirstOrDefault(j => j.Id == id); }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Retourne la liste de tous les traits.</summary>
    public List<TraitData> GetTraits()
    {
        _lock.EnterReadLock();
        try { return [.. _model.Traits]; }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Retourne un trait par son identifiant, ou null s'il n'existe pas.</summary>
    public TraitData? GetTrait(string id)
    {
        _lock.EnterReadLock();
        try { return _model.Traits.FirstOrDefault(t => t.Id == id); }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Retourne la liste de toutes les compétences.</summary>
    public List<AbilityData> GetAbilities()
    {
        _lock.EnterReadLock();
        try { return [.. _model.Abilities]; }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Retourne une compétence par son identifiant, ou null si elle n'existe pas.</summary>
    public AbilityData? GetAbility(string id)
    {
        _lock.EnterReadLock();
        try { return _model.Abilities.FirstOrDefault(a => a.Id == id); }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Retourne la liste de toutes les actions.</summary>
    public List<ActionData> GetActions()
    {
        _lock.EnterReadLock();
        try { return [.. _model.Actions]; }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Retourne une action par son identifiant, ou null si elle n'existe pas.</summary>
    public ActionData? GetAction(string id)
    {
        _lock.EnterReadLock();
        try { return _model.Actions.FirstOrDefault(a => a.Id == id); }
        finally { _lock.ExitReadLock(); }
    }

    // ─── Écriture : Jobs ──────────────────────────────────────────────────────

    /// <summary>
    /// Ajoute un job.
    /// </summary>
    /// <returns>True si ajouté, false si un job avec le même Id existe déjà.</returns>
    public bool AddJob(JobData job)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_model.Jobs.Any(j => j.Id == job.Id)) return false;
            _model.Jobs.Add(job);
            Persist();
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>
    /// Met à jour un job existant.
    /// </summary>
    /// <returns>True si mis à jour, false si non trouvé.</returns>
    public bool UpdateJob(string id, JobData updated)
    {
        _lock.EnterWriteLock();
        try
        {
            var index = _model.Jobs.FindIndex(j => j.Id == id);
            if (index < 0) return false;
            updated.Id = id; // L'Id ne change pas
            _model.Jobs[index] = updated;
            Persist();
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>
    /// Supprime un job.
    /// </summary>
    /// <returns>True si supprimé, false si non trouvé.</returns>
    public bool DeleteJob(string id)
    {
        _lock.EnterWriteLock();
        try
        {
            var removed = _model.Jobs.RemoveAll(j => j.Id == id) > 0;
            if (removed) Persist();
            return removed;
        }
        finally { _lock.ExitWriteLock(); }
    }

    // ─── Écriture : Traits ────────────────────────────────────────────────────

    /// <summary>Ajoute un trait.</summary>
    /// <returns>True si ajouté, false si l'Id existe déjà.</returns>
    public bool AddTrait(TraitData trait)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_model.Traits.Any(t => t.Id == trait.Id)) return false;
            _model.Traits.Add(trait);
            Persist();
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Met à jour un trait existant.</summary>
    /// <returns>True si mis à jour, false si non trouvé.</returns>
    public bool UpdateTrait(string id, TraitData updated)
    {
        _lock.EnterWriteLock();
        try
        {
            var index = _model.Traits.FindIndex(t => t.Id == id);
            if (index < 0) return false;
            updated.Id = id;
            _model.Traits[index] = updated;
            Persist();
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Supprime un trait.</summary>
    /// <returns>True si supprimé, false si non trouvé.</returns>
    public bool DeleteTrait(string id)
    {
        _lock.EnterWriteLock();
        try
        {
            var removed = _model.Traits.RemoveAll(t => t.Id == id) > 0;
            if (removed) Persist();
            return removed;
        }
        finally { _lock.ExitWriteLock(); }
    }

    // ─── Écriture : Abilities ─────────────────────────────────────────────────

    /// <summary>Ajoute une compétence.</summary>
    /// <returns>True si ajoutée, false si l'Id existe déjà.</returns>
    public bool AddAbility(AbilityData ability)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_model.Abilities.Any(a => a.Id == ability.Id)) return false;
            _model.Abilities.Add(ability);
            Persist();
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Met à jour une compétence existante.</summary>
    /// <returns>True si mise à jour, false si non trouvée.</returns>
    public bool UpdateAbility(string id, AbilityData updated)
    {
        _lock.EnterWriteLock();
        try
        {
            var index = _model.Abilities.FindIndex(a => a.Id == id);
            if (index < 0) return false;
            updated.Id = id;
            _model.Abilities[index] = updated;
            Persist();
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Supprime une compétence.</summary>
    /// <returns>True si supprimée, false si non trouvée.</returns>
    public bool DeleteAbility(string id)
    {
        _lock.EnterWriteLock();
        try
        {
            var removed = _model.Abilities.RemoveAll(a => a.Id == id) > 0;
            if (removed) Persist();
            return removed;
        }
        finally { _lock.ExitWriteLock(); }
    }

    // ─── Écriture : Actions ───────────────────────────────────────────────────

    /// <summary>Ajoute une action.</summary>
    /// <returns>True si ajoutée, false si l'Id existe déjà.</returns>
    public bool AddAction(ActionData action)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_model.Actions.Any(a => a.Id == action.Id)) return false;
            _model.Actions.Add(action);
            Persist();
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Met à jour une action existante.</summary>
    /// <returns>True si mise à jour, false si non trouvée.</returns>
    public bool UpdateAction(string id, ActionData updated)
    {
        _lock.EnterWriteLock();
        try
        {
            var index = _model.Actions.FindIndex(a => a.Id == id);
            if (index < 0) return false;
            updated.Id = id;
            _model.Actions[index] = updated;
            Persist();
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Supprime une action.</summary>
    /// <returns>True si supprimée, false si non trouvée.</returns>
    public bool DeleteAction(string id)
    {
        _lock.EnterWriteLock();
        try
        {
            var removed = _model.Actions.RemoveAll(a => a.Id == id) > 0;
            if (removed) Persist();
            return removed;
        }
        finally { _lock.ExitWriteLock(); }
    }

    // ─── Privé ────────────────────────────────────────────────────────────────

    /// <summary>Charge le data.json depuis le disque et initialise le modèle en mémoire.</summary>
    private void Load()
    {
        _lock.EnterWriteLock();
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogWarning("Fichier data.json introuvable : {Path}", _filePath);
                _model = new DataModel();
                _rawJsonCache = "{}";
                return;
            }

            var raw = File.ReadAllText(_filePath);
            var model = JsonSerializer.Deserialize<DataModel>(raw, JsonReadOptions);

            if (model is null)
            {
                _logger.LogError("Impossible de désérialiser data.json — modèle null.");
                return;
            }

            _model = model;
            _rawJsonCache = raw;
            _logger.LogInformation(
                "data.json chargé — {Jobs} jobs, {Traits} traits, {Abilities} compétences, {Actions} actions.",
                _model.Jobs.Count, _model.Traits.Count, _model.Abilities.Count, _model.Actions.Count);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "data.json invalide — JSON malformé.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement de data.json.");
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>
    /// Sérialise le modèle en mémoire et l'écrit sur le disque.
    /// Doit être appelé sous verrou d'écriture.
    /// </summary>
    private void Persist()
    {
        try
        {
            var json = JsonSerializer.Serialize(_model, JsonWriteOptions);
            File.WriteAllText(_filePath, json);
            _rawJsonCache = json;
            _logger.LogInformation("data.json persisté ({Bytes} octets).", json.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'écriture de data.json.");
            throw;
        }
    }
}
