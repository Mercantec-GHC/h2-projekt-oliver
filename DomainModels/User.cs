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

    [JsonIgnore]
    public string HashedPassword { get; set; } = string.Empty;

    public DateTimeOffset LastLogin { get; set; }

    [JsonIgnore] //Tester
    public string PasswordBackdoor { get; set; } = string.Empty;

    // Role
    public int RoleId { get; set; }
    public Role Role { get; set; } = default!;

    // Navigation
    public List<Booking> Bookings { get; set; } = new();
}
