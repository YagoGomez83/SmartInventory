using SmartInventory.Application.DTOs.Orders;
using SmartInventory.Application.Interfaces;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.Application.Services
{
    /// <summary>
    /// Implementación del servicio de gestión de pedidos.
    /// </summary>
    /// <remarks>
    /// ARQUITECTURA CLEAN:
    /// - Capa de Aplicación: Orquesta lógica de negocio compleja entre múltiples repositorios.
    /// - Implementa transacciones explícitas para garantizar atomicidad (ACID).
    /// 
    /// TRANSACCIONALIDAD CRÍTICA:
    /// Este servicio coordina operaciones que DEBEN ejecutarse atómicamente:
    /// 1. Crear Order
    /// 2. Crear OrderItems
    /// 3. Reducir stock de productos
    /// 4. Registrar StockMovements
    /// 
    /// Si alguna operación falla, TODAS deben revertirse (Rollback).
    /// Ejemplo: Si hay stock insuficiente para el ítem #3 de 5, NO se debe crear
    /// el pedido parcial ni reducir stock de los primeros 2 items.
    /// 
    /// PATRÓN DE DISEÑO:
    /// - Unit of Work: IUnitOfWork maneja transacciones sin exponer DbContext.
    /// - Repository Pattern: Abstracción del acceso a datos.
    /// - Domain Logic: Order.CalculateTotal() encapsula reglas de negocio.
    /// 
    /// PRECIO SNAPSHOT:
    /// Los OrderItems capturan el precio actual del producto en el momento de la compra.
    /// Esto es CRÍTICO para auditoría y contabilidad (los precios pueden cambiar después).
    /// </remarks>
    public sealed class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IStockMovementRepository _stockMovementRepository;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        /// <param name="unitOfWork">Unit of Work para manejar transacciones.</param>
        /// <param name="orderRepository">Repositorio de pedidos.</param>
        /// <param name="productRepository">Repositorio de productos.</param>
        /// <param name="stockMovementRepository">Repositorio de movimientos de stock.</param>
        public OrderService(
            IUnitOfWork unitOfWork,
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            IStockMovementRepository stockMovementRepository)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _stockMovementRepository = stockMovementRepository ?? throw new ArgumentNullException(nameof(stockMovementRepository));
        }

        /// <inheritdoc />
        public async Task<OrderResponseDto> CreateOrderAsync(
            CreateOrderDto dto,
            int userId,
            CancellationToken cancellationToken = default)
        {
            // ========================================
            // VALIDACIÓN DE ENTRADA
            // ========================================
            if (dto is null)
            {
                throw new ArgumentNullException(nameof(dto), "El DTO de pedido es requerido.");
            }

            if (dto.Items is null || !dto.Items.Any())
            {
                throw new ArgumentException("El pedido debe contener al menos un item.", nameof(dto));
            }

            // ========================================
            // INICIO DE TRANSACCIÓN EXPLÍCITA
            // ========================================
            // CRÍTICO: Todas las operaciones deben ejecutarse atómicamente.
            // Si cualquier paso falla, se hace Rollback automático al salir del using.
            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // ========================================
                // PASO 1: CREAR EL PEDIDO (MAESTRO)
                // ========================================
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    TotalAmount = 0 // Se calculará después de agregar items
                };

                // ========================================
                // PASO 2: PROCESAR CADA ITEM DEL PEDIDO
                // ========================================
                // Diccionario para cachear productos y usarlos en el DTO de respuesta
                var productsCache = new Dictionary<int, Product>();

                foreach (var itemDto in dto.Items)
                {
                    // =======================================
                    // 2.1: OBTENER PRODUCTO CON VALIDACIÓN
                    // =======================================
                    var product = await _productRepository.GetByIdAsync(itemDto.ProductId, cancellationToken);

                    if (product is null)
                    {
                        throw new KeyNotFoundException(
                            $"El producto con ID {itemDto.ProductId} no existe en el sistema. " +
                            $"Verifica que el ID sea correcto.");
                    }

                    // Cachear el producto para usar en el DTO de respuesta
                    productsCache[product.Id] = product;

                    // =======================================
                    // 2.2: VALIDAR STOCK SUFICIENTE
                    // =======================================
                    // REGLA DE NEGOCIO CRÍTICA: No se puede vender lo que no existe en inventario.
                    if (product.StockQuantity < itemDto.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"Stock insuficiente para el producto '{product.Name}' (ID: {product.Id}). " +
                            $"Disponible: {product.StockQuantity}, Solicitado: {itemDto.Quantity}. " +
                            $"No se puede completar el pedido.");
                    }

                    // =======================================
                    // 2.3: REDUCIR STOCK DEL PRODUCTO
                    // =======================================
                    // Actualiza el inventario disponible restando la cantidad vendida.
                    int previousStock = product.StockQuantity;
                    product.StockQuantity -= itemDto.Quantity;

                    // Persiste el cambio de stock en el repositorio
                    await _productRepository.UpdateAsync(product, cancellationToken);

                    // =======================================
                    // 2.4: REGISTRAR MOVIMIENTO DE STOCK (AUDITORÍA)
                    // =======================================
                    // CRÍTICO: Cada salida de inventario debe registrarse para trazabilidad completa.
                    // Esto es esencial para:
                    // - Auditorías (¿Cuándo y por qué salió este stock?)
                    // - Conciliación contable (inventario físico vs. sistema)
                    // - Análisis de ventas y rotación de productos
                    var stockMovement = new StockMovement
                    {
                        ProductId = product.Id,
                        Type = MovementType.Sale, // Salida por venta
                        Quantity = itemDto.Quantity, // Siempre positivo en BD
                        Reason = $"Venta - Pedido pendiente (UserId: {userId})",
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _stockMovementRepository.AddAsync(stockMovement, cancellationToken);

                    // =======================================
                    // 2.5: CREAR ITEM DEL PEDIDO (DETALLE)
                    // =======================================
                    // PRECIO SNAPSHOT: Captura el precio actual del producto.
                    // Esto garantiza que el pedido refleje el precio en el momento de la compra,
                    // incluso si el producto cambia de precio después.
                    var orderItem = new OrderItem
                    {
                        Order = order, // Relación con el maestro
                        ProductId = product.Id,
                        Quantity = itemDto.Quantity,
                        UnitPrice = product.Price // ← SNAPSHOT: Precio actual del producto
                        // Total se calcula automáticamente en la propiedad de OrderItem
                    };

                    // Agrega el item a la colección del pedido
                    order.OrderItems.Add(orderItem);
                }

                // ========================================
                // PASO 3: CALCULAR TOTAL DEL PEDIDO
                // ========================================
                // Invoca el método de dominio que encapsula la lógica de cálculo.
                // TotalAmount = Suma de (Quantity * UnitPrice) de todos los items.
                order.CalculateTotal();

                // ========================================
                // PASO 4: PERSISTIR EL PEDIDO
                // ========================================
                // AddAsync del repositorio registra el Order y sus OrderItems en el contexto.
                // EF Core maneja automáticamente la cascada (maestro + detalles).
                await _orderRepository.AddAsync(order);

                // Guarda todos los cambios pendientes en la base de datos.
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // ========================================
                // PASO 5: COMMIT DE LA TRANSACCIÓN
                // ========================================
                // Si llegamos aquí, todas las operaciones fueron exitosas.
                // Confirma los cambios de manera permanente.
                await transaction.CommitAsync(cancellationToken);

                // ========================================
                // PASO 6: CONSTRUIR Y RETORNAR DTO DE RESPUESTA
                // ========================================
                // Proyecta la entidad Order a un DTO optimizado para el cliente.
                var response = new OrderResponseDto
                {
                    Id = order.Id,
                    OrderDate = order.OrderDate,
                    Status = order.Status.ToString(), // Convierte enum a string
                    TotalAmount = order.TotalAmount,
                    Items = order.OrderItems.Select(oi => new OrderItemResponseDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = productsCache[oi.ProductId].Name, // Usa el nombre del cache
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        Total = oi.Total
                    }).ToList()
                };

                return response;
            }
            catch
            {
                // ========================================
                // MANEJO DE ERRORES: ROLLBACK AUTOMÁTICO
                // ========================================
                // Si ocurre cualquier excepción, el using statement llama a
                // transaction.Dispose(), que hace Rollback si no hubo Commit.
                // 
                // Esto revierte TODAS las operaciones:
                // - Order no se crea
                // - OrderItems no se insertan
                // - Stock NO se reduce
                // - StockMovements NO se registran
                // 
                // La base de datos queda en el estado original (consistencia ACID).
                // La excepción se propaga al controlador para que retorne un error HTTP adecuado.
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OrderResponseDto>> GetMyOrdersAsync(
            int userId,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            // ========================================
            // VALIDACIÓN DE PARÁMETROS
            // ========================================
            if (page < 1)
            {
                throw new ArgumentException("El número de página debe ser mayor a 0.", nameof(page));
            }

            if (pageSize < 1 || pageSize > 100)
            {
                throw new ArgumentException("El tamaño de página debe estar entre 1 y 100.", nameof(pageSize));
            }

            // ========================================
            // OBTENER PEDIDOS CON PAGINACIÓN
            // ========================================
            // El repositorio retorna Orders con OrderItems y Product incluidos (eager loading)
            var orders = await _orderRepository.GetAllAsync(userId, page, pageSize);

            // ========================================
            // MAPEAR A DTOs
            // ========================================
            // Proyecta las entidades Order a DTOs optimizados para el cliente
            var response = orders.Select(order => new OrderResponseDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                Status = order.Status.ToString(),
                TotalAmount = order.TotalAmount,
                Items = order.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "Producto desconocido",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Total = oi.Total
                }).ToList()
            });

            return response;
        }

        /// <inheritdoc />
        public async Task<OrderResponseDto> GetOrderByIdAsync(
            int orderId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            // ========================================
            // OBTENER PEDIDO
            // ========================================
            // El repositorio retorna Order con OrderItems y Product incluidos (eager loading)
            var order = await _orderRepository.GetByIdAsync(orderId);

            // ========================================
            // VALIDACIÓN: PEDIDO EXISTE
            // ========================================
            if (order is null)
            {
                throw new KeyNotFoundException($"El pedido con ID {orderId} no existe.");
            }

            // ========================================
            // VALIDACIÓN CRÍTICA: OWNERSHIP (IDOR PREVENTION)
            // ========================================
            // Verifica que el pedido pertenece al usuario que lo solicita
            // Esto previene ataques IDOR donde un usuario intenta acceder a pedidos de otros
            if (order.UserId != userId)
            {
                throw new UnauthorizedAccessException(
                    $"No tienes permisos para acceder al pedido {orderId}. " +
                    "Solo puedes ver tus propios pedidos.");
            }

            // ========================================
            // MAPEAR A DTO
            // ========================================
            var response = new OrderResponseDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                Status = order.Status.ToString(),
                TotalAmount = order.TotalAmount,
                Items = order.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "Producto desconocido",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Total = oi.Total
                }).ToList()
            };

            return response;
        }
    }
}
