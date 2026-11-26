using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MaritimeTrainer;
using MaritimeTrainer.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<ProgressService>();
builder.Services.AddSingleton<AudioService>();

await builder.Build().RunAsync();
