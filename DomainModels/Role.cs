using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainModels;
public class Role : Common
{
    public required string Name { get; set; }

    // Navigation til brugeren (valgfrit ved 1:N)
    public List<User> Users { get; set; } = new();
}
