using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sts.Admin;
using Sts.Admin.Services;
using Sts.Admin.ViewModels;
using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;
using Sts.Domain.Content.UseCases;
using STS.Admin.Repositories;
using STS.Admin.ViewModels;
using Sts.Domain.DataSource;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


// Repositories
builder.Services.AddScoped<IQuickLinksRepository, QuickLinksRemoteRepository>();
builder.Services.AddScoped<ISiteSettingsRepository, SiteSettingsRemoteRepository>();
builder.Services.AddScoped<IQuickLinksReadRepository>(sp =>
    sp.GetRequiredService<IQuickLinksRepository>());
builder.Services.AddScoped<ISiteSettingsReadRepository>(sp =>
    sp.GetRequiredService<ISiteSettingsRepository>());

builder.Services.AddScoped<IHomeCardRepository, HomeCardRemoteRepository>();
builder.Services.AddScoped<IHomeCardReadRepository>(sp =>
    sp.GetRequiredService<IHomeCardRepository>());


// Use cases — mêmes implémentations que STS.Api, repo différent
builder.Services.AddScoped<IGetQuickLinksUseCase, GetQuickLinksUseCase>();
builder.Services.AddScoped<IGetVisibleQuickLinksUseCase, GetVisibleQuickLinksUseCase>();
builder.Services.AddScoped<ICreateQuickLinkUseCase, CreateQuickLinkUseCase>();
builder.Services.AddScoped<IUpdateQuickLinkUseCase, UpdateQuickLinkUseCase>();
builder.Services.AddScoped<IDeleteQuickLinkUseCase, DeleteQuickLinkUseCase>();
builder.Services.AddScoped<IGetSiteSettingsUseCase, GetSiteSettingsUseCase>();
builder.Services.AddScoped<IUpdateSiteSettingsUseCase, UpdateSiteSettingsUseCase>();

builder.Services.AddScoped<IGetHomeCardsUseCase, GetHomeCardsUseCase>();
builder.Services.AddScoped<IGetVisibleHomeCardsUseCase, GetVisibleHomeCardsUseCase>();
builder.Services.AddScoped<ICreateHomeCardUseCase, CreateHomeCardUseCase>();
builder.Services.AddScoped<IUpdateHomeCardUseCase, UpdateHomeCardUseCase>();
builder.Services.AddScoped<IDeleteHomeCardUseCase, DeleteHomeCardUseCase>();

// ViewModels
builder.Services.AddScoped<QuickLinksViewModel>();
builder.Services.AddScoped<SiteSettingsViewModel>();
builder.Services.AddScoped<UsersViewModel>();
builder.Services.AddScoped<CharactersViewModel>();
builder.Services.AddScoped<HomeCardsViewModel>();

// En prod, STS.Api sert l'app donc même origine — BaseAddress est correct.
// En dev, lancer STS.Api (qui sert aussi le WASM) plutôt que le DevServer standalone.
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ApiClient>();

await builder.Build().RunAsync();
