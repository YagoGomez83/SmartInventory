namespace SmartInventory.Application.DTOs.Auth
{
    /// <summary>
    /// DTO de respuesta tras autenticación exitosa.
    /// </summary>
    /// <remarks>
    /// JWT TOKEN ESTRUCTURA:
    /// Un JWT tiene 3 partes separadas por puntos (header.payload.signature):
    /// 
    /// 1. Header (metadata):
    ///    { "alg": "HS256", "typ": "JWT" }
    /// 
    /// 2. Payload (claims):
    ///    {
    ///      "sub": "usuario@email.com",      // Subject (identificador único)
    ///      "jti": "unique-token-id",        // JWT ID (previene replay attacks)
    ///      "name": "Juan Pérez",
    ///      "role": "Admin",
    ///      "iat": 1609459200,               // Issued At (timestamp)
    ///      "exp": 1609545600                // Expiration (24h después)
    ///    }
    /// 
    /// 3. Signature (validación de integridad):
    ///    HMACSHA256(base64UrlEncode(header) + "." + base64UrlEncode(payload), secret)
    /// 
    /// BUENAS PRÁCTICAS:
    /// - Tiempo de expiración corto (15 min - 1 hora para access tokens).
    /// - Usar Refresh Tokens para renovar sin pedir credenciales nuevamente.
    /// - Guardar secret en variables de entorno, NUNCA en código.
    /// - En producción, usar RS256 (asimétrico) en lugar de HS256 (simétrico).
    /// </remarks>
    /// <param name="Token">Token JWT para autenticación en requests subsecuentes.</param>
    /// <param name="Email">Email del usuario autenticado.</param>
    /// <param name="Role">Rol del usuario (para UI conditional rendering).</param>
    /// <param name="ExpiresAt">Timestamp de expiración del token (ISO 8601).</param>
    public record AuthResponseDto(
        string Token,
        string Email,
        string Role,
        DateTime ExpiresAt
    );
}
