using System.ComponentModel.DataAnnotations;

namespace AlmacenApi.Models.Dtos;

public class SalidaCreateDto
{
    public int? ClienteId { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }

    /// <summary>"Efectivo", "Transferencia" o "Credito" — validado en el controller (ver metodosPagoValidos).</summary>
    [Required, StringLength(20)]
    public string MetodoPago { get; set; } = "Efectivo";

    [Range(typeof(decimal), "0", "100")]
    public decimal DescuentoGeneralPorcentaje { get; set; }

    [Required, MinLength(1)]
    public List<SalidaDetalleCreateDto> Detalles { get; set; } = [];
}

public class SalidaDetalleCreateDto
{
    [Required]
    public int ProductoId { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Cantidad { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal PrecioUnitario { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal DescuentoPorcentaje { get; set; }
}

/// <summary>Registrar un abono (pago parcial) contra una venta a crédito ya completada.</summary>
public class AbonoCreateDto
{
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Monto { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }
}

/// <summary>
/// El almacén del que sale la mercancía se decide al confirmar, no al crear
/// la salida (el modelo Salida no guarda un almacén fijo por encabezado).
/// </summary>
public class ConfirmarSalidaDto
{
    [Required]
    public int AlmacenId { get; set; }

    public int? UsuarioId { get; set; }
}
