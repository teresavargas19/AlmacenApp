using AlmacenApi.Data;
using AlmacenApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnidadesMedidaController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UnidadMedida>>> GetUnidades(CancellationToken cancellationToken)
    {
        return Ok(await context.UnidadesMedida
            .AsNoTracking()
            .Where(unidad => unidad.Activo)
            .OrderBy(unidad => unidad.Nombre)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UnidadMedida>> GetUnidad(int id, CancellationToken cancellationToken)
    {
        var unidad = await context.UnidadesMedida.AsNoTracking()
            .FirstOrDefaultAsync(unidad => unidad.Id == id, cancellationToken);

        return unidad is null ? NotFound() : Ok(unidad);
    }

    [HttpPost]
    [Authorize(Policy = "Permiso:catalogos")]
    public async Task<ActionResult<UnidadMedida>> CrearUnidad(UnidadMedida unidad, CancellationToken cancellationToken)
    {
        unidad.Id = 0;
        context.UnidadesMedida.Add(unidad);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetUnidad), new { id = unidad.Id }, unidad);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permiso:catalogos")]
    public async Task<IActionResult> ActualizarUnidad(int id, UnidadMedida unidad, CancellationToken cancellationToken)
    {
        if (id != unidad.Id)
        {
            return BadRequest();
        }

        var existente = await context.UnidadesMedida.FindAsync([id], cancellationToken);
        if (existente is null)
        {
            return NotFound();
        }

        existente.Nombre = unidad.Nombre;
        existente.Abreviatura = unidad.Abreviatura;
        existente.Activo = unidad.Activo;

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permiso:catalogos")]
    public async Task<IActionResult> EliminarUnidad(int id, CancellationToken cancellationToken)
    {
        var unidad = await context.UnidadesMedida.FindAsync([id], cancellationToken);
        if (unidad is null)
        {
            return NotFound();
        }

        var enUso = await context.Productos.AnyAsync(producto => producto.UnidadMedidaId == id, cancellationToken);
        if (enUso)
        {
            return Conflict(new { message = "No se puede eliminar: hay productos con esta unidad de medida." });
        }

        unidad.Activo = false;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
