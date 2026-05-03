using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sts.Domain.DataSource;

/// <summary>Modèle racine du fichier data.json.</summary>
public class DataModel
{
    [JsonPropertyName("jobs")]
    public List<JobData> Jobs { get; set; } = [];

    [JsonPropertyName("traits")]
    public List<TraitData> Traits { get; set; } = [];

    [JsonPropertyName("actions")]
    public List<ActionData> Actions { get; set; } = [];

    [JsonPropertyName("abilities")]
    public List<AbilityData> Abilities { get; set; } = [];
}

/// <summary>Modèle JSON d'un job.</summary>
public class JobData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>Modèle JSON d'un trait.</summary>
public class TraitData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("requiredJobId")]
    public string? RequiredJobId { get; set; } = null;

    [JsonPropertyName("requiredJobIds")]
    public List<string>? RequiredJobIds { get; set; } = null;

    [JsonPropertyName("exclusiveGroup")]
    public string? ExclusiveGroup { get; set; } = null;

    [JsonPropertyName("effects")]
    public List<TraitEffectData> Effects { get; set; } = [];
}

/// <summary>Modèle JSON d'un effet de trait.</summary>
public class TraitEffectData
{
    /// <summary>Type d'effet (ex : "BonusRerolls", "ForceRollMode"...).</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Valeur numérique de l'effet. 0 par défaut.</summary>
    [JsonPropertyName("value")]
    public int Value { get; set; } = 0;

    /// <summary>Mode de jet imposé pour ForceRollMode (ex : "Avantage", "Desavantage").</summary>
    [JsonPropertyName("forcedMode")]
    public string? ForcedMode { get; set; } = null;

    /// <summary>Contexte conditionnel. Null = effet permanent.</summary>
    [JsonPropertyName("context")]
    public string? Context { get; set; } = null;
}

/// <summary>Modèle JSON d'une compétence.</summary>
public class AbilityData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("requiredJobId")]
    public string? RequiredJobId { get; set; } = null;

    [JsonPropertyName("requiredJobIds")]
    public List<string>? RequiredJobIds { get; set; } = null;

    [JsonPropertyName("usageLimit")]
    public string? UsageLimit { get; set; } = null;

    [JsonPropertyName("startLevel")]
    public int StartLevel { get; set; } = 1;

    [JsonPropertyName("levels")]
    public List<AbilityLevelData> Levels { get; set; } = [];
}

/// <summary>Modèle JSON d'un niveau de compétence.</summary>
public class AbilityLevelData
{
    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>Modèle JSON d'une action de jet prédéfinie.</summary>
public class ActionData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("contexts")]
    public List<string> Contexts { get; set; } = [];

    [JsonPropertyName("requirements")]
    public List<string> Requirements { get; set; } = [];
}
