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
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Agregar soporte para Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartInventory API",
        Version = "v1",
        Description = "API para gestión inteligente de inventario con Clean Architecture",
        Contact = new OpenApiContact
        {
            Name = "SmartInventory Team",
            Email = "contact@smartinventory.com"
        }
    });

    // ═══════════════════════════════════════════════════════════════════════════════
    // CONFIGURACIÓN DE SEGURIDAD JWT EN SWAGGER
    // ═══════════════════════════════════════════════════════════════════════════════
    // Esto agrega el botón "Authorize" en la UI de Swagger que permite:
    // 1. Hacer clic en "Authorize"
    // 2. Pegar tu token JWT (sin "Bearer", solo el token)
    // 3. Swagger automáticamente enviará "Authorization: Bearer {token}" en todos los requests

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // ═══════════════════════════════════════════════════════════════════════════════
    // HABILITAR XML COMMENTS PARA DOCUMENTACIÓN
    // ═══════════════════════════════════════════════════════════════════════════════
    // Esto permite que Swagger lea los comentarios /// <summary> de tus controladores
    // y los muestre en la interfaz web. ¡Hace que tu API se auto-documente!

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});



// Configurar DbContext con PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("SmartInventory.Infrastructure")));

// Registro de Repositorios (Infrastructure Layer)
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

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

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


builder.Services.AddScoped<IOrderService, OrderService>();
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

    // ═══════════════════════════════════════════════════════════════════════════════
    // HABILITAR SWAGGER UI
    // ═══════════════════════════════════════════════════════════════════════════════
    // Swagger estará disponible en: https://localhost:5001/swagger
    // Aquí podrás:
    // - Ver todos tus endpoints documentados
    // - Probar cada endpoint directamente desde el navegador
    // - Usar el botón "Authorize" para autenticarte con JWT

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartInventory API v1");
        options.RoutePrefix = "swagger"; // Accesible en /swagger
    });
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
