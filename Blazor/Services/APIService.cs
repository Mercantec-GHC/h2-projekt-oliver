using Blazor.Services;
using DomainModels;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;

namespace Blazor.Services;
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
        _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task ApplyAuthAsync()
    {
        var token = await _storage.GetTokenAsync();
        _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);
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
        return body is null
            ? await _http.PostAsync(url, new StringContent("", Encoding.UTF8, "application/json"))
            : await _http.PostAsJsonAsync(url, body, _json);
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

    // --- Auth ---
    public record LoginRequest(string Username, string Password);
    public record RegisterRequest(string Username, string Email, string Password);

    public Task<HttpResponseMessage> LoginAsync(LoginDto dto) =>
        _http.PostAsJsonAsync("auth/login", dto);

    public Task<HttpResponseMessage> RegisterAsync(object anonymousDto) =>
        _http.PostAsJsonAsync("auth/register", anonymousDto);

    public async Task<string?> LoginAsync(string username, string password)
    {
        var res = await PostAsync("auth/login", new LoginRequest(username, password));
        if (!res.IsSuccessStatusCode) return null;

        var json = await res.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("token", out var t))
                return t.GetString();
        }
        catch {  }
        return null;
    }

    public Task<HttpResponseMessage> RegisterAsync(string username, string email, string password) =>
        PostAsync("auth/register", new RegisterRequest(username, email, password));

    // --- Rooms ---
    public Task<List<RoomDto>?> GetRoomsAsync() =>
        _http.GetFromJsonAsync<List<RoomDto>>("rooms", _json);

    public Task<List<RoomDto>?> GetRoomsAsync(DateTimeOffset from, DateTimeOffset to) =>
        _http.GetFromJsonAsync<List<RoomDto>>(
            $"rooms?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}", _json);

    public async Task<List<RoomDto>> GetRoomsOrEmptyAsync()
    {
        await ApplyAuthAsync();
        var list = await _http.GetFromJsonAsync<List<RoomDto>>("rooms", _json);
        return list ?? new List<RoomDto>();
    }
    // --- Rooms (extra helpers — optional but useful) ---
    public Task<RoomDto?> GetRoomAsync(int id) =>
        _http.GetFromJsonAsync<RoomDto>($"rooms/{id}", _json);

    // Used by calendars or details pages to shade booked dates (optional)
    public class SpanVm { public DateTimeOffset CheckIn { get; set; } public DateTimeOffset CheckOut { get; set; } }
    public Task<List<SpanVm>?> GetRoomBookedSpansAsync(int id, DateTimeOffset from, DateTimeOffset to) =>
        _http.GetFromJsonAsync<List<SpanVm>>(
            $"rooms/{id}/booked?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}",
            _json);



    // --- Bookings ---
    public Task<HttpResponseMessage> CreateBookingAsync(BookingDto dto) =>
        _http.PostAsJsonAsync("bookings", dto, _json);

    public Task<HttpResponseMessage> GetMyBookingsAsync() =>
        _http.GetAsync("bookings/my");
}
