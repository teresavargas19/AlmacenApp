using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlmacenApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "usuarios",
                columns: new[] { "Id", "Activo", "Email", "Nombre", "PasswordHash", "RolId" },
                values: new object[] { 1, true, "admin@almacenapp.com", "Administrador", "$2b$11$5ydz3pP.marxFzFZ3mxFyOdxcicbJiT7YRAirmcxt7RvEpUAW99xe", 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "usuarios",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
