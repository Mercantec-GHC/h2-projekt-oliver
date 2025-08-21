using API.Repositories;
using DomainModels;
using OneOf;

namespace API.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _repo;

        public BookingService(IBookingRepository repo) => _repo = repo;

        public async Task<IReadOnlyList<object>> GetBookingsForUserAsync(int userId)
        {
            var list = await _repo.GetByUserWithRoomAsync(userId);
            return list.Select(b => new
            {
                b.Id,
                b.RoomId,
                RoomNumber = b.Room.RoomNumber,
                b.CheckIn,
                b.CheckOut,
                b.IsConfirmed
            } as object).ToList();
        }

        public async Task<IReadOnlyList<object>> GetAllAsync()
        {
            var list = await _repo.GetAllWithUserAndRoomAsync();
            return list.Select(b => new
            {
                b.Id,
                User = new { b.User.Id, b.User.Email, b.User.Username },
                Room = new { b.Room.Id, b.Room.RoomNumber },
                b.CheckIn,
                b.CheckOut,
                b.IsConfirmed
            } as object).ToList();
        }

        public async Task<OneOf<object, BookingError>> CreateAsync(int userId, BookingDto dto)
        {
            if (!await _repo.RoomExistsAsync(dto.RoomId)) return BookingError.NotFound;
            if (await _repo.HasOverlapAsync(dto.RoomId, dto.CheckIn, dto.CheckOut)) return BookingError.Overlap;

            var now = DateTime.UtcNow;
            var booking = new Booking
            {
                UserId = userId,
                RoomId = dto.RoomId,
                CheckIn = dto.CheckIn,
                CheckOut = dto.CheckOut,
                CreatedAt = now,
                UpdatedAt = now,
                IsConfirmed = true
            };

            await _repo.AddAsync(booking);
            await _repo.SaveChangesAsync();

            return new
            {
                message = "Booking oprettet!",
                booking.Id,
                booking.RoomId,
                booking.CheckIn,
                booking.CheckOut
            };
        }
    }
}
