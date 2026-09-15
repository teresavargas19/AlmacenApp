using AlmacenApi.Data;
using AlmacenApi.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlmacenApi.Tests;

/// <summary>
/// InventarioHelpers es el corazón del control de stock: todo controller que
/// mueve inventario (Compras, Salidas, Movimientos) pasa por aquí.
/// </summary>
public class InventarioHelpersTests
{
    [Fact]
    public async Task ObtenerOCrearExistenciaAsync_CreaNuevaExistencia_SiNoExiste()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-HLP-1");
        await context.SaveChangesAsync();

        var existencia = await context.ObtenerOCrearExistenciaAsync(producto.Id, almacenId: 1, ubicacionId: null, CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Equal(0, existencia.Cantidad);
        Assert.Equal(producto.Id, existencia.ProductoId);
        Assert.Equal(1, existencia.AlmacenId);
        Assert.Single(context.Existencias);
    }

    [Fact]
    public async Task ObtenerOCrearExistenciaAsync_DevuelveLaMismaExistencia_SiYaExiste()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-HLP-2");
        await context.SaveChangesAsync();

        var primera = await context.ObtenerOCrearExistenciaAsync(producto.Id, 1, null, CancellationToken.None);
        primera.Cantidad = 15;
        await context.SaveChangesAsync();

        var segunda = await context.ObtenerOCrearExistenciaAsync(producto.Id, 1, null, CancellationToken.None);

        Assert.Equal(primera.Id, segunda.Id);
        Assert.Equal(15, segunda.Cantidad);
        Assert.Single(context.Existencias);
    }

    [Fact]
    public async Task RecalcularStockProductoAsync_SumaLasExistenciasDeTodosLosAlmacenes()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-HLP-3");
        var almacen2 = TestSupport.CrearAlmacen(context, "Sucursal 2");
        await context.SaveChangesAsync();

        context.Existencias.Add(new Existencia { ProductoId = producto.Id, AlmacenId = 1, Cantidad = 10 });
        context.Existencias.Add(new Existencia { ProductoId = producto.Id, AlmacenId = almacen2.Id, Cantidad = 5 });
        await context.SaveChangesAsync();

        await context.RecalcularStockProductoAsync(producto.Id, CancellationToken.None);
        await context.SaveChangesAsync();

        var productoActualizado = await context.Productos.FindAsync(producto.Id);
        Assert.Equal(15, productoActualizado!.Stock);
    }

    [Theory]
    [InlineData(7.4, 7)]
    [InlineData(7.5, 8)]
    [InlineData(7.6, 8)]
    public async Task RecalcularStockProductoAsync_RedondeaAlEnteroMasCercano(decimal cantidad, int esperado)
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, $"SKU-HLP-ROUND-{cantidad}");
        await context.SaveChangesAsync();

        context.Existencias.Add(new Existencia { ProductoId = producto.Id, AlmacenId = 1, Cantidad = cantidad });
        await context.SaveChangesAsync();

        await context.RecalcularStockProductoAsync(producto.Id, CancellationToken.None);
        await context.SaveChangesAsync();

        var productoActualizado = await context.Productos.FindAsync(producto.Id);
        Assert.Equal(esperado, productoActualizado!.Stock);
    }
}
