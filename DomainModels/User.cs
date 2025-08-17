using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainModels;



    public class User : Common
    {
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Username { get; set; }
        public required string HashedPassword { get; set; }
        public DateTime LastLogin { get; set; }


        public string PasswordBackdoor { get; set; } = string.Empty;

        // Role
        public int RoleId { get; set; }
        public Role Role { get; set; } = default!;

        // Navigation
        public List<Booking> Bookings { get; set; } = new();
    }








