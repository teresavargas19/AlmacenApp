using System.ComponentModel.DataAnnotations;
using AlmacenApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

/// <summary>
/// Solo lectura de existencias y ajuste del mínimo. La cantidad en sí solo
/// cambia a través de MovimientosInventarioController (o de Compras/Salidas
/// confirmadas), nunca directamente aquí.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExistenciasController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetExistencias(
        [FromQuery] int? productoId,
        [FromQuery] int? almacenId,
        CancellationToken cancellationToken)
    {
        var query = context.Existencias.AsNoTracking().AsQueryable();

        if (productoId is not null)
        {
            query = query.Where(existencia => existencia.ProductoId == productoId);
        }

        if (almacenId is not null)
        {
            query = query.Where(existencia => existencia.AlmacenId == almacenId);
        }

        var resultado = await query
            .OrderBy(existencia => existencia.ProductoId)
            .Select(existencia => new
            {
                existencia.Id,
                existencia.ProductoId,
                ProductoNombre = existencia.Producto.Nombre,
                ProductoSku = existencia.Producto.Sku,
                existencia.AlmacenId,
                AlmacenNombre = existencia.Almacen.Nombre,
                existencia.UbicacionId,
                UbicacionNombre = existencia.Ubicacion != null ? existencia.Ubicacion.Nombre : null,
                existencia.Cantidad,
                existencia.CantidadMinima
            })
            .ToListAsync(cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("bajo-minimo")]
    public async Task<ActionResult> GetBajoMinimo(CancellationToken cancellationToken)
    {
        var resultado = await context.Existencias.AsNoTracking()
            .Where(existencia => existencia.Cantidad < existencia.CantidadMinima)
            .OrderBy(existencia => existencia.Producto.Nombre)
            .Select(existencia => new
            {
                existencia.Id,
                existencia.ProductoId,
                ProductoNombre = existencia.Producto.Nombre,
                ProductoSku = existencia.Producto.Sku,
                existencia.AlmacenId,
                AlmacenNombre = existencia.Almacen.Nombre,
                existencia.Cantidad,
                existencia.CantidadMinima
            })
            .ToListAsync(cancellationToken);

        return Ok(resultado);
    }

    [HttpPut("{id:int}/minimo")]
    [Authorize(Policy = "Permiso:existencias")]
    public async Task<IActionResult> ActualizarMinimo(int id, ExistenciaMinimoDto dto, CancellationToken cancellationToken)
    {
        var existencia = await context.Existencias.FindAsync([id], cancellationToken);
        if (existencia is null)
        {
            return NotFound();
        }

        existencia.CantidadMinima = dto.CantidadMinima;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public class ExistenciaMinimoDto
{
    [Range(0, double.MaxValue)]
    public decimal CantidadMinima { get; set; }
}
