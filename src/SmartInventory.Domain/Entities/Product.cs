using SmartInventory.Domain.Common;

namespace SmartInventory.Domain.Entities
{
    /// <summary>
    /// Entidad que representa un producto en el inventario.
    /// </summary>
    /// <remarks>
    /// CRITERIOS DE DISEÑO DE DOMINIO:
    /// - Sealed: Indica intención final de diseño, mejora rendimiento.
    /// - decimal para Price: En sistemas financieros NUNCA uses float/double.
    ///   Los tipos de punto flotante tienen errores de redondeo que causan
    ///   inconsistencias monetarias. Ejemplo: 0.1 + 0.2 != 0.3 en float.
    /// - SKU: Stock Keeping Unit. Identificador único de producto en logística.
    /// 
    /// FUTURAS CONSIDERACIONES:
    /// - Para multi-moneda, agregar Currency (enum o tabla).
    /// - Para productos con variantes (tallas, colores), considerar modelo Producto-Variante.
    /// - Para tracking de lotes (farmacia, alimentos), agregar BatchNumber y ExpiryDate.
    /// </remarks>
    public sealed class Product : BaseEntity
    {
        /// <summary>
        /// Nombre del producto.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Descripción detallada del producto.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Precio unitario del producto.
        /// </summary>
        /// <remarks>
        /// CRÍTICO: Usar tipo decimal para representar dinero.
        /// - decimal: 128 bits, precisión de 28-29 dígitos, sin errores de redondeo.
        /// - float/double: Punto flotante IEEE 754, tiene errores de redondeo.
        /// 
        /// Ejemplo del problema:
        ///   float price = 0.1f + 0.2f; // Resultado: 0.30000001 (INCORRECTO en finanzas)
        ///   decimal price = 0.1m + 0.2m; // Resultado: 0.3 (CORRECTO)
        /// 
        /// En base de datos PostgreSQL se mapeará a NUMERIC(18,2).
        /// </remarks>
        public decimal Price { get; set; }

        /// <summary>
        /// Stock Keeping Unit - Código único de identificación del producto.
        /// </summary>
        /// <remarks>
        /// SKU es un identificador alfanumérico único usado en sistemas de gestión de inventario
        /// para rastrear productos de manera única. Es independiente del ID de base de datos.
        /// 
        /// Formato típico: Puede incluir categoría, año, número secuencial.
        /// Ejemplo: "ELC-2024-00123" (Electrónica, año 2024, secuencial 123).
        /// 
        /// En EF Core, debe configurarse como índice único:
        ///   modelBuilder.Entity&lt;Product&gt;().HasIndex(p => p.SKU).IsUnique();
        /// 
        /// Diferencia con Código de Barras (Barcode):
        /// - SKU: Interno de la empresa, define el negocio.
        /// - Barcode (UPC/EAN): Estándar global, define el fabricante.
        /// </remarks>
        public string SKU { get; set; } = string.Empty;

        /// <summary>
        /// Cantidad disponible en stock (inventario físico).
        /// </summary>
        /// <remarks>
        /// IMPORTANTE: En sistemas de alta concurrencia (múltiples usuarios comprando simultáneamente),
        /// considera usar Optimistic Concurrency Control (EF Core: [ConcurrencyCheck] o RowVersion)
        /// para evitar race conditions tipo:
        /// 
        /// Usuario A lee Stock=10 → Usuario B lee Stock=10 →
        /// Usuario A compra 5 (Stock=5) → Usuario B compra 8 (Stock=2) ❌ DEBERÍA SER -3
        /// 
        /// Solución: Usar transacciones con nivel de aislamiento REPEATABLE READ o SERIALIZABLE,
        /// o implementar Optimistic Locking con EF Core.
        /// </remarks>
        public int StockQuantity { get; set; }
    }
}
