namespace Sts.Domain.Character;

/// <summary>Race jouable dans FFXIV / STS.</summary>
public enum CharacterRace
{
    Hyur,
    Elezen,
    Miqote,
    Lalafell,
    Roegadyn,
    AuRa,
    Hrothgar,
    Viera,
    Garlean,
}

/// <summary>Extensions d'affichage pour <see cref="CharacterRace"/>.</summary>
public static class CharacterRaceExtensions
{
    /// <summary>Retourne le label affiché de la race.</summary>
    public static string Label(this CharacterRace race) => race switch
    {
        CharacterRace.Hyur     => "Hyur",
        CharacterRace.Elezen   => "Elezen",
        CharacterRace.Miqote   => "Miqo'te",
        CharacterRace.Lalafell => "Lalafell",
        CharacterRace.Roegadyn => "Roegadyn",
        CharacterRace.AuRa     => "Au Ra",
        CharacterRace.Hrothgar => "Hrothgar",
        CharacterRace.Viera    => "Viera",
        CharacterRace.Garlean  => "Garlemaldais(e)",
        _                      => race.ToString(),
    };
}
