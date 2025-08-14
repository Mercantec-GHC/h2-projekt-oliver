using Microsoft.EntityFrameworkCore;
using DomainModels;

namespace API.DBContext;

public class AppDBContext : DbContext
{
    public AppDBContext(DbContextOptions<AppDBContext> options)
        : base(options)
    {
    }
        public DbSet<User> Users { get; set; } = null!;
    
}