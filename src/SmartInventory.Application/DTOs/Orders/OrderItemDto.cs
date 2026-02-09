namespace SmartInventory.Application.DTOs.Orders
{
    /// <summary>
    /// DTO para un item/línea de pedido en la entrada de datos.
    /// </summary>
    /// <remarks>
    /// PROPÓSITO:
    /// Usado en CreateOrderDto para especificar qué productos y en qué cantidad
    /// se incluyen en un nuevo pedido.
    /// 
    /// VALIDACIONES (implementar con FluentValidation):
    /// - ProductId > 0
    /// - Quantity > 0 (no se permiten cantidades negativas o cero)
    /// </remarks>
    public sealed class OrderItemDto
    {
        /// <summary>
        /// ID del producto a incluir en el pedido.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Cantidad de unidades del producto.
        /// </summary>
        public int Quantity { get; set; }
    }
}
