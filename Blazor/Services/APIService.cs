using System.Net.Http.Headers;
using System.Net.Http.Json;
using DomainModels;

namespace Blazor.Services
{
    public class APIService
    {
        private readonly HttpClient _http;

        public APIService(HttpClient httpClient)
        {
            _http = httpClient;
        }

        public void SetBearer(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                _http.DefaultRequestHeaders.Authorization = null;
            else
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

 
        public Task<HttpResponseMessage> RegisterAsync(object anonymousDto) =>
            _http.PostAsJsonAsync("api/auth/register", anonymousDto);

        public Task<HttpResponseMessage> LoginAsync(DomainModels.LoginDto dto) =>
            _http.PostAsJsonAsync("api/auth/login", dto);

        public Task<List<DomainModels.RoomDto>> GetRoomsAsync() =>
    _http.GetFromJsonAsync<List<DomainModels.RoomDto>>("api/rooms")!;

        public Task<List<DomainModels.RoomDto>> GetRoomsAsync(DateTimeOffset from, DateTimeOffset to) =>
            _http.GetFromJsonAsync<List<DomainModels.RoomDto>>(
                $"api/rooms?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}")!;

        public Task<HttpResponseMessage> CreateBookingAsync(DomainModels.BookingDto dto) =>
            _http.PostAsJsonAsync("api/bookings", dto);

        public Task<HttpResponseMessage> GetMyBookingsAsync() =>
            _http.GetAsync("api/bookings/my");
    }
}
