using AlmacenApi.Data;
using AlmacenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlmacenesController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Almacen>>> GetAlmacenes(CancellationToken cancellationToken)
    {
        return Ok(await context.Almacenes
            .AsNoTracking()
            .Where(almacen => almacen.Activo)
            .OrderBy(almacen => almacen.Nombre)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Almacen>> GetAlmacen(int id, CancellationToken cancellationToken)
    {
        var almacen = await context.Almacenes.AsNoTracking()
            .FirstOrDefaultAsync(almacen => almacen.Id == id, cancellationToken);

        return almacen is null ? NotFound() : Ok(almacen);
    }

    [HttpPost]
    public async Task<ActionResult<Almacen>> CrearAlmacen(Almacen almacen, CancellationToken cancellationToken)
    {
        almacen.Id = 0;
        context.Almacenes.Add(almacen);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAlmacen), new { id = almacen.Id }, almacen);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarAlmacen(int id, Almacen almacen, CancellationToken cancellationToken)
    {
        if (id != almacen.Id)
        {
            return BadRequest();
        }

        var existente = await context.Almacenes.FindAsync([id], cancellationToken);
        if (existente is null)
        {
            return NotFound();
        }

        existente.Nombre = almacen.Nombre;
        existente.Direccion = almacen.Direccion;
        existente.Activo = almacen.Activo;

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarAlmacen(int id, CancellationToken cancellationToken)
    {
        var almacen = await context.Almacenes.FindAsync([id], cancellationToken);
        if (almacen is null)
        {
            return NotFound();
        }

        var enUso = await context.Existencias.AnyAsync(existencia => existencia.AlmacenId == id, cancellationToken);
        if (enUso)
        {
            return Conflict(new { message = "No se puede eliminar: el almacén tiene existencias registradas." });
        }

        almacen.Activo = false;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
