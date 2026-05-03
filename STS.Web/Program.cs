using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sts.Domain.Content;
using Sts.Domain.Content.Repositories;
using Sts.Domain.Content.UseCases;
using STS.Web;
using STS.Web.Pages.Home;
using STS.Web.Repositories;
using STS.Web.Services;
using STS.Web.ViewModels;
using Sts.Domain.DataSource;
using Sts.Domain.Repository;
using STS.Web.DataSource;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// En dev, pointer vers l'API locale
// En prod, l'API et le site sont co-hébergés donc BaseAddress suffit
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

builder.Services.AddScoped<HttpDataSource>();
builder.Services.AddScoped<IDataSource>(sp =>
    sp.GetRequiredService<HttpDataSource>());

builder.Services.AddScoped<StsDataService>();

// Repositories lecture seule
builder.Services.AddScoped<IQuickLinksReadRepository, QuickLinksPublicRepository>();
builder.Services.AddScoped<ISiteSettingsReadRepository, SiteSettingsPublicRepository>();

builder.Services.AddScoped<TraitRepository>(sp =>
    new DefaultTraitRepository(sp.GetRequiredService<IDataSource>()));
builder.Services.AddScoped<JobRepository>(sp =>
    new DefaultJobRepository(sp.GetRequiredService<IDataSource>()));
builder.Services.AddScoped<AbilityRepository>(sp =>
    new DefaultAbilityRepository(sp.GetRequiredService<IDataSource>()));
builder.Services.AddScoped<ActionRepository>(sp =>
    new DefaultActionRepository(sp.GetRequiredService<IDataSource>()));

// Use cases lecture
builder.Services.AddScoped<IGetVisibleQuickLinksUseCase, GetVisibleQuickLinksUseCase>();
builder.Services.AddScoped<IGetSiteSettingsUseCase, GetSiteSettingsUseCase>();

builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<CharacterApiService>();
builder.Services.AddScoped<CharactersViewModel>();
builder.Services.AddScoped<CharacterDetailViewModel>();
builder.Services.AddScoped<CharacterCreateViewModel>();
builder.Services.AddScoped<CharacterEditViewModel>();

builder.Services.AddScoped<HomeViewModel>();
//await builder.Build().RunAsync();

var host = builder.Build();

// Pre-load des données de référence avant le démarrage de l'app
// Les repositories singletons appelleront Load() de façon synchrone ensuite
await host.Services
    .GetRequiredService<HttpDataSource>()
    .LoadAsync();

var auth = host.Services.GetRequiredService<AuthService>();
await auth.TryRestoreSessionAsync();

await host.RunAsync();
