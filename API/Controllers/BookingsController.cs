using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API.Services;
using DomainModels;

namespace API.Controllers
{
    /// <summary>
    /// Booking-endpoints. Kræver auth for standardbrugere.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _service;

        public BookingsController(IBookingService service)
        {
            _service = service;
        }

        /// <summary>
        /// Hent alle bookinger for den aktuelle bruger.
        /// </summary>
        /// <returns>Liste over brugerens bookinger.</returns>
        [HttpGet("my")]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyBookings()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var bookings = await _service.GetBookingsForUserAsync(userId.Value);
            return Ok(bookings);
        }

        /// <summary>
        /// Opretter en booking for den aktuelle bruger.
        /// </summary>
        /// <param name="dto">Booking data.</param>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateBooking([FromBody] BookingDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _service.CreateAsync(userId.Value, dto);
            return result.Match<IActionResult>(
                ok => Ok(ok),
                err => err switch
                {
                    BookingError.NotFound => NotFound(new { message = "Værelset findes ikke." }),
                    BookingError.Overlap => BadRequest(new { message = "Værelset er allerede booket i denne periode." }),
                    _ => BadRequest(new { message = "Kunne ikke oprette booking." })
                }
            );
        }

        /// <summary>
        /// Henter alle bookinger (kun Admin/Manager).
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "Admin,Manager")]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllBookings()
        {
            var bookings = await _service.GetAllAsync();
            return Ok(bookings);
        }

        // Hent brugerId fra JWT
        private int? GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(idClaim, out var id) ? id : null;
        }
    }
}
