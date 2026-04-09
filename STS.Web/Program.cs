using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using STS.Web;
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

await builder.Build().RunAsync();
