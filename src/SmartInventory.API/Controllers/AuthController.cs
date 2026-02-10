using Microsoft.AspNetCore.Mvc;
using SmartInventory.Application.DTOs.Auth;
using SmartInventory.Application.Interfaces;

namespace SmartInventory.API.Controllers
{
    /// <summary>
    /// Controlador para operaciones de autenticación y autorización.
    /// </summary>
    /// <remarks>
    /// CLEAN ARCHITECTURE - PRESENTATION LAYER:
    /// - Los Controllers son la capa más externa (UI/API).
    /// - NO contienen lógica de negocio.
    /// - Solo coordinan el flujo: reciben request → llaman al servicio → devuelven response.
    /// 
    /// THIN CONTROLLERS:
    /// - Mantén los controllers delgados.
    /// - La lógica compleja va en los Services (Application Layer).
    /// - Los Controllers solo validan entrada básica y manejan respuestas HTTP.
    /// 
    /// INYECCIÓN DE DEPENDENCIAS:
    /// - ASP.NET Core resuelve automáticamente IAuthService del contenedor IoC.
    /// - Registrado en Program.cs: builder.Services.AddScoped&lt;IAuthService, AuthService&gt;();
    /// 
    /// ROUTING:
    /// - [Route("api/[controller]")] → api/auth
    /// - Todos los endpoints heredan este prefijo.
    /// - [HttpPost("register")] → POST api/auth/register
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        /// <param name="authService">Servicio de autenticación.</param>
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registra un nuevo usuario en el sistema.
        /// </summary>
        /// <param name="dto">Datos del usuario a registrar.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>Respuesta con token JWT y datos del usuario.</returns>
        /// <remarks>
        /// EJEMPLO DE REQUEST:
        /// POST /api/auth/register
        /// Content-Type: application/json
        /// 
        /// {
        ///   "firstName": "Juan",
        ///   "lastName": "Pérez",
        ///   "email": "juan.perez@empresa.com",
        ///   "password": "PasswordSeguro123!"
        /// }
        /// 
        /// RESPUESTAS HTTP:
        /// - 200 OK: Usuario registrado exitosamente.
        /// - 400 BAD REQUEST: Error de validación o email duplicado.
        /// - 500 INTERNAL SERVER ERROR: Error inesperado del servidor.
        /// 
        /// MEJORAS FUTURAS:
        /// - Retornar 201 Created en lugar de 200 OK (semánticamente más correcto).
        /// - Agregar validación con FluentValidation antes de llamar al servicio.
        /// - Implementar rate limiting para prevenir ataques de fuerza bruta.
        /// - Enviar email de confirmación (verificación de cuenta).
        /// </remarks>
        /// <response code="200">Usuario registrado exitosamente. Retorna token JWT.</response>
        /// <response code="400">Error de validación o email ya existe.</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterUserDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Delegar toda la lógica al servicio de aplicación
                var result = await _authService.RegisterAsync(dto, cancellationToken);

                // Retornar respuesta exitosa con el token JWT
                return Ok(result);
            }
            catch (Exception ex)
            {
                // MANEJO DE ERRORES:
                // En producción, deberías:
                // 1. Usar un middleware global de excepciones.
                // 2. Loggear el error completo (con stack trace) en tu sistema de logs.
                // 3. NO exponer detalles internos al cliente (seguridad).
                // 4. Retornar mensajes genéricos como "An error occurred".
                // 
                // Por ahora, retornamos el mensaje de la excepción para facilitar el desarrollo.
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Inicia sesión con credenciales de usuario.
        /// </summary>
        /// <param name="dto">Credenciales del usuario (email y contraseña).</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>Respuesta con token JWT si las credenciales son válidas.</returns>
        /// <remarks>
        /// EJEMPLO DE REQUEST:
        /// POST /api/auth/login
        /// Content-Type: application/json
        /// 
        /// {
        ///   "email": "juan.perez@empresa.com",
        ///   "password": "PasswordSeguro123!"
        /// }
        /// 
        /// RESPUESTAS HTTP:
        /// - 200 OK: Autenticación exitosa, retorna token JWT.
        /// - 400 BAD REQUEST: Credenciales inválidas.
        /// - 500 INTERNAL SERVER ERROR: Error inesperado del servidor.
        /// 
        /// SEGURIDAD:
        /// - Las contraseñas se comparan usando BCrypt (previene timing attacks).
        /// - Los mensajes de error son genéricos para prevenir user enumeration.
        /// - El token JWT contiene claims del usuario (id, email, role).
        /// - El token expira según la configuración (JwtSettings:ExpiryMinutes).
        /// 
        /// MEJORAS FUTURAS:
        /// - Implementar rate limiting (max 5 intentos por IP cada 15 min).
        /// - Agregar account lockout tras X intentos fallidos.
        /// - Registrar auditoría de logins (IP, timestamp, user agent).
        /// - Implementar 2FA (autenticación de dos factores).
        /// </remarks>
        /// <response code="200">Login exitoso. Retorna token JWT.</response>
        /// <response code="400">Credenciales inválidas.</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Delegar toda la lógica al servicio de aplicación
                var result = await _authService.LoginAsync(dto, cancellationToken);

                // Retornar respuesta exitosa con el token JWT
                return Ok(result);
            }
            catch
            {
                // MANEJO DE ERRORES:
                // Por seguridad, siempre retornar mensaje genérico
                // No revelar si el email existe o si la contraseña es incorrecta
                return BadRequest(new { error = "Credenciales inválidas" });
            }
        }
    }
}
