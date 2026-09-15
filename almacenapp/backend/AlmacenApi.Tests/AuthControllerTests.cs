using AlmacenApi.Controllers;
using AlmacenApi.Models;
using AlmacenApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlmacenApi.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_DevuelveToken_ConCredencialesValidas()
    {
        await using var context = TestSupport.CreateContext();
        var controller = new AuthController(context, TestSupport.CreateJwtConfiguration());

        var resultado = await controller.Login(
            new LoginDto { Email = "admin@almacenapp.com", Password = "Admin123!" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var respuesta = Assert.IsType<LoginResponseDto>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(respuesta.Token));
        Assert.Equal("admin@almacenapp.com", respuesta.Usuario.Email);
        Assert.Equal("Administrador", respuesta.Usuario.RolNombre);
        Assert.Equal("*", respuesta.Usuario.Permisos);
    }

    [Fact]
    public async Task Login_RechazaContrasenaIncorrecta()
    {
        await using var context = TestSupport.CreateContext();
        var controller = new AuthController(context, TestSupport.CreateJwtConfiguration());

        var resultado = await controller.Login(
            new LoginDto { Email = "admin@almacenapp.com", Password = "clave-incorrecta" },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(resultado.Result);
    }

    [Fact]
    public async Task Login_RechazaCorreoInexistente()
    {
        await using var context = TestSupport.CreateContext();
        var controller = new AuthController(context, TestSupport.CreateJwtConfiguration());

        var resultado = await controller.Login(
            new LoginDto { Email = "no-existe@almacenapp.com", Password = "cualquiera" },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(resultado.Result);
    }

    [Fact]
    public async Task Login_IncluyePermisosDeUnRolPersonalizado()
    {
        await using var context = TestSupport.CreateContext();
        context.Roles.Add(new Rol { Nombre = "Vendedor", Permisos = "compras,salidas,clientes" });
        await context.SaveChangesAsync();
        var rolVendedor = await context.Roles.SingleAsync(r => r.Nombre == "Vendedor");

        context.Usuarios.Add(new Usuario
        {
            RolId = rolVendedor.Id,
            Nombre = "Vendedora",
            Email = "vendedora@almacenapp.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Clave123!"),
            Activo = true,
        });
        await context.SaveChangesAsync();

        var controller = new AuthController(context, TestSupport.CreateJwtConfiguration());
        var resultado = await controller.Login(
            new LoginDto { Email = "vendedora@almacenapp.com", Password = "Clave123!" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var respuesta = Assert.IsType<LoginResponseDto>(ok.Value);
        Assert.Equal("compras,salidas,clientes", respuesta.Usuario.Permisos);
    }

    [Fact]
    public async Task Login_RechazaUsuarioInactivo()
    {
        await using var context = TestSupport.CreateContext();
        context.Usuarios.Add(new Usuario
        {
            RolId = 1,
            Nombre = "Inactivo",
            Email = "inactivo@almacenapp.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Clave123!"),
            Activo = false,
        });
        await context.SaveChangesAsync();

        var controller = new AuthController(context, TestSupport.CreateJwtConfiguration());
        var resultado = await controller.Login(
            new LoginDto { Email = "inactivo@almacenapp.com", Password = "Clave123!" },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(resultado.Result);
    }
}
