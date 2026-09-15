using AlmacenApi.Controllers;
using AlmacenApi.Models;
using AlmacenApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlmacenApi.Tests;

public class SalidasControllerTests
{
    [Fact]
    public async Task CrearSalida_Rechaza_SiClienteNoExiste()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-SALIDA-1");
        await context.SaveChangesAsync();

        var controller = new SalidasController(context);
        var resultado = await controller.CrearSalida(new SalidaCreateDto
        {
            ClienteId = 999,
            Detalles = [new SalidaDetalleCreateDto { ProductoId = producto.Id, Cantidad = 2 }],
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task CrearSalida_CreaSalidaEnEstadoPendiente_SinTocarInventario()
    {
        await using var context = TestSupport.CreateContext();
        var cliente = TestSupport.CrearCliente(context);
        var producto = TestSupport.CrearProducto(context, "SKU-SALIDA-2");
        await context.SaveChangesAsync();

        var controller = new SalidasController(context);
        var resultado = await controller.CrearSalida(new SalidaCreateDto
        {
            ClienteId = cliente.Id,
            Detalles = [new SalidaDetalleCreateDto { ProductoId = producto.Id, Cantidad = 3 }],
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(resultado);

        var salidaGuardada = await context.Salidas.Include(s => s.Detalles).SingleAsync();
        Assert.Equal("Pendiente", salidaGuardada.Estado);
        Assert.Empty(context.MovimientosInventario);
    }

    [Fact]
    public async Task ConfirmarSalida_DisminuyeExistenciaYStock_GeneraMovimientoYMarcaCompletada()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-SALIDA-3", stock: 10);
        await context.SaveChangesAsync();
        context.Existencias.Add(new Existencia { ProductoId = producto.Id, AlmacenId = 1, Cantidad = 10 });
        await context.SaveChangesAsync();

        var salida = new Salida
        {
            Estado = "Pendiente",
            Detalles = [new SalidaDetalle { ProductoId = producto.Id, Cantidad = 4 }],
        };
        context.Salidas.Add(salida);
        await context.SaveChangesAsync();

        var controller = new SalidasController(context);
        var resultado = await controller.ConfirmarSalida(salida.Id, new ConfirmarSalidaDto { AlmacenId = 1 }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(resultado);

        var salidaActualizada = await context.Salidas.FindAsync(salida.Id);
        Assert.Equal("Completada", salidaActualizada!.Estado);

        var existencia = await context.Existencias.SingleAsync();
        Assert.Equal(6, existencia.Cantidad);

        var productoActualizado = await context.Productos.FindAsync(producto.Id);
        Assert.Equal(6, productoActualizado!.Stock);

        var movimiento = await context.MovimientosInventario.SingleAsync();
        Assert.Equal(2, movimiento.TipoMovimientoId); // Salida
        Assert.Equal(4, movimiento.Cantidad);
    }

    [Fact]
    public async Task ConfirmarSalida_Rechaza_SiNoHayExistenciaSuficiente_YNoAplicaCambios()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-SALIDA-4", stock: 5);
        await context.SaveChangesAsync();
        context.Existencias.Add(new Existencia { ProductoId = producto.Id, AlmacenId = 1, Cantidad = 5 });
        await context.SaveChangesAsync();

        var salida = new Salida
        {
            Estado = "Pendiente",
            Detalles = [new SalidaDetalle { ProductoId = producto.Id, Cantidad = 10 }],
        };
        context.Salidas.Add(salida);
        await context.SaveChangesAsync();

        var controller = new SalidasController(context);
        var resultado = await controller.ConfirmarSalida(salida.Id, new ConfirmarSalidaDto { AlmacenId = 1 }, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(resultado);

        var existencia = await context.Existencias.SingleAsync();
        Assert.Equal(5, existencia.Cantidad); // sin cambios

        var productoSinCambios = await context.Productos.FindAsync(producto.Id);
        Assert.Equal(5, productoSinCambios!.Stock); // sin cambios

        var salidaSinCambios = await context.Salidas.FindAsync(salida.Id);
        Assert.Equal("Pendiente", salidaSinCambios!.Estado); // sigue pendiente

        Assert.Empty(context.MovimientosInventario);
    }

    [Fact]
    public async Task ConfirmarSalida_Rechaza_SiYaNoEstaPendiente()
    {
        await using var context = TestSupport.CreateContext();
        var producto = TestSupport.CrearProducto(context, "SKU-SALIDA-5");
        await context.SaveChangesAsync();

        var salida = new Salida
        {
            Estado = "Cancelada",
            Detalles = [new SalidaDetalle { ProductoId = producto.Id, Cantidad = 1 }],
        };
        context.Salidas.Add(salida);
        await context.SaveChangesAsync();

        var controller = new SalidasController(context);
        var resultado = await controller.ConfirmarSalida(salida.Id, new ConfirmarSalidaDto { AlmacenId = 1 }, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(resultado);
    }

    [Fact]
    public async Task CancelarSalida_MarcaCancelada_SiEstabaPendiente()
    {
        await using var context = TestSupport.CreateContext();
        var salida = new Salida { Estado = "Pendiente" };
        context.Salidas.Add(salida);
        await context.SaveChangesAsync();

        var controller = new SalidasController(context);
        var resultado = await controller.CancelarSalida(salida.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        var salidaActualizada = await context.Salidas.FindAsync(salida.Id);
        Assert.Equal("Cancelada", salidaActualizada!.Estado);
    }

    [Fact]
    public async Task CancelarSalida_Rechaza_SiNoEstaPendiente()
    {
        await using var context = TestSupport.CreateContext();
        var salida = new Salida { Estado = "Completada" };
        context.Salidas.Add(salida);
        await context.SaveChangesAsync();

        var controller = new SalidasController(context);
        var resultado = await controller.CancelarSalida(salida.Id, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(resultado);
    }
}
