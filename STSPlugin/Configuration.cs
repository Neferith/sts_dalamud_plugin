using Dalamud.Configuration;
using System;


namespace STSPlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>Rang persisté entre sessions.</summary>
    public string LastRank { get; set; } = "aventurier";

    /// <summary>Si true, /sts roll poste le résultat dans le chat.</summary>
    public bool EchoToChat { get; set; } = true;

    /// <summary>Channel cible. Ex : "say", "party", "ls1", "cwls1"…</summary>
    public string ChatChannel { get; set; } = "say";

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}

