using System.ComponentModel.DataAnnotations;

namespace AlmacenApi.Models.Dtos;

public class SalidaCreateDto
{
    public int? ClienteId { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }

    [Required, MinLength(1)]
    public List<SalidaDetalleCreateDto> Detalles { get; set; } = [];
}

public class SalidaDetalleCreateDto
{
    [Required]
    public int ProductoId { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Cantidad { get; set; }
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
