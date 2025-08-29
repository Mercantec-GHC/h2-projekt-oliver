using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Blazor.Services
{
    /// <summary>
    /// Reads JWT from localStorage and exposes it as AuthenticationState.
    /// </summary>
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private readonly TokenStorage _storage;

        public AuthStateProvider(TokenStorage storage)
        {
            _storage = storage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _storage.GetTokenAsync();
            var identity = ValidateAndBuildIdentityFromToken(token);
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
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
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
        }

        private static ClaimsIdentity ValidateAndBuildIdentityFromToken(string? jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt)) return new ClaimsIdentity();

            try
            {
                
                var parts = jwt.Split('.');
                if (parts.Length != 3) return new ClaimsIdentity();

                string payload = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '='); // fix padding
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
                                 .Select(p => new Claim(p.Name, p.Value.ToString()))
                                 .ToList();

                return new ClaimsIdentity(claims, authenticationType: "jwt");
            }
            catch
            {
                return new ClaimsIdentity();
            }
        }
    }
}
