using AlmacenApi.Data;
using AlmacenApi.Models;
using AlmacenApi.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlmacenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController(AlmacenDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetUsuarios(CancellationToken cancellationToken)
    {
        return Ok(await context.Usuarios
            .AsNoTracking()
            .Where(usuario => usuario.Activo)
            .OrderBy(usuario => usuario.Nombre)
            .Select(usuario => new UsuarioDto
            {
                Id = usuario.Id,
                RolId = usuario.RolId,
                RolNombre = usuario.Rol.Nombre,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Activo = usuario.Activo
            })
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> GetUsuario(int id, CancellationToken cancellationToken)
    {
        var usuario = await context.Usuarios.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UsuarioDto
            {
                Id = u.Id,
                RolId = u.RolId,
                RolNombre = u.Rol.Nombre,
                Nombre = u.Nombre,
                Email = u.Email,
                Activo = u.Activo
            })
            .FirstOrDefaultAsync(cancellationToken);

        return usuario is null ? NotFound() : Ok(usuario);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<UsuarioDto>> CrearUsuario(UsuarioCreateDto dto, CancellationToken cancellationToken)
    {
        var rolExiste = await context.Roles.AnyAsync(rol => rol.Id == dto.RolId, cancellationToken);
        if (!rolExiste)
        {
            return BadRequest(new { message = "El rol indicado no existe." });
        }

        var emailUsado = await context.Usuarios.AnyAsync(u => u.Email == dto.Email, cancellationToken);
        if (emailUsado)
        {
            return Conflict(new { message = "Ya existe un usuario con ese correo." });
        }

        var usuario = new Usuario
        {
            RolId = dto.RolId,
            Nombre = dto.Nombre,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Activo = true
        };

        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync(cancellationToken);

        var rol = await context.Roles.AsNoTracking().FirstAsync(r => r.Id == usuario.RolId, cancellationToken);
        var resultado = new UsuarioDto
        {
            Id = usuario.Id,
            RolId = usuario.RolId,
            RolNombre = rol.Nombre,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Activo = usuario.Activo
        };

        return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, resultado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ActualizarUsuario(int id, UsuarioUpdateDto dto, CancellationToken cancellationToken)
    {
        var usuario = await context.Usuarios.FindAsync([id], cancellationToken);
        if (usuario is null)
        {
            return NotFound();
        }

        var rolExiste = await context.Roles.AnyAsync(rol => rol.Id == dto.RolId, cancellationToken);
        if (!rolExiste)
        {
            return BadRequest(new { message = "El rol indicado no existe." });
        }

        var emailUsado = await context.Usuarios.AnyAsync(u => u.Email == dto.Email && u.Id != id, cancellationToken);
        if (emailUsado)
        {
            return Conflict(new { message = "Ya existe otro usuario con ese correo." });
        }

        usuario.RolId = dto.RolId;
        usuario.Nombre = dto.Nombre;
        usuario.Email = dto.Email;
        usuario.Activo = dto.Activo;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EliminarUsuario(int id, CancellationToken cancellationToken)
    {
        var usuario = await context.Usuarios.FindAsync([id], cancellationToken);
        if (usuario is null)
        {
            return NotFound();
        }

        usuario.Activo = false;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
