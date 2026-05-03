using Dalamud.Plugin.Services;
using STS.Domain;
using System;

/// <summary>
/// Implémentation de <see cref="IStsLogger"/> utilisant le système de log Dalamud.
/// </summary>
public sealed class DalamudStsLogger : IStsLogger
{
    private readonly IPluginLog _log;

    /// <summary>Initialise le logger avec le service Dalamud fourni.</summary>
    /// <param name="log">Service de logging Dalamud.</param>
    public DalamudStsLogger(IPluginLog log) => _log = log;

    /// <inheritdoc/>
    public void Information(string message) => _log.Information(message);

    /// <inheritdoc/>
    public void Debug(string message) => _log.Debug(message);

    /// <inheritdoc/>
    public void Warning(Exception? exception, string message, params object[] values)
        => _log.Warning(exception, message, values);

    /// <inheritdoc/>
    public void Error(Exception? exception, string message, params object[] values)
        => _log.Error(exception, message, values);
}
