using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlmacenApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVentasCompletas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoGeneralPorcentaje",
                table: "salidas",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Itbis",
                table: "salidas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "salidas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "SaldoPendiente",
                table: "salidas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "salidas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "salidas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoPorcentaje",
                table: "salida_detalles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioUnitario",
                table: "salida_detalles",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "abonos_salida",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalidaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abonos_salida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_abonos_salida_salidas_SalidaId",
                        column: x => x.SalidaId,
                        principalTable: "salidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_abonos_salida_SalidaId",
                table: "abonos_salida",
                column: "SalidaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abonos_salida");

            migrationBuilder.DropColumn(
                name: "DescuentoGeneralPorcentaje",
                table: "salidas");

            migrationBuilder.DropColumn(
                name: "Itbis",
                table: "salidas");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "salidas");

            migrationBuilder.DropColumn(
                name: "SaldoPendiente",
                table: "salidas");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "salidas");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "salidas");

            migrationBuilder.DropColumn(
                name: "DescuentoPorcentaje",
                table: "salida_detalles");

            migrationBuilder.DropColumn(
                name: "PrecioUnitario",
                table: "salida_detalles");
        }
    }
}
