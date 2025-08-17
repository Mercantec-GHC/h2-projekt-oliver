using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainModels
{
    public class Room : Common
    {
        [Required]
        public int RoomNumber { get; set; } // 1–400

        [Required]
        public string Type { get; set; } = "Standard"; // Standard, Deluxe, Suite

        public bool IsAvailable { get; set; } = true;

        // Navigation
        public List<Booking> Bookings { get; set; } = new();
    }
}
