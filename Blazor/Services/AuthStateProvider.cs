using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;                      // Tilføjet fra fil 1 (bruges af List<Claim>)
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;               // Tilføjet fra fil 1 (bruges i BuildPrincipalFromToken)

namespace Blazor.Services
{
    // Holder på auth tilstand baseret på JWT i TokenStorage (fra fil 1)
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private readonly TokenStorage _storage;

        // Tilføjet fra fil 1 - nem repræsentation af anonym bruger
        private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

        public AuthStateProvider(TokenStorage storage) => _storage = storage;

        // Returerer nuværende bruger udfra JWT hvis muligt (fil 2 original)
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _storage.GetTokenAsync();
            var identity = ValidateAndBuildIdentityFromToken(token);
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        // Marker som logget ind og broadcast ændring (fil 2 original)
        public async Task MarkUserAsAuthenticatedAsync(string token)
        {
            await _storage.SetTokenAsync(token);
            var identity = ValidateAndBuildIdentityFromToken(token);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
        }

        // Logget ud, gemmer token hvis muligt (fil 2 original)
        public async Task MarkUserAsLoggedOutAsync()
        {
            await _storage.ClearAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        }

        // --- Tilføjet fra fil 1: wrapper metoder ---
        public Task NotifyUserAuthentication(string token) => MarkUserAsAuthenticatedAsync(token);
        public Task NotifyUserLogout() => MarkUserAsLoggedOutAsync();

        // --- Fil 2 original: Manuel token validering og claims bygning ---
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

        // --- Tilføjet fra fil 1: Bygger ClaimsPrincipal ud fra JWT ---
        private static ClaimsPrincipal BuildPrincipalFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwt;

            try { jwt = handler.ReadJwtToken(token); }
            catch { return Anonymous; }

            var claims = new List<Claim>(jwt.Claims);

            // Mapper rolle til ClaimTypes (fra fil 1)
            foreach (var c in jwt.Claims)
            {
                if (c.Type.Equals("role", StringComparison.OrdinalIgnoreCase) ||
                    c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase))
                {
                    claims.Add(new Claim(ClaimTypes.Role, c.Value));
                }
            }

            // Sørger for et Navn (fra fil 1)
            if (!claims.Any(c => c.Type == ClaimTypes.Name))
            {
                var name = jwt.Claims.FirstOrDefault(c =>
                               c.Type == "username" ||
                               c.Type == ClaimTypes.Name ||
                               c.Type == "unique_name" ||
                               c.Type == "name")
                           ?.Value ?? "";
                if (!string.IsNullOrWhiteSpace(name))
                    claims.Add(new Claim(ClaimTypes.Name, name));
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
    }
}
