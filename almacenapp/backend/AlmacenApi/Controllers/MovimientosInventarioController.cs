using AlmacenApi.Data;
using AlmacenApi.Models;
using AlmacenApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

/// <summary>
/// Ledger de movimientos de inventario. Cada movimiento registrado aquí
/// actualiza la Existencia correspondiente y recalcula Producto.Stock.
/// Tipos: 1=Entrada, 2=Salida, 3=Ajuste (delta con signo), 4=Transferencia
/// (usar el endpoint /transferencia, que crea dos movimientos).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MovimientosInventarioController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetMovimientos(
        [FromQuery] int? productoId,
        [FromQuery] int? almacenId,
        [FromQuery] int? tipoMovimientoId,
        CancellationToken cancellationToken)
    {
        var query = context.MovimientosInventario.AsNoTracking().AsQueryable();

        if (productoId is not null) query = query.Where(m => m.ProductoId == productoId);
        if (almacenId is not null) query = query.Where(m => m.AlmacenId == almacenId);
        if (tipoMovimientoId is not null) query = query.Where(m => m.TipoMovimientoId == tipoMovimientoId);

        var resultado = await query
            .OrderByDescending(m => m.Fecha)
            .Select(m => new
            {
                m.Id,
                m.ProductoId,
                ProductoNombre = m.Producto.Nombre,
                m.AlmacenId,
                AlmacenNombre = m.Almacen.Nombre,
                m.TipoMovimientoId,
                TipoMovimientoNombre = m.TipoMovimiento.Nombre,
                m.Cantidad,
                m.Fecha,
                m.Referencia,
                m.Observaciones
            })
            .ToListAsync(cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<MovimientoInventario>> GetMovimiento(long id, CancellationToken cancellationToken)
    {
        var movimiento = await context.MovimientosInventario.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        return movimiento is null ? NotFound() : Ok(movimiento);
    }

    [HttpPost]
    public async Task<ActionResult<MovimientoInventario>> RegistrarMovimiento(
        MovimientoCreateDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.TipoMovimientoId == 4)
        {
            return BadRequest(new { message = "Para transferencias use POST /api/movimientosinventario/transferencia." });
        }

        var tipo = await context.TiposMovimiento.FindAsync([dto.TipoMovimientoId], cancellationToken);
        if (tipo is null)
        {
            return BadRequest(new { message = "Tipo de movimiento inválido." });
        }

        var productoExiste = await context.Productos.AnyAsync(p => p.Id == dto.ProductoId, cancellationToken);
        if (!productoExiste)
        {
            return BadRequest(new { message = "El producto indicado no existe." });
        }

        var almacenExiste = await context.Almacenes.AnyAsync(a => a.Id == dto.AlmacenId, cancellationToken);
        if (!almacenExiste)
        {
            return BadRequest(new { message = "El almacén indicado no existe." });
        }

        var existencia = await context.ObtenerOCrearExistenciaAsync(
            dto.ProductoId, dto.AlmacenId, dto.UbicacionId, cancellationToken);

        decimal delta;
        decimal cantidadRegistrada;

        switch (dto.TipoMovimientoId)
        {
            case 1: // Entrada
                if (dto.Cantidad <= 0)
                {
                    return BadRequest(new { message = "La cantidad de una entrada debe ser positiva." });
                }
                delta = dto.Cantidad;
                cantidadRegistrada = dto.Cantidad;
                break;

            case 2: // Salida
                if (dto.Cantidad <= 0)
                {
                    return BadRequest(new { message = "La cantidad de una salida debe ser positiva." });
                }
                if (existencia.Cantidad < dto.Cantidad)
                {
                    return Conflict(new { message = "No hay existencia suficiente para esta salida." });
                }
                delta = -dto.Cantidad;
                cantidadRegistrada = dto.Cantidad;
                break;

            case 3: // Ajuste (delta con signo)
                if (existencia.Cantidad + dto.Cantidad < 0)
                {
                    return Conflict(new { message = "El ajuste dejaría la existencia en negativo." });
                }
                delta = dto.Cantidad;
                cantidadRegistrada = dto.Cantidad;
                break;

            default:
                return BadRequest(new { message = "Tipo de movimiento no soportado." });
        }

        existencia.Cantidad += delta;

        var movimiento = new MovimientoInventario
        {
            ProductoId = dto.ProductoId,
            AlmacenId = dto.AlmacenId,
            TipoMovimientoId = dto.TipoMovimientoId,
            UsuarioId = dto.UsuarioId,
            Cantidad = cantidadRegistrada,
            Fecha = DateTime.UtcNow,
            Referencia = dto.Referencia,
            Observaciones = dto.Observaciones
        };
        context.MovimientosInventario.Add(movimiento);

        await context.RecalcularStockProductoAsync(dto.ProductoId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetMovimiento), new { id = movimiento.Id }, movimiento);
    }

    [HttpPost("transferencia")]
    public async Task<ActionResult> RegistrarTransferencia(
        TransferenciaCreateDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.AlmacenOrigenId == dto.AlmacenDestinoId && dto.UbicacionOrigenId == dto.UbicacionDestinoId)
        {
            return BadRequest(new { message = "El origen y el destino de la transferencia no pueden ser iguales." });
        }

        var productoExiste = await context.Productos.AnyAsync(p => p.Id == dto.ProductoId, cancellationToken);
        if (!productoExiste)
        {
            return BadRequest(new { message = "El producto indicado no existe." });
        }

        var origen = await context.ObtenerOCrearExistenciaAsync(
            dto.ProductoId, dto.AlmacenOrigenId, dto.UbicacionOrigenId, cancellationToken);

        if (origen.Cantidad < dto.Cantidad)
        {
            return Conflict(new { message = "No hay existencia suficiente en el almacén de origen." });
        }

        var destino = await context.ObtenerOCrearExistenciaAsync(
            dto.ProductoId, dto.AlmacenDestinoId, dto.UbicacionDestinoId, cancellationToken);

        origen.Cantidad -= dto.Cantidad;
        destino.Cantidad += dto.Cantidad;

        var fecha = DateTime.UtcNow;
        context.MovimientosInventario.AddRange(
            new MovimientoInventario
            {
                ProductoId = dto.ProductoId,
                AlmacenId = dto.AlmacenOrigenId,
                TipoMovimientoId = 4,
                UsuarioId = dto.UsuarioId,
                Cantidad = dto.Cantidad,
                Fecha = fecha,
                Referencia = dto.Referencia,
                Observaciones = dto.Observaciones ?? "Salida por transferencia"
            },
            new MovimientoInventario
            {
                ProductoId = dto.ProductoId,
                AlmacenId = dto.AlmacenDestinoId,
                TipoMovimientoId = 4,
                UsuarioId = dto.UsuarioId,
                Cantidad = dto.Cantidad,
                Fecha = fecha,
                Referencia = dto.Referencia,
                Observaciones = dto.Observaciones ?? "Entrada por transferencia"
            });

        // El total global del producto no cambia con una transferencia, pero se
        // recalcula igual por si Producto.Stock estaba desincronizado.
        await context.RecalcularStockProductoAsync(dto.ProductoId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Transferencia registrada." });
    }
}
