using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sts.Domain.Content;
using Sts.Domain.Content.Repositories;
using Sts.Domain.Content.UseCases;
using STS.Web;
using STS.Web.Pages.Home;
using STS.Web.Repositories;
using STS.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// En dev, pointer vers l'API locale
// En prod, l'API et le site sont co-hébergés donc BaseAddress suffit
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

builder.Services.AddScoped<StsDataService>();

// Repositories lecture seule
builder.Services.AddScoped<IQuickLinksReadRepository, QuickLinksPublicRepository>();
builder.Services.AddScoped<ISiteSettingsReadRepository, SiteSettingsPublicRepository>();

// Use cases lecture
builder.Services.AddScoped<IGetVisibleQuickLinksUseCase, GetVisibleQuickLinksUseCase>();
builder.Services.AddScoped<IGetSiteSettingsUseCase, GetSiteSettingsUseCase>();

builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<HomeViewModel>();
await builder.Build().RunAsync();
