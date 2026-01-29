using SmartInventory.Application.DTOs.Auth;

namespace SmartInventory.Application.Interfaces
{
    /// <summary>
    /// Contrato para servicios de autenticación y autorización.
    /// </summary>
    /// <remarks>
    /// PATRÓN DE DISEÑO: Service Layer Pattern
    /// - Los servicios orquestan la lógica de negocio.
    /// - Coordinan múltiples repositorios y operaciones.
    /// - Mantienen la lógica de negocio fuera de los Controllers (Thin Controllers).
    /// 
    /// INYECCIÓN DE DEPENDENCIAS:
    /// - Esta interfaz se registra en el contenedor IoC de .NET.
    /// - Configuración en Program.cs:
    ///   builder.Services.AddScoped<IAuthService, AuthService>();
    /// - Los Controllers reciben esta interfaz, no la implementación concreta.
    /// 
    /// TESTING:
    /// - Podemos crear un MockAuthService para tests sin tocar la BD.
    /// - Ejemplo: var mockService = new Mock<IAuthService>();
    /// </remarks>
    public interface IAuthService
    {
        /// <summary>
        /// Registra un nuevo usuario en el sistema.
        /// </summary>
        /// <param name="dto">Datos del usuario a registrar.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>Respuesta con token JWT y datos del usuario.</returns>
        /// <remarks>
        /// FLUJO DE REGISTRO:
        /// 1. Validar que el email no exista (evitar duplicados).
        /// 2. Hashear la contraseña con BCrypt/Argon2.
        /// 3. Crear entidad User con rol por defecto (Employee).
        /// 4. Guardar en BD mediante IUserRepository.
        /// 5. Generar JWT token.
        /// 6. Retornar token + datos básicos del usuario.
        /// 
        /// EXCEPCIONES:
        /// - EmailAlreadyExistsException: Si el email ya está registrado.
        /// - ValidationException: Si los datos no cumplen las reglas de negocio.
        /// </remarks>
        Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Autentica un usuario y genera un token de acceso.
        /// </summary>
        /// <param name="dto">Credenciales del usuario (email y password).</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>Respuesta con token JWT y datos del usuario.</returns>
        /// <remarks>
        /// FLUJO DE LOGIN:
        /// 1. Buscar usuario por email.
        /// 2. Verificar contraseña usando BCrypt.Verify(password, hash).
        /// 3. Verificar que el usuario esté activo (IsActive = true).
        /// 4. Generar JWT token con claims (id, email, role).
        /// 5. Registrar login exitoso (auditoría - opcional).
        /// 6. Retornar token.
        /// 
        /// SEGURIDAD:
        /// - NUNCA retornes mensajes específicos ("email no existe", "contraseña incorrecta").
        /// - Usa siempre: "Credenciales inválidas" (previene user enumeration).
        /// - Implementa rate limiting (ej: 5 intentos por IP en 15 minutos).
        /// - Considera account lockout tras X intentos fallidos.
        /// 
        /// EXCEPCIONES:
        /// - InvalidCredentialsException: Credenciales incorrectas (mensaje genérico).
        /// - UserInactiveException: El usuario fue desactivado.
        /// </remarks>
        Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida un token JWT y retorna los claims del usuario.
        /// </summary>
        /// <param name="token">Token JWT a validar.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>Datos del usuario extraídos del token.</returns>
        /// <remarks>
        /// VALIDACIÓN DE TOKEN:
        /// 1. Verificar firma (que no haya sido modificado).
        /// 2. Verificar expiración (exp claim).
        /// 3. Verificar issuer y audience (previene tokens robados de otras apps).
        /// 4. Extraer claims (id, email, role).
        /// 
        /// USO:
        /// - Middleware de autenticación lo hace automáticamente.
        /// - Útil para operaciones manuales (ej: refresh token).
        /// </remarks>
        Task<AuthResponseDto?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    }
}
