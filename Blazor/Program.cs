using Blazor;
using Blazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Auth & State
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();

// API Service
builder.Services.AddScoped<APIService>();

var host = builder.Build();

_ = host.RunAsync();
