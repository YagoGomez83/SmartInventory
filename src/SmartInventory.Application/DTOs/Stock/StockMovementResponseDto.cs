using SmartInventory.Domain.Enums;

namespace SmartInventory.Application.DTOs.Stock
{
    /// <summary>
    /// DTO de respuesta tras registrar un movimiento de inventario.
    /// </summary>
    /// <remarks>
    /// PROPÓSITO:
    /// - Confirmar al cliente que el movimiento se registró exitosamente.
    /// - Proporcionar el nuevo stock actualizado para sincronización con el frontend.
    /// - Incluir el ID del movimiento para referencia y auditoría.
    /// 
    /// EJEMPLO DE RESPUESTA API:
    /// {
    ///   "movementId": 42,
    ///   "productId": 1,
    ///   "productName": "Laptop Dell XPS 15",
    ///   "previousStock": 100,
    ///   "newStock": 110,
    ///   "quantityChanged": 10,
    ///   "movementType": "Purchase",
    ///   "reason": "Compra a proveedor ABC",
    ///   "createdAt": "2024-01-15T10:30:00Z",
    ///   "createdByUserId": 5
    /// }
    /// 
    /// USO EN FRONTEND (React):
    /// const response = await fetch('/api/stock/adjustment', { ... });
    /// const data = await response.json();
    /// console.log(`Nuevo stock: ${data.newStock}`);
    /// </remarks>
    /// <param name="MovementId">ID del movimiento de stock registrado.</param>
    /// <param name="ProductId">ID del producto afectado.</param>
    /// <param name="ProductName">Nombre del producto (para confirmación visual).</param>
    /// <param name="PreviousStock">Stock anterior antes del movimiento.</param>
    /// <param name="NewStock">Stock actualizado después del movimiento.</param>
    /// <param name="QuantityChanged">Cantidad de unidades modificadas (siempre positivo).</param>
    /// <param name="MovementType">Tipo de movimiento realizado: Purchase, Sale, Adjustment.</param>
    /// <param name="Reason">Razón del movimiento de inventario.</param>
    /// <param name="CreatedAt">Fecha y hora del movimiento.</param>
    /// <param name="CreatedByUserId">ID del usuario que realizó el movimiento.</param>
    public record StockMovementResponseDto(
        int MovementId,
        int ProductId,
        string ProductName,
        int PreviousStock,
        int NewStock,
        int QuantityChanged,
        MovementType MovementType,
        string Reason,
        DateTime CreatedAt,
        int CreatedByUserId
    );
}
