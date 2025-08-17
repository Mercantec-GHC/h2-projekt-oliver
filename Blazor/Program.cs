using System;
using System.Net.Http;
using Blazor;
using Blazor.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Blazor;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // Vælg API endpoint (lokal vs prod)
        var envApiEndpoint = Environment.GetEnvironmentVariable("API_ENDPOINT");

        string apiEndpoint;
        if (!string.IsNullOrEmpty(envApiEndpoint))
        {
            apiEndpoint = envApiEndpoint;
        }
        else if (builder.HostEnvironment.IsDevelopment())
        {
            apiEndpoint = "https://localhost:5001/"; // lokal API
        }
        else
        {
            apiEndpoint = "https://h2api.mercantec.tech/"; // prod API
        }

        Console.WriteLine($"API Endpoint: {apiEndpoint}");

        // Registrer APIService og HttpClient
        builder.Services.AddScoped<APIService>();
        builder.Services.AddHttpClient<APIService>(client =>
        {
            client.BaseAddress = new Uri(apiEndpoint);
            Console.WriteLine($"APIService BaseAddress: {client.BaseAddress}");
        });

        await builder.Build().RunAsync();
    }
}
