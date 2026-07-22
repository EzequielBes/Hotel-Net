using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.ValueObjects;

namespace CheckInApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Room> Rooms { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RoomCategory> RoomCategories { get; set; }
    public DbSet<RatePlan> RatePlans { get; set; }
    public DbSet<BookingOrder> BookingOrders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>().OwnsOne(r => r.CurrentGuest);

        modelBuilder.Entity<User>(b =>
        {
            b.Property(u => u.Email)
                .HasConversion(
                    vo => vo.Value,
                    str => new Email(str),
                    new ValueComparer<Email>(
                        (a, b) => a!.Value == b!.Value,
                        v => v.Value.GetHashCode(),
                        v => new Email(v.Value)))
                .HasColumnName("Email");

            b.Property(u => u.Cpf)
                .HasConversion(
                    vo => vo.Value,
                    str => new Cpf(str),
                    new ValueComparer<Cpf>(
                        (a, b) => a!.Value == b!.Value,
                        v => v.Value.GetHashCode(),
                        v => new Cpf(v.Value)))
                .HasColumnName("Cpf");
        });

        modelBuilder.Entity<BookingOrder>()
            .HasIndex(b => b.IdempotencyKey)
            .IsUnique();

        modelBuilder.Entity<RoomCategory>().HasData(
            new RoomCategory { Id = 1, Name = "Standard", MaxCapacity = 4, MinStayDays = 1, MaxStayDays = 14 }
        );

        modelBuilder.Entity<RatePlan>().HasData(
            new RatePlan { Id = 1, RoomCategoryId = 1, Name = "12h", HoursPackage = 12, PricePerDay = 150.00m },
            new RatePlan { Id = 2, RoomCategoryId = 1, Name = "8h", HoursPackage = 8, PricePerDay = 100.00m }
        );

        modelBuilder.Entity<Room>().HasData(
            new Room { Id = 1, Number = 101, Status = RoomStatus.Available, DailyRate = 100.00m, MaxCapacity = 2, RoomCategoryId = 1 },
            new Room { Id = 2, Number = 102, Status = RoomStatus.Available, DailyRate = 150.00m, MaxCapacity = 3, RoomCategoryId = 1 },
            new Room { Id = 3, Number = 103, Status = RoomStatus.Available, DailyRate = 200.00m, MaxCapacity = 4, RoomCategoryId = 1 }
        );

        // Seed with Restore — existing data, without re-hashing password
        modelBuilder.Entity<User>().HasData(
            User.Restore(
                id: 1,
                name: "System Administrator",
                email: "admin@hotel.com",
                cpf: "000.000.000-00",
                passwordHash: BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                phoneNumber: "00000000000",
                role: UserRole.Administrator
            )
        );
    }
}
