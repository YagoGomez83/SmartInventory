using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;

namespace SmartInventory.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuración de Entity Framework Core para la entidad Order.
    /// </summary>
    /// <remarks>
    /// RELACIONES CONFIGURADAS:
    /// 1. Order -> OrderItems (One-to-Many con Cascade Delete):
    ///    - Al borrar un Order, se borran automáticamente todos sus OrderItems.
    ///    - Mantiene integridad referencial del patrón Maestro-Detalle.
    /// 
    /// 2. Order -> User (Many-to-One con Restrict Delete):
    ///    - NO permite borrar un User si tiene Orders asociadas.
    ///    - Preserva historial de pedidos para auditoría y reportes.
    /// 
    /// CONFIGURACIONES FINANCIERAS:
    /// - TotalAmount: Mapeado a NUMERIC(18,2) para evitar errores de redondeo.
    ///   - 18 dígitos totales (precisión).
    ///   - 2 decimales (escala).
    /// 
    /// CONVERSIÓN DE ENUMS:
    /// - Status: Se persiste como string en BD (legibilidad en queries SQL).
    ///   - Alternativa: int (más eficiente, pero menos legible).
    ///   - Mejor práctica: string para enums que cambian raramente.
    /// </remarks>
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // Nombre de tabla
            builder.ToTable("Orders");

            // Clave primaria
            builder.HasKey(o => o.Id);

            // Propiedades escalares
            builder.Property(o => o.UserId)
                .IsRequired()
                .HasComment("ID del usuario que realizó el pedido.");

            builder.Property(o => o.OrderDate)
                .IsRequired()
                .HasComment("Fecha y hora de creación del pedido (UTC).");

            // Configuración crítica para valores monetarios
            builder.Property(o => o.TotalAmount)
                .IsRequired()
                .HasPrecision(18, 2) // NUMERIC(18,2) en PostgreSQL
                .HasComment("Monto total del pedido. Usa NUMERIC para evitar errores de redondeo.");

            // Conversión de enum a string
            builder.Property(o => o.Status)
                .IsRequired()
                .HasConversion(
                    v => v.ToString(),
                    v => (OrderStatus)Enum.Parse(typeof(OrderStatus), v))
                .HasMaxLength(50)
                .HasComment("Estado del pedido: Pending, Paid, Shipped, Completed, Cancelled.");

            // Propiedades de auditoría (heredadas de BaseEntity)
            builder.Property(o => o.CreatedAt)
                .IsRequired()
                .HasComment("Fecha de creación del registro.");

            builder.Property(o => o.LastModifiedAt)
                .HasComment("Fecha de última actualización del registro.");

            builder.Property(o => o.IsActive)
                .IsRequired()
                .HasDefaultValue(true)
                .HasComment("Indica si el registro está activo (soft delete).");

            // RELACIÓN: Order -> User (Many-to-One)
            // DeleteBehavior.Restrict: NO permitir borrar User si tiene Orders
            builder.HasOne(o => o.User)
                .WithMany() // No configuramos navegación inversa en User
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Protege historial de pedidos

            // RELACIÓN: Order -> OrderItems (One-to-Many)
            // DeleteBehavior.Cascade: Al borrar Order, se borran automáticamente los OrderItems
            builder.HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // Borrado en cascada de items

            // Índices para optimización de queries
            builder.HasIndex(o => o.UserId)
                .HasDatabaseName("IX_Orders_UserId");

            builder.HasIndex(o => o.OrderDate)
                .HasDatabaseName("IX_Orders_OrderDate");

            builder.HasIndex(o => o.Status)
                .HasDatabaseName("IX_Orders_Status");

            // Índice compuesto para queries comunes: buscar pedidos de un usuario por fecha
            builder.HasIndex(o => new { o.UserId, o.OrderDate })
                .HasDatabaseName("IX_Orders_UserId_OrderDate");
        }
    }
}
