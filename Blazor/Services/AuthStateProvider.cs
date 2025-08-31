using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Blazor.Services
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private readonly TokenStorage _storage;
        public AuthStateProvider(TokenStorage storage) => _storage = storage;

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _storage.GetTokenAsync();
            var identity = ValidateAndBuildIdentityFromToken(token);
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public async Task MarkUserAsAuthenticatedAsync(string token)
        {
            await _storage.SetTokenAsync(token);
            var identity = ValidateAndBuildIdentityFromToken(token);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
        }

        public async Task MarkUserAsLoggedOutAsync()
        {
            await _storage.ClearAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        }

        private static ClaimsIdentity ValidateAndBuildIdentityFromToken(string? jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt)) return new ClaimsIdentity();
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length != 3) return new ClaimsIdentity();
                string payload = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=');
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("exp", out var expProp) && expProp.TryGetInt64(out long exp))
                {
                    var expUtc = DateTimeOffset.FromUnixTimeSeconds(exp);
                    if (DateTimeOffset.UtcNow >= expUtc) return new ClaimsIdentity(); // expired
                }

                var claims = root.EnumerateObject()
                    .Where(p => p.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                    .Select(p => new Claim(p.Name, p.Value.ToString()));
                return new ClaimsIdentity(claims, "jwt");
            }
            catch
            {
                return new ClaimsIdentity();
            }
        }
    }
}
