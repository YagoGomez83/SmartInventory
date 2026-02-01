using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInventory.Domain.Entities;

namespace SmartInventory.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuración de Entity Framework Core para la entidad Product.
    /// </summary>
    /// <remarks>
    /// CONFIGURACIONES FINANCIERAS:
    /// - Price: Se mapea a NUMERIC(18,2) en PostgreSQL.
    ///   - 18 dígitos totales (precisión).
    ///   - 2 decimales (escala).
    ///   - Ejemplo: Hasta 9,999,999,999,999,999.99
    /// 
    /// ÍNDICES:
    /// - SKU: Índice único para evitar duplicados.
    /// - Name: Índice no único para búsquedas rápidas por nombre.
    /// 
    /// CONSIDERACIONES FUTURAS:
    /// - Para multi-tenancy, agregar índice compuesto (TenantId, SKU).
    /// - Para soft deletes con reúso de SKU, índice condicional: WHERE IsDeleted = false.
    /// </remarks>
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            // Nombre de tabla
            builder.ToTable("Products");

            // Clave primaria
            builder.HasKey(p => p.Id);

            // Propiedades
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(1000);

            // Configuración crítica para valores monetarios
            builder.Property(p => p.Price)
                .IsRequired()
                .HasPrecision(18, 2) // NUMERIC(18,2) en PostgreSQL
                .HasComment("Precio unitario del producto. Usa NUMERIC para evitar errores de redondeo.");

            builder.Property(p => p.SKU)
                .IsRequired()
                .HasMaxLength(50);

            // Índice único en SKU
            builder.HasIndex(p => p.SKU)
                .IsUnique()
                .HasDatabaseName("IX_Products_SKU");

            // Índice no único en Name para búsquedas
            builder.HasIndex(p => p.Name)
                .HasDatabaseName("IX_Products_Name");

            builder.Property(p => p.StockQuantity)
                .IsRequired()
                .HasDefaultValue(0);

            // Propiedades de auditoría de BaseEntity
            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.LastModifiedAt)
                .IsRequired(false);

            builder.Property(p => p.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
        }
    }
}
