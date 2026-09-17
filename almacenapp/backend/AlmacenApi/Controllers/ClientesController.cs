using AlmacenApi.Data;
using AlmacenApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes(CancellationToken cancellationToken)
    {
        return Ok(await context.Clientes
            .AsNoTracking()
            .Where(cliente => cliente.Activo)
            .OrderBy(cliente => cliente.Nombre)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Cliente>> GetCliente(int id, CancellationToken cancellationToken)
    {
        var cliente = await context.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(cliente => cliente.Id == id, cancellationToken);

        return cliente is null ? NotFound() : Ok(cliente);
    }

    [HttpPost]
    [Authorize(Policy = "Permiso:clientes")]
    public async Task<ActionResult<Cliente>> CrearCliente(Cliente cliente, CancellationToken cancellationToken)
    {
        cliente.Id = 0;
        context.Clientes.Add(cliente);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permiso:clientes")]
    public async Task<IActionResult> ActualizarCliente(int id, Cliente cliente, CancellationToken cancellationToken)
    {
        if (id != cliente.Id)
        {
            return BadRequest();
        }

        var existente = await context.Clientes.FindAsync([id], cancellationToken);
        if (existente is null)
        {
            return NotFound();
        }

        existente.Nombre = cliente.Nombre;
        existente.Telefono = cliente.Telefono;
        existente.Email = cliente.Email;
        existente.Direccion = cliente.Direccion;
        existente.Activo = cliente.Activo;

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permiso:clientes")]
    public async Task<IActionResult> EliminarCliente(int id, CancellationToken cancellationToken)
    {
        var cliente = await context.Clientes.FindAsync([id], cancellationToken);
        if (cliente is null)
        {
            return NotFound();
        }

        var enUso = await context.Salidas.AnyAsync(salida => salida.ClienteId == id, cancellationToken);
        if (enUso)
        {
            return Conflict(new { message = "No se puede eliminar: el cliente tiene salidas registradas." });
        }

        cliente.Activo = false;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
