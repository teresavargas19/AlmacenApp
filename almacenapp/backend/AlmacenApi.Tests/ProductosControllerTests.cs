using AlmacenApi.Controllers;
using AlmacenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlmacenApi.Tests;

public class ProductosControllerTests
{
    [Fact]
    public async Task CrearProducto_Rechaza_SiElSkuYaExiste()
    {
        await using var context = TestSupport.CreateContext();
        TestSupport.CrearProducto(context, "SKU-DUP");
        await context.SaveChangesAsync();

        var controller = new ProductosController(context);
        var resultado = await controller.CrearProducto(new Producto
        {
            CategoriaId = 1,
            UnidadMedidaId = 1,
            Sku = "SKU-DUP",
            Nombre = "Otro producto",
            Precio = 5,
        }, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(resultado.Result);
    }

    [Fact]
    public async Task CrearProducto_Crea_SiElSkuEsUnico()
    {
        await using var context = TestSupport.CreateContext();
        var controller = new ProductosController(context);

        var resultado = await controller.CrearProducto(new Producto
        {
            CategoriaId = 1,
            UnidadMedidaId = 1,
            Sku = "SKU-NUEVO",
            Nombre = "Producto nuevo",
            Precio = 25,
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(resultado.Result);
        Assert.True(await context.Productos.AnyAsync(p => p.Sku == "SKU-NUEVO"));
    }

    [Fact]
    public async Task ActualizarProducto_ActualizaLosCampos()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-UPD", nombre: "Nombre viejo", precio: 10);
        await context.SaveChangesAsync();

        var controller = new ProductosController(context);
        var actualizado = new Producto
        {
            Id = producto.Id,
            CategoriaId = 1,
            UnidadMedidaId = 1,
            Sku = "SKU-UPD",
            Nombre = "Nombre nuevo",
            Precio = 20,
            StockMinimo = 3,
            Activo = true,
        };

        var resultado = await controller.ActualizarProducto(producto.Id, actualizado, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        var guardado = await context.Productos.FindAsync(producto.Id);
        Assert.Equal("Nombre nuevo", guardado!.Nombre);
        Assert.Equal(20, guardado.Precio);
        Assert.Equal(3, guardado.StockMinimo);
    }

    [Fact]
    public async Task EliminarProducto_LoDesactiva_EnVezDeBorrarlo()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-DEL");
        await context.SaveChangesAsync();

        var controller = new ProductosController(context);
        var resultado = await controller.EliminarProducto(producto.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        var guardado = await context.Productos.FindAsync(producto.Id);
        Assert.NotNull(guardado); // sigue existiendo en la base
        Assert.False(guardado!.Activo);
    }

    [Fact]
    public async Task GetProductos_SoloDevuelveLosActivos()
    {
        await using var context = TestSupport.CreateContext();
        TestSupport.CrearProducto(context, "SKU-ACTIVO", nombre: "Activo");
        var inactivo = TestSupport.CrearProducto(context, "SKU-INACTIVO", nombre: "Inactivo");
        inactivo.Activo = false;
        await context.SaveChangesAsync();

        var controller = new ProductosController(context);
        var resultado = await controller.GetProductos(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var productos = Assert.IsAssignableFrom<IEnumerable<Producto>>(ok.Value);
        Assert.Single(productos, p => p.Sku == "SKU-ACTIVO");
    }
}
