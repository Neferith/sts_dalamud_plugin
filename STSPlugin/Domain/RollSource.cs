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

    /// <summary>Source des données de référence (jobs, traits, actions, compétences).</summary>
    public enum DataSourceMode
    {
        /// <summary>Lecture du data.json embarqué dans le plugin.</summary>
        Local,
        /// <summary>Récupération depuis l'API distante.</summary>
        Remote,
    }

}
