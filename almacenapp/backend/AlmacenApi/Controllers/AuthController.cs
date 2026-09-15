using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AlmacenApi.Data;
using AlmacenApi.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AlmacenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController(AlmacenDbContext context, IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto, CancellationToken cancellationToken)
    {
        var usuario = await context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Activo, cancellationToken);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            return Unauthorized(new { message = "Correo o contraseña incorrectos." });
        }

        var jwtSection = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var minutos = int.TryParse(jwtSection["ExpiresMinutes"], out var valor) ? valor : 480;
        var expira = DateTime.UtcNow.AddMinutes(minutos);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol.Nombre),
        };

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expira,
            signingCredentials: credenciales);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponseDto
        {
            Token = tokenString,
            ExpiresAt = expira,
            Usuario = new UsuarioDto
            {
                Id = usuario.Id,
                RolId = usuario.RolId,
                RolNombre = usuario.Rol.Nombre,
                Permisos = usuario.Rol.Permisos,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Activo = usuario.Activo,
            },
        });
    }
}
