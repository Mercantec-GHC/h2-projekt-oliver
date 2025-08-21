using System.Net.Http.Headers;
using System.Net.Http.Json;
using DomainModels;

namespace Blazor.Services
{
    public partial class APIService
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

        // Users
        public Task<HttpResponseMessage> RegisterAsync(RegisterDto dto) =>
            _http.PostAsJsonAsync("api/users/register", dto);

        public Task<HttpResponseMessage> LoginAsync(LoginDto dto) =>
            _http.PostAsJsonAsync("api/users/login", dto);

        public Task<HttpResponseMessage> MeAsync() =>
            _http.GetAsync("api/users/me");

        // Bookings
        public Task<HttpResponseMessage> GetMyBookingsAsync() =>
            _http.GetAsync("api/bookings/my");

        public Task<HttpResponseMessage> CreateBookingAsync(BookingDto dto) =>
            _http.PostAsJsonAsync("api/bookings", dto);
    }
}
