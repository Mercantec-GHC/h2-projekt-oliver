using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DomainModels;

namespace Blazor.Services
{
    public class APIService
    {
        private readonly HttpClient _http;
        private readonly TokenStorage _storage;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public APIService(HttpClient http, TokenStorage storage)
        {
            _http = http;
            _storage = storage;
        }

        public void SetBearer(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                _http.DefaultRequestHeaders.Authorization = null;
            else
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private async Task ApplyAuthAsync()
        {
            var token = await _storage.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            else
                _http.DefaultRequestHeaders.Authorization = null;
        }

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
            if (body is null)
                return await _http.PostAsync(url, new StringContent("", Encoding.UTF8, "application/json"));
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


        public Task<HttpResponseMessage> LoginAsync(LoginDto dto) =>
            _http.PostAsJsonAsync("api/auth/login", dto);

        public Task<HttpResponseMessage> RegisterAsync(object anonymousDto) =>
            _http.PostAsJsonAsync("api/auth/register", anonymousDto);

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
            catch { }
            return null;
        }

        public Task<HttpResponseMessage> RegisterAsync(string username, string email, string password) =>
            PostAsync("api/auth/register", new RegisterRequest(username, email, password));

        public Task<List<RoomDto>> GetRoomsAsync() =>
            _http.GetFromJsonAsync<List<RoomDto>>("api/rooms")!;

        public Task<List<RoomDto>> GetRoomsAsync(DateTimeOffset from, DateTimeOffset to) =>
            _http.GetFromJsonAsync<List<RoomDto>>(
                $"api/rooms?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}")!;

        public Task<HttpResponseMessage> CreateBookingAsync(BookingDto dto) =>
            _http.PostAsJsonAsync("api/bookings", dto);

        public Task<HttpResponseMessage> GetMyBookingsAsync() =>
            _http.GetAsync("api/bookings/my");
    }
}
