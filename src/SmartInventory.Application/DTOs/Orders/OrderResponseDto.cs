namespace SmartInventory.Application.DTOs.Orders
{
    /// <summary>
    /// DTO de respuesta con la información completa de un pedido.
    /// </summary>
    /// <remarks>
    /// PATRÓN:
    /// Este DTO representa la proyección optimizada de Order + OrderItems
    /// para enviar al cliente. Incluye joins pre-calculados para evitar N+1 queries.
    /// 
    /// PROPÓSITO:
    /// - Mostrar detalles completos del pedido creado.
    /// - Incluir información desnormalizada (ProductName) para evitar lookups adicionales en el frontend.
    /// </remarks>
    public sealed class OrderResponseDto
    {
        /// <summary>
        /// ID único del pedido.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Fecha y hora de creación del pedido (UTC).
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// Estado actual del pedido (Pending, Paid, Shipped, Cancelled).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Monto total del pedido (suma de todos los items).
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Lista de items incluidos en el pedido con detalles completos.
        /// </summary>
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO de respuesta para un item individual del pedido.
    /// </summary>
    public sealed class OrderItemResponseDto
    {
        /// <summary>
        /// ID del producto.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Nombre del producto (desnormalizado para evitar lookups).
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Cantidad de unidades compradas.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Precio unitario en el momento de la compra (snapshot).
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Total para este item (Quantity * UnitPrice).
        /// </summary>
        public decimal Total { get; set; }
    }
}
