using System;
using System.Collections.Generic;
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
    public required string Salt { get; set; }
    public DateTime LastLogin { get; set; }

    public string PasswordBackdoor { get; set; }
    // Only for educational purposes, not in the final product!
}