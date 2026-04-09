using System;
using System.Collections.Generic;
using System.Text;

namespace STSPlugin.ConfigDomain
{
    /// <summary>Source des valeurs de dés utilisées lors d'un jet.</summary>
    public enum RollSource
    {
        /// <summary>RNG interne du plugin.</summary>
        Internal,
        /// <summary>/random du jeu — vérifiable par tous.</summary>
        GameRandom
    }

}
