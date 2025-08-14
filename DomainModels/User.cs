using System;
using System.Collections.Generic;
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
    public required string Salt { get; set; }
    public DateTime LastLogin { get; set; }

    public string PasswordBackdoor { get; set; }
    
    // FK + navigation til rolle (én rolle pr. bruger)
    public string RoleId { get; set; } = default!;  // navigation 
    public Role Role { get; set; } = default!;

}

// DTO til registrering
public class RegisterDto
{
    public required string Email { get; set; } = string.Empty;
    public required string Username { get; set; }

    public required string Password { get; set; } = string.Empty;
}

// DTO til login
public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}