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

        var envApiEndpoint = Environment.GetEnvironmentVariable("API_ENDPOINT");
        string apiEndpoint = !string.IsNullOrEmpty(envApiEndpoint)
     ? envApiEndpoint
     : (builder.HostEnvironment.IsDevelopment()
         ? "https://localhost:8052/" // backend dev
         : "https://h2api.mercantec.tech/"); // prod


        Console.WriteLine($"API Endpoint: {apiEndpoint}");
        apiEndpoint = "https://localhost:8052/"; // API backend

        builder.Services.AddBlazoredLocalStorage();

        // IMPORTANT: register only as a typed HttpClient (do NOT call AddScoped<APIService>())
        builder.Services.AddHttpClient<APIService>(client =>
        {
            client.BaseAddress = new Uri(apiEndpoint);
            Console.WriteLine($"APIService BaseAddress: {client.BaseAddress}");
        });

        await builder.Build().RunAsync();
    }
}
