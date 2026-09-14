using AlmacenApi.Data;
using AlmacenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

/// <summary>
/// Catálogo fijo (Entrada, Salida, Ajuste, Transferencia). Solo lectura:
/// los ids 1-4 ya están sembrados y el resto del sistema depende de ellos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TiposMovimientoController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TipoMovimiento>>> GetTipos(CancellationToken cancellationToken)
    {
        return Ok(await context.TiposMovimiento
            .AsNoTracking()
            .Where(tipo => tipo.Activo)
            .OrderBy(tipo => tipo.Id)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TipoMovimiento>> GetTipo(int id, CancellationToken cancellationToken)
    {
        var tipo = await context.TiposMovimiento.AsNoTracking()
            .FirstOrDefaultAsync(tipo => tipo.Id == id, cancellationToken);

        return tipo is null ? NotFound() : Ok(tipo);
    }
}
