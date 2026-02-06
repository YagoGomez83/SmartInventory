using SmartInventory.Domain.Enums;

namespace SmartInventory.Application.DTOs.Stock
{
    /// <summary>
    /// DTO para el ajuste de inventario (entrada o salida de productos).
    /// </summary>
    /// <remarks>
    /// PROPÓSITO:
    /// - Permite registrar movimientos de inventario (compras, ventas, ajustes).
    /// - Cada movimiento afecta el stock del producto y genera un registro en StockMovement.
    /// 
    /// VALIDACIÓN (a implementar con FluentValidation):
    /// - ProductId: Requerido, debe existir en la base de datos.
    /// - Quantity: Requerido, debe ser mayor que 0.
    /// - Type: Requerido, valores válidos: Purchase, Sale, Adjustment.
    /// - Reason: Requerido, longitud mínima 3, máxima 500 caracteres.
    /// 
    /// LÓGICA DE NEGOCIO:
    /// - Si Type = Purchase o Adjustment (entrada): StockQuantity += Quantity.
    /// - Si Type = Sale o Adjustment (salida): StockQuantity -= Quantity.
    /// - No se permite que StockQuantity sea negativo (validar en servicio).
    /// 
    /// EJEMPLO DE USO EN FRONTEND (React):
    /// const adjustment = {
    ///   productId: 1,
    ///   quantity: 10,
    ///   type: "Purchase", // o "Sale" o "Adjustment"
    ///   reason: "Compra a proveedor ABC"
    /// };
    /// 
    /// await fetch('/api/stock/adjustment', {
    ///   method: 'POST',
    ///   headers: { 'Content-Type': 'application/json' },
    ///   body: JSON.stringify(adjustment)
    /// });
    /// </remarks>
    /// <param name="ProductId">ID del producto al que se le ajustará el inventario.</param>
    /// <param name="Quantity">Cantidad de productos a agregar o restar del inventario.</param>
    /// <param name="Type">Tipo de movimiento: Purchase (entrada), Sale (salida), Adjustment (corrección).</param>
    /// <param name="Reason">Descripción detallada del motivo del movimiento de inventario.</param>
    public record StockAdjustmentDto(
        int ProductId,
        int Quantity,
        MovementType Type,
        string Reason
    );
}
