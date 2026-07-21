using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CheckInApp.Migrations
{
    /// <inheritdoc />
    public partial class SemeandoQuartos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Quartos",
                columns: new[] { "Id", "CapacidadeMaxima", "HospedeAtual", "Numero", "Status", "ValorDiaria" },
                values: new object[,]
                {
                    { 1, 2, null, 101, 0, 100.00m },
                    { 2, 3, null, 102, 0, 150.00m },
                    { 3, 4, null, 103, 0, 200.00m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Quartos",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Quartos",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Quartos",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
