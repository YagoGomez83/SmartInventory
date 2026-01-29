namespace SmartInventory.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para el inicio de sesión de usuarios.
    /// </summary>
    /// <remarks>
    /// AUTENTICACIÓN:
    /// - El flujo es: Usuario envía Email + Password → Backend valida → Retorna JWT Token.
    /// - JWT (JSON Web Token) es un estándar para autenticación stateless en APIs REST.
    /// 
    /// SEGURIDAD - PREVENCIÓN DE ATAQUES:
    /// 1. RATE LIMITING: Limita a 5 intentos por IP en 15 minutos (evita brute-force).
    /// 2. MENSAJES GENÉRICOS: Si falla login, NO digas "email no existe" o "contraseña incorrecta".
    ///    Usa siempre: "Credenciales inválidas". Esto previene user enumeration attacks.
    /// 3. TIMING ATTACKS: Usa algoritmos de comparación de tiempo constante.
    /// 4. ACCOUNT LOCKOUT: Bloquea cuenta tras X intentos fallidos.
    /// 
    /// EJEMPLO DE RESPUESTA:
    /// Success: { "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." }
    /// Failure: { "error": "Credenciales inválidas" } (nunca especifiques qué falló)
    /// </remarks>
    /// <param name="Email">Correo electrónico del usuario.</param>
    /// <param name="Password">Contraseña en texto plano (viaja por HTTPS).</param>
    public record LoginDto(
        string Email,
        string Password
    );
}
