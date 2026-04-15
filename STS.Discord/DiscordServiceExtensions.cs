using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sts.Discord.Decorators;
using Sts.Domain.Content.UseCases;

namespace Sts.Discord;

/// <summary>
/// Extensions d'enregistrement des services Discord pour <see cref="IServiceCollection"/>.
/// </summary>
public static class DiscordServiceExtensions
{
    /// <summary>
    /// Enregistre le bot Discord si <c>Discord:BotToken</c> est présent dans la configuration,
    /// sinon enregistre un <see cref="NullDiscordPublisher"/> silencieux.
    /// Les use cases de posts sont automatiquement décorés pour notifier Discord.
    /// </summary>
    /// <param name="services">Collection de services.</param>
    /// <param name="configuration">Configuration de l'application.</param>
    /// <param name="mappingsFilePath">
    /// Chemin vers le fichier <c>discord-mappings.json</c>.
    /// Par défaut : <c>discord-mappings.json</c> à la racine du répertoire courant.
    /// </param>
    /// <returns>La collection de services pour le chaînage.</returns>
    public static IServiceCollection AddDiscordBot(
        this IServiceCollection services,
        IConfiguration configuration,
        string? mappingsFilePath = null)
    {
        var token = configuration["Discord:BotToken"];

        if (string.IsNullOrWhiteSpace(token))
        {
            services.AddSingleton<IDiscordPublisher, NullDiscordPublisher>();
            return services;
        }

        // ── Bot ──────────────────────────────────────────────────────────────

        var filePath = mappingsFilePath
            ?? Path.Combine(Directory.GetCurrentDirectory(), "discord-mappings.json");

        services.AddSingleton(new DiscordMappingStore(filePath));

        services.AddSingleton<DiscordBotService>(sp =>
            new DiscordBotService(
                mappingStore: sp.GetRequiredService<DiscordMappingStore>(),
                botToken: token,
                logger: sp.GetRequiredService<ILogger<DiscordBotService>>()));

        services.AddSingleton<IDiscordPublisher>(sp => sp.GetRequiredService<DiscordBotService>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<DiscordBotService>());

        // ── Décorateurs ──────────────────────────────────────────────────────

        services.DecorateScoped<ICreatePostUseCase, DiscordAwareCreatePostUseCase>();
        services.DecorateScoped<IUpdatePostUseCase, DiscordAwareUpdatePostUseCase>();
        services.DecorateScoped<IDeletePostUseCase, DiscordAwareDeletePostUseCase>();

        return services;
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Remplace l'enregistrement Scoped existant de <typeparamref name="TService"/>
    /// par <typeparamref name="TDecorator"/>, en injectant l'implémentation originale
    /// comme paramètre du constructeur.
    /// </summary>
    private static IServiceCollection DecorateScoped<TService, TDecorator>(
        this IServiceCollection services)
        where TService : class
        where TDecorator : class, TService
    {
        var descriptor = services.LastOrDefault(d =>
            d.ServiceType == typeof(TService) &&
            d.Lifetime == ServiceLifetime.Scoped)
            ?? throw new InvalidOperationException(
                $"{typeof(TService).Name} n'est pas enregistré en Scoped " +
                $"— appelez AddDiscordBot() après les use cases.");

        services.Remove(descriptor);

        services.AddScoped<TService>(sp =>
        {
            // Reconstruit l'implémentation d'origine depuis son descripteur.
            TService inner = descriptor switch
            {
                { ImplementationType: not null } d =>
                    (TService)ActivatorUtilities.CreateInstance(sp, d.ImplementationType),
                { ImplementationFactory: not null } d =>
                    (TService)d.ImplementationFactory(sp),
                { ImplementationInstance: not null } d =>
                    (TService)d.ImplementationInstance,
                _ => throw new InvalidOperationException(
                    $"Descripteur invalide pour {typeof(TService).Name}.")
            };

            // ActivatorUtilities injecte IDiscordPublisher depuis le container
            // et passe `inner` comme paramètre supplémentaire.
            return ActivatorUtilities.CreateInstance<TDecorator>(sp, inner);
        });

        return services;
    }
}
