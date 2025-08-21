using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using API.Data;
using API.Services;
using DomainModels;
using System.Text.Json.Serialization;

namespace API.Controllers
{
    /// <summary>
    /// Brugerstyring (registrering/login/me).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDBContext _db;
        private readonly JwtService _jwt;

        public UsersController(AppDBContext db, JwtService jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        /// <summary>Opretter en ny bruger.</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var emailExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Email == dto.Email);
            if (emailExists) return BadRequest(new { message = "En bruger med denne e-mail findes allerede." });

            var now = DateTime.UtcNow;

            var user = new User
            {
                Email = dto.Email.Trim(),
                Username = dto.Username.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PasswordBackdoor = dto.Password, // kun til opgave/test – gemmes men skjules i JSON
                RoleId = 3, // Customer
                CreatedAt = now,
                UpdatedAt = now,
                LastLogin = now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Bruger oprettet!", user = new { user.Id, user.Email, user.Username, user.RoleId } });
        }

        /// <summary>Login – returnerer JWT.</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.HashedPassword))
                return Unauthorized(new { message = "Forkert e-mail eller adgangskode." });

            user.LastLogin = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var token = _jwt.GenerateToken(user);

            return Ok(new
            {
                token,
                user = new { user.Id, user.Email, user.Username, role = user.Role?.Name ?? "Customer" }
            });
        }

        /// <summary>Returnerer info om den aktuelle bruger.</summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            if (!int.TryParse(idClaim, out var userId)) return Unauthorized();

            var user = await _db.Users.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.Email,
                user.Username,
                user.PhoneNumber,
                role = user.Role?.Name ?? "Customer",
                user.CreatedAt,
                user.UpdatedAt,
                user.LastLogin
            });
        }
    }
}
