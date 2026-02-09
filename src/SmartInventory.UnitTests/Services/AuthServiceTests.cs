using FluentAssertions;
using Moq;
using SmartInventory.Application.DTOs.Auth;
using SmartInventory.Application.Interfaces;
using SmartInventory.Application.Services;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.UnitTests.Services
{
    /// <summary>
    /// 🧪 UNIT TESTS PARA AUTHSERVICE - SEGURIDAD Y AUTENTICACIÓN
    /// ═══════════════════════════════════════════════════════════════════════════════
    /// OBJETIVO:
    /// Validar el sistema de autenticación y autorización del sistema.
    /// 
    /// COBERTURA:
    /// ✓ Registro de usuarios con hash de contraseña BCrypt
    /// ✓ Validación de emails duplicados
    /// ✓ Login exitoso con generación de JWT
    /// ✓ Login con credenciales inválidas (usuario no existe)
    /// ✓ Login con contraseña incorrecta
    /// ✓ Protección contra user enumeration
    /// 
    /// SEGURIDAD CRÍTICA:
    /// Este servicio protege el acceso al sistema completo.
    /// Si estos tests fallan, hay vulnerabilidades de seguridad.
    /// 
    /// TÉCNICAS AVANZADAS:
    /// - Mockear JWT token generator
    /// - Validar hashing BCrypt (contraseñas NUNCA en texto plano)
    /// - Simular ataques de enumeración de usuarios
    /// </summary>
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

            _authService = new AuthService(
                _userRepositoryMock.Object,
                _jwtTokenGeneratorMock.Object
            );
        }

        /// <summary>
        /// Helper method para capturar el User pasado a mocks.
        /// Necesario porque BCrypt.Verify no puede usarse en árboles de expresión (It.Is).
        /// </summary>
        private static bool CaptureUser(User user, ref User? capturedUser)
        {
            capturedUser = user;
            return true;
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // TESTS DE REGISTRO (REGISTER)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// ✅ HAPPY PATH: Registro exitoso de un nuevo usuario.
        /// Verifica que la contraseña se hashea con BCrypt y se genera un token JWT.
        /// </summary>
        [Fact]
        public async Task RegisterAsync_WithValidData_ShouldCreateUserAndReturnToken()
        {
            // Arrange
            var registerDto = new RegisterUserDto(
                FirstName: "Juan",
                LastName: "Pérez",
                Email: "juan.perez@example.com",
                Password: "SecurePassword123!"
            );

            // Configuramos que el email NO existe (validación pasa)
            _userRepositoryMock
                .Setup(repo => repo.ExistsByEmailAsync("juan.perez@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Configuramos que AddAsync retorna el usuario con ID generado
            _userRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User u, CancellationToken _) =>
                {
                    u.Id = 1; // Simulamos ID auto-generado
                    return u;
                });

            // Configuramos el mock del generador de tokens JWT
            _jwtTokenGeneratorMock
                .Setup(gen => gen.GenerateToken(It.IsAny<User>()))
                .Returns("fake-jwt-token-12345");

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be("fake-jwt-token-12345", "Debe retornar el token JWT generado");
            result.Email.Should().Be("juan.perez@example.com");
            result.Role.Should().Be(UserRole.Employee.ToString(), "Por defecto los nuevos usuarios son Employee");

            // ═══════════════════════════════════════════════════════════════════════════
            // VERIFICACIÓN CRÍTICA: CONTRASEÑA HASHEADA CON BCRYPT
            // ═══════════════════════════════════════════════════════════════════════════

            // Capturamos el usuario que se pasó a AddAsync para verificarlo después
            User? capturedUser = null;
            _userRepositoryMock.Verify(
                repo => repo.AddAsync(
                    It.Is<User>(u => CaptureUser(u, ref capturedUser)),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once,
                "Debe crear el usuario con contraseña hasheada correctamente"
            );

            // Verificaciones del usuario capturado
            capturedUser.Should().NotBeNull();
            capturedUser!.FirstName.Should().Be("Juan");
            capturedUser.LastName.Should().Be("Pérez");
            capturedUser.Email.Should().Be("juan.perez@example.com");
            capturedUser.PasswordHash.Should().NotBe(registerDto.Password,
                "La contraseña NUNCA debe guardarse en texto plano");

            // Verifica que el hash BCrypt es válido
            BCrypt.Net.BCrypt.Verify(registerDto.Password, capturedUser.PasswordHash)
                .Should().BeTrue("El hash BCrypt debe poder verificar la contraseña original");

            capturedUser.Role.Should().Be(UserRole.Employee);

            // Verifica que se generó el token JWT
            _jwtTokenGeneratorMock.Verify(
                gen => gen.GenerateToken(It.IsAny<User>()),
                Times.Once,
                "Debe generar un token JWT para el usuario registrado"
            );
        }

        /// <summary>
        /// ❌ SAD PATH: Intentar registrar un usuario con email duplicado.
        /// </summary>
        [Fact]
        public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var registerDto = new RegisterUserDto(
                FirstName: "María",
                LastName: "García",
                Email: "existing@example.com",
                Password: "Password123!"
            );

            // Configuramos que el email YA EXISTE
            _userRepositoryMock
                .Setup(repo => repo.ExistsByEmailAsync("existing@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            Func<Task> action = async () => await _authService.RegisterAsync(registerDto);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*email*ya está registrado*",
                    "Debe lanzar excepción cuando el email está duplicado");

            // Verifica que NUNCA se intentó crear el usuario
            _userRepositoryMock.Verify(
                repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "No debe crear el usuario si el email ya existe"
            );

            // Verifica que NUNCA se generó token
            _jwtTokenGeneratorMock.Verify(
                gen => gen.GenerateToken(It.IsAny<User>()),
                Times.Never,
                "No debe generar token si el registro falla"
            );
        }

        /// <summary>
        /// ✅ VERIFICACIÓN: El email se normaliza a minúsculas.
        /// Previene problemas de case-sensitivity (user@EXAMPLE.com vs user@example.com).
        /// </summary>
        [Fact]
        public async Task RegisterAsync_ShouldNormalizeEmailToLowerCase()
        {
            // Arrange
            var registerDto = new RegisterUserDto(
                FirstName: "Ana",
                LastName: "López",
                Email: "ANA.LOPEZ@EXAMPLE.COM", // Email en MAYÚSCULAS
                Password: "Password123!"
            );

            _userRepositoryMock
                .Setup(repo => repo.ExistsByEmailAsync("ana.lopez@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _userRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User u, CancellationToken _) => { u.Id = 1; return u; });

            _jwtTokenGeneratorMock
                .Setup(gen => gen.GenerateToken(It.IsAny<User>()))
                .Returns("token");

            // Act
            await _authService.RegisterAsync(registerDto);

            // Assert
            _userRepositoryMock.Verify(
                repo => repo.AddAsync(
                    It.Is<User>(u => u.Email == "ana.lopez@example.com"), // ⭐ Debe estar en minúsculas
                    It.IsAny<CancellationToken>()
                ),
                Times.Once,
                "Debe normalizar el email a minúsculas para evitar duplicados por case"
            );
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // TESTS DE LOGIN
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// ✅ HAPPY PATH: Login exitoso con credenciales válidas.
        /// </summary>
        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            const string plainPassword = "MySecurePassword123!";
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            var existingUser = new User
            {
                Id = 1,
                FirstName = "Carlos",
                LastName = "Ruiz",
                Email = "carlos.ruiz@example.com",
                PasswordHash = passwordHash, // ⭐ Contraseña hasheada
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };

            var loginDto = new LoginDto(
                Email: "carlos.ruiz@example.com",
                Password: plainPassword // ⭐ Contraseña en texto plano
            );

            // Configuramos que el usuario existe
            _userRepositoryMock
                .Setup(repo => repo.GetByEmailAsync("carlos.ruiz@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Configuramos el generador de tokens
            _jwtTokenGeneratorMock
                .Setup(gen => gen.GenerateToken(existingUser))
                .Returns("valid-jwt-token-67890");

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be("valid-jwt-token-67890");
            result.Email.Should().Be("carlos.ruiz@example.com");
            result.Role.Should().Be(UserRole.Admin.ToString());

            // Verifica que se generó el token JWT
            _jwtTokenGeneratorMock.Verify(
                gen => gen.GenerateToken(existingUser),
                Times.Once,
                "Debe generar un token JWT para el usuario autenticado"
            );
        }

        /// <summary>
        /// ❌ SAD PATH: Login con usuario que no existe.
        /// SEGURIDAD: Mensaje genérico para prevenir user enumeration.
        /// </summary>
        [Fact]
        public async Task LoginAsync_WithNonExistentUser_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var loginDto = new LoginDto(
                Email: "noexiste@example.com",
                Password: "AnyPassword123!"
            );

            // Configuramos que el usuario NO existe
            _userRepositoryMock
                .Setup(repo => repo.GetByEmailAsync("noexiste@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            Func<Task> action = async () => await _authService.LoginAsync(loginDto);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Credenciales inválidas*",
                    "Debe usar mensaje genérico para prevenir user enumeration");

            // Verifica que NUNCA se generó token
            _jwtTokenGeneratorMock.Verify(
                gen => gen.GenerateToken(It.IsAny<User>()),
                Times.Never,
                "No debe generar token si el usuario no existe"
            );
        }

        /// <summary>
        /// ❌ SAD PATH: Login con contraseña incorrecta.
        /// SEGURIDAD: Mismo mensaje que usuario no existente (prevenir enumeration).
        /// </summary>
        [Fact]
        public async Task LoginAsync_WithWrongPassword_ShouldThrowInvalidOperationException()
        {
            // Arrange
            const string correctPassword = "CorrectPassword123!";
            const string wrongPassword = "WrongPassword999!";
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(correctPassword);

            var existingUser = new User
            {
                Id = 2,
                FirstName = "Laura",
                LastName = "Martínez",
                Email = "laura.martinez@example.com",
                PasswordHash = passwordHash,
                Role = UserRole.Employee,
                CreatedAt = DateTime.UtcNow
            };

            var loginDto = new LoginDto(
                Email: "laura.martinez@example.com",
                Password: wrongPassword // ⚠️ Contraseña INCORRECTA
            );

            // Configuramos que el usuario existe
            _userRepositoryMock
                .Setup(repo => repo.GetByEmailAsync("laura.martinez@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act
            Func<Task> action = async () => await _authService.LoginAsync(loginDto);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Credenciales inválidas*",
                    "Debe usar el MISMO mensaje que cuando el usuario no existe");

            // Verifica que NUNCA se generó token
            _jwtTokenGeneratorMock.Verify(
                gen => gen.GenerateToken(It.IsAny<User>()),
                Times.Never,
                "No debe generar token si la contraseña es incorrecta"
            );
        }

        /// <summary>
        /// ✅ VERIFICACIÓN: El login normaliza el email a minúsculas (consistency con Register).
        /// </summary>
        [Fact]
        public async Task LoginAsync_ShouldNormalizeEmailToLowerCase()
        {
            // Arrange
            string passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

            var existingUser = new User
            {
                Id = 3,
                Email = "test@example.com",
                PasswordHash = passwordHash,
                Role = UserRole.Employee,
                CreatedAt = DateTime.UtcNow
            };

            var loginDto = new LoginDto(
                Email: "TEST@EXAMPLE.COM", // Email en MAYÚSCULAS
                Password: "Password123!"
            );

            // Configuramos que debe buscar con email en minúsculas
            _userRepositoryMock
                .Setup(repo => repo.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            _jwtTokenGeneratorMock
                .Setup(gen => gen.GenerateToken(It.IsAny<User>()))
                .Returns("token");

            // Act
            await _authService.LoginAsync(loginDto);

            // Assert
            _userRepositoryMock.Verify(
                repo => repo.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()),
                Times.Once,
                "Debe buscar con email normalizado a minúsculas"
            );
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 🎯 TESTS DE SEGURIDAD AVANZADOS
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 🔒 SEGURIDAD: Verificar que las contraseñas NUNCA se guardan en texto plano.
        /// Este test es CRÍTICO para compliance (GDPR, PCI-DSS, SOC 2).
        /// </summary>
        [Fact]
        public async Task RegisterAsync_ShouldNeverStorePasswordInPlainText()
        {
            // Arrange
            const string plainPassword = "SuperSecretPassword123!";

            var registerDto = new RegisterUserDto(
                FirstName: "Security",
                LastName: "Test",
                Email: "security@test.com",
                Password: plainPassword
            );

            _userRepositoryMock
                .Setup(repo => repo.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _userRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User u, CancellationToken _) => { u.Id = 1; return u; });

            _jwtTokenGeneratorMock
                .Setup(gen => gen.GenerateToken(It.IsAny<User>()))
                .Returns("token");

            // Act
            await _authService.RegisterAsync(registerDto);

            // Assert
            _userRepositoryMock.Verify(
                repo => repo.AddAsync(
                    It.Is<User>(u =>
                        u.PasswordHash != plainPassword && // ⚠️ NUNCA debe contener la contraseña en texto plano
                        u.PasswordHash.StartsWith("$2a$") && // BCrypt hash siempre empieza con $2a$ o $2b$
                        u.PasswordHash.Length >= 60 // BCrypt hash tiene mínimo 60 caracteres
                    ),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once,
                "CRÍTICO: La contraseña DEBE estar hasheada con BCrypt, NUNCA en texto plano"
            );
        }

        /// <summary>
        /// 🔒 SEGURIDAD: BCrypt genera un hash diferente cada vez (salt aleatorio).
        /// Esto previene rainbow table attacks.
        /// </summary>
        [Fact]
        public void BCrypt_ShouldGenerateDifferentHashesForSamePassword()
        {
            // Arrange
            const string password = "TestPassword123!";

            // Act
            string hash1 = BCrypt.Net.BCrypt.HashPassword(password);
            string hash2 = BCrypt.Net.BCrypt.HashPassword(password);

            // Assert
            hash1.Should().NotBe(hash2,
                "BCrypt debe generar un hash diferente cada vez (salt aleatorio)");

            // Pero ambos hashes deben poder verificar la misma contraseña
            BCrypt.Net.BCrypt.Verify(password, hash1).Should().BeTrue();
            BCrypt.Net.BCrypt.Verify(password, hash2).Should().BeTrue();
        }
    }
}
