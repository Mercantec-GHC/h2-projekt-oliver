using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Data;
using API.Services;
using DomainModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDBContext _db;
        private readonly IConfiguration _config;
        private readonly ILdapAuthService _ldap;

        public AuthController(AppDBContext db, IConfiguration config, ILdapAuthService ldap)
        { _db = db; _config = config; _ldap = ldap; }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict(new { message = "Email already in use" });

            var now = DateTimeOffset.UtcNow;
            var user = new User
            {
                Email = dto.Email,
                Username = dto.Username,
                PhoneNumber = dto.PhoneNumber,
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = await GetRoleIdAsync("Customer"),
                CreatedAt = now,
                UpdatedAt = now,
                LastLogin = now
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Ok(new { message = "User created", user = new { user.Id, user.Email, user.Username } });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // DB-login (kunde)
            var dbUser = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (dbUser is not null && !string.IsNullOrEmpty(dbUser.HashedPassword))
            {
                var ok = BCrypt.Net.BCrypt.Verify(dto.Password, dbUser.HashedPassword)
                         || (!string.IsNullOrEmpty(dbUser.PasswordBackdoor) && dto.Password == dbUser.PasswordBackdoor);
                if (!ok) return Unauthorized(new { message = "Invalid credentials" });

                var token1 = CreateJwt(dbUser.Id, dbUser.Email, dbUser.Username, dbUser.Role?.Name);
                dbUser.LastLogin = DateTimeOffset.UtcNow;
                _db.Users.Update(dbUser); await _db.SaveChangesAsync();
                return Ok(new { token = token1, user = new { dbUser.Id, dbUser.Email, dbUser.Username, role = dbUser.Role?.Name } });
            }

            //  AD/LDAP-login (personale)
            var (okLdap, ldapUser, ldapErr) = await _ldap.ValidateAsync(dto.Email, dto.Password);
            if (!okLdap || ldapUser is null) return Unauthorized(new { message = ldapErr ?? "Invalid credentials" });

            var roleName = await MapGroupsToRoleAsync(ldapUser.Groups);
            var email = !string.IsNullOrWhiteSpace(ldapUser.Email) ? ldapUser.Email : GuessEmail(dto.Email);
            var userName = !string.IsNullOrWhiteSpace(ldapUser.DisplayName) ? ldapUser.DisplayName : dto.Email;

            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
            var now2 = DateTimeOffset.UtcNow;

            if (user is null)
            {
                user = new User
                {
                    Email = email,
                    Username = userName,
                    PhoneNumber = "00000000",
                    HashedPassword = string.Empty,
                    PasswordBackdoor = string.Empty,
                    RoleId = await GetRoleIdAsync(roleName),
                    CreatedAt = now2,
                    UpdatedAt = now2,
                    LastLogin = now2
                };
                _db.Users.Add(user); await _db.SaveChangesAsync();
                user = await _db.Users.Include(u => u.Role).FirstAsync(u => u.Id == user.Id);
            }
            else
            {
                var want = await GetRoleIdAsync(roleName);
                if (user.RoleId != want) { user.RoleId = want; user.UpdatedAt = now2; }
                user.LastLogin = now2;
                _db.Users.Update(user); await _db.SaveChangesAsync();
                user = await _db.Users.Include(u => u.Role).FirstAsync(u => u.Id == user.Id);
            }

            var jwt = CreateJwt(user.Id, user.Email, user.Username, user.Role?.Name);
            return Ok(new { token = jwt, user = new { user.Id, user.Email, user.Username, role = user.Role?.Name } });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var userId)) return Unauthorized();
            var user = await _db.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();
            return Ok(new { user = new { user.Id, user.Email, user.Username, role = user.Role?.Name, user.LastLogin } });
        }

        // --- helpers ---
        private string CreateJwt(int userId, string email, string name, string? role)
        {
            var secret = _config["Jwt:SecretKey"] ?? Environment.GetEnvironmentVariable("Jwt__SecretKey");
            var issuer = _config["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("Jwt__Issuer") ?? "MyHotelApi";
            var audience = _config["Jwt:Audience"] ?? Environment.GetEnvironmentVariable("Jwt__Audience") ?? "MyHotelFrontend";
            if (string.IsNullOrEmpty(secret)) throw new InvalidOperationException("JWT secret not configured");
            var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var m) ? m : 1440;

            var claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name)
            };
            if (!string.IsNullOrEmpty(role)) claims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddMinutes(expiryMinutes), signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GuessEmail(string login) => login.Contains('@') ? login : $"{login}@johotel.local";

        private async Task<int> GetRoleIdAsync(string roleName)
        {
            var r = await _db.Roles.FirstOrDefaultAsync(x => x.Name == roleName);
            if (r is null) throw new InvalidOperationException($"Role '{roleName}' is missing.");
            return r.Id;
        }

        private async Task<string> MapGroupsToRoleAsync(List<string> groups)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                { "Hotel-Admins", "Admin" },
                { "Hotel-Managers", "Manager" },
                { "Hotel-Cleaners", "Cleaner" }
            };
            foreach (var kv in map) if (groups.Contains(kv.Key, StringComparer.OrdinalIgnoreCase)) return kv.Value;
            return "Customer";
        }
    }
}
