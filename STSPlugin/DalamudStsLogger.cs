using Dalamud.Plugin.Services;
using STS.Domain;

public sealed class DalamudStsLogger : IStsLogger
{
    private readonly IPluginLog _log;

    public DalamudStsLogger(IPluginLog log) => _log = log;

    public void Debug(string message) => _log.Debug(message);
    public void Warning(string message) => _log.Warning(message);
    public void Error(string message) => _log.Error(message);
}
