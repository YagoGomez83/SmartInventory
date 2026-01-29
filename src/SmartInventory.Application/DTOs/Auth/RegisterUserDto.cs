namespace SmartInventory.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para el registro de nuevos usuarios.
    /// </summary>
    /// <remarks>
    /// RECORD vs CLASS:
    /// - 'record' (C# 9.0+) es inmutable por defecto y perfecto para DTOs.
    /// - Genera automáticamente: Equals, GetHashCode, ToString, Deconstruction.
    /// - Usa "value-based equality" en lugar de "reference equality".
    /// 
    /// VALIDACIÓN:
    /// - En producción, usaremos FluentValidation para validar:
    ///   * Email válido (regex o librería MailAddress).
    ///   * Password seguro (mín. 8 chars, mayúscula, número, símbolo).
    ///   * FirstName y LastName no vacíos.
    /// 
    /// SEGURIDAD:
    /// - Password viaja en texto plano SOLO en el canal HTTPS (TLS).
    /// - Nunca se guarda en texto plano. Se hashea inmediatamente con BCrypt/Argon2.
    /// - En logs, NUNCA registres el password (cumplimiento OWASP).
    /// 
    /// EJEMPLO DE USO:
    /// var dto = new RegisterUserDto(
    ///     FirstName: "Juan",
    ///     LastName: "Pérez",
    ///     Email: "juan@example.com",
    ///     Password: "SecureP@ss123"
    /// );
    /// </remarks>
    /// <param name="FirstName">Nombre del usuario.</param>
    /// <param name="LastName">Apellido del usuario.</param>
    /// <param name="Email">Correo electrónico (debe ser único en el sistema).</param>
    /// <param name="Password">Contraseña en texto plano (se hasheará antes de guardar).</param>
    public record RegisterUserDto(
        string FirstName,
        string LastName,
        string Email,
        string Password
    );
}
