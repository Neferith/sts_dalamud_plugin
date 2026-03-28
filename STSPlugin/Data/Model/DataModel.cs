using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace STSPlugin.DataSource;

/// <summary>Modèle racine du fichier data.json.</summary>
public class DataModel
{
    [JsonPropertyName("jobs")]
    public List<JobData> Jobs { get; set; } = [];

    [JsonPropertyName("traits")]
    public List<TraitData> Traits { get; set; } = [];
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
