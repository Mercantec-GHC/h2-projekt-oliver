using API.Data;
using DomainModels;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories
{
    // Snakker med databasen om bookinger
    public class BookingRepository : EfRepository<Booking>, IBookingRepository
    {
        public BookingRepository(AppDBContext db) : base(db) { }

        // Findes værelset?
        public Task<bool> RoomExistsAsync(int roomId) =>
            _db.Rooms.AsNoTracking().AnyAsync(r => r.Id == roomId);

        // Tjek overlap:
        // eksisterende.CheckIn < ny.CheckOut && eksisterende.CheckOut > ny.CheckIn
        public Task<bool> HasOverlapAsync(int roomId, DateTimeOffset checkIn, DateTimeOffset checkOut) =>
            _db.Bookings.AsNoTracking().AnyAsync(b =>
                b.RoomId == roomId &&
                b.IsConfirmed &&
                b.CheckIn < checkOut &&
                b.CheckOut > checkIn);

        // Alle bookinger med bruger + værelse
        public async Task<IReadOnlyList<Booking>> GetAllWithUserAndRoomAsync() =>
            await _db.Bookings
                .AsNoTracking()
                .Include(b => b.User)
                .Include(b => b.Room)
                .ToListAsync();

        // Bookinger for en bestemt bruger (med værelse)
        public async Task<IReadOnlyList<Booking>> GetByUserWithRoomAsync(int userId) =>
            await _db.Bookings
                .AsNoTracking()
                .Include(b => b.Room)
                .Where(b => b.UserId == userId)
                .ToListAsync();

        // Hent ét værelse (til type/nummer)
        public Task<Room?> GetRoomAsync(int roomId) =>
            _db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId);
    }
}
