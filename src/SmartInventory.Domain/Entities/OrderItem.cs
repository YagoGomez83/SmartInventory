using SmartInventory.Domain.Common;

namespace SmartInventory.Domain.Entities
{
    /// <summary>
    /// Entidad que representa una línea/item de un pedido (detalle).
    /// </summary>
    /// <remarks>
    /// PATRÓN MAESTRO-DETALLE:
    /// OrderItem es el "Detalle". No puede existir sin un Order (Maestro).
    /// Relación: Order 1 ----> N OrderItems.
    /// 
    /// SNAPSHOTTING DE PRECIOS (Regla de Oro del E-commerce):
    /// UnitPrice NO es una FK al precio actual del Product. Es una COPIA CONGELADA
    /// del precio en el momento de la compra. Razones:
    /// 1. Auditoría: "¿A qué precio vendimos?". Necesario para contabilidad/impuestos.
    /// 2. Historial: Si Product.Price cambia mañana, los pedidos antiguos no deben mutar.
    /// 3. Cumplimiento: Facturas electrónicas deben mostrar precios originales (SAT México, AFIP Argentina).
    /// 
    /// OPTIMIZACIÓN:
    /// - Total como Computed Property: Evita redundancia en BD. EF Core puede mapear con .Property().HasComputedColumnSql().
    /// - sealed: Rendimiento (devirtualización de llamadas).
    /// </remarks>
    public sealed class OrderItem : BaseEntity
    {
        /// <summary>
        /// ID del pedido al que pertenece este item.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Navegación al pedido maestro (required en EF Core 8+).
        /// </summary>
        public Order Order { get; set; } = null!;

        /// <summary>
        /// ID del producto que se está comprando.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Navegación al producto (para joins eficientes).
        /// </summary>
        public Product Product { get; set; } = null!;

        /// <summary>
        /// Cantidad de unidades compradas.
        /// </summary>
        /// <remarks>
        /// VALIDACIÓN: Debe ser > 0. FluentValidation debe rechazar 0 o negativos.
        /// </remarks>
        public int Quantity { get; set; }

        /// <summary>
        /// Precio unitario en el momento de la compra (SNAPSHOT).
        /// </summary>
        /// <remarks>
        /// NO usar Product.Price directamente. Este valor es inmutable después de crear el pedido.
        /// </remarks>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Subtotal de la línea (Quantity * UnitPrice).
        /// </summary>
        /// <remarks>
        /// Propiedad calculada. No se persiste en BD (será una columna computada o se calcula en runtime).
        /// </remarks>
        public decimal Total => Quantity * UnitPrice;
    }
}
