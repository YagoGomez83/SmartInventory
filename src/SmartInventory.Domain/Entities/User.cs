using SmartInventory.Domain.Common;
using SmartInventory.Domain.Enums;

namespace SmartInventory.Domain.Entities
{
    /// <summary>
    /// Entidad que representa un usuario del sistema.
    /// </summary>
    /// <remarks>
    /// CRITERIOS DE DISEÑO:
    /// - Sealed: Optimización de rendimiento. El compilador puede devirtualizar llamadas.
    ///   En benchmarks, puede representar hasta 15% más de velocidad en hot paths.
    /// - string.Empty: Evita NullReferenceException. En C# 11+, considera usar required properties.
    /// - PasswordHash vs Password: NUNCA guardes contraseñas en texto plano.
    ///   Usaremos BCrypt o Argon2 (ganador de Password Hashing Competition 2015).
    /// 
    /// CUMPLIMIENTO:
    /// - GDPR: Email es dato personal. Implementar derecho al olvido (soft delete).
    /// - OWASP: PasswordHash debe usar algoritmo resistente a rainbow tables y GPU cracking.
    /// </remarks>
    public sealed class User : BaseEntity
    {
        /// <summary>
        /// Nombre del usuario.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Apellido del usuario.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Dirección de correo electrónico (debe ser única en el sistema).
        /// </summary>
        /// <remarks>
        /// La unicidad se garantiza mediante un índice único en la base de datos.
        /// En EF Core: modelBuilder.Entity&lt;User&gt;().HasIndex(u => u.Email).IsUnique();
        /// </remarks>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Hash de la contraseña del usuario.
        /// </summary>
        /// <remarks>
        /// NUNCA almacenes contraseñas en texto plano.
        /// Algoritmo recomendado: BCrypt con work factor >= 12 o Argon2id.
        /// Ejemplo con BCrypt.Net:
        ///   string hash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
        ///   bool isValid = BCrypt.Net.BCrypt.Verify(plainTextPassword, hash);
        /// </remarks>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Rol del usuario en el sistema.
        /// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// Nombre completo del usuario (propiedad calculada).
        /// </summary>
        /// <remarks>
        /// En lugar de almacenar esto en BD, lo calculamos dinámicamente.
        /// Reduce redundancia y evita problemas de sincronización.
        /// EF Core no mapeará esta propiedad automáticamente (no tiene setter).
        /// </remarks>
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
