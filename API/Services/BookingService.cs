using API.Repositories;
using DomainModels;
using OneOf;
using API.BookingService;
using OneOf.Types;

namespace API.Services
{
    /// <summary>
    /// Simpelt booking-service lag.
    /// Tager imod ønsker om at hente, oprette og aflyse bookinger
    /// og snakker sammen med repository (database).
    /// </summary>
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _repo;

        // Vi får repo ind udefra (dependency injection)
        public BookingService(IBookingRepository repo) => _repo = repo;

        /// <summary>
        /// Hent alle bookinger for en bestemt bruger.
        /// Returnerer et "fladt" objekt pr. booking med det mest nødvendige.
        /// </summary>
        public async Task<IReadOnlyList<object>> GetBookingsForUserAsync(int userId)
        {
            // Henter bookinger inkl. værelse-data
            var list = await _repo.GetByUserWithRoomAsync(userId);

            // Mapper til et letvægts-objekt (godt til lister i UI)
            return list.Select(b => new
            {
                b.Id,
                b.RoomId,
                RoomNumber = b.Room.RoomNumber,
                b.CheckIn,
                b.CheckOut,
                b.IsConfirmed,
                // Pris regnes on-the-fly ud fra værelset og datoerne
                TotalPrice = Pricing.PriceForStay(b.Room.Type, b.CheckIn, b.CheckOut) ?? 0m
            } as object).ToList();
        }

        /// <summary>
        /// Hent alle bookinger (admin-agtigt overblik).
        /// </summary>
        public async Task<IReadOnlyList<object>> GetAllAsync()
        {
            var list = await _repo.GetAllWithUserAndRoomAsync();

            // Igen: mapper til simple objekter der er nemme at sende ud via API
            return list.Select(b => new
            {
                b.Id,
                User = new { b.User.Id, b.User.Email, b.User.Username },
                Room = new { b.Room.Id, b.Room.RoomNumber },
                b.CheckIn,
                b.CheckOut,
                b.IsConfirmed,
                TotalPrice = Pricing.PriceForStay(b.Room.Type, b.CheckIn, b.CheckOut) ?? 0m
            } as object).ToList();
        }

        /// <summary>
        /// Opret en booking. Tjekker om værelset findes og om der er overlap.
        /// Returnerer enten et objekt med info (success) eller en BookingError.
        /// </summary>
        public async Task<OneOf<object, BookingError>> CreateAsync(int userId, BookingDto dto)
        {
            // Vi kører alt i UTC for at undgå rod med tidszoner
            var utcCheckIn = dto.CheckIn.ToUniversalTime();
            var utcCheckOut = dto.CheckOut.ToUniversalTime();

            // --- Validering ---
            if (!await _repo.RoomExistsAsync(dto.RoomId)) return BookingError.NotFound;           // værelset findes ikke
            if (await _repo.HasOverlapAsync(dto.RoomId, utcCheckIn, utcCheckOut)) return BookingError.Overlap; // datoerne rammer noget optaget

            // Hent værelse-info (skal bl.a. bruge type og nummer)
            var room = await _repo.GetRoomAsync(dto.RoomId);
            if (room is null) return BookingError.NotFound;

            // Regn pris og nætter
            var nights = (utcCheckOut.Date - utcCheckIn.Date).Days;
            var totalPrice = Pricing.PriceForStay(room.Type, utcCheckIn, utcCheckOut) ?? 0m;

            // Lav selve booking-objektet
            var now = DateTimeOffset.UtcNow;
            var booking = new Booking
            {
                UserId = userId,
                RoomId = dto.RoomId,
                CheckIn = utcCheckIn,
                CheckOut = utcCheckOut,
                CreatedAt = now,
                UpdatedAt = now,
                IsConfirmed = true // her siger vi bare "ja" – kan ændres hvis I vil have manuel godkendelse
            };

            // Gem i databasen
            await _repo.AddAsync(booking);
            await _repo.SaveChangesAsync();

            // Returnér noget som controller (og evt. e-mail) let kan bruge
            return new
            {
                message = "Booking oprettet!",
                booking.Id,
                booking.RoomId,
                RoomNumber = room.RoomNumber,
                RoomType = room.Type,
                CheckIn = booking.CheckIn,
                CheckOut = booking.CheckOut,
                Nights = nights,
                NumberOfGuests = 1, // bare en placeholder indtil I gemmer rigtigt antal gæster
                HotelName = "JoHotel",
                TotalPrice = totalPrice
            };
        }

        /// <summary>
        /// Aflys en booking hvis det er brugerens egen og der er mere end 24 timer til check-in.
        /// Returnerer Success eller en BookingError.
        /// </summary>
        public async Task<OneOf<Success, BookingError>> CancelAsync(int userId, int bookingId)
        {
            // Vi skal kunne ændre (asNoTracking: false), ellers kan vi ikke slette/ændre
            var booking = await _repo.GetByIdAsync(bookingId, asNoTracking: false);
            if (booking is null) return BookingError.NotFound;          // findes ikke

            if (booking.UserId != userId) return BookingError.Forbidden; // må ikke aflyse andres

            // Fortrydelsesfrist: 24 timer før ankomst
            if (booking.CheckIn <= DateTimeOffset.UtcNow.AddHours(24))
                return BookingError.TooLate;

            // Slet og gem
            _repo.Remove(booking);
            await _repo.SaveChangesAsync();
            return new Success();
        }
    }
}
