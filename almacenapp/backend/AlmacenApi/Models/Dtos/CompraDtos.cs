using System.ComponentModel.DataAnnotations;

namespace AlmacenApi.Models.Dtos;

public class CompraCreateDto
{
    [Required]
    public int ProveedorId { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }

    [Required, MinLength(1)]
    public List<CompraDetalleCreateDto> Detalles { get; set; } = [];
}

public class CompraDetalleCreateDto
{
    [Required]
    public int ProductoId { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Cantidad { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal PrecioUnitario { get; set; }
}

/// <summary>
/// El almacén que recibe la mercancía se decide al confirmar, no al crear la
/// compra (el modelo Compra no guarda un almacén fijo por encabezado).
/// </summary>
public class ConfirmarCompraDto
{
    [Required]
    public int AlmacenId { get; set; }

    public int? UsuarioId { get; set; }
}
