using SmartInventory.Domain.Entities;

namespace SmartInventory.Domain.Interfaces
{
    /// <summary>
    /// Contrato de repositorio para gestión de pedidos (Orders).
    /// </summary>
    /// <remarks>
    /// PATRÓN REPOSITORIO:
    /// Abstrae la lógica de acceso a datos. El dominio no depende de Entity Framework.
    /// Beneficio: Testeable (mocks), agnóstico de ORM (cambiar EF por Dapper), SOLID (DIP).
    /// 
    /// EAGER LOADING OBLIGATORIO:
    /// GetByIdAsync DEBE incluir .Include(o => o.OrderItems) y .Include(o => o.User).
    /// Razón: Cargar master sin detalles viola el patrón Aggregate Root.
    /// Sin eager loading, tendríamos Lazy Loading N+1 (1 query por el Order + N queries por cada OrderItem).
    /// 
    /// PAGINACIÓN:
    /// GetAllAsync implementa paginación para evitar timeouts con millones de registros.
    /// Escenario: Amazon tiene +200M pedidos. Sin paginación, una query devuelve gigabytes.
    /// </remarks>
    public interface IOrderRepository
    {
        /// <summary>
        /// Obtiene un pedido por su ID, incluyendo sus items y usuario.
        /// </summary>
        /// <param name="id">ID del pedido.</param>
        /// <returns>Orden con OrderItems y User cargados, o null si no existe.</returns>
        /// <remarks>
        /// IMPLEMENTACIÓN EF CORE:
        /// return await _context.Orders
        ///     .Include(o => o.OrderItems)
        ///         .ThenInclude(oi => oi.Product) // Para mostrar nombre del producto
        ///     .Include(o => o.User)
        ///     .AsNoTracking() // Read-only, mejor performance
        ///     .FirstOrDefaultAsync(o => o.Id == id);
        /// </remarks>
        Task<Order?> GetByIdAsync(int id);

        /// <summary>
        /// Obtiene pedidos de un usuario con paginación.
        /// </summary>
        /// <param name="userId">ID del usuario propietario de los pedidos.</param>
        /// <param name="page">Número de página (1-based).</param>
        /// <param name="pageSize">Cantidad de pedidos por página (default: 10).</param>
        /// <returns>Lista de pedidos con OrderItems incluidos.</returns>
        /// <remarks>
        /// FILTRO DE SEGURIDAD:
        /// userId es obligatorio. Un usuario solo puede ver SUS pedidos.
        /// Admin puede ver todos pasando userId = 0 (lógica a implementar en la capa de servicio).
        /// 
        /// ORDENAMIENTO:
        /// Debe ordenar por OrderDate DESC (pedidos recientes primero).
        /// 
        /// IMPLEMENTACIÓN:
        /// .Skip((page - 1) * pageSize).Take(pageSize)
        /// </remarks>
        Task<IEnumerable<Order>> GetAllAsync(int userId, int page = 1, int pageSize = 10);

        /// <summary>
        /// Crea un nuevo pedido con sus items.
        /// </summary>
        /// <param name="order">Orden a crear (con colección OrderItems poblada).</param>
        /// <returns>Orden creada con IDs asignados.</returns>
        /// <remarks>
        /// TRANSACCIÓN IMPLÍCITA:
        /// EF Core persiste Order + OrderItems en una sola transacción.
        /// Si falla al insertar un OrderItem, se hace rollback del Order (atomicidad ACID).
        /// 
        /// IMPORTANTE:
        /// Antes de llamar AddAsync, el servicio DEBE:
        /// 1. Validar stock disponible (Product.Stock >= OrderItem.Quantity).
        /// 2. Crear StockMovement de tipo 'Reserved' o 'Sale'.
        /// 3. Llamar order.CalculateTotal().
        /// </remarks>
        Task<Order> AddAsync(Order order);

        /// <summary>
        /// Actualiza el estado de un pedido.
        /// </summary>
        /// <param name="order">Orden con estado modificado.</param>
        /// <returns>Tarea completada.</returns>
        /// <remarks>
        /// CASOS DE USO:
        /// - Pending -> Paid: Cuando se confirma el pago.
        /// - Paid -> Shipped: Cuando el courier recoge el paquete.
        /// - Pending/Paid -> Cancelled: Si el usuario cancela o el pago es rechazado.
        /// 
        /// REGLA DE NEGOCIO:
        /// Si se cancela (OrderStatus.Cancelled), debe crearse un StockMovement de tipo 'Return'
        /// para devolver el inventario. Esto se hace en el OrderService, NO en el repositorio.
        /// </remarks>
        Task UpdateAsync(Order order);

        /// <summary>
        /// Obtiene pedidos pendientes creados hace más de X minutos (para expiración).
        /// </summary>
        /// <param name="minutesOld">Minutos desde la creación.</param>
        /// <returns>Lista de pedidos Pending expirados.</returns>
        /// <remarks>
        /// BACKGROUND JOB:
        /// Un job (Hangfire, Quartz) ejecuta esto cada 5 minutos:
        /// var expiredOrders = await _orderRepo.GetExpiredPendingOrdersAsync(15);
        /// foreach (var order in expiredOrders)
        /// {
        ///     order.Status = OrderStatus.Cancelled;
        ///     // Devolver stock...
        /// }
        /// 
        /// QUERY:
        /// WHERE Status = Pending AND OrderDate < DATEADD(MINUTE, -15, GETUTCDATE())
        /// </remarks>
        Task<IEnumerable<Order>> GetExpiredPendingOrdersAsync(int minutesOld);
    }
}
