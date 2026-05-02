namespace STS.Domain;

/// <summary>
/// Interface de logging pour le domaine STS.
/// </summary>
/// <remarks>
/// Les surcharges sans <see cref="Exception"/> sont fournies par défaut
/// et délèguent à la surcharge principale — les implémentations n'ont
/// qu'une seule méthode par niveau à redéfinir.
/// </remarks>
public interface IStsLogger
{
    /// <summary>Journalise un message d'information.</summary>
    void Information(string message);

    /// <summary>Journalise un message de debug.</summary>
    void Debug(string message);

    /// <summary>Journalise un avertissement, avec exception optionnelle.</summary>
    /// <param name="exception">Exception associée, ou <c>null</c>.</param>
    /// <param name="message">Modèle de message (syntaxe positionnelle).</param>
    /// <param name="values">Valeurs substituées dans le modèle.</param>
    void Warning(Exception? exception, string message, params object[] values);

    /// <summary>Journalise un avertissement sans exception.</summary>
    /// <param name="message">Modèle de message (syntaxe positionnelle).</param>
    /// <param name="values">Valeurs substituées dans le modèle.</param>
    void Warning(string message, params object[] values)
        => Warning(null, message, values);

    /// <summary>Journalise une erreur, avec exception optionnelle.</summary>
    /// <param name="exception">Exception associée, ou <c>null</c>.</param>
    /// <param name="message">Modèle de message (syntaxe positionnelle).</param>
    /// <param name="values">Valeurs substituées dans le modèle.</param>
    void Error(Exception? exception, string message, params object[] values);

    /// <summary>Journalise une erreur sans exception.</summary>
    /// <param name="message">Modèle de message (syntaxe positionnelle).</param>
    /// <param name="values">Valeurs substituées dans le modèle.</param>
    void Error(string message, params object[] values)
        => Error(null, message, values);
}

/// <summary>
/// Implémentation no-op utilisée par défaut quand aucun logger n'est injecté.
/// </summary>
public sealed class NullStsLogger : IStsLogger
{
    /// <summary>Instance partagée unique.</summary>
    public static readonly IStsLogger Instance = new NullStsLogger();

    private NullStsLogger() { }

    /// <inheritdoc/>
    public void Information(string message) { }

    /// <inheritdoc/>
    public void Debug(string message) { }

    /// <inheritdoc/>
    public void Warning(Exception? exception, string message, params object[] values) { }

    /// <inheritdoc/>
    public void Error(Exception? exception, string message, params object[] values) { }
}
