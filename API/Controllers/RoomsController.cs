using Microsoft.AspNetCore.Mvc;
using API.Data;
using DomainModels;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly AppDBContext _db;
        public RoomsController(AppDBContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var fromUtc = (from ?? nowUtc).ToUniversalTime();
            var toUtc = (to ?? nowUtc.AddDays(1)).ToUniversalTime();

            var rooms = await _db.Rooms
                .AsNoTracking()
                .OrderBy(r => r.RoomNumber)
                .Select(r => new RoomDto
                {
                    Id = r.Id,
                    RoomNumber = r.RoomNumber,
                    Type = r.Type,
                    IsAvailable = !_db.Bookings.Any(b =>
                        b.RoomId == r.Id &&
                        b.CheckIn < toUtc &&
                        b.CheckOut > fromUtc)
                })
                .ToListAsync();

            return Ok(rooms);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var fromUtc = (from ?? nowUtc).ToUniversalTime();
            var toUtc = (to ?? nowUtc.AddDays(1)).ToUniversalTime();

            var room = await _db.Rooms
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new RoomDto
                {
                    Id = r.Id,
                    RoomNumber = r.RoomNumber,
                    Type = r.Type,
                    IsAvailable = !_db.Bookings.Any(b =>
                        b.RoomId == r.Id &&
                        b.CheckIn < toUtc &&
                        b.CheckOut > fromUtc)
                })
                .FirstOrDefaultAsync();

            if (room is null) return NotFound();
            return Ok(room);
        }
    }
}
