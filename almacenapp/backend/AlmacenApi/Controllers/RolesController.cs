using AlmacenApi.Data;
using AlmacenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Rol>>> GetRoles(CancellationToken cancellationToken)
    {
        return Ok(await context.Roles
            .AsNoTracking()
            .OrderBy(rol => rol.Nombre)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Rol>> GetRol(int id, CancellationToken cancellationToken)
    {
        var rol = await context.Roles.AsNoTracking().FirstOrDefaultAsync(rol => rol.Id == id, cancellationToken);
        return rol is null ? NotFound() : Ok(rol);
    }

    [HttpPost]
    public async Task<ActionResult<Rol>> CrearRol(Rol rol, CancellationToken cancellationToken)
    {
        rol.Id = 0;
        context.Roles.Add(rol);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetRol), new { id = rol.Id }, rol);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarRol(int id, Rol rol, CancellationToken cancellationToken)
    {
        if (id != rol.Id)
        {
            return BadRequest();
        }

        var existente = await context.Roles.FindAsync([id], cancellationToken);
        if (existente is null)
        {
            return NotFound();
        }

        existente.Nombre = rol.Nombre;
        existente.Permisos = rol.Permisos;

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarRol(int id, CancellationToken cancellationToken)
    {
        var rol = await context.Roles.FindAsync([id], cancellationToken);
        if (rol is null)
        {
            return NotFound();
        }

        var enUso = await context.Usuarios.AnyAsync(usuario => usuario.RolId == id, cancellationToken);
        if (enUso)
        {
            return Conflict(new { message = "No se puede eliminar: hay usuarios con este rol." });
        }

        context.Roles.Remove(rol);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
