using AlmacenApi.Data;
using AlmacenApi.Models;
using AlmacenApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

/// <summary>
/// Salidas/despachos: se crean en estado "Pendiente" (solo encabezado y
/// detalles, sin tocar inventario). Al confirmarlas se elige el almacén del
/// que sale la mercancía, se valida existencia suficiente, se generan los
/// MovimientoInventario (Salida) y se descuentan las Existencias.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SalidasController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetSalidas([FromQuery] string? estado, CancellationToken cancellationToken)
    {
        var query = context.Salidas.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(salida => salida.Estado == estado);
        }

        var resultado = await query
            .OrderByDescending(salida => salida.Fecha)
            .Select(salida => new
            {
                salida.Id,
                salida.ClienteId,
                ClienteNombre = salida.Cliente != null ? salida.Cliente.Nombre : null,
                salida.Fecha,
                salida.Estado,
                salida.Observaciones,
                TotalUnidades = salida.Detalles.Sum(detalle => detalle.Cantidad)
            })
            .ToListAsync(cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetSalida(int id, CancellationToken cancellationToken)
    {
        var salida = await context.Salidas.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new
            {
                s.Id,
                s.ClienteId,
                ClienteNombre = s.Cliente != null ? s.Cliente.Nombre : null,
                s.Fecha,
                s.Estado,
                s.Observaciones,
                Detalles = s.Detalles.Select(detalle => new
                {
                    detalle.Id,
                    detalle.ProductoId,
                    ProductoNombre = detalle.Producto.Nombre,
                    ProductoSku = detalle.Producto.Sku,
                    detalle.Cantidad
                })
            })
            .FirstOrDefaultAsync(cancellationToken);

        return salida is null ? NotFound() : Ok(salida);
    }

    [HttpPost]
    public async Task<ActionResult> CrearSalida(SalidaCreateDto dto, CancellationToken cancellationToken)
    {
        if (dto.ClienteId is not null)
        {
            var clienteExiste = await context.Clientes.AnyAsync(c => c.Id == dto.ClienteId, cancellationToken);
            if (!clienteExiste)
            {
                return BadRequest(new { message = "El cliente indicado no existe." });
            }
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

        var salida = new Salida
        {
            ClienteId = dto.ClienteId,
            Fecha = DateTime.UtcNow,
            Estado = "Pendiente",
            Observaciones = dto.Observaciones,
            Detalles = dto.Detalles.Select(detalle => new SalidaDetalle
            {
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad
            }).ToList()
        };

        context.Salidas.Add(salida);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetSalida), new { id = salida.Id }, new { salida.Id });
    }

    [HttpPost("{id:int}/confirmar")]
    public async Task<ActionResult> ConfirmarSalida(int id, ConfirmarSalidaDto dto, CancellationToken cancellationToken)
    {
        var salida = await context.Salidas
            .Include(s => s.Detalles)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (salida is null)
        {
            return NotFound();
        }

        if (salida.Estado != "Pendiente")
        {
            return Conflict(new { message = $"La salida ya está en estado '{salida.Estado}' y no se puede confirmar." });
        }

        var almacenExiste = await context.Almacenes.AnyAsync(a => a.Id == dto.AlmacenId, cancellationToken);
        if (!almacenExiste)
        {
            return BadRequest(new { message = "El almacén indicado no existe." });
        }

        // Validar existencia suficiente para TODAS las líneas antes de aplicar nada.
        var faltantes = new List<string>();
        foreach (var detalle in salida.Detalles)
        {
            var existencia = await context.Existencias.AsNoTracking().FirstOrDefaultAsync(
                e => e.ProductoId == detalle.ProductoId && e.AlmacenId == dto.AlmacenId,
                cancellationToken);

            if (existencia is null || existencia.Cantidad < detalle.Cantidad)
            {
                var producto = await context.Productos.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == detalle.ProductoId, cancellationToken);
                faltantes.Add(producto?.Nombre ?? $"Producto #{detalle.ProductoId}");
            }
        }

        if (faltantes.Count > 0)
        {
            return Conflict(new
            {
                message = "No hay existencia suficiente en el almacén indicado para: " + string.Join(", ", faltantes)
            });
        }

        var fecha = DateTime.UtcNow;
        var productosAfectados = new HashSet<int>();

        foreach (var detalle in salida.Detalles)
        {
            var existencia = await context.ObtenerOCrearExistenciaAsync(
                detalle.ProductoId, dto.AlmacenId, null, cancellationToken);

            existencia.Cantidad -= detalle.Cantidad;

            context.MovimientosInventario.Add(new MovimientoInventario
            {
                ProductoId = detalle.ProductoId,
                AlmacenId = dto.AlmacenId,
                TipoMovimientoId = 2, // Salida
                UsuarioId = dto.UsuarioId,
                Cantidad = detalle.Cantidad,
                Fecha = fecha,
                Referencia = $"Salida #{salida.Id}",
                Observaciones = salida.Observaciones
            });

            productosAfectados.Add(detalle.ProductoId);
        }

        foreach (var productoId in productosAfectados)
        {
            await context.RecalcularStockProductoAsync(productoId, cancellationToken);
        }

        salida.Estado = "Completada";
        await context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Salida confirmada y existencias actualizadas." });
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<ActionResult> CancelarSalida(int id, CancellationToken cancellationToken)
    {
        var salida = await context.Salidas.FindAsync([id], cancellationToken);
        if (salida is null)
        {
            return NotFound();
        }

        if (salida.Estado != "Pendiente")
        {
            return Conflict(new { message = $"La salida ya está en estado '{salida.Estado}' y no se puede cancelar." });
        }

        salida.Estado = "Cancelada";
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
