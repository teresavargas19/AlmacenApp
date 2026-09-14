using AlmacenApi.Data;
using AlmacenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriasController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Categoria>>> GetCategorias(CancellationToken cancellationToken)
    {
        return Ok(await context.Categorias
            .AsNoTracking()
            .Where(categoria => categoria.Activo)
            .OrderBy(categoria => categoria.Nombre)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Categoria>> GetCategoria(int id, CancellationToken cancellationToken)
    {
        var categoria = await context.Categorias.AsNoTracking()
            .FirstOrDefaultAsync(categoria => categoria.Id == id, cancellationToken);

        return categoria is null ? NotFound() : Ok(categoria);
    }

    [HttpPost]
    public async Task<ActionResult<Categoria>> CrearCategoria(Categoria categoria, CancellationToken cancellationToken)
    {
        categoria.Id = 0;
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCategoria), new { id = categoria.Id }, categoria);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarCategoria(int id, Categoria categoria, CancellationToken cancellationToken)
    {
        if (id != categoria.Id)
        {
            return BadRequest();
        }

        var existente = await context.Categorias.FindAsync([id], cancellationToken);
        if (existente is null)
        {
            return NotFound();
        }

        existente.Nombre = categoria.Nombre;
        existente.Activo = categoria.Activo;

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarCategoria(int id, CancellationToken cancellationToken)
    {
        var categoria = await context.Categorias.FindAsync([id], cancellationToken);
        if (categoria is null)
        {
            return NotFound();
        }

        var enUso = await context.Productos.AnyAsync(producto => producto.CategoriaId == id, cancellationToken);
        if (enUso)
        {
            return Conflict(new { message = "No se puede eliminar: hay productos asignados a esta categoría." });
        }

        categoria.Activo = false;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
