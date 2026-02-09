using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInventory.Domain.Entities;

namespace SmartInventory.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuración de Entity Framework Core para la entidad OrderItem.
    /// </summary>
    /// <remarks>
    /// PROPIEDAD CALCULADA:
    /// - Total: Se ignora en el mapeo porque es una propiedad computed (Quantity * UnitPrice).
    ///   - EF Core no la persistirá en BD.
    ///   - Se calcula en runtime cada vez que se accede.
    ///   - Alternativa: Usar HasComputedColumnSql() para columna calculada en BD.
    /// 
    /// RELACIÓN:
    /// - OrderItem -> Product (Many-to-One con Restrict Delete):
    ///   - NO permite borrar un Product si está referenciado en OrderItems.
    ///   - Preserva historial de ventas para auditoría e informes.
    /// 
    /// CONFIGURACIONES FINANCIERAS:
    /// - UnitPrice: Mapeado a NUMERIC(18,2) para evitar errores de redondeo.
    ///   - Este es el "snapshot" del precio en el momento de la compra.
    ///   - NO debe cambiar aunque Product.Price cambie.
    /// </remarks>
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            // Nombre de tabla
            builder.ToTable("OrderItems");

            // Clave primaria
            builder.HasKey(oi => oi.Id);

            // Propiedades escalares
            builder.Property(oi => oi.OrderId)
                .IsRequired()
                .HasComment("ID del pedido al que pertenece este item.");

            builder.Property(oi => oi.ProductId)
                .IsRequired()
                .HasComment("ID del producto que se está comprando.");

            builder.Property(oi => oi.Quantity)
                .IsRequired()
                .HasComment("Cantidad de unidades compradas. Debe ser > 0.");

            // Configuración crítica para valores monetarios
            builder.Property(oi => oi.UnitPrice)
                .IsRequired()
                .HasPrecision(18, 2) // NUMERIC(18,2) en PostgreSQL
                .HasComment("Precio unitario CONGELADO en el momento de la compra (snapshot).");

            // Ignorar propiedad calculada Total
            // EF Core no creará una columna para esto en la BD
            builder.Ignore(oi => oi.Total);

            // Propiedades de auditoría (heredadas de BaseEntity)
            builder.Property(oi => oi.CreatedAt)
                .IsRequired()
                .HasComment("Fecha de creación del registro.");

            builder.Property(oi => oi.LastModifiedAt)
                .HasComment("Fecha de última actualización del registro.");

            builder.Property(oi => oi.IsActive)
                .IsRequired()
                .HasDefaultValue(true)
                .HasComment("Indica si el registro está activo (soft delete).");

            // RELACIÓN: OrderItem -> Order (Many-to-One)
            // Ya configurada en OrderConfiguration.cs (lado principal de la relación)

            // RELACIÓN: OrderItem -> Product (Many-to-One)
            // DeleteBehavior.Restrict: NO permitir borrar Product si está en OrderItems
            builder.HasOne(oi => oi.Product)
                .WithMany() // No configuramos navegación inversa en Product
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Protege historial de ventas

            // Índices para optimización de queries
            builder.HasIndex(oi => oi.OrderId)
                .HasDatabaseName("IX_OrderItems_OrderId");

            builder.HasIndex(oi => oi.ProductId)
                .HasDatabaseName("IX_OrderItems_ProductId");

            // Índice compuesto para auditoría: items de un producto en un pedido
            builder.HasIndex(oi => new { oi.OrderId, oi.ProductId })
                .HasDatabaseName("IX_OrderItems_OrderId_ProductId");
        }
    }
}
