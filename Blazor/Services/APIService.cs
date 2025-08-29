using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Blazor.Services
{
    public class APIService
    {
        private readonly HttpClient _http;
        private readonly TokenStorage _storage;

        public APIService(HttpClient http, TokenStorage storage)
        {
            _http = http;
            _storage = storage;
        }

        private async Task ApplyAuthAsync()
        {
            var token = await _storage.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            else
                _http.DefaultRequestHeaders.Authorization = null;
        }

        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public async Task<T?> GetAsync<T>(string url)
        {
            await ApplyAuthAsync();
            return await _http.GetFromJsonAsync<T>(url, _json);
        }

        public async Task<HttpResponseMessage> GetRawAsync(string url)
        {
            await ApplyAuthAsync();
            return await _http.GetAsync(url);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body)
        {
            await ApplyAuthAsync();
            var res = await _http.PostAsJsonAsync(url, body, _json);
            if (!res.IsSuccessStatusCode) return default;
            return await res.Content.ReadFromJsonAsync<TResponse>(_json);
        }

        public async Task<HttpResponseMessage> PostAsync(string url, object? body = null)
        {
            await ApplyAuthAsync();
            if (body is null) return await _http.PostAsync(url, new StringContent("", Encoding.UTF8, "application/json"));
            return await _http.PostAsJsonAsync(url, body, _json);
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest body)
        {
            await ApplyAuthAsync();
            var res = await _http.PutAsJsonAsync(url, body, _json);
            if (!res.IsSuccessStatusCode) return default;
            return await res.Content.ReadFromJsonAsync<TResponse>(_json);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            await ApplyAuthAsync();
            return await _http.DeleteAsync(url);
        }

        // Hjælpere til login/register 
        public record LoginRequest(string Username, string Password);
        public record RegisterRequest(string Username, string Email, string Password);

        public async Task<string?> LoginAsync(string username, string password)
        {
            var res = await PostAsync("api/auth/login", new LoginRequest(username, password));
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("token", out var t))
                    return t.GetString();
            }
            catch { /* fallback */ }

            return null;
        }

        public Task<HttpResponseMessage> RegisterAsync(string username, string email, string password)
            => PostAsync("api/auth/register", new RegisterRequest(username, email, password));
    }
}
