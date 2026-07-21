using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Enums;
using CheckInApp.Domain.ValueObjects;

namespace CheckInApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Quarto> Quartos { get; set; }
    public DbSet<Reserva> Reservas { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quarto>().OwnsOne(q => q.HospedeAtual);

  
        modelBuilder.Entity<Usuario>(b =>
        {
            b.Property(u => u.Email)
                .HasConversion(
                    vo => vo.Value,
                    str => new Email(str),
                    new ValueComparer<Email>(
                        (a, b) => a!.Value == b!.Value,  // igualdade
                        v => v.Value.GetHashCode(),       // hash
                        v => new Email(v.Value)))         // snapshot
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

        modelBuilder.Entity<Quarto>().HasData(
            new Quarto { Id = 1, Numero = 101, Status = StatusQuarto.Disponivel, ValorDiaria = 100.00m, CapacidadeMaxima = 2 },
            new Quarto { Id = 2, Numero = 102, Status = StatusQuarto.Disponivel, ValorDiaria = 150.00m, CapacidadeMaxima = 3 },
            new Quarto { Id = 3, Numero = 103, Status = StatusQuarto.Disponivel, ValorDiaria = 200.00m, CapacidadeMaxima = 4 }
        );

        // Seed com Restaurar — dados já existentes, sem re-hashear senha
        modelBuilder.Entity<Usuario>().HasData(
            Usuario.Restaurar(
                id: 1,
                nome: "Administrador do Sistema",
                email: "admin@hotel.com",
                cpf: "000.000.000-00",
                senhaHash: BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                numeroCelular: "00000000000",
                role: TipoUsuario.Administrador
            )
        );
    }
}
