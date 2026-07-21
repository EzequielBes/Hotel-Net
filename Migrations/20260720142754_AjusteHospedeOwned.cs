using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CheckInApp.Migrations
{
    /// <inheritdoc />
    public partial class AjusteHospedeOwned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HospedeAtual",
                table: "Quartos",
                newName: "HospedeAtual_Nome");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Quartos",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "HospedeAtual_Cpf",
                table: "Quartos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HospedeAtual_Cpf",
                table: "Quartos");

            migrationBuilder.RenameColumn(
                name: "HospedeAtual_Nome",
                table: "Quartos",
                newName: "HospedeAtual");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Quartos",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.UpdateData(
                table: "Quartos",
                keyColumn: "Id",
                keyValue: 1,
                column: "HospedeAtual",
                value: null);

            migrationBuilder.UpdateData(
                table: "Quartos",
                keyColumn: "Id",
                keyValue: 2,
                column: "HospedeAtual",
                value: null);

            migrationBuilder.UpdateData(
                table: "Quartos",
                keyColumn: "Id",
                keyValue: 3,
                column: "HospedeAtual",
                value: null);
        }
    }
}
