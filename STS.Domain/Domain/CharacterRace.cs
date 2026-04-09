namespace Sts.Domain;

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

public static class CharacterRaceExtensions
{
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
