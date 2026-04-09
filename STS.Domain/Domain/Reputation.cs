using System;

namespace Sts.Domain;

public static class Reputation
{
    public const int Min = -5;
    public const int Max = 10;

    public static string GetLabel(int level) => level switch
    {
        -5 => "Criminel notoire",
        -4 => "Mis à prix",
        -3 => "Surveillé",
        -2 => "Hostile",
        -1 => "Suspect",
         0 => "Inexistant",
         1 => "Insignifiant",
         2 => "Mineur",
         3 => "Faible",
         4 => "Modeste",
         5 => "Modéré",
         6 => "Reconnu",
         7 => "Considérable",
         8 => "Héroïque",
         9 => "Régionale",
        10 => "Légendaire",
         _ => level.ToString(),
    };

    public static int Clamp(int level) => Math.Clamp(level, Min, Max);
}
