using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AlmacenApi.Migrations
{
    /// <inheritdoc />
    public partial class SeedBaseCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "almacenes",
                columns: new[] { "Id", "Activo", "Direccion", "Nombre" },
                values: new object[] { 1, true, null, "Almacén principal" });

            migrationBuilder.InsertData(
                table: "categorias",
                columns: new[] { "Id", "Activo", "Nombre" },
                values: new object[] { 1, true, "General" });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "Id", "Nombre", "Permisos" },
                values: new object[] { 1, "Administrador", "*" });

            migrationBuilder.InsertData(
                table: "tipos_movimiento",
                columns: new[] { "Id", "Activo", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "Entrada" },
                    { 2, true, "Salida" },
                    { 3, true, "Ajuste" },
                    { 4, true, "Transferencia" }
                });

            migrationBuilder.InsertData(
                table: "unidades_medida",
                columns: new[] { "Id", "Abreviatura", "Activo", "Nombre" },
                values: new object[] { 1, "und", true, "Unidad" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "almacenes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "categorias",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "tipos_movimiento",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "tipos_movimiento",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "tipos_movimiento",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "tipos_movimiento",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "unidades_medida",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
