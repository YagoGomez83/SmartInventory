using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartInventory.Application.Interfaces;
using SmartInventory.Domain.Entities;

namespace SmartInventory.Infrastructure.Authentication
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            // Leer configuración JWT
            var secret = _configuration["JwtSettings:Secret"] 
                ?? throw new InvalidOperationException("JWT Secret no está configurado");
            var issuer = _configuration["JwtSettings:Issuer"] 
                ?? throw new InvalidOperationException("JWT Issuer no está configurado");
            var audience = _configuration["JwtSettings:Audience"] 
                ?? throw new InvalidOperationException("JWT Audience no está configurado");
            var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "60");

            // Crear los Claims (información del usuario en el token)
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            // Crear la clave de seguridad
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Crear el token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            // Convertir el token a string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
