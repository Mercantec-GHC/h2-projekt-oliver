using DomainModels;
using OneOf;
namespace API.BookingService
{
    public enum BookingError { NotFound, Overlap, Unknown }

    public interface IBookingService
    {
        Task<IReadOnlyList<object>> GetBookingsForUserAsync(int userId);
        Task<IReadOnlyList<object>> GetAllAsync();
        Task<OneOf.OneOf<object, BookingError>> CreateAsync(int userId, BookingDto dto);
    }
}
