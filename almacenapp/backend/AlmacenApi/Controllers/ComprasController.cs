using AlmacenApi.Data;
using AlmacenApi.Models;
using AlmacenApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

/// <summary>
/// Compras: se crean en estado "Pendiente" (solo el encabezado y los
/// detalles, sin tocar inventario). Al confirmarlas se elige el almacén que
/// recibe la mercancía, se generan los MovimientoInventario (Entrada) y se
/// actualizan las Existencias.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ComprasController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetCompras([FromQuery] string? estado, CancellationToken cancellationToken)
    {
        var query = context.Compras.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(compra => compra.Estado == estado);
        }

        var resultado = await query
            .OrderByDescending(compra => compra.Fecha)
            .Select(compra => new
            {
                compra.Id,
                compra.ProveedorId,
                ProveedorNombre = compra.Proveedor.Nombre,
                compra.Fecha,
                compra.Estado,
                compra.Observaciones,
                Total = compra.Detalles.Sum(detalle => detalle.Cantidad * detalle.PrecioUnitario)
            })
            .ToListAsync(cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetCompra(int id, CancellationToken cancellationToken)
    {
        var compra = await context.Compras.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.ProveedorId,
                ProveedorNombre = c.Proveedor.Nombre,
                c.Fecha,
                c.Estado,
                c.Observaciones,
                Detalles = c.Detalles.Select(detalle => new
                {
                    detalle.Id,
                    detalle.ProductoId,
                    ProductoNombre = detalle.Producto.Nombre,
                    ProductoSku = detalle.Producto.Sku,
                    detalle.Cantidad,
                    detalle.PrecioUnitario,
                    Subtotal = detalle.Cantidad * detalle.PrecioUnitario
                })
            })
            .FirstOrDefaultAsync(cancellationToken);

        return compra is null ? NotFound() : Ok(compra);
    }

    [HttpPost]
    public async Task<ActionResult> CrearCompra(CompraCreateDto dto, CancellationToken cancellationToken)
    {
        var proveedorExiste = await context.Proveedores.AnyAsync(p => p.Id == dto.ProveedorId, cancellationToken);
        if (!proveedorExiste)
        {
            return BadRequest(new { message = "El proveedor indicado no existe." });
        }

        var productoIds = dto.Detalles.Select(detalle => detalle.ProductoId).Distinct().ToList();
        var productosValidos = await context.Productos
            .Where(producto => productoIds.Contains(producto.Id))
            .Select(producto => producto.Id)
            .ToListAsync(cancellationToken);

        if (productosValidos.Count != productoIds.Count)
        {
            return BadRequest(new { message = "Uno o más productos indicados no existen." });
        }

        var compra = new Compra
        {
            ProveedorId = dto.ProveedorId,
            Fecha = DateTime.UtcNow,
            Estado = "Pendiente",
            Observaciones = dto.Observaciones,
            Detalles = dto.Detalles.Select(detalle => new CompraDetalle
            {
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario
            }).ToList()
        };

        context.Compras.Add(compra);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCompra), new { id = compra.Id }, new { compra.Id });
    }

    [HttpPost("{id:int}/confirmar")]
    public async Task<ActionResult> ConfirmarCompra(int id, ConfirmarCompraDto dto, CancellationToken cancellationToken)
    {
        var compra = await context.Compras
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (compra is null)
        {
            return NotFound();
        }

        if (compra.Estado != "Pendiente")
        {
            return Conflict(new { message = $"La compra ya está en estado '{compra.Estado}' y no se puede confirmar." });
        }

        var almacenExiste = await context.Almacenes.AnyAsync(a => a.Id == dto.AlmacenId, cancellationToken);
        if (!almacenExiste)
        {
            return BadRequest(new { message = "El almacén indicado no existe." });
        }

        var fecha = DateTime.UtcNow;
        var productosAfectados = new HashSet<int>();

        foreach (var detalle in compra.Detalles)
        {
            var existencia = await context.ObtenerOCrearExistenciaAsync(
                detalle.ProductoId, dto.AlmacenId, null, cancellationToken);

            existencia.Cantidad += detalle.Cantidad;

            context.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = detalle.ProductoId,
                AlmacenId = dto.AlmacenId,
                TipoMovimientoId = 1, // Entrada
                UsuarioId = dto.UsuarioId,
                Cantidad = detalle.Cantidad,
                Fecha = fecha,
                Referencia = $"Compra #{compra.Id}",
                Observaciones = compra.Observaciones
            });

            productosAfectados.Add(detalle.ProductoId);
        }

        foreach (var productoId in productosAfectados)
        {
            await context.RecalcularStockProductoAsync(productoId, cancellationToken);
        }

        compra.Estado = "Completada";
        await context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Compra confirmada y existencias actualizadas." });
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<ActionResult> CancelarCompra(int id, CancellationToken cancellationToken)
    {
        var compra = await context.Compras.FindAsync([id], cancellationToken);
        if (compra is null)
        {
            return NotFound();
        }

        if (compra.Estado != "Pendiente")
        {
            return Conflict(new { message = $"La compra ya está en estado '{compra.Estado}' y no se puede cancelar." });
        }

        compra.Estado = "Cancelada";
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
