using Microsoft.EntityFrameworkCore;
using SmartInventory.Infrastructure.Data;
using SmartInventory.Domain.Interfaces;
using SmartInventory.Infrastructure.Repositories;
using SmartInventory.Application.Interfaces;
using SmartInventory.Application.Services;
using SmartInventory.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Agregar soporte para Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });



// Configurar DbContext con PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("SmartInventory.Infrastructure")));

// Registro de Repositorios (Infrastructure Layer)
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Registro de Servicios de Autenticación
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Registro de Servicios (Application Layer)
builder.Services.AddScoped<IAuthService, AuthService>();

// Repositorios
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Servicios
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();
builder.Services.AddScoped<IStockService, StockService>();

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIGURACIÓN DE AUTENTICACIÓN JWT
// ═══════════════════════════════════════════════════════════════════════════════
// Esta configuración le dice a ASP.NET Core cómo validar los tokens JWT entrantes.
// Cuando un cliente envía un request con Header: Authorization: Bearer <token>,
// este middleware automáticamente:
// 1. Extrae el token
// 2. Verifica la firma usando la misma clave secreta
// 3. Valida Issuer, Audience, y Expiración
// 4. Si es válido, pobla HttpContext.User con los Claims del token
// 5. Si es inválido, retorna 401 Unauthorized

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,           // Verifica que el token fue emitido por nosotros
            ValidateAudience = true,         // Verifica que el token es para nuestra app
            ValidateLifetime = true,         // Verifica que el token no haya expirado
            ValidateIssuerSigningKey = true, // Verifica la firma criptográfica
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Secret"]!)),
            // Mapeo de claims para que ASP.NET Core reconozca correctamente los roles
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });

// Agregar políticas de autorización (opcional, pero recomendado)
builder.Services.AddAuthorization();

var app = builder.Build();

// Aplicar migraciones automáticamente al iniciar la aplicación con reintentos
var maxRetries = 10;
var delay = TimeSpan.FromSeconds(3);

for (int i = 0; i < maxRetries; i++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        app.Logger.LogInformation("Intentando conectar a la base de datos... (Intento {Attempt}/{MaxRetries})", i + 1, maxRetries);
        await context.Database.MigrateAsync();
        app.Logger.LogInformation("Migraciones aplicadas exitosamente.");
        break; // Salir del bucle si tiene éxito
    }
    catch (Exception ex)
    {
        if (i == maxRetries - 1)
        {
            app.Logger.LogError(ex, "Error al aplicar migraciones después de {MaxRetries} intentos.", maxRetries);
            throw;
        }

        app.Logger.LogWarning(ex, "Error al conectar a la base de datos. Reintentando en {Delay} segundos...", delay.TotalSeconds);
        await Task.Delay(delay);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// ═══════════════════════════════════════════════════════════════════════════════
// MIDDLEWARES DE AUTENTICACIÓN Y AUTORIZACIÓN
// ═══════════════════════════════════════════════════════════════════════════════
// ORDEN CRÍTICO: UseAuthentication() DEBE ir ANTES de UseAuthorization()
// 
// UseAuthentication(): ¿Quién eres? (Verifica el token JWT)
// - Lee el Header Authorization: Bearer <token>
// - Valida el token
// - Popula HttpContext.User con los Claims
// 
// UseAuthorization(): ¿Qué puedes hacer? (Verifica permisos)
// - Verifica atributos [Authorize], [AllowAnonymous]
// - Verifica roles: [Authorize(Roles = "Admin")]
// - Si no estás autenticado y el endpoint requiere auth → 401 Unauthorized
// - Si estás autenticado pero no tienes el rol → 403 Forbidden

app.UseAuthentication();
app.UseAuthorization();

// Mapear Controllers
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
