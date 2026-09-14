using System.ComponentModel.DataAnnotations;

namespace AlmacenApi.Models;

public class Producto
{
    public int Id { get; set; }

    public int CategoriaId { get; set; }
    public int UnidadMedidaId { get; set; }

    [Required, StringLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Descripcion { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [Range(0, int.MaxValue)]
    public int StockMinimo { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Precio { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public Categoria Categoria { get; set; } = null!;
    public UnidadMedida UnidadMedida { get; set; } = null!;
    public ICollection<Existencia> Existencias { get; set; } = new List<Existencia>();
    public ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
    public ICollection<CompraDetalle> ComprasDetalle { get; set; } = new List<CompraDetalle>();
    public ICollection<SalidaDetalle> SalidasDetalle { get; set; } = new List<SalidaDetalle>();
}