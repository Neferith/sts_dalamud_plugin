namespace STS.Domain;

/// <summary>
/// Interface de logging pour le domaine STS.
/// </summary>
public interface IStsLogger
{
    /// <summary>Journalise un message de debug.</summary>
    void Debug(string message);

    /// <summary>Journalise un avertissement.</summary>
    void Warning(string message);

    /// <summary>Journalise une erreur.</summary>
    void Error(string message);
}

/// <summary>
/// Implémentation no-op utilisée par défaut quand aucun logger n'est injecté.
/// </summary>
public sealed class NullStsLogger : IStsLogger
{
    /// <summary>Instance partagée.</summary>
    public static readonly IStsLogger Instance = new NullStsLogger();

    /// <inheritdoc/>
    public void Debug(string message) { }

    /// <inheritdoc/>
    public void Warning(string message) { }

    /// <inheritdoc/>
    public void Error(string message) { }
}
