using SmartInventory.Application.DTOs.Stock;

namespace SmartInventory.Application.Interfaces
{
    /// <summary>
    /// Contrato del servicio de gestión de inventario.
    /// </summary>
    /// <remarks>
    /// RESPONSABILIDAD:
    /// - Orquestar las operaciones de ajuste de stock (entrada/salida de productos).
    /// - Validar reglas de negocio CRÍTICAS:
    ///   * El producto debe existir.
    ///   * El stock NUNCA puede ser negativo.
    ///   * Cada movimiento debe registrarse para auditoría completa.
    /// 
    /// PATRÓN DE DISEÑO:
    /// - Capa de Aplicación en Clean Architecture.
    /// - Coordina entre Repositorios (Infraestructura) y Controladores (API).
    /// - NO contiene lógica de base de datos, solo lógica de negocio.
    /// 
    /// CASOS DE USO:
    /// 1. Compra de mercancía a proveedor → AdjustStockAsync (Purchase).
    /// 2. Venta de producto a cliente → AdjustStockAsync (Sale).
    /// 3. Corrección por inventario físico → AdjustStockAsync (Adjustment).
    /// 
    /// SEGURIDAD:
    /// - userId es obligatorio para trazabilidad (auditoría de quién hizo cada cambio).
    /// - En sistemas enterprise, esto es requisito de ISO 9001, SOX, GDPR.
    /// </remarks>
    public interface IStockService
    {
        /// <summary>
        /// Ajusta el stock de un producto (entrada o salida) y registra el movimiento.
        /// </summary>
        /// <param name="dto">Datos del ajuste de stock (ProductId, Quantity, Type, Reason).</param>
        /// <param name="userId">ID del usuario que realiza el movimiento (para auditoría).</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>Información del movimiento registrado y el nuevo stock.</returns>
        /// <exception cref="KeyNotFoundException">Si el producto no existe.</exception>
        /// <exception cref="InvalidOperationException">Si el stock resultante sería negativo.</exception>
        /// <remarks>
        /// LÓGICA DE CÁLCULO:
        /// - Purchase (Entrada): NuevoStock = StockActual + Quantity
        /// - Sale (Salida): NuevoStock = StockActual - Quantity
        /// - Adjustment (Ajuste): NuevoStock = StockActual + Quantity (por ahora, entrada positiva)
        /// 
        /// VALIDACIÓN CRÍTICA:
        /// - Si NuevoStock &lt; 0 → lanza InvalidOperationException.
        /// 
        /// PASOS DE EJECUCIÓN:
        /// 1. Obtener producto por ID (lanza KeyNotFoundException si no existe).
        /// 2. Calcular nuevo stock según el tipo de movimiento.
        /// 3. Validar que el nuevo stock no sea negativo.
        /// 4. Actualizar Product.StockQuantity.
        /// 5. Crear y registrar StockMovement para auditoría.
        /// 6. Retornar DTO con confirmación y nuevo stock.
        /// 
        /// EJEMPLO DE LLAMADA:
        /// var adjustment = new StockAdjustmentDto(
        ///     ProductId: 1,
        ///     Quantity: 10,
        ///     Type: MovementType.Purchase,
        ///     Reason: "Compra a proveedor ABC"
        /// );
        /// var result = await stockService.AdjustStockAsync(adjustment, currentUserId);
        /// Console.WriteLine($"Nuevo stock: {result.NewStock}");
        /// </remarks>
        Task<StockMovementResponseDto> AdjustStockAsync(
            StockAdjustmentDto dto,
            int userId,
            CancellationToken cancellationToken = default);
    }
}
