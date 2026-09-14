using AlmacenApi.Data;
using AlmacenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProveedoresController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Proveedor>>> GetProveedores(CancellationToken cancellationToken)
    {
        return Ok(await context.Proveedores
            .AsNoTracking()
            .Where(proveedor => proveedor.Activo)
            .OrderBy(proveedor => proveedor.Nombre)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Proveedor>> GetProveedor(int id, CancellationToken cancellationToken)
    {
        var proveedor = await context.Proveedores.AsNoTracking()
            .FirstOrDefaultAsync(proveedor => proveedor.Id == id, cancellationToken);

        return proveedor is null ? NotFound() : Ok(proveedor);
    }

    [HttpPost]
    public async Task<ActionResult<Proveedor>> CrearProveedor(Proveedor proveedor, CancellationToken cancellationToken)
    {
        proveedor.Id = 0;
        context.Proveedores.Add(proveedor);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetProveedor), new { id = proveedor.Id }, proveedor);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarProveedor(int id, Proveedor proveedor, CancellationToken cancellationToken)
    {
        if (id != proveedor.Id)
        {
            return BadRequest();
        }

        var existente = await context.Proveedores.FindAsync([id], cancellationToken);
        if (existente is null)
        {
            return NotFound();
        }

        existente.Nombre = proveedor.Nombre;
        existente.Telefono = proveedor.Telefono;
        existente.Email = proveedor.Email;
        existente.Direccion = proveedor.Direccion;
        existente.Activo = proveedor.Activo;

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarProveedor(int id, CancellationToken cancellationToken)
    {
        var proveedor = await context.Proveedores.FindAsync([id], cancellationToken);
        if (proveedor is null)
        {
            return NotFound();
        }

        var enUso = await context.Compras.AnyAsync(compra => compra.ProveedorId == id, cancellationToken);
        if (enUso)
        {
            return Conflict(new { message = "No se puede eliminar: el proveedor tiene compras registradas." });
        }

        proveedor.Activo = false;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
