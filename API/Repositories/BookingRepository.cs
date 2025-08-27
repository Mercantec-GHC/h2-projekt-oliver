using API.Data;
using DomainModels;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories
{
    public class BookingRepository : EfRepository<Booking>, IBookingRepository
    {
        public BookingRepository(AppDBContext db) : base(db) { }

        public Task<bool> RoomExistsAsync(int roomId) =>
            _db.Rooms.AsNoTracking().AnyAsync(r => r.Id == roomId);

        public Task<bool> HasOverlapAsync(int roomId, DateTimeOffset checkIn, DateTimeOffset checkOut) =>
            _db.Bookings.AsNoTracking().AnyAsync(b =>
                b.RoomId == roomId &&
                ((checkIn >= b.CheckIn && checkIn < b.CheckOut) ||
                 (checkOut > b.CheckIn && checkOut <= b.CheckOut)));

        public async Task<IReadOnlyList<Booking>> GetAllWithUserAndRoomAsync() =>
            await _db.Bookings
                .AsNoTracking()
                .Include(b => b.User)
                .Include(b => b.Room)
                .ToListAsync();

        public async Task<IReadOnlyList<Booking>> GetByUserWithRoomAsync(int userId) =>
            await _db.Bookings
                .AsNoTracking()
                .Include(b => b.Room)
                .Where(b => b.UserId == userId)
                .ToListAsync();
    }
}
