using AlmacenApi.Controllers;
using AlmacenApi.Models;
using AlmacenApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlmacenApi.Tests;

public class MovimientosInventarioControllerTests
{
    [Fact]
    public async Task RegistrarMovimiento_Entrada_AumentaExistenciaYStock()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-MOV-1", stock: 0);
        await context.SaveChangesAsync();

        var controller = new MovimientosInventarioController(context);
        var resultado = await controller.RegistrarMovimiento(new MovimientoCreateDto
        {
            ProductoId = producto.Id,
            AlmacenId = 1,
            TipoMovimientoId = 1, // Entrada
            Cantidad = 8,
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(resultado.Result);

        var productoActualizado = await context.Productos.FindAsync(producto.Id);
        Assert.Equal(8, productoActualizado!.Stock);
    }

    [Fact]
    public async Task RegistrarMovimiento_Salida_DisminuyeExistencia_SiHaySuficiente()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-MOV-2", stock: 10);
        await context.SaveChangesAsync();
        context.Existencias.Add(new Existencia { ProductoId = producto.Id, AlmacenId = 1, Cantidad = 10 });
        await context.SaveChangesAsync();

        var controller = new MovimientosInventarioController(context);
        var resultado = await controller.RegistrarMovimiento(new MovimientoCreateDto
        {
            ProductoId = producto.Id,
            AlmacenId = 1,
            TipoMovimientoId = 2, // Salida
            Cantidad = 3,
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(resultado.Result);
        var existencia = await context.Existencias.SingleAsync();
        Assert.Equal(7, existencia.Cantidad);
    }

    [Fact]
    public async Task RegistrarMovimiento_Salida_Rechaza_SiNoHaySuficiente()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-MOV-3", stock: 2);
        await context.SaveChangesAsync();
        context.Existencias.Add(new Existencia { ProductoId = producto.Id, AlmacenId = 1, Cantidad = 2 });
        await context.SaveChangesAsync();

        var controller = new MovimientosInventarioController(context);
        var resultado = await controller.RegistrarMovimiento(new MovimientoCreateDto
        {
            ProductoId = producto.Id,
            AlmacenId = 1,
            TipoMovimientoId = 2, // Salida
            Cantidad = 5,
        }, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(resultado.Result);
        var existencia = await context.Existencias.SingleAsync();
        Assert.Equal(2, existencia.Cantidad); // sin cambios
    }

    [Fact]
    public async Task RegistrarMovimiento_Ajuste_AplicaDeltaNegativo_SiNoQuedaEnNegativo()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-MOV-4", stock: 10);
        await context.SaveChangesAsync();
        context.Existencias.Add(new Existencia { ProductoId = producto.Id, AlmacenId = 1, Cantidad = 10 });
        await context.SaveChangesAsync();

        var controller = new MovimientosInventarioController(context);
        var resultado = await controller.RegistrarMovimiento(new MovimientoCreateDto
        {
            ProductoId = producto.Id,
            AlmacenId = 1,
            TipoMovimientoId = 3, // Ajuste
            Cantidad = -4,
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(resultado.Result);
        var existencia = await context.Existencias.SingleAsync();
        Assert.Equal(6, existencia.Cantidad);
    }

    [Fact]
    public async Task RegistrarMovimiento_Ajuste_Rechaza_SiDejaLaExistenciaEnNegativo()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-MOV-5", stock: 3);
        await context.SaveChangesAsync();
        context.Existencias.Add(new Existencia { ProductoId = producto.Id, AlmacenId = 1, Cantidad = 3 });
        await context.SaveChangesAsync();

        var controller = new MovimientosInventarioController(context);
        var resultado = await controller.RegistrarMovimiento(new MovimientoCreateDto
        {
            ProductoId = producto.Id,
            AlmacenId = 1,
            TipoMovimientoId = 3, // Ajuste
            Cantidad = -10,
        }, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(resultado.Result);
    }

    [Fact]
    public async Task RegistrarMovimiento_Rechaza_TipoTransferenciaDirectamente()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-MOV-6");
        await context.SaveChangesAsync();

        var controller = new MovimientosInventarioController(context);
        var resultado = await controller.RegistrarMovimiento(new MovimientoCreateDto
        {
            ProductoId = producto.Id,
            AlmacenId = 1,
            TipoMovimientoId = 4, // Transferencia
            Cantidad = 1,
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado.Result);
    }

    [Fact]
    public async Task RegistrarTransferencia_MueveExistenciaEntreAlmacenes_SinCambiarElTotal()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-MOV-7", stock: 10);
        var almacen2 = TestSupport.CrearAlmacen(context, "Sucursal 2");
        await context.SaveChangesAsync();
        context.Existencias.Add(new Existencia { ProductoId = producto.Id, AlmacenId = 1, Cantidad = 10 });
        await context.SaveChangesAsync();

        var controller = new MovimientosInventarioController(context);
        var resultado = await controller.RegistrarTransferencia(new TransferenciaCreateDto
        {
            ProductoId = producto.Id,
            AlmacenOrigenId = 1,
            AlmacenDestinoId = almacen2.Id,
            Cantidad = 4,
        }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(resultado);

        var origen = await context.Existencias.SingleAsync(e => e.AlmacenId == 1);
        var destino = await context.Existencias.SingleAsync(e => e.AlmacenId == almacen2.Id);
        Assert.Equal(6, origen.Cantidad);
        Assert.Equal(4, destino.Cantidad);

        var productoActualizado = await context.Productos.FindAsync(producto.Id);
        Assert.Equal(10, productoActualizado!.Stock); // el total no cambia con una transferencia

        Assert.Equal(2, await context.MovimientosInventario.CountAsync());
    }

    [Fact]
    public async Task RegistrarTransferencia_Rechaza_SiOrigenYDestinoSonIguales()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-MOV-8");
        await context.SaveChangesAsync();

        var controller = new MovimientosInventarioController(context);
        var resultado = await controller.RegistrarTransferencia(new TransferenciaCreateDto
        {
            ProductoId = producto.Id,
            AlmacenOrigenId = 1,
            AlmacenDestinoId = 1,
            Cantidad = 1,
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task RegistrarTransferencia_Rechaza_SiNoHayExistenciaSuficienteEnOrigen()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-MOV-9", stock: 2);
        var almacen2 = TestSupport.CrearAlmacen(context, "Sucursal 3");
        await context.SaveChangesAsync();
        context.Existencias.Add(new Existencia { ProductoId = producto.Id, AlmacenId = 1, Cantidad = 2 });
        await context.SaveChangesAsync();

        var controller = new MovimientosInventarioController(context);
        var resultado = await controller.RegistrarTransferencia(new TransferenciaCreateDto
        {
            ProductoId = producto.Id,
            AlmacenOrigenId = 1,
            AlmacenDestinoId = almacen2.Id,
            Cantidad = 5,
        }, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(resultado);
    }
}
