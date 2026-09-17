using AlmacenApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Data;

public class AlmacenDbContext(DbContextOptions<AlmacenDbContext> options) : DbContext(options)
{
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
    public DbSet<Almacen> Almacenes => Set<Almacen>();
    public DbSet<Ubicacion> Ubicaciones => Set<Ubicacion>();
    public DbSet<Existencia> Existencias => Set<Existencia>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<TipoMovimiento> TiposMovimiento => Set<TipoMovimiento>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> CompraDetalles => Set<CompraDetalle>();
    public DbSet<Salida> Salidas => Set<Salida>();
    public DbSet<SalidaDetalle> SalidaDetalles => Set<SalidaDetalle>();
    public DbSet<AbonoSalida> AbonosSalida => Set<AbonoSalida>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>().ToTable("productos");
        modelBuilder.Entity<Categoria>().ToTable("categorias");
        modelBuilder.Entity<UnidadMedida>().ToTable("unidades_medida");
        modelBuilder.Entity<Almacen>().ToTable("almacenes");
        modelBuilder.Entity<Ubicacion>().ToTable("ubicaciones");
        modelBuilder.Entity<Existencia>().ToTable("existencias");
        modelBuilder.Entity<MovimientoInventario>().ToTable("movimientos_inventario");
        modelBuilder.Entity<TipoMovimiento>().ToTable("tipos_movimiento");
        modelBuilder.Entity<Proveedor>().ToTable("proveedores");
        modelBuilder.Entity<Cliente>().ToTable("clientes");
        modelBuilder.Entity<Compra>().ToTable("compras");
        modelBuilder.Entity<CompraDetalle>().ToTable("compra_detalles");
        modelBuilder.Entity<Salida>().ToTable("salidas");
        modelBuilder.Entity<SalidaDetalle>().ToTable("salida_detalles");
        modelBuilder.Entity<AbonoSalida>().ToTable("abonos_salida");
        modelBuilder.Entity<Usuario>().ToTable("usuarios");
        modelBuilder.Entity<Rol>().ToTable("roles");

        modelBuilder.Entity<Producto>()
            .HasIndex(producto => producto.Sku)
            .IsUnique();

        modelBuilder.Entity<Producto>()
            .HasOne(producto => producto.Categoria)
            .WithMany(categoria => categoria.Productos)
            .HasForeignKey(producto => producto.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Producto>()
            .HasOne(producto => producto.UnidadMedida)
            .WithMany(unidad => unidad.Productos)
            .HasForeignKey(producto => producto.UnidadMedidaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Existencia>()
            .HasIndex(existencia => new { existencia.ProductoId, existencia.AlmacenId, existencia.UbicacionId })
            .IsUnique();

        modelBuilder.Entity<Existencia>().Property(existencia => existencia.Cantidad).HasPrecision(18, 4);
        modelBuilder.Entity<Existencia>().Property(existencia => existencia.CantidadMinima).HasPrecision(18, 4);
        modelBuilder.Entity<MovimientoInventario>().Property(movimiento => movimiento.Cantidad).HasPrecision(18, 4);
        modelBuilder.Entity<CompraDetalle>().Property(detalle => detalle.Cantidad).HasPrecision(18, 4);
        modelBuilder.Entity<CompraDetalle>().Property(detalle => detalle.PrecioUnitario).HasPrecision(18, 2);
        modelBuilder.Entity<SalidaDetalle>().Property(detalle => detalle.Cantidad).HasPrecision(18, 4);
        modelBuilder.Entity<SalidaDetalle>().Property(detalle => detalle.PrecioUnitario).HasPrecision(18, 2);
        modelBuilder.Entity<SalidaDetalle>().Property(detalle => detalle.DescuentoPorcentaje).HasPrecision(5, 2);
        modelBuilder.Entity<Salida>().Property(salida => salida.DescuentoGeneralPorcentaje).HasPrecision(5, 2);
        modelBuilder.Entity<Salida>().Property(salida => salida.Subtotal).HasPrecision(18, 2);
        modelBuilder.Entity<Salida>().Property(salida => salida.Itbis).HasPrecision(18, 2);
        modelBuilder.Entity<Salida>().Property(salida => salida.Total).HasPrecision(18, 2);
        modelBuilder.Entity<Salida>().Property(salida => salida.SaldoPendiente).HasPrecision(18, 2);
        modelBuilder.Entity<AbonoSalida>().Property(abono => abono.Monto).HasPrecision(18, 2);

        modelBuilder.Entity<Ubicacion>()
            .HasOne(ubicacion => ubicacion.Almacen)
            .WithMany(almacen => almacen.Ubicaciones)
            .HasForeignKey(ubicacion => ubicacion.AlmacenId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Existencia>()
            .HasOne(existencia => existencia.Almacen)
            .WithMany(almacen => almacen.Existencias)
            .HasForeignKey(existencia => existencia.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Existencia>()
            .HasOne(existencia => existencia.Ubicacion)
            .WithMany(ubicacion => ubicacion.Existencias)
            .HasForeignKey(existencia => existencia.UbicacionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MovimientoInventario>()
            .HasOne(movimiento => movimiento.Usuario)
            .WithMany(usuario => usuario.MovimientosInventario)
            .HasForeignKey(movimiento => movimiento.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Compra>().HasOne(compra => compra.Proveedor).WithMany(proveedor => proveedor.Compras)
            .HasForeignKey(compra => compra.ProveedorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CompraDetalle>().HasOne(detalle => detalle.Compra).WithMany(compra => compra.Detalles)
            .HasForeignKey(detalle => detalle.CompraId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CompraDetalle>().HasOne(detalle => detalle.Producto).WithMany(producto => producto.ComprasDetalle)
            .HasForeignKey(detalle => detalle.ProductoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Salida>().HasOne(salida => salida.Cliente).WithMany(cliente => cliente.Salidas)
            .HasForeignKey(salida => salida.ClienteId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<SalidaDetalle>().HasOne(detalle => detalle.Salida).WithMany(salida => salida.Detalles)
            .HasForeignKey(detalle => detalle.SalidaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SalidaDetalle>().HasOne(detalle => detalle.Producto).WithMany(producto => producto.SalidasDetalle)
            .HasForeignKey(detalle => detalle.ProductoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AbonoSalida>().HasOne(abono => abono.Salida).WithMany(salida => salida.Abonos)
            .HasForeignKey(abono => abono.SalidaId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Usuario>().HasIndex(usuario => usuario.Email).IsUnique();
        modelBuilder.Entity<Usuario>().HasOne(usuario => usuario.Rol).WithMany(rol => rol.Usuarios)
            .HasForeignKey(usuario => usuario.RolId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Categoria>().HasData(new Categoria
        {
            Id = 1,
            Nombre = "General",
            Activo = true
        });

        modelBuilder.Entity<UnidadMedida>().HasData(new UnidadMedida
        {
            Id = 1,
            Nombre = "Unidad",
            Abreviatura = "und",
            Activo = true
        });

        modelBuilder.Entity<Almacen>().HasData(new Almacen
        {
            Id = 1,
            Nombre = "Almacén principal",
            Activo = true
        });

        modelBuilder.Entity<TipoMovimiento>().HasData(
            new TipoMovimiento { Id = 1, Nombre = "Entrada", Activo = true },
            new TipoMovimiento { Id = 2, Nombre = "Salida", Activo = true },
            new TipoMovimiento { Id = 3, Nombre = "Ajuste", Activo = true },
            new TipoMovimiento { Id = 4, Nombre = "Transferencia", Activo = true });

        modelBuilder.Entity<Rol>().HasData(new Rol
        {
            Id = 1,
            Nombre = "Administrador",
            Permisos = "*"
        });

        // Usuario administrador inicial para poder iniciar sesión desde el
        // primer arranque. Contraseña: Admin123!  — cámbiala apenas puedas
        // entrar (PUT /api/usuarios/{id} con un nuevo password).
        modelBuilder.Entity<Usuario>().HasData(new Usuario
        {
            Id = 1,
            RolId = 1,
            Nombre = "Administrador",
            Email = "admin@almacenapp.com",
            PasswordHash = "$2b$11$5ydz3pP.marxFzFZ3mxFyOdxcicbJiT7YRAirmcxt7RvEpUAW99xe",
            Activo = true
        });

        modelBuilder.Entity<Producto>()
            .Property(producto => producto.Precio)
            .HasPrecision(18, 2);
    }
}
