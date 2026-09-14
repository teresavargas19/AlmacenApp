using System.ComponentModel.DataAnnotations;

namespace AlmacenApi.Models.Dtos;

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UsuarioDto Usuario { get; set; } = null!;
}
