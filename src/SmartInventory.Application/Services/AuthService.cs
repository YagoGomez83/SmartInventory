using SmartInventory.Application.DTOs.Auth;
using SmartInventory.Application.Interfaces;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.Application.Services
{
    /// <summary>
    /// Implementación del servicio de autenticación.
    /// </summary>
    /// <remarks>
    /// ARQUITECTURA - DEPENDENCY INVERSION PRINCIPLE (DIP):
    /// 
    /// Observa cómo este servicio NO conoce:
    /// - PostgreSQL, SQL Server, MongoDB → Solo conoce IUserRepository (interfaz).
    /// - Entity Framework, Dapper, ADO.NET → La implementación está en Infrastructure.
    /// - JWT, OAuth, Cookies → La generación de tokens estará en un IJwtTokenGenerator.
    /// 
    /// Si mañana cambiamos de PostgreSQL a MongoDB:
    /// 1. Creamos MongoUserRepository en Infrastructure.
    /// 2. Cambiamos la inyección de dependencias en Program.cs.
    /// 3. Este servicio NO SE MODIFICA (0 líneas cambiadas).
    /// 
    /// Eso es arquitectura limpia y desacoplada.
    /// 
    /// RESPONSABILIDADES:
    /// - Validar reglas de negocio (email único, contraseña fuerte).
    /// - Orquestar operaciones (hashear password, generar token).
    /// - Coordinar repositorios.
    /// - Lanzar excepciones de dominio descriptivas.
    /// 
    /// NO ES RESPONSABLE DE:
    /// - HTTP (eso es del Controller).
    /// - Validación de datos de entrada (eso lo hará FluentValidation en Pipeline).
    /// - Acceso directo a BD (eso es del Repository).
    /// </remarks>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        // TODO: Inyectar IJwtTokenGenerator cuando lo implementemos
        // TODO: Inyectar IPasswordHasher cuando lo implementemos

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        /// <param name="userRepository">Repositorio de usuarios (abstracción).</param>
        /// <remarks>
        /// CONSTRUCTOR INJECTION:
        /// - Es la forma más común de DI en .NET.
        /// - Alternativas: Property Injection, Method Injection (menos comunes).
        /// 
        /// El contenedor IoC de .NET (.NET Core DI Container) hace esto:
        /// 1. Busca quién implementa IUserRepository.
        /// 2. Instancia la implementación (ej: UserRepository).
        /// 3. La pasa al constructor automáticamente.
        /// 
        /// BENEFICIOS:
        /// - Testeable: Podemos pasar un Mock en tests unitarios.
        /// - Flexible: Cambiar implementación sin tocar este código.
        /// - Explícito: Las dependencias se ven claramente en el constructor.
        /// </remarks>
        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        /// <inheritdoc />
        public async Task<AuthResponseDto> RegisterAsync(
            RegisterUserDto dto,
            CancellationToken cancellationToken = default)
        {
            // ═══════════════════════════════════════════════════════════════════
            // PASO 1: VALIDAR REGLAS DE NEGOCIO
            // ═══════════════════════════════════════════════════════════════════

            // Regla: El email debe ser único en el sistema
            if (await _userRepository.ExistsByEmailAsync(dto.Email, cancellationToken))
            {
                // TODO: Crear EmailAlreadyExistsException en Domain/Exceptions
                throw new InvalidOperationException(
                    $"El email '{dto.Email}' ya está registrado en el sistema.");
            }

            // TODO: Validar fortaleza de contraseña
            // - Mínimo 8 caracteres
            // - Al menos 1 mayúscula, 1 minúscula, 1 número, 1 símbolo
            // Esto se puede hacer con FluentValidation en la capa de API

            // ═══════════════════════════════════════════════════════════════════
            // PASO 2: TRANSFORMAR DTO → ENTIDAD (Mapping)
            // ═══════════════════════════════════════════════════════════════════

            // TODO: Implementar hashing real con BCrypt o Argon2
            // Ejemplo con BCrypt.Net:
            //   string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);
            // Por ahora, usamos un placeholder (NUNCA hacer esto en producción)
            string passwordHash = $"HASH_{dto.Password}"; // ⚠️ TEMPORAL - Implementar hashing real

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email.ToLowerInvariant(), // Normalizar email a minúsculas
                PasswordHash = passwordHash,
                Role = UserRole.Employee, // Por defecto, todos son Employee
                // BaseEntity ya inicializa: CreatedAt, IsActive
            };

            // ═══════════════════════════════════════════════════════════════════
            // PASO 3: PERSISTIR EN BASE DE DATOS
            // ═══════════════════════════════════════════════════════════════════

            var createdUser = await _userRepository.AddAsync(user, cancellationToken);

            // ═══════════════════════════════════════════════════════════════════
            // PASO 4: GENERAR TOKEN JWT
            // ═══════════════════════════════════════════════════════════════════

            // TODO: Implementar IJwtTokenGenerator
            // El token debe contener claims:
            // - sub: createdUser.Id (subject)
            // - email: createdUser.Email
            // - role: createdUser.Role.ToString()
            // - exp: DateTime.UtcNow.AddHours(1) (expiración)

            string jwtToken = $"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.PLACEHOLDER.{createdUser.Id}";
            DateTime expiresAt = DateTime.UtcNow.AddHours(1);

            // ═══════════════════════════════════════════════════════════════════
            // PASO 5: RETORNAR RESPUESTA
            // ═══════════════════════════════════════════════════════════════════

            return new AuthResponseDto(
                Token: jwtToken,
                Email: createdUser.Email,
                Role: createdUser.Role.ToString(),
                ExpiresAt: expiresAt
            );
        }

        /// <inheritdoc />
        public async Task<AuthResponseDto> LoginAsync(
            LoginDto dto,
            CancellationToken cancellationToken = default)
        {
            // ═══════════════════════════════════════════════════════════════════
            // PASO 1: BUSCAR USUARIO POR EMAIL
            // ═══════════════════════════════════════════════════════════════════

            var user = await _userRepository.GetByEmailAsync(
                dto.Email.ToLowerInvariant(),
                cancellationToken);

            // ═══════════════════════════════════════════════════════════════════
            // PASO 2: VALIDAR EXISTENCIA Y ESTADO
            // ═══════════════════════════════════════════════════════════════════

            // SEGURIDAD: Mensaje genérico para prevenir user enumeration
            // ❌ MAL: "El usuario no existe" → El atacante sabe que puede probar otro email
            // ✅ BIEN: "Credenciales inválidas" → No se sabe si falló email o password

            if (user is null || !user.IsActive)
            {
                // TODO: Crear InvalidCredentialsException en Domain/Exceptions
                throw new UnauthorizedAccessException("Credenciales inválidas.");
            }

            // ═══════════════════════════════════════════════════════════════════
            // PASO 3: VERIFICAR CONTRASEÑA
            // ═══════════════════════════════════════════════════════════════════

            // TODO: Implementar verificación real con BCrypt
            // Ejemplo:
            //   bool isValidPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            // Por ahora, comparación temporal (INSEGURO - solo para desarrollo)
            bool isValidPassword = user.PasswordHash == $"HASH_{dto.Password}";

            if (!isValidPassword)
            {
                // IMPORTANTE: Mismo mensaje de error que arriba (user enumeration prevention)
                throw new UnauthorizedAccessException("Credenciales inválidas.");
            }

            // ═══════════════════════════════════════════════════════════════════
            // PASO 4: GENERAR TOKEN JWT
            // ═══════════════════════════════════════════════════════════════════

            // TODO: Usar IJwtTokenGenerator
            string jwtToken = $"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.PLACEHOLDER.{user.Id}";
            DateTime expiresAt = DateTime.UtcNow.AddHours(1);

            // ═══════════════════════════════════════════════════════════════════
            // PASO 5: AUDITORÍA (Opcional pero recomendado en producción)
            // ═══════════════════════════════════════════════════════════════════

            // TODO: Registrar login exitoso en tabla de auditoría
            // - User ID
            // - IP Address
            // - Timestamp
            // - User Agent
            // Útil para detectar accesos sospechosos

            // ═══════════════════════════════════════════════════════════════════
            // PASO 6: RETORNAR RESPUESTA
            // ═══════════════════════════════════════════════════════════════════

            return new AuthResponseDto(
                Token: jwtToken,
                Email: user.Email,
                Role: user.Role.ToString(),
                ExpiresAt: expiresAt
            );
        }

        /// <inheritdoc />
        public Task<AuthResponseDto?> ValidateTokenAsync(
            string token,
            CancellationToken cancellationToken = default)
        {
            // TODO: Implementar validación de JWT
            // 1. Verificar firma usando la misma clave secreta
            // 2. Verificar expiración (exp claim)
            // 3. Extraer claims (sub, email, role)
            // 4. Opcionalmente, verificar que el usuario siga activo

            throw new NotImplementedException(
                "ValidateTokenAsync se implementará cuando tengamos IJwtTokenGenerator.");
        }
    }
}
