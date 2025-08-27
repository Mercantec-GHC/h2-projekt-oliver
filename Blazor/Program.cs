using System;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using Blazor.Services;

namespace Blazor;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // Prefer config (wwwroot/appsettings.json) -> fallback to sensible values
        var apiEndpoint = builder.Configuration["ApiEndpoint"];
        if (string.IsNullOrWhiteSpace(apiEndpoint))
        {
            apiEndpoint = builder.HostEnvironment.IsDevelopment()
                ? "https://localhost:8052/"     // local dev backend (HTTPS)
                : builder.HostEnvironment.BaseAddress; // production: assume same origin
        }

        Console.WriteLine($"API Endpoint: {apiEndpoint}");

        builder.Services.AddBlazoredLocalStorage();

        builder.Services.AddHttpClient<APIService>(client =>
        {
            client.BaseAddress = new Uri(apiEndpoint);
            Console.WriteLine($"APIService BaseAddress: {client.BaseAddress}");
        });

        await builder.Build().RunAsync();
    }
}
