using Microsoft.AspNetCore.Authorization;

namespace AlmacenApi.Authorization;

/// <summary>
/// Exige que el token del usuario tenga el permiso de módulo indicado (o
/// "*", que da acceso total). El valor viaja en el claim "permisos" que
/// AuthController agrega al token a partir de Rol.Permisos.
/// </summary>
public class PermisoRequirement(string clave) : IAuthorizationRequirement
{
    public string Clave { get; } = clave;
}
