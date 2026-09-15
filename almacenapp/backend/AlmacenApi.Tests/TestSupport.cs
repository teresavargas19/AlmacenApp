using AlmacenApi.Data;
using AlmacenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AlmacenApi.Tests;

/// <summary>
/// Utilidades compartidas por las pruebas: cada prueba recibe su propia base
/// de datos en memoria, aislada de las demás, pero con los mismos datos
/// semilla que define AlmacenDbContext.OnModelCreating (Categoria/UnidadMedida
/// /Almacen Id=1, TiposMovimiento 1-4, Rol "Administrador" Id=1 y el usuario
/// admin@almacenapp.com con la contraseña real "Admin123!").
/// </summary>
public static class TestSupport
{
    public static AlmacenDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AlmacenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AlmacenDbContext(options);

        // El proveedor InMemory no aplica los datos de HasData automáticamente
        // (a diferencia de una migración real contra SQL Server); hay que
        // pedirle explícitamente que cree la base para que se materialicen.
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>Configuración JWT mínima para instanciar AuthController en pruebas.</summary>
    public static IConfiguration CreateJwtConfiguration()
    {
        var datos = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "clave-de-pruebas-0123456789abcdef0123456789abcdef",
            ["Jwt:Issuer"] = "AlmacenApi.Tests",
            ["Jwt:Audience"] = "AlmacenApp.Tests",
            ["Jwt:ExpiresMinutes"] = "480",
        };

        return new ConfigurationBuilder().AddInMemoryCollection(datos).Build();
    }

    public static Producto CrearProducto(
        AlmacenDbContext context,
        string sku,
        string nombre = "Producto de prueba",
        int stock = 0,
        decimal precio = 100)
    {
        var producto = new Producto
        {
            CategoriaId = 1,
            UnidadMedidaId = 1,
            Sku = sku,
            Nombre = nombre,
            Stock = stock,
            StockMinimo = 0,
            Precio = precio,
            Activo = true,
        };
        context.Productos.Add(producto);
        return producto;
    }

    public static Proveedor CrearProveedor(AlmacenDbContext context, string nombre = "Proveedor de prueba")
    {
        var proveedor = new Proveedor { Nombre = nombre, Activo = true };
        context.Proveedores.Add(proveedor);
        return proveedor;
    }

    public static Cliente CrearCliente(AlmacenDbContext context, string nombre = "Cliente de prueba")
    {
        var cliente = new Cliente { Nombre = nombre, Activo = true };
        context.Clientes.Add(cliente);
        return cliente;
    }

    public static Almacen CrearAlmacen(AlmacenDbContext context, string nombre = "Almacén secundario")
    {
        var almacen = new Almacen { Nombre = nombre, Activo = true };
        context.Almacenes.Add(almacen);
        return almacen;
    }
}
