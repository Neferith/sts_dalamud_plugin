using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sts.Domain.User;

namespace Sts.Api.Services;

/// <summary>
/// Service hébergé exécuté au démarrage de l'API.
/// Lit les identifiants admin depuis la configuration et crée le compte
/// s'il n'existe pas encore en base.
///
/// Configuration attendue dans <c>appsettings.json</c> / variables d'environnement :
/// <code>
/// "Admin": {
///   "Username": "...",
///   "Password": "..."
/// }
/// </code>
/// </summary>
public class AdminSeedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration       _configuration;
    private readonly ILogger<AdminSeedService> _logger;

    public AdminSeedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AdminSeedService> logger)
    {
        _scopeFactory  = scopeFactory;
        _configuration = configuration;
        _logger        = logger;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var username = _configuration["Admin:Username"];
        var password = _configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "AdminSeedService: Admin:Username ou Admin:Password absent de la configuration. " +
                "Aucun compte admin ne sera créé automatiquement.");
            return;
        }

        await using var scope   = _scopeFactory.CreateAsyncScope();
        var seedAdmin = scope.ServiceProvider.GetRequiredService<ISeedAdminUseCase>();

        await seedAdmin.ExecuteAsync(username, password);
        _logger.LogInformation("AdminSeedService: seed admin terminé pour '{Username}'.", username);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
