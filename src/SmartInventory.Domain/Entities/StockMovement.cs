using SmartInventory.Domain.Common;
using SmartInventory.Domain.Enums;

namespace SmartInventory.Domain.Entities
{
    /// <summary>
    /// Entidad que representa un movimiento de stock (Kardex).
    /// Implementa el patrón Event Sourcing parcial para trazabilidad total.
    /// </summary>
    /// <remarks>
    /// DISEÑO ARQUITECTÓNICO:
    /// - NUNCA modificamos Product.StockQuantity directamente.
    /// - En su lugar, creamos un StockMovement y el stock se calcula como:
    ///   StockActual = Σ (Entradas) - Σ (Salidas) + Σ (Ajustes)
    /// 
    /// VENTAJAS:
    /// - Auditoría completa: Sabemos exactamente cuándo, quién y por qué cambió el stock.
    /// - Trazabilidad: Cumple con normativas (GDPR, SOX, ISO 9001).
    /// - Debugging: Si el stock está mal, podemos ver TODOS los movimientos.
    /// - Analytics: Podemos calcular rotación de inventario, productos más vendidos, etc.
    /// 
    /// REGLAS DE NEGOCIO:
    /// - Quantity siempre es positivo. El tipo de movimiento determina si suma o resta.
    /// - CreatedBy es obligatorio: SIEMPRE sabemos quién hizo el cambio.
    /// - Reason es opcional para Purchase/Sale (puede venir de factura), pero
    ///   obligatorio para Adjustment (debe explicar por qué se corrige).
    /// </remarks>
    public sealed class StockMovement : BaseEntity
    {
        /// <summary>
        /// ID del producto relacionado.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Propiedad de navegación hacia el producto.
        /// </summary>
        /// <remarks>
        /// EF Core cargará automáticamente esta relación con .Include(x => x.Product).
        /// </remarks>
        public Product Product { get; set; } = null!;

        /// <summary>
        /// Cantidad de unidades en el movimiento.
        /// </summary>
        /// <remarks>
        /// IMPORTANTE: Este valor SIEMPRE es positivo.
        /// - Si Type = Purchase → suma al stock.
        /// - Si Type = Sale → resta del stock.
        /// - Si Type = Adjustment → puede sumar o restar dependiendo del stock actual vs cantidad deseada.
        /// 
        /// Ejemplo:
        /// - Compra de 100 unidades: Quantity = 100, Type = Purchase
        /// - Venta de 50 unidades: Quantity = 50, Type = Sale
        /// - Ajuste a 75 unidades (stock actual 80): Quantity = 5, Type = Adjustment (resta implícita)
        /// </remarks>
        public int Quantity { get; set; }

        /// <summary>
        /// Tipo de movimiento: Entrada, Salida o Ajuste.
        /// </summary>
        public MovementType Type { get; set; }

        /// <summary>
        /// Razón o motivo del movimiento.
        /// </summary>
        /// <remarks>
        /// OBLIGATORIO para Type = Adjustment (auditoría).
        /// OPCIONAL para Purchase/Sale (puede venir del sistema de facturación).
        /// 
        /// Ejemplos:
        /// - "Compra factura #12345"
        /// - "Venta orden #ORD-001"
        /// - "Ajuste por conteo físico - faltante detectado"
        /// - "Merma por producto dañado"
        /// - "Corrección error de carga inicial"
        /// </remarks>
        public string? Reason { get; set; }

        /// <summary>
        /// ID del usuario que realizó el movimiento.
        /// </summary>
        /// <remarks>
        /// CRÍTICO para auditoría: SIEMPRE debemos saber quién modificó el stock.
        /// En endpoints protegidos, se obtiene de User.FindFirstValue(ClaimTypes.NameIdentifier).
        /// </remarks>
        public int CreatedBy { get; set; }

        // NOTA: No incluimos una propiedad de navegación User aquí para evitar
        // referencias circulares complejas. Si se necesita el nombre del usuario,
        // se puede cargar explícitamente en la capa de aplicación con un join.
    }
}
