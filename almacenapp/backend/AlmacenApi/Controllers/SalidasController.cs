using AlmacenApi.Data;
using AlmacenApi.Models;
using AlmacenApi.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
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
    /// <summary>Tasa de ITBIS de República Dominicana. Si algún día hace falta que sea configurable, mover a appsettings.</summary>
    private const decimal ItbisPorcentaje = 0.18m;

    private static readonly string[] MetodosPagoValidos = ["Efectivo", "Transferencia", "Credito"];

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
                salida.MetodoPago,
                salida.Total,
                salida.SaldoPendiente,
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
                s.MetodoPago,
                s.DescuentoGeneralPorcentaje,
                s.Subtotal,
                s.Itbis,
                s.Total,
                s.SaldoPendiente,
                Detalles = s.Detalles.Select(detalle => new
                {
                    detalle.Id,
                    detalle.ProductoId,
                    ProductoNombre = detalle.Producto.Nombre,
                    ProductoSku = detalle.Producto.Sku,
                    detalle.Cantidad,
                    detalle.PrecioUnitario,
                    detalle.DescuentoPorcentaje,
                    Subtotal = detalle.Cantidad * detalle.PrecioUnitario * (1 - detalle.DescuentoPorcentaje / 100m)
                }),
                Abonos = s.Abonos
                    .OrderByDescending(abono => abono.Fecha)
                    .Select(abono => new { abono.Id, abono.Fecha, abono.Monto, abono.Observaciones })
            })
            .FirstOrDefaultAsync(cancellationToken);

        return salida is null ? NotFound() : Ok(salida);
    }

    [HttpPost]
    [Authorize(Policy = "Permiso:salidas")]
    public async Task<ActionResult> CrearSalida(SalidaCreateDto dto, CancellationToken cancellationToken)
    {
        if (!MetodosPagoValidos.Contains(dto.MetodoPago))
        {
            return BadRequest(new { message = "Método de pago inválido. Debe ser Efectivo, Transferencia o Credito." });
        }

        if (dto.MetodoPago == "Credito" && dto.ClienteId is null)
        {
            return BadRequest(new { message = "Las ventas a crédito requieren indicar un cliente." });
        }

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

        // Subtotal: suma de cada línea (cantidad * precio) ya con su descuento de
        // línea aplicado, todavía sin el descuento general ni el ITBIS.
        var subtotal = dto.Detalles.Sum(detalle =>
            detalle.Cantidad * detalle.PrecioUnitario * (1 - detalle.DescuentoPorcentaje / 100m));
        var subtotalConDescuentoGeneral = subtotal * (1 - dto.DescuentoGeneralPorcentaje / 100m);
        var itbis = Math.Round(subtotalConDescuentoGeneral * ItbisPorcentaje, 2);
        var total = Math.Round(subtotalConDescuentoGeneral + itbis, 2);

        var salida = new Salida
        {
            ClienteId = dto.ClienteId,
            Fecha = DateTime.UtcNow,
            Estado = "Pendiente",
            Observaciones = dto.Observaciones,
            MetodoPago = dto.MetodoPago,
            DescuentoGeneralPorcentaje = dto.DescuentoGeneralPorcentaje,
            Subtotal = Math.Round(subtotal, 2),
            Itbis = itbis,
            Total = total,
            // El saldo pendiente de una venta a crédito nace en 0 y se activa
            // (= Total) al confirmar: antes de eso la mercancía no ha salido
            // todavía y no hay nada que cobrar. Ver ConfirmarSalida.
            SaldoPendiente = 0,
            Detalles = dto.Detalles.Select(detalle => new SalidaDetalle
            {
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                DescuentoPorcentaje = detalle.DescuentoPorcentaje
            }).ToList()
        };

        context.Salidas.Add(salida);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetSalida), new { id = salida.Id }, new { salida.Id });
    }

    [HttpPost("{id:int}/confirmar")]
    [Authorize(Policy = "Permiso:salidas")]
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

        salida.Estado = "Completada";
        // Recién ahora la mercancía sale de verdad, así que si es a crédito
        // es el momento en que nace la deuda del cliente.
        salida.SaldoPendiente = salida.MetodoPago == "Credito" ? salida.Total : 0m;

        // Hay que guardar las Existencias antes de recalcular el stock:
        // RecalcularStockProductoAsync suma las Existencias tal como están en la
        // base de datos, así que si no se guarda primero, sumaría el valor
        // anterior (sin esta salida) y Producto.Stock quedaría desfasado.
        await context.SaveChangesAsync(cancellationToken);

        foreach (var productoId in productosAfectados)
        {
            await context.RecalcularStockProductoAsync(productoId, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Salida confirmada y existencias actualizadas." });
    }

    [HttpPost("{id:int}/cancelar")]
    [Authorize(Policy = "Permiso:salidas")]
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

    /// <summary>Registrar un abono (pago parcial) contra una venta a crédito ya completada.</summary>
    [HttpPost("{id:int}/abonos")]
    [Authorize(Policy = "Permiso:salidas")]
    public async Task<ActionResult> RegistrarAbono(int id, AbonoCreateDto dto, CancellationToken cancellationToken)
    {
        var salida = await context.Salidas.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (salida is null)
        {
            return NotFound();
        }

        if (salida.Estado != "Completada")
        {
            return Conflict(new { message = "Solo se pueden registrar abonos en ventas completadas." });
        }

        if (salida.MetodoPago != "Credito")
        {
            return Conflict(new { message = "Esta venta no es a crédito; no tiene saldo pendiente que abonar." });
        }

        if (dto.Monto > salida.SaldoPendiente)
        {
            return BadRequest(new
            {
                message = $"El abono ({dto.Monto:0.00}) no puede ser mayor que el saldo pendiente ({salida.SaldoPendiente:0.00})."
            });
        }

        context.AbonosSalida.Add(new AbonoSalida
        {
            SalidaId = id,
            Fecha = DateTime.UtcNow,
            Monto = dto.Monto,
            Observaciones = dto.Observaciones
        });

        salida.SaldoPendiente -= dto.Monto;
        await context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Abono registrado.", saldoPendiente = salida.SaldoPendiente });
    }
}
