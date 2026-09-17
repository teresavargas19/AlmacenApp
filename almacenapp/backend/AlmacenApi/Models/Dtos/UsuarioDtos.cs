using System.ComponentModel.DataAnnotations;

namespace AlmacenApi.Models.Dtos;

/// <summary>Forma de salida para usuarios: nunca incluye PasswordHash.</summary>
public class UsuarioDto
{
    public int Id { get; set; }
    public int RolId { get; set; }
    public string RolNombre { get; set; } = string.Empty;

    /// <summary>
    /// Permisos del rol del usuario: "*" para acceso total, o una lista separada
    /// por comas de claves de módulo (ej. "productos,compras"). El frontend usa
    /// esto para decidir qué secciones mostrarle a este usuario.
    /// </summary>
    public string? Permisos { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class UsuarioCreateDto
{
    [Required]
    public int RolId { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// NOTA: por ahora se guarda tal cual en PasswordHash. Antes de producción,
    /// hashear con algo como BCrypt o ASP.NET Core Identity en vez de enviar
    /// la contraseña en texto plano.
    /// </summary>
    [Required, StringLength(255)]
    public string Password { get; set; } = string.Empty;
}

public class UsuarioUpdateDto
{
    [Required]
    public int RolId { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    /// <summary>Si viene vacío o nulo, no se cambia la contraseña actual.</summary>
    [StringLength(255)]
    public string? Password { get; set; }
}

/// <summary>Usado por POST /api/usuarios/cambiar-password: cualquier usuario logueado
/// cambia su propia contraseña, comprobando primero la actual.</summary>
public class CambiarPasswordDto
{
    [Required]
    public string PasswordActual { get; set; } = string.Empty;

    [Required, MinLength(6), StringLength(255)]
    public string PasswordNueva { get; set; } = string.Empty;
}
