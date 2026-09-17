using Microsoft.AspNetCore.Authorization;

namespace AlmacenApi.Authorization;

/// <summary>
/// Valida un PermisoRequirement contra el claim "permisos" del token:
/// "*" da acceso a todo (así queda seeded el rol Administrador), o el
/// permiso pedido debe estar en la lista separada por comas.
/// </summary>
public class PermisoAuthorizationHandler : AuthorizationHandler<PermisoRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermisoRequirement requirement)
    {
        var permisos = context.User.FindFirst("permisos")?.Value ?? string.Empty;

        var tieneAcceso = permisos.Trim() == "*"
            || permisos
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(requirement.Clave);

        if (tieneAcceso)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
