using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;

namespace SmartInventory.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuración de Entity Framework Core para la entidad User.
    /// </summary>
    /// <remarks>
    /// PATRÓN: Fluent API separada del DbContext.
    /// VENTAJAS:
    /// - Cumple Single Responsibility Principle (SRP).
    /// - Facilita testing unitario de configuraciones.
    /// - Evita DbContext gigantes con métodos OnModelCreating de 500+ líneas.
    /// 
    /// CONFIGURACIONES CLAVE:
    /// - Email: Índice único para garantizar unicidad a nivel de BD.
    /// - Role: Se guarda como string (legible) en lugar de int (mejora auditoría).
    /// - PasswordHash: Longitud máxima considerando BCrypt (60 chars) o Argon2 (mayor).
    /// </remarks>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Nombre de tabla
            builder.ToTable("Users");

            // Clave primaria
            builder.HasKey(u => u.Id);

            // Propiedades
            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            // Índice único en Email
            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(500); // Suficiente para BCrypt, Argon2, PBKDF2

            // Enum como string para legibilidad en BD
            builder.Property(u => u.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            // Propiedades de auditoría de BaseEntity
            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.LastModifiedAt)
                .IsRequired(false);

            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
        }
    }
}
