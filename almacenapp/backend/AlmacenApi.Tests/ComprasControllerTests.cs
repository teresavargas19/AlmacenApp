using AlmacenApi.Controllers;
using AlmacenApi.Models;
using AlmacenApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlmacenApi.Tests;

public class ComprasControllerTests
{
    [Fact]
    public async Task CrearCompra_Rechaza_SiProveedorNoExiste()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-COMPRA-1");
        await context.SaveChangesAsync();

        var controller = new ComprasController(context);
        var resultado = await controller.CrearCompra(new CompraCreateDto
        {
            ProveedorId = 999,
            Detalles = [new CompraDetalleCreateDto { ProductoId = producto.Id, Cantidad = 5, PrecioUnitario = 10 }],
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task CrearCompra_Rechaza_SiAlgunProductoNoExiste()
    {
        await using var context = TestSupport.CreateContext();
        var proveedor = TestSupport.CrearProveedor(context);
        await context.SaveChangesAsync();

        var controller = new ComprasController(context);
        var resultado = await controller.CrearCompra(new CompraCreateDto
        {
            ProveedorId = proveedor.Id,
            Detalles = [new CompraDetalleCreateDto { ProductoId = 999, Cantidad = 5, PrecioUnitario = 10 }],
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task CrearCompra_CreaCompraEnEstadoPendiente_SinTocarInventario()
    {
        await using var context = TestSupport.CreateContext();
        var proveedor = TestSupport.CrearProveedor(context);
        var producto = TestSupport.CrearProducto(context, "SKU-COMPRA-2");
        await context.SaveChangesAsync();

        var controller = new ComprasController(context);
        var resultado = await controller.CrearCompra(new CompraCreateDto
        {
            ProveedorId = proveedor.Id,
            Detalles = [new CompraDetalleCreateDto { ProductoId = producto.Id, Cantidad = 5, PrecioUnitario = 10 }],
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(resultado);

        var compraGuardada = await context.Compras.Include(c => c.Detalles).SingleAsync();
        Assert.Equal("Pendiente", compraGuardada.Estado);
        Assert.Single(compraGuardada.Detalles);
        Assert.Empty(context.Existencias);
        Assert.Empty(context.MovimientosInventario);
    }

    [Fact]
    public async Task ConfirmarCompra_AumentaExistenciaYStock_GeneraMovimientoYMarcaCompletada()
    {
        await using var context = TestSupport.CreateContext();
        var proveedor = TestSupport.CrearProveedor(context);
        var producto = TestSupport.CrearProducto(context, "SKU-COMPRA-3", stock: 0);
        await context.SaveChangesAsync();

        var compra = new Compra
        {
            ProveedorId = proveedor.Id,
            Estado = "Pendiente",
            Detalles = [new CompraDetalle { ProductoId = producto.Id, Cantidad = 20, PrecioUnitario = 15 }],
        };
        context.Compras.Add(compra);
        await context.SaveChangesAsync();

        var controller = new ComprasController(context);
        var resultado = await controller.ConfirmarCompra(compra.Id, new ConfirmarCompraDto { AlmacenId = 1 }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(resultado);

        var compraActualizada = await context.Compras.FindAsync(compra.Id);
        Assert.Equal("Completada", compraActualizada!.Estado);

        var existencia = await context.Existencias.SingleAsync(e => e.ProductoId == producto.Id && e.AlmacenId == 1);
        Assert.Equal(20, existencia.Cantidad);

        var productoActualizado = await context.Productos.FindAsync(producto.Id);
        Assert.Equal(20, productoActualizado!.Stock);

        var movimiento = await context.MovimientosInventario.SingleAsync();
        Assert.Equal(1, movimiento.TipoMovimientoId); // Entrada
        Assert.Equal(20, movimiento.Cantidad);
    }

    [Fact]
    public async Task ConfirmarCompra_Rechaza_SiAlmacenNoExiste()
    {
        await using var context = TestSupport.CreateContext();
        var proveedor = TestSupport.CrearProveedor(context);
        var producto = TestSupport.CrearProducto(context, "SKU-COMPRA-4");
        await context.SaveChangesAsync();

        var compra = new Compra
        {
            ProveedorId = proveedor.Id,
            Estado = "Pendiente",
            Detalles = [new CompraDetalle { ProductoId = producto.Id, Cantidad = 5, PrecioUnitario = 10 }],
        };
        context.Compras.Add(compra);
        await context.SaveChangesAsync();

        var controller = new ComprasController(context);
        var resultado = await controller.ConfirmarCompra(compra.Id, new ConfirmarCompraDto { AlmacenId = 999 }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado);
        var compraSinCambios = await context.Compras.FindAsync(compra.Id);
        Assert.Equal("Pendiente", compraSinCambios!.Estado);
    }

    [Fact]
    public async Task ConfirmarCompra_Rechaza_SiYaNoEstaPendiente()
    {
        await using var context = TestSupport.CreateContext();
        var proveedor = TestSupport.CrearProveedor(context);
        var producto = TestSupport.CrearProducto(context, "SKU-COMPRA-5");
        await context.SaveChangesAsync();

        var compra = new Compra
        {
            ProveedorId = proveedor.Id,
            Estado = "Completada",
            Detalles = [new CompraDetalle { ProductoId = producto.Id, Cantidad = 5, PrecioUnitario = 10 }],
        };
        context.Compras.Add(compra);
        await context.SaveChangesAsync();

        var controller = new ComprasController(context);
        var resultado = await controller.ConfirmarCompra(compra.Id, new ConfirmarCompraDto { AlmacenId = 1 }, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(resultado);
    }

    [Fact]
    public async Task CancelarCompra_MarcaCancelada_SiEstabaPendiente()
    {
        await using var context = TestSupport.CreateContext();
        var proveedor = TestSupport.CrearProveedor(context);
        await context.SaveChangesAsync();

        var compra = new Compra { ProveedorId = proveedor.Id, Estado = "Pendiente" };
        context.Compras.Add(compra);
        await context.SaveChangesAsync();

        var controller = new ComprasController(context);
        var resultado = await controller.CancelarCompra(compra.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        var compraActualizada = await context.Compras.FindAsync(compra.Id);
        Assert.Equal("Cancelada", compraActualizada!.Estado);
    }

    [Fact]
    public async Task CancelarCompra_Rechaza_SiNoEstaPendiente()
    {
        await using var context = TestSupport.CreateContext();
        var proveedor = TestSupport.CrearProveedor(context);
        await context.SaveChangesAsync();

        var compra = new Compra { ProveedorId = proveedor.Id, Estado = "Completada" };
        context.Compras.Add(compra);
        await context.SaveChangesAsync();

        var controller = new ComprasController(context);
        var resultado = await controller.CancelarCompra(compra.Id, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(resultado);
    }
}
