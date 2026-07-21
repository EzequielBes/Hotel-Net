using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CheckInApp.Migrations
{
    /// <inheritdoc />
    public partial class AdicionandoNovasColunas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Acompanhantes",
                table: "Reservas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CheckoutComplete",
                table: "Reservas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NomeHospede",
                table: "Reservas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observacoes",
                table: "Reservas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxasAdicionais",
                table: "Reservas",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorMinimo",
                table: "Reservas",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorTotal",
                table: "Reservas",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Acompanhantes",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "CheckoutComplete",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "NomeHospede",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "Observacoes",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "TaxasAdicionais",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "ValorMinimo",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "ValorTotal",
                table: "Reservas");
        }
    }
}
