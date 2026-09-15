using AlmacenApi.Controllers;
using AlmacenApi.Models;
using AlmacenApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlmacenApi.Tests;

public class UsuariosControllerTests
{
    [Fact]
    public async Task GetUsuarios_IncluyeLosPermisosDelRol()
    {
        await using var context = TestSupport.CreateContext();
        var controller = new UsuariosController(context);

        var resultado = await controller.GetUsuarios(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var usuarios = Assert.IsAssignableFrom<IEnumerable<UsuarioDto>>(ok.Value);
        var admin = Assert.Single(usuarios, u => u.Email == "admin@almacenapp.com");
        Assert.Equal("*", admin.Permisos);
    }

    [Fact]
    public async Task CrearUsuario_DevuelveLosPermisosDelRolAsignado()
    {
        await using var context = TestSupport.CreateContext();
        context.Roles.Add(new Rol { Nombre = "Vendedor", Permisos = "compras,salidas" });
        await context.SaveChangesAsync();
        var rolVendedor = await context.Roles.SingleAsync(r => r.Nombre == "Vendedor");

        var controller = new UsuariosController(context);
        var resultado = await controller.CrearUsuario(new UsuarioCreateDto
        {
            RolId = rolVendedor.Id,
            Nombre = "Nueva Vendedora",
            Email = "vendedora2@almacenapp.com",
            Password = "Clave123!",
        }, CancellationToken.None);

        var creado = Assert.IsType<CreatedAtActionResult>(resultado.Result);
        var usuario = Assert.IsType<UsuarioDto>(creado.Value);
        Assert.Equal("compras,salidas", usuario.Permisos);
    }

    [Fact]
    public async Task CrearUsuario_GuardaLaContrasenaHasheada_NuncaEnTextoPlano()
    {
        await using var context = TestSupport.CreateContext();
        var controller = new UsuariosController(context);

        var resultado = await controller.CrearUsuario(new UsuarioCreateDto
        {
            RolId = 1,
            Nombre = "Nuevo",
            Email = "nuevo@almacenapp.com",
            Password = "Clave123!",
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(resultado.Result);

        var usuario = await context.Usuarios.SingleAsync(u => u.Email == "nuevo@almacenapp.com");
        Assert.NotEqual("Clave123!", usuario.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Clave123!", usuario.PasswordHash));
    }

    [Fact]
    public async Task CrearUsuario_Rechaza_SiElCorreoYaExiste()
    {
        await using var context = TestSupport.CreateContext();
        var controller = new UsuariosController(context);

        // admin@almacenapp.com ya viene sembrado por AlmacenDbContext.
        var resultado = await controller.CrearUsuario(new UsuarioCreateDto
        {
            RolId = 1,
            Nombre = "Duplicado",
            Email = "admin@almacenapp.com",
            Password = "Clave123!",
        }, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(resultado.Result);
    }

    [Fact]
    public async Task CrearUsuario_Rechaza_SiElRolNoExiste()
    {
        await using var context = TestSupport.CreateContext();
        var controller = new UsuariosController(context);

        var resultado = await controller.CrearUsuario(new UsuarioCreateDto
        {
            RolId = 999,
            Nombre = "Sin rol",
            Email = "sinrol@almacenapp.com",
            Password = "Clave123!",
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado.Result);
    }

    [Fact]
    public async Task ActualizarUsuario_CambiaLaContrasena_SiSeEnviaUnaNueva()
    {
        await using var context = TestSupport.CreateContext();
        var usuarioExistente = await context.Usuarios.SingleAsync(u => u.Email == "admin@almacenapp.com");
        var hashOriginal = usuarioExistente.PasswordHash;

        var controller = new UsuariosController(context);
        var resultado = await controller.ActualizarUsuario(usuarioExistente.Id, new UsuarioUpdateDto
        {
            RolId = usuarioExistente.RolId,
            Nombre = usuarioExistente.Nombre,
            Email = usuarioExistente.Email,
            Activo = true,
            Password = "NuevaClave123!",
        }, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);

        var guardado = await context.Usuarios.FindAsync(usuarioExistente.Id);
        Assert.NotEqual(hashOriginal, guardado!.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("NuevaClave123!", guardado.PasswordHash));
    }

    [Fact]
    public async Task ActualizarUsuario_MantieneLaContrasena_SiNoSeEnviaUnaNueva()
    {
        await using var context = TestSupport.CreateContext();
        var usuarioExistente = await context.Usuarios.SingleAsync(u => u.Email == "admin@almacenapp.com");
        var hashOriginal = usuarioExistente.PasswordHash;

        var controller = new UsuariosController(context);
        var resultado = await controller.ActualizarUsuario(usuarioExistente.Id, new UsuarioUpdateDto
        {
            RolId = usuarioExistente.RolId,
            Nombre = "Administrador General",
            Email = usuarioExistente.Email,
            Activo = true,
            Password = null,
        }, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);

        var guardado = await context.Usuarios.FindAsync(usuarioExistente.Id);
        Assert.Equal(hashOriginal, guardado!.PasswordHash);
        Assert.Equal("Administrador General", guardado.Nombre);
    }
}
