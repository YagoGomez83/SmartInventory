namespace SmartInventory.Application.DTOs.Orders
{
    /// <summary>
    /// DTO para crear un nuevo pedido.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION:
    /// UserId NO está en el DTO porque se obtiene del token JWT (ClaimsPrincipal).
    /// Esto previene ataques de "Insecure Direct Object Reference" (IDOR):
    /// Un usuario malicioso no puede crear pedidos a nombre de otros.
    /// 
    /// VALIDACIONES (implementar con FluentValidation):
    /// - Items no puede ser null o vacío
    /// - Cada item debe tener ProductId > 0 y Quantity > 0
    /// </remarks>
    public sealed class CreateOrderDto
    {
        /// <summary>
        /// Lista de items/productos a incluir en el pedido.
        /// </summary>
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
