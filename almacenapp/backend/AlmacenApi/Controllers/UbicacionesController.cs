using AlmacenApi.Data;
using AlmacenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UbicacionesController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ubicacion>>> GetUbicaciones(
        [FromQuery] int? almacenId,
        CancellationToken cancellationToken)
    {
        var query = context.Ubicaciones.AsNoTracking().Where(ubicacion => ubicacion.Activo);

        if (almacenId is not null)
        {
            query = query.Where(ubicacion => ubicacion.AlmacenId == almacenId);
        }

        return Ok(await query.OrderBy(ubicacion => ubicacion.Nombre).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Ubicacion>> GetUbicacion(int id, CancellationToken cancellationToken)
    {
        var ubicacion = await context.Ubicaciones.AsNoTracking()
            .FirstOrDefaultAsync(ubicacion => ubicacion.Id == id, cancellationToken);

        return ubicacion is null ? NotFound() : Ok(ubicacion);
    }

    [HttpPost]
    public async Task<ActionResult<Ubicacion>> CrearUbicacion(Ubicacion ubicacion, CancellationToken cancellationToken)
    {
        var almacenExiste = await context.Almacenes.AnyAsync(almacen => almacen.Id == ubicacion.AlmacenId, cancellationToken);
        if (!almacenExiste)
        {
            return BadRequest(new { message = "El almacén indicado no existe." });
        }

        ubicacion.Id = 0;
        context.Ubicaciones.Add(ubicacion);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetUbicacion), new { id = ubicacion.Id }, ubicacion);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarUbicacion(int id, Ubicacion ubicacion, CancellationToken cancellationToken)
    {
        if (id != ubicacion.Id)
        {
            return BadRequest();
        }

        var existente = await context.Ubicaciones.FindAsync([id], cancellationToken);
        if (existente is null)
        {
            return NotFound();
        }

        existente.Nombre = ubicacion.Nombre;
        existente.Pasillo = ubicacion.Pasillo;
        existente.Estante = ubicacion.Estante;
        existente.Gaveta = ubicacion.Gaveta;
        existente.Activo = ubicacion.Activo;

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarUbicacion(int id, CancellationToken cancellationToken)
    {
        var ubicacion = await context.Ubicaciones.FindAsync([id], cancellationToken);
        if (ubicacion is null)
        {
            return NotFound();
        }

        var enUso = await context.Existencias.AnyAsync(existencia => existencia.UbicacionId == id, cancellationToken);
        if (enUso)
        {
            return Conflict(new { message = "No se puede eliminar: la ubicación tiene existencias registradas." });
        }

        ubicacion.Activo = false;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
