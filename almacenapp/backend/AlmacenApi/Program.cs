using System.Text;
using AlmacenApi.Authorization;
using AlmacenApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    // Requiere autenticación por defecto en todos los controllers;
    // AuthController se marca con [AllowAnonymous] para el login.
    options.Filters.Add(new AuthorizeFilter());
});
builder.Services.AddDbContext<AlmacenDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AlmacenDb")));
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        // El frontend (almacenapp, Vite) corre en el puerto fijado por
        // vite.config.ts (52567), no en el 5173 por defecto de Vite.
        policy.WithOrigins("http://localhost:52567", "https://localhost:52567")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!)),
        };
    });
// Autorización por permiso de módulo (ver Authorization/PermisoRequirement.cs):
// cada acción de escritura de un módulo exige [Authorize(Policy = "Permiso:xxx")],
// y el handler revisa el claim "permisos" del token (viene de Rol.Permisos).
// Las lecturas (GET) siguen abiertas a cualquier usuario autenticado, igual que
// antes — así el Dashboard, que junta datos de varios módulos, sigue funcionando
// para cualquier rol sin necesitar todos los permisos.
builder.Services.AddSingleton<IAuthorizationHandler, PermisoAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    string[] modulosConPermiso =
    [
        "productos", "existencias", "movimientos", "compras",
        "salidas", "proveedores", "clientes", "catalogos",
    ];

    foreach (var modulo in modulosConPermiso)
    {
        options.AddPolicy($"Permiso:{modulo}", policy => policy.Requirements.Add(new PermisoRequirement(modulo)));
    }
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// En Docker la base de datos parte vacía: si AutoMigrate=true (lo activa
// docker-compose.yml) se aplican las migraciones automáticamente al
// arrancar, lo que también materializa los datos semilla (HasData). En
// desarrollo local normal esto queda apagado (falta la clave o es "false")
// y las migraciones se siguen aplicando a mano con `dotnet ef database update`.
if (app.Configuration.GetValue<bool>("AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AlmacenDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllers();

app.Run();
