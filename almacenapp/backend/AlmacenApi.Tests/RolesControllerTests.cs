using AlmacenApi.Controllers;
using AlmacenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlmacenApi.Tests;

public class RolesControllerTests
{
    [Fact]
    public async Task CrearRol_GuardaNombreYPermisos()
    {
        await using var context = TestSupport.CreateContext();
        var controller = new RolesController(context);

        var resultado = await controller.CrearRol(
            new Rol { Nombre = "Vendedor", Permisos = "compras,salidas,clientes" },
            CancellationToken.None);

        var creado = Assert.IsType<CreatedAtActionResult>(resultado.Result);
        var rol = Assert.IsType<Rol>(creado.Value);
        Assert.NotEqual(0, rol.Id);

        var guardado = await context.Roles.FindAsync(rol.Id);
        Assert.Equal("Vendedor", guardado!.Nombre);
        Assert.Equal("compras,salidas,clientes", guardado.Permisos);
    }

    [Fact]
    public async Task ActualizarRol_CambiaNombreYPermisos()
    {
        await using var context = TestSupport.CreateContext();
        context.Roles.Add(new Rol { Nombre = "Vendedor", Permisos = "compras" });
        await context.SaveChangesAsync();
        var rol = await context.Roles.SingleAsync(r => r.Nombre == "Vendedor");

        var controller = new RolesController(context);
        var resultado = await controller.ActualizarRol(
            rol.Id,
            new Rol { Id = rol.Id, Nombre = "Vendedor Senior", Permisos = "compras,salidas" },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);

        var guardado = await context.Roles.FindAsync(rol.Id);
        Assert.Equal("Vendedor Senior", guardado!.Nombre);
        Assert.Equal("compras,salidas", guardado.Permisos);
    }

    [Fact]
    public async Task EliminarRol_Rechaza_SiTieneUsuariosAsignados()
    {
        // El rol "Administrador" (Id=1) ya tiene el usuario admin sembrado
        // por AlmacenDbContext.OnModelCreating.
        await using var context = TestSupport.CreateContext();
        var controller = new RolesController(context);

        var resultado = await controller.EliminarRol(1, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(resultado);
        Assert.NotNull(await context.Roles.FindAsync(1));
    }

    [Fact]
    public async Task EliminarRol_Permite_SiNoTieneUsuariosAsignados()
    {
        await using var context = TestSupport.CreateContext();
        context.Roles.Add(new Rol { Nombre = "Sin uso", Permisos = "productos" });
        await context.SaveChangesAsync();
        var rol = await context.Roles.SingleAsync(r => r.Nombre == "Sin uso");

        var controller = new RolesController(context);
        var resultado = await controller.EliminarRol(rol.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        Assert.Null(await context.Roles.FindAsync(rol.Id));
    }
}
