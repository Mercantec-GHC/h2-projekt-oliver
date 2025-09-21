namespace API.Services.Mail;

public interface IMailService
{
    Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default);

    Task SendWelcomeEmailAsync(string toEmail, string username, CancellationToken ct = default);

    Task SendBookingConfirmationEmailAsync(
        string toEmail, string username, string hotelName, string roomNumber,
        DateTime startDate, DateTime endDate, int numberOfGuests, decimal totalPrice, int bookingId,
        CancellationToken ct = default);
}
