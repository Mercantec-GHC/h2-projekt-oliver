using System.ComponentModel.DataAnnotations;

namespace DomainModels;

public class BookingDto
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    public DateTime CheckIn { get; set; }

    [Required]
    public DateTime CheckOut { get; set; }
}
