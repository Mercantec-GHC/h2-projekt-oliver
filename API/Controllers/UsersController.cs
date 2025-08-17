using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using API.Data;
using API.Services;
using DomainModels;

namespace API.Controllers
{
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

        /// Registrerer en ny bruger. Adgangskoden hashes m. BCrypt
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var emailExists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailExists) return BadRequest(new { message = "En bruger med denne e-mail findes allerede." });

            var now = DateTime.UtcNow;

            var user = new User
            {
                Email = dto.Email.Trim(),
                Username = dto.Username.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PasswordBackdoor = dto.Password, // behold kun hvis opgaven kræver det
                RoleId = 3, // Customer default
                CreatedAt = now,
                UpdatedAt = now,
                LastLogin = now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            
            return Ok(new
            {
                message = "Bruger oprettet!",
                user = new { user.Id, user.Email, user.Username, user.RoleId }
            });
        }

        /// Logger ind - udsteder JWT token
        [HttpPost("login")]
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
                user = new
                {
                    user.Id,
                    user.Email,
                    user.Username,
                    role = user.Role?.Name ?? "Customer"
                }
            });
        }

        
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
                return Unauthorized();

            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
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
