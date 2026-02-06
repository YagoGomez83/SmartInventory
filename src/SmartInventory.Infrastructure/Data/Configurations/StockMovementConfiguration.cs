using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;

namespace SmartInventory.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuración de Entity Framework Core para la entidad StockMovement.
    /// </summary>
    /// <remarks>
    /// DISEÑO DE RELACIONES:
    /// - Un Product tiene muchos StockMovements (1:N).
    /// - DeleteBehavior.Restrict: CRÍTICO - No permitir borrado de producto si tiene movimientos.
    ///   Esto preserva el historial completo de auditoría (Event Sourcing parcial).
    /// 
    /// CONVERSIÓN DE ENUMS:
    /// - MovementType se guarda como string en BD para:
    ///   1. Legibilidad: Al hacer SELECT vemos "Purchase"/"Sale"/"Adjustment" en lugar de 0/1/2.
    ///   2. Refactoring seguro: Si reordenamos el enum, la BD no se corrompe.
    ///   3. Interoperabilidad: Otras apps pueden leer los datos sin conocer el enum.
    /// 
    /// AUDITORÍA:
    /// - CreatedBy es obligatorio: SIEMPRE sabemos quién hizo el cambio.
    /// - Quantity es obligatorio: No puede haber movimientos sin cantidad.
    /// - Type es obligatorio: Debe especificarse el tipo de movimiento.
    /// 
    /// ÍNDICES:
    /// - ProductId: Para consultas frecuentes de historial de producto.
    /// - CreatedAt: Para consultas de movimientos por fecha.
    /// - Type: Para filtrar por tipo de movimiento (reportes de ventas/compras).
    /// </remarks>
    public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
    {
        public void Configure(EntityTypeBuilder<StockMovement> builder)
        {
            // Nombre de tabla
            builder.ToTable("StockMovements");

            // Clave primaria
            builder.HasKey(sm => sm.Id);

            // Configuración de relación con Product (1:N)
            builder.HasOne(sm => sm.Product)
                .WithMany(p => p.StockMovements) // Product tiene colección de StockMovements navegable
                .HasForeignKey(sm => sm.ProductId)
                .OnDelete(DeleteBehavior.Restrict) // IMPORTANTE: No borrar historial si se borra producto
                .HasConstraintName("FK_StockMovements_Products");

            // Propiedades requeridas
            builder.Property(sm => sm.Quantity)
                .IsRequired()
                .HasComment("Cantidad de unidades en el movimiento. Siempre positivo.");

            builder.Property(sm => sm.CreatedBy)
                .IsRequired()
                .HasComment("ID del usuario que realizó el movimiento. Obligatorio para auditoría.");

            // Conversión de enum a string
            builder.Property(sm => sm.Type)
                .IsRequired()
                .HasConversion<string>() // Guarda "Purchase", "Sale", "Adjustment" en lugar de 0, 1, 2
                .HasMaxLength(50)
                .HasComment("Tipo de movimiento: Purchase (entrada), Sale (salida), Adjustment (ajuste).");

            // Propiedades opcionales
            builder.Property(sm => sm.Reason)
                .IsRequired(false)
                .HasMaxLength(500)
                .HasComment("Razón o motivo del movimiento. Obligatorio para ajustes, opcional para compras/ventas.");

            // Propiedades de auditoría de BaseEntity
            builder.Property(sm => sm.CreatedAt)
                .IsRequired()
                .HasComment("Fecha UTC de creación del movimiento.");

            builder.Property(sm => sm.LastModifiedAt)
                .IsRequired(false);

            builder.Property(sm => sm.IsActive)
                .IsRequired()
                .HasDefaultValue(true)
                .HasComment("Indica si el registro está activo (soft delete).");

            // Índices para optimizar consultas comunes
            builder.HasIndex(sm => sm.ProductId)
                .HasDatabaseName("IX_StockMovements_ProductId")
                .HasFilter(null); // Sin filtro, incluye todos los registros

            builder.HasIndex(sm => sm.CreatedAt)
                .HasDatabaseName("IX_StockMovements_CreatedAt");

            builder.HasIndex(sm => sm.Type)
                .HasDatabaseName("IX_StockMovements_Type");

            // Índice compuesto para consultas por producto y fecha
            builder.HasIndex(sm => new { sm.ProductId, sm.CreatedAt })
                .HasDatabaseName("IX_StockMovements_ProductId_CreatedAt");
        }
    }
}
