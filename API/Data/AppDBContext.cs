using Microsoft.EntityFrameworkCore;
using DomainModels;

namespace API.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId);
            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.RoomId, b.CheckIn, b.CheckOut })
                .HasDatabaseName("IX_Bookings_RoomId_CheckIn_CheckOut");

            var staticDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", CreatedAt = staticDate, UpdatedAt = staticDate },
                new Role { Id = 2, Name = "Manager", CreatedAt = staticDate, UpdatedAt = staticDate },
                new Role { Id = 3, Name = "Customer", CreatedAt = staticDate, UpdatedAt = staticDate },
                new Role { Id = 4, Name = "Cleaner", CreatedAt = staticDate, UpdatedAt = staticDate }
            );

            var rooms = new List<Room>();
            for (int i = 1; i <= 400; i++)
            {
                rooms.Add(new Room
                {
                    Id = i,
                    RoomNumber = i,
                    Type = "Standard",
                    IsAvailable = true,
                    CreatedAt = staticDate,
                    UpdatedAt = staticDate
                });
            }
            modelBuilder.Entity<Room>().HasData(rooms);

            var adminId = 1;
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = adminId,
                Email = "admin@hotel.test",
                Username = "Admin",
                PhoneNumber = "00000000",
                HashedPassword = "$2a$11$C2sHsoVgVdP2rzn93K9c2O8u9i4cVtFjYJya0w1PKgJjLgM9bIr96",
                PasswordBackdoor = "Admin123!",
                RoleId = 1,
                CreatedAt = staticDate,
                UpdatedAt = staticDate,
                LastLogin = staticDate
            });
        }
    }
}
