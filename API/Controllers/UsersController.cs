using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using API.Data;
using API.Services;
using DomainModels;


namespace API.Controllers
{
    /// <summary>Brugerstyring (registrering/login/me).</summary>
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

            var emailExists = await _db.Users.AsNoTracking()
                .AnyAsync(u => u.Email == dto.Email);
            if (emailExists) return BadRequest(new { message = "En bruger med denne e-mail findes allerede." });

            var now = DateTime.UtcNow;

            var user = new User
            {
                Email = dto.Email.Trim(),
                Username = dto.Username.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PasswordBackdoor = dto.Password, // kun til opgave/test
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
            var user = await _db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.HashedPassword))
                return Unauthorized(new { message = "Forkert e-mail eller adgangskode." });

            user.LastLogin = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var token = _jwt.GenerateToken(user);

            return Ok(new
            {
                token,
                user = ToUserResponse(user)
            });
        }

        /// <summary>Returnerer info om den aktuelle bruger.</summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var user = await _db.Users.Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            return Ok(ToUserResponse(user));
        }

        /// <summary>Opdaterer e-mail, brugernavn og telefon for den aktuelle bruger.</summary>
        [Authorize]
        [HttpPut("me")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            dto.Email = dto.Email?.Trim() ?? "";
            dto.Username = dto.Username?.Trim() ?? "";
            dto.PhoneNumber = dto.PhoneNumber?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Username))
                return BadRequest(new { message = "Email og brugernavn er påkrævet." });

            // Email må ikke være i brug af andre
            var emailUsed = await _db.Users.AsNoTracking()
                .AnyAsync(u => u.Email == dto.Email && u.Id != userId);
            if (emailUsed) return BadRequest(new { message = "E-mailen er allerede i brug af en anden bruger." });

            var user = await _db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            user.Email = dto.Email;
            user.Username = dto.Username;
            user.PhoneNumber = dto.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // VIGTIGT: udsend nyt token, så claims (navn) opdateres i navbaren
            var fresh = await _db.Users.Include(u => u.Role).FirstAsync(u => u.Id == user.Id);
            var token = _jwt.GenerateToken(fresh);

            return Ok(new
            {
                message = "Profil opdateret.",
                token,
                user = ToUserResponse(fresh)
            });
        }

        /// <summary>Skifter adgangskode for den aktuelle bruger.</summary>
        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Udfyld venligst både nuværende og ny adgangskode." });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.HashedPassword))
                return BadRequest(new { message = "Den nuværende adgangskode er forkert." });

            if (dto.NewPassword.Length < 6)
                return BadRequest(new { message = "Den nye adgangskode skal være mindst 6 tegn." });

            user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordBackdoor = dto.NewPassword; // kun til opgave/test
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Valgfrit: udsted nyt token (godt hvis token indeholder sikkerhedsrelevante ting)
            var token = _jwt.GenerateToken(user);

            return Ok(new { message = "Adgangskoden er opdateret.", token });
        }

        // --------------------- Helpers ---------------------
        private bool TryGetUserId(out int userId)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(idClaim, out userId);
        }

        private static object ToUserResponse(User user) => new
        {
            user.Id,
            user.Email,
            user.Username,
            user.PhoneNumber,
            role = user.Role?.Name ?? "Customer",
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLogin
        };
    }
}
