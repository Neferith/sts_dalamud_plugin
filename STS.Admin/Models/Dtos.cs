using System.Text.Json.Serialization;

namespace Sts.Admin.Models;

// ─── Auth ─────────────────────────────────────────────────────────────────────

public class LoginResponse
{
    // L'API retourne "Token" (PascalCase) — PropertyNameCaseInsensitive gère ça
    public string Token { get; set; } = "";
}

// ─── Jobs ─────────────────────────────────────────────────────────────────────

public class JobDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    public JobDto Clone() => new() { Id = Id, Name = Name, IconUrl = IconUrl, };
}

// ─── Traits ───────────────────────────────────────────────────────────────────

public class TraitDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("requiredJobIds")]
    public List<string>? RequiredJobIds { get; set; }

    [JsonPropertyName("exclusiveGroup")]
    public string? ExclusiveGroup { get; set; }

    [JsonPropertyName("effects")]
    public List<TraitEffectDto> Effects { get; set; } = [];

    [JsonPropertyName("usageLimit")]
    public string? UsageLimit { get; set; }

    public TraitDto Clone() => new()
    {
        Id            = Id,
        Name          = Name,
        Description   = Description,
        Category      = Category,
        RequiredJobIds = RequiredJobIds is null ? null : [.. RequiredJobIds],
        ExclusiveGroup = ExclusiveGroup,
        Effects       = Effects.Select(e => e.Clone()).ToList(),
        UsageLimit = UsageLimit
    };
}

public class TraitEffectDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "BonusRerolls";

    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("forcedMode")]
    public string? ForcedMode { get; set; }

    [JsonPropertyName("context")]
    public string? Context { get; set; }

    public TraitEffectDto Clone() => new()
    {
        Type       = Type,
        Value      = Value,
        ForcedMode = ForcedMode,
        Context    = Context,
    };
}

// ─── Abilities ────────────────────────────────────────────────────────────────

public class AbilityDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Job";

    [JsonPropertyName("requiredJobIds")]
    public List<string>? RequiredJobIds { get; set; }

    [JsonPropertyName("usageLimit")]
    public string? UsageLimit { get; set; }

    [JsonPropertyName("startLevel")]
    public int StartLevel { get; set; } = 1;

    [JsonPropertyName("levels")]
    public List<AbilityLevelDto> Levels { get; set; } = [];

    public AbilityDto Clone() => new()
    {
        Id            = Id,
        Name          = Name,
        Category      = Category,
        RequiredJobIds = RequiredJobIds is null ? null : [.. RequiredJobIds],
        UsageLimit    = UsageLimit,
        StartLevel    = StartLevel,
        Levels        = Levels.Select(l => l.Clone()).ToList(),
    };
}

public class AbilityLevelDto
{
    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    public AbilityLevelDto Clone() => new() { Level = Level, Description = Description };
}

// ─── Actions ──────────────────────────────────────────────────────────────────

public class ActionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("contexts")]
    public List<string> Contexts { get; set; } = [];

    [JsonPropertyName("requirements")]
    public List<string> Requirements { get; set; } = [];

    public ActionDto Clone() => new()
    {
        Id           = Id,
        Name         = Name,
        Contexts     = [.. Contexts],
        Requirements = [.. Requirements],
    };
}

// ─── Constantes métier ────────────────────────────────────────────────────────

public static class StsConstants
{
    public static readonly string[] TraitCategories =
        ["Origine", "Connaissance", "RoleDps", "RoleSoigneur", "RoleTank", "Job"];

    public static readonly string[] AbilityCategories =
        ["Weapon", "RoleDps", "RoleSoigneur", "RoleTank", "Job"];

    public static readonly string[] TraitEffectTypes =
        ["BonusRerolls", "BonusPalier", "ForceRollMode", "BonusSuccessOnZero",
         "BonusSuccess", "MalusSuccess", "BonusSuccessOnReroll", "Manual"];

    public static readonly string[] RollModes = ["Avantage", "Desavantage"];

    public static readonly string[] UsageLimits =
        ["None", "OncePerCombat", "TwicePerCombat", "OncePerEvent", "TwicePerEvent", "ThreeTimesPerEvent"];
}
