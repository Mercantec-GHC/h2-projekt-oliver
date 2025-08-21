using DomainModels;

namespace API.Repositories
{
    public interface IBookingRepository : IRepository<Booking>
    {
        Task<bool> RoomExistsAsync(int roomId);
        Task<bool> HasOverlapAsync(int roomId, DateTime checkIn, DateTime checkOut);
        Task<IReadOnlyList<Booking>> GetAllWithUserAndRoomAsync();
        Task<IReadOnlyList<Booking>> GetByUserWithRoomAsync(int userId);
    }
}
