using AlmacenApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Data;

/// <summary>
/// Operaciones compartidas por los controllers que afectan existencias y el stock
/// resumido en Producto.Stock (Producto.Stock siempre queda como la suma de
/// Existencias.Cantidad para ese producto en todos los almacenes).
/// </summary>
public static class InventarioHelpers
{
    public static async Task<Existencia> ObtenerOCrearExistenciaAsync(
        this AlmacenDbContext context,
        int productoId,
        int almacenId,
        int? ubicacionId,
        CancellationToken cancellationToken)
    {
        var existencia = await context.Existencias.FirstOrDefaultAsync(
            e => e.ProductoId == productoId && e.AlmacenId == almacenId && e.UbicacionId == ubicacionId,
            cancellationToken);

        if (existencia is null)
        {
            existencia = new Existencia
            {
                ProductoId = productoId,
                AlmacenId = almacenId,
                UbicacionId = ubicacionId,
                Cantidad = 0,
                CantidadMinima = 0
            };
            context.Existencias.Add(existencia);
        }

        return existencia;
    }

    public static async Task RecalcularStockProductoAsync(
        this AlmacenDbContext context,
        int productoId,
        CancellationToken cancellationToken)
    {
        var total = await context.Existencias
            .Where(e => e.ProductoId == productoId)
            .SumAsync(e => e.Cantidad, cancellationToken);

        var producto = await context.Productos.FindAsync([productoId], cancellationToken);
        if (producto is not null)
        {
            producto.Stock = (int)Math.Round(total, MidpointRounding.AwayFromZero);
        }
    }
}
