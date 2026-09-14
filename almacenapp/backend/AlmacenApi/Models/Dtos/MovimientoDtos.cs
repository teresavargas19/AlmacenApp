using System.ComponentModel.DataAnnotations;

namespace AlmacenApi.Models.Dtos;

/// <summary>
/// Registra un movimiento simple (Entrada, Salida o Ajuste) sobre un almacén.
/// Para Entrada/Salida, Cantidad debe ser positiva. Para Ajuste, Cantidad es un
/// delta que se suma directamente a la existencia (puede ser negativo).
/// </summary>
public class MovimientoCreateDto
{
    [Required]
    public int ProductoId { get; set; }

    [Required]
    public int AlmacenId { get; set; }

    public int? UbicacionId { get; set; }

    [Required]
    public int TipoMovimientoId { get; set; }

    public int? UsuarioId { get; set; }

    public decimal Cantidad { get; set; }

    [StringLength(100)]
    public string? Referencia { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }
}

/// <summary>
/// Transferencia de existencias entre dos almacenes: genera dos movimientos
/// (tipo Transferencia) con el mismo monto, uno de salida en el origen y uno
/// de entrada en el destino.
/// </summary>
public class TransferenciaCreateDto
{
    [Required]
    public int ProductoId { get; set; }

    [Required]
    public int AlmacenOrigenId { get; set; }

    public int? UbicacionOrigenId { get; set; }

    [Required]
    public int AlmacenDestinoId { get; set; }

    public int? UbicacionDestinoId { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Cantidad { get; set; }

    public int? UsuarioId { get; set; }

    [StringLength(100)]
    public string? Referencia { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }
}
