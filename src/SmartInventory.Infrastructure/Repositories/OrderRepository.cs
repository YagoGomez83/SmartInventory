using Microsoft.EntityFrameworkCore;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;
using SmartInventory.Domain.Interfaces;
using SmartInventory.Infrastructure.Data;

namespace SmartInventory.Infrastructure.Repositories
{
    /// <summary>
    /// Implementación del repositorio de pedidos utilizando Entity Framework Core.
    /// </summary>
    /// <remarks>
    /// MEJORES PRÁCTICAS IMPLEMENTADAS:
    /// 
    /// 1. Eager Loading con Include/ThenInclude:
    ///    - GetByIdAsync incluye OrderItems + Product + User.
    ///    - GetAllAsync incluye OrderItems + Product para mostrar detalles.
    ///    - Evita N+1 queries (1 query maestro + N queries por cada detalle).
    /// 
    /// 2. AsNoTracking() en lecturas:
    ///    - Usado en GetByIdAsync y GetAllAsync.
    ///    - Mejora el rendimiento al no cargar entidades en el change tracker.
    ///    - NO usado en UpdateAsync porque necesitamos tracking.
    /// 
    /// 3. AddAsync con Cascade:
    ///    - Solo agrega la Order al contexto.
    ///    - EF Core detecta OrderItems por la relación y los inserta automáticamente.
    ///    - Transacción ACID: Si falla un OrderItem, se hace rollback del Order.
    /// 
    /// 4. Paginación:
    ///    - GetAllAsync usa Skip/Take para cargar solo la página solicitada.
    ///    - Ordenado por OrderDate DESC (pedidos recientes primero).
    /// 
    /// 5. Filtros de seguridad:
    ///    - GetAllAsync filtra por UserId (usuario solo ve SUS pedidos).
    ///    - Soft delete implícito con IsActive.
    /// </remarks>
    public sealed class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor con inyección de dependencias del contexto de base de datos.
        /// </summary>
        /// <param name="context">Contexto de Entity Framework Core.</param>
        public OrderRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Obtiene un pedido por su ID, incluyendo sus items, productos y usuario.
        /// </summary>
        /// <remarks>
        /// EAGER LOADING COMPLETO:
        /// - Include(o => o.OrderItems): Carga los items del pedido.
        /// - ThenInclude(oi => oi.Product): Por cada item, carga el producto correspondiente.
        /// - Include(o => o.User): Carga la información del usuario.
        /// 
        /// Esto genera un INNER JOIN en SQL:
        /// SELECT o.*, oi.*, p.*, u.*
        /// FROM Orders o
        /// INNER JOIN OrderItems oi ON o.Id = oi.OrderId
        /// INNER JOIN Products p ON oi.ProductId = p.Id
        /// INNER JOIN Users u ON o.UserId = u.Id
        /// WHERE o.Id = @id AND o.IsActive = true
        /// </remarks>
        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .AsNoTracking() // Read-only, mejor performance
                .FirstOrDefaultAsync(o => o.Id == id && o.IsActive);
        }

        /// <summary>
        /// Obtiene pedidos de un usuario con paginación.
        /// </summary>
        /// <remarks>
        /// FILTRO DE SEGURIDAD:
        /// - WHERE UserId = @userId: Usuario solo ve SUS pedidos.
        /// - AND IsActive = true: Soft delete.
        /// 
        /// PAGINACIÓN:
        /// - Skip((page - 1) * pageSize): Salta los registros de páginas anteriores.
        /// - Take(pageSize): Toma solo los registros de la página actual.
        /// 
        /// ORDENAMIENTO:
        /// - OrderByDescending(o => o.OrderDate): Pedidos recientes primero.
        /// 
        /// Ejemplo: page=2, pageSize=10
        ///   Skip((2-1)*10) = Skip(10) -> Salta los primeros 10 registros (página 1)
        ///   Take(10) -> Toma los siguientes 10 registros (página 2)
        /// </remarks>
        public async Task<IEnumerable<Order>> GetAllAsync(int userId, int page = 1, int pageSize = 10)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId && o.IsActive)
                .OrderByDescending(o => o.OrderDate) // Pedidos recientes primero
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Crea un nuevo pedido con sus items.
        /// </summary>
        /// <remarks>
        /// IMPORTANTE: Solo agregamos Order al contexto.
        /// EF Core detecta automáticamente OrderItems por la relación configurada
        /// en OrderConfiguration.cs (HasMany/WithOne) y los inserta en cascada.
        /// 
        /// TRANSACCIÓN IMPLÍCITA:
        /// SaveChangesAsync() ejecuta todo en una transacción ACID:
        /// 1. INSERT INTO Orders (...)
        /// 2. INSERT INTO OrderItems (...) -- Por cada item
        /// Si falla cualquier INSERT, se hace ROLLBACK de todo.
        /// 
        /// PRECONDICIONES (deben validarse en OrderService):
        /// 1. order.CalculateTotal() debe haberse llamado.
        /// 2. Stock debe haberse validado/reservado.
        /// 3. OrderItems debe tener al menos 1 elemento.
        /// </remarks>
        public async Task<Order> AddAsync(Order order)
        {
            // Solo agregamos Order. EF Core insertará OrderItems automáticamente.
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            return order;
        }

        /// <summary>
        /// Actualiza un pedido (típicamente el estado).
        /// </summary>
        /// <remarks>
        /// TRACKING NECESARIO:
        /// - NO usamos AsNoTracking() aquí.
        /// - EF Core debe trackear la entidad para detectar cambios.
        /// 
        /// USO TÍPICO:
        /// var order = await _orderRepo.GetByIdAsync(id); // Carga con tracking
        /// order.Status = OrderStatus.Shipped;
        /// await _orderRepo.UpdateAsync(order);
        /// </remarks>
        public async Task UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Obtiene pedidos pendientes creados hace más de X minutos.
        /// </summary>
        /// <remarks>
        /// USO: Background job para expirar pedidos abandonados.
        /// 
        /// QUERY:
        /// WHERE Status = 'Pending' 
        /// AND OrderDate < DATEADD(MINUTE, -@minutesOld, GETUTCDATE())
        /// AND IsActive = true
        /// 
        /// Ejemplo: minutesOld = 15
        /// - Ahora: 2026-02-06 14:00:00 UTC
        /// - Límite: 2026-02-06 13:45:00 UTC
        /// - Encuentra pedidos Pending creados antes de 13:45
        /// </remarks>
        public async Task<IEnumerable<Order>> GetExpiredPendingOrdersAsync(int minutesOld)
        {
            var expirationTime = DateTime.UtcNow.AddMinutes(-minutesOld);

            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Status == OrderStatus.Pending
                    && o.OrderDate < expirationTime
                    && o.IsActive)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
