using AlmacenApi.Data;
using AlmacenApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Producto>>> GetProductos(CancellationToken cancellationToken)
    {
        return Ok(await context.Productos
            .AsNoTracking()
            .Where(producto => producto.Activo)
            .OrderBy(producto => producto.Nombre)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Producto>> GetProducto(int id, CancellationToken cancellationToken)
    {
        var producto = await context.Productos
            .AsNoTracking()
            .FirstOrDefaultAsync(producto => producto.Id == id, cancellationToken);

        return producto is null ? NotFound() : Ok(producto);
    }

    [HttpPost]
    [Authorize(Policy = "Permiso:productos")]
    public async Task<ActionResult<Producto>> CrearProducto(Producto producto, CancellationToken cancellationToken)
    {
        var skuExiste = await context.Productos.AnyAsync(item => item.Sku == producto.Sku, cancellationToken);
        if (skuExiste)
        {
            return Conflict(new { message = "Ya existe un producto con ese SKU." });
        }

        producto.Id = 0;
        producto.CreadoEn = DateTime.UtcNow;
        context.Productos.Add(producto);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, producto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permiso:productos")]
    public async Task<IActionResult> ActualizarProducto(int id, Producto producto, CancellationToken cancellationToken)
    {
        if (id != producto.Id)
        {
            return BadRequest();
        }

        var productoExistente = await context.Productos.FindAsync([id], cancellationToken);
        if (productoExistente is null)
        {
            return NotFound();
        }

        var skuUsado = await context.Productos.AnyAsync(
            item => item.Sku == producto.Sku && item.Id != id,
            cancellationToken);
        if (skuUsado)
        {
            return Conflict(new { message = "Ya existe otro producto con ese SKU." });
        }

        productoExistente.Sku = producto.Sku;
        productoExistente.Nombre = producto.Nombre;
        productoExistente.Descripcion = producto.Descripcion;
        productoExistente.Stock = producto.Stock;
        productoExistente.StockMinimo = producto.StockMinimo;
        productoExistente.Precio = producto.Precio;
        productoExistente.Activo = producto.Activo;

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Permiso:productos")]
    public async Task<IActionResult> EliminarProducto(int id, CancellationToken cancellationToken)
    {
        var producto = await context.Productos.FindAsync([id], cancellationToken);
        if (producto is null)
        {
            return NotFound();
        }

        producto.Activo = false;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}