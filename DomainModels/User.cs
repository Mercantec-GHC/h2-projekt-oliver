using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DomainModels;

public class User : Common
{
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string PhoneNumber { get; set; }

    [Required]
    public required string Username { get; set; }

    [Required]
    [JsonIgnore] // send aldrig hashes til klienten
    public required string HashedPassword { get; set; }

    public DateTime LastLogin { get; set; }

    [JsonIgnore] // skjul test/backdoor i API responses
    public string PasswordBackdoor { get; set; } = string.Empty;

    // Role
    public int RoleId { get; set; }
    public Role Role { get; set; } = default!;

    // Navigation
    public List<Booking> Bookings { get; set; } = new();
}
