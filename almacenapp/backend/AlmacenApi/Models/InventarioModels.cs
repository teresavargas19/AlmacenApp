using System.ComponentModel.DataAnnotations;

namespace AlmacenApi.Models;

public class Categoria
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}

public class UnidadMedida
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string Abreviatura { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}

public class Almacen
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Direccion { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<Ubicacion> Ubicaciones { get; set; } = new List<Ubicacion>();
    public ICollection<Existencia> Existencias { get; set; } = new List<Existencia>();
    public ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
}

public class Ubicacion
{
    public int Id { get; set; }
    public int AlmacenId { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Pasillo { get; set; }

    [StringLength(50)]
    public string? Estante { get; set; }

    [StringLength(50)]
    public string? Gaveta { get; set; }

    public bool Activo { get; set; } = true;

    public Almacen Almacen { get; set; } = null!;
    public ICollection<Existencia> Existencias { get; set; } = new List<Existencia>();
}

public class Existencia
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int AlmacenId { get; set; }
    public int? UbicacionId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CantidadMinima { get; set; }

    public Producto Producto { get; set; } = null!;
    public Almacen Almacen { get; set; } = null!;
    public Ubicacion? Ubicacion { get; set; }
}

public class TipoMovimiento
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
}

public class MovimientoInventario
{
    public long Id { get; set; }
    public int ProductoId { get; set; }
    public int AlmacenId { get; set; }
    public int TipoMovimientoId { get; set; }
    public int? UsuarioId { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Cantidad { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? Referencia { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }

    public Producto Producto { get; set; } = null!;
    public Almacen Almacen { get; set; } = null!;
    public TipoMovimiento TipoMovimiento { get; set; } = null!;
    public Usuario? Usuario { get; set; }
}

public class Proveedor
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Telefono { get; set; }

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [StringLength(250)]
    public string? Direccion { get; set; }

    public bool Activo { get; set; } = true;
    public ICollection<Compra> Compras { get; set; } = new List<Compra>();
}

public class Cliente
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Telefono { get; set; }

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [StringLength(250)]
    public string? Direccion { get; set; }

    public bool Activo { get; set; } = true;
    public ICollection<Salida> Salidas { get; set; } = new List<Salida>();
}

public class Compra
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Required, StringLength(30)]
    public string Estado { get; set; } = "Pendiente";

    [StringLength(500)]
    public string? Observaciones { get; set; }

    public Proveedor Proveedor { get; set; } = null!;
    public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
}

public class CompraDetalle
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public int ProductoId { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Cantidad { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal PrecioUnitario { get; set; }

    public Compra Compra { get; set; } = null!;
    public Producto Producto { get; set; } = null!;
}

public class Salida
{
    public int Id { get; set; }
    public int? ClienteId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Required, StringLength(30)]
    public string Estado { get; set; } = "Pendiente";

    [StringLength(500)]
    public string? Observaciones { get; set; }

    public Cliente? Cliente { get; set; }
    public ICollection<SalidaDetalle> Detalles { get; set; } = new List<SalidaDetalle>();
}

public class SalidaDetalle
{
    public int Id { get; set; }
    public int SalidaId { get; set; }
    public int ProductoId { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Cantidad { get; set; }

    public Salida Salida { get; set; } = null!;
    public Producto Producto { get; set; } = null!;
}

public class Rol
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Permisos { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}

public class Usuario
{
    public int Id { get; set; }
    public int RolId { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public Rol Rol { get; set; } = null!;
    public ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
}