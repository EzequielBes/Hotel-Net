using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CheckInApp.Migrations
{
    /// <inheritdoc />
    public partial class RenomearSenhaParaSenhaHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Senha",
                table: "Usuarios",
                newName: "SenhaHash");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Quartos",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Cpf", "SenhaHash" },
                values: new object[] { "000.000.000-00", "$2a$11$eu5r2bWUTUOLFjUYQdidFuGzO0I/4ZMf0UUtX3pym44HaBKKd9CMq" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SenhaHash",
                table: "Usuarios",
                newName: "Senha");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Quartos",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Cpf", "Senha" },
                values: new object[] { "00000000000", "$2a$11$ZlBI2OENpUckfkGPW0a/qepOFQt4kAfeAy7GNsnUb3BIqOSQYLzmK" });
        }
    }
}
