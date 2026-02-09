using SmartInventory.Application.DTOs.Orders;

namespace SmartInventory.Application.Interfaces
{
    /// <summary>
    /// Contrato del servicio de gestión de pedidos.
    /// </summary>
    /// <remarks>
    /// RESPONSABILIDAD:
    /// - Orquestar la creación de pedidos con lógica transaccional compleja.
    /// - Coordinar entre múltiples repositorios (Orders, Products, Stock).
    /// - Garantizar consistencia de datos (transacciones ACID).
    /// 
    /// REGLAS DE NEGOCIO CRÍTICAS:
    /// - Validar stock disponible antes de crear el pedido.
    /// - Registrar movimientos de stock (StockMovement tipo 'Sale').
    /// - Capturar precios actuales (snapshot) en OrderItems.
    /// - TODO: En producción, atomicidad en transacciones distribuidas.
    /// 
    /// PATRÓN DE DISEÑO:
    /// - Interfaz en Application (Clean Architecture).
    /// - Implementación también en Application (no en Infrastructure).
    /// - Esto permite testing sin dependencias de BD.
    /// </remarks>
    public interface IOrderService
    {
        /// <summary>
        /// Crea un nuevo pedido con validación de stock y registro de movimientos.
        /// </summary>
        /// <param name="dto">Datos del pedido a crear (items con ProductId y Quantity).</param>
        /// <param name="userId">ID del usuario que realiza el pedido (desde token JWT).</param>
        /// <param name="cancellationToken">Token de cancelación para operaciones asíncronas.</param>
        /// <returns>DTO con los detalles completos del pedido creado.</returns>
        /// <exception cref="ArgumentNullException">Si dto es null.</exception>
        /// <exception cref="KeyNotFoundException">Si algún producto no existe.</exception>
        /// <exception cref="InvalidOperationException">Si no hay stock suficiente para algún producto.</exception>
        /// <remarks>
        /// TRANSACCIONALIDAD:
        /// Este método ejecuta múltiples operaciones en una transacción explícita:
        /// 1. Crear Order
        /// 2. Validar y restar stock de cada producto
        /// 3. Registrar StockMovement tipo 'Sale' por cada item
        /// 4. Crear OrderItems con precio actual
        /// 
        /// Si cualquier paso falla, se hace ROLLBACK automático.
        /// </remarks>
        Task<OrderResponseDto> CreateOrderAsync(
            CreateOrderDto dto,
            int userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene todos los pedidos de un usuario con paginación.
        /// </summary>
        /// <param name="userId">ID del usuario propietario de los pedidos.</param>
        /// <param name="page">Número de página (1-based, default: 1).</param>
        /// <param name="pageSize">Cantidad de registros por página (default: 10).</param>
        /// <param name="cancellationToken">Token de cancelación para operaciones asíncronas.</param>
        /// <returns>Lista de pedidos del usuario con sus items.</returns>
        /// <remarks>
        /// SEGURIDAD:
        /// Solo retorna pedidos del usuario especificado (filtro obligatorio).
        /// No permite ver pedidos de otros usuarios (IDOR prevention).
        /// 
        /// PAGINACIÓN:
        /// Implementa paginación para evitar cargar miles de pedidos de usuarios con mucha actividad.
        /// </remarks>
        Task<IEnumerable<OrderResponseDto>> GetMyOrdersAsync(
            int userId,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un pedido por su ID con validación de ownership.
        /// </summary>
        /// <param name="orderId">ID del pedido a obtener.</param>
        /// <param name="userId">ID del usuario que solicita el pedido (desde token JWT).</param>
        /// <param name="cancellationToken">Token de cancelación para operaciones asíncronas.</param>
        /// <returns>DTO con los detalles completos del pedido.</returns>
        /// <exception cref="KeyNotFoundException">Si el pedido no existe.</exception>
        /// <exception cref="UnauthorizedAccessException">Si el pedido no pertenece al usuario.</exception>
        /// <remarks>
        /// VALIDACIÓN DE SEGURIDAD CRÍTICA:
        /// Verifica que order.UserId == userId para prevenir IDOR (Insecure Direct Object Reference).
        /// Si un usuario intenta acceder al pedido de otro, lanza UnauthorizedAccessException.
        /// 
        /// EJEMPLO DE ATAQUE IDOR:
        /// Usuario A (ID=5) intenta acceder GET /api/orders/123.
        /// Si el Order #123 pertenece al Usuario B (ID=8), debe retornar 403 Forbidden.
        /// </remarks>
        Task<OrderResponseDto> GetOrderByIdAsync(
            int orderId,
            int userId,
            CancellationToken cancellationToken = default);
    }
}
