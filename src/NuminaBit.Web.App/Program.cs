using Radzen;
using NuminaBit.Web.App;
using NuminaBit.Services;
using NuminaBit.Web.App.Services;
using Microsoft.AspNetCore.Components.Web;
using NuminaBit.Web.App.Services.Interfaces;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddRadzenComponents();

builder.Services.AddNuminaBitServices();

builder.Services.TryAddSingleton<IBasePathService, BasePathService>();

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
var isGitHubPages = baseAddress.Host.Contains("github.io");

builder.RootComponents.Add<App>("#app");

builder.RootComponents.Add<HeadOutlet>("head::after");

await builder.Build().RunAsync();
