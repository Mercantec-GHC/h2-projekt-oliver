using System;
using System.Net.Http;
using Blazor;
using Blazor.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Blazor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            var apiEndpoint = builder.Configuration["ApiEndpoint"];
            if (string.IsNullOrWhiteSpace(apiEndpoint))
            {
                apiEndpoint = builder.HostEnvironment.IsDevelopment()
                    ? "https://localhost:8052/"
                    : builder.HostEnvironment.BaseAddress;
            }

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<TokenStorage>();
            builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
            builder.Services.AddBlazoredLocalStorage();

            builder.Services.AddHttpClient<APIService>(client =>
            {
                client.BaseAddress = new Uri(apiEndpoint);
                Console.WriteLine($"APIService BaseAddress: {client.BaseAddress}");
            });

            var host = builder.Build();
            await host.RunAsync();
        }
    }
}
