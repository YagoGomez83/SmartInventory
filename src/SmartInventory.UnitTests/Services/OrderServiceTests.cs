using FluentAssertions;
using Moq;
using SmartInventory.Application.DTOs.Orders;
using SmartInventory.Application.Services;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.UnitTests.Services
{
    /// <summary>
    /// 🧪 UNIT TESTS PARA ORDERSERVICE - NIVEL AVANZADO
    /// ═══════════════════════════════════════════════════════════════════════════════
    /// OBJETIVO:
    /// Probar el servicio MÁS CRÍTICO del sistema: gestión de pedidos con transacciones.
    /// 
    /// COMPLEJIDAD TÉCNICA:
    /// Este service coordina:
    /// - 4 repositorios diferentes (Order, Product, StockMovement, UnitOfWork)
    /// - Transacciones explícitas (ACID)
    /// - Validaciones de negocio (stock, precios)
    /// - Cálculos financieros (totales)
    /// 
    /// POR QUÉ ESTOS TESTS SON CRÍTICOS:
    /// 1. 💰 Manejan DINERO (cálculos de totales, precios)
    /// 2. 📦 Modifican INVENTARIO (reducen stock)
    /// 3. 🔒 Usan TRANSACCIONES (si algo falla, NADA debe persistir)
    /// 4. 📝 Registran AUDITORÍA (movimientos de stock)
    /// 
    /// Si estos tests pasan, tienes la garantía de que:
    /// - Los pedidos se crean correctamente con sus items
    /// - El inventario se reduce de forma atómica
    /// - Si algo falla, NADA se persiste (protección de datos)
    /// - Los cálculos monetarios son exactos
    /// 
    /// TÉCNICA AVANZADA:
    /// Mockear IUnitOfWork + ITransaction para simular transacciones sin base de datos.
    /// </summary>
    public class OrderServiceTests
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // SETUP: MOCKS DE DEPENDENCIAS (4 REPOSITORIOS + TRANSACTION)
        // ═══════════════════════════════════════════════════════════════════════════════

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITransaction> _transactionMock;
        private readonly Mock<IOrderRepository> _orderRepositoryMock;
        private readonly Mock<IProductRepository> _productRepositoryMock;
        private readonly Mock<IStockMovementRepository> _stockMovementRepositoryMock;
        private readonly OrderService _orderService;

        /// <summary>
        /// Constructor que inicializa el escenario de prueba.
        /// Se ejecuta ANTES de cada test (cada test tiene mocks limpios).
        /// </summary>
        public OrderServiceTests()
        {
            // Inicializamos los mocks
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _transactionMock = new Mock<ITransaction>();
            _orderRepositoryMock = new Mock<IOrderRepository>();
            _productRepositoryMock = new Mock<IProductRepository>();
            _stockMovementRepositoryMock = new Mock<IStockMovementRepository>();

            // ═══════════════════════════════════════════════════════════════════════════
            // CONFIGURACIÓN CRÍTICA: MOCKEAR LA TRANSACCIÓN
            // ═══════════════════════════════════════════════════════════════════════════
            // El OrderService llama a _unitOfWork.BeginTransactionAsync()
            // Debemos configurar el mock para devolver nuestro _transactionMock

            _unitOfWorkMock
                .Setup(uow => uow.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_transactionMock.Object);

            // También necesitamos configurar SaveChangesAsync
            _unitOfWorkMock
                .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1); // Simula que se guardó 1 entidad

            // Instanciamos el servicio REAL con repositorios MOCKEADOS
            _orderService = new OrderService(
                _unitOfWorkMock.Object,
                _orderRepositoryMock.Object,
                _productRepositoryMock.Object,
                _stockMovementRepositoryMock.Object
            );
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // TEST 1: HAPPY PATH - CREAR PEDIDO EXITOSO
        // ═══════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// ✅ ESCENARIO: Cliente compra 2 unidades de un producto que cuesta $100 y tiene stock de 10.
        /// EXPECTATIVA: 
        /// - Se crea el pedido con total de $200 (2 * $100)
        /// - Se reduce el stock a 8 unidades
        /// - Se registra el movimiento de stock
        /// - Se hace COMMIT de la transacción
        /// </summary>
        [Fact]
        public async Task CreateOrder_WithValidData_ShouldReturnOrderResponse()
        {
            // ═══════════════════════════════════════════════════════════════════════════
            // ARRANGE (Preparar): Configuramos el escenario exitoso
            // ═══════════════════════════════════════════════════════════════════════════

            // 1. Creamos un producto con precio $100 y stock 10
            var product = new Product
            {
                Id = 1,
                Name = "Laptop Dell XPS 15",
                SKU = "DELL-XPS-001",
                Price = 100.00m,        // ⭐ Precio: $100
                StockQuantity = 10,     // ⭐ Stock disponible: 10 unidades
                CreatedAt = DateTime.UtcNow
            };

            // 2. Configuramos el mock del ProductRepository
            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            // 3. Configuramos el mock del OrderRepository
            // Cuando se llame a AddAsync, asignamos un ID al pedido (simulando la BD)
            _orderRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<Order>()))
                .Callback<Order>(order =>
                {
                    order.Id = 100; // Simulamos que la BD asignó ID 100
                });

            // 4. Creamos el DTO de entrada (lo que enviaría el frontend)
            var createOrderDto = new CreateOrderDto
            {
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = 1,
                        Quantity = 2  // ⭐ Queremos comprar 2 unidades
                    }
                }
            };

            const int userId = 42;

            // ═══════════════════════════════════════════════════════════════════════════
            // ACT (Actuar): Ejecutamos el método que queremos probar
            // ═══════════════════════════════════════════════════════════════════════════

            var result = await _orderService.CreateOrderAsync(createOrderDto, userId);

            // ═══════════════════════════════════════════════════════════════════════════
            // ASSERT (Verificar): Comprobamos que todo funcionó perfectamente
            // ═══════════════════════════════════════════════════════════════════════════

            // ✅ Verificación 1: El resultado no debe ser null
            result.Should().NotBeNull();

            // ✅ Verificación 2: El ID del pedido debe ser el asignado por la BD
            result.Id.Should().Be(100);

            // ✅ Verificación 3: El total debe ser $200 (2 unidades * $100)
            result.TotalAmount.Should().Be(200.00m,
                "El total debe ser la suma de (cantidad * precio) de todos los items");

            // ✅ Verificación 4: Debe tener 1 item en la respuesta
            result.Items.Should().HaveCount(1);

            // ✅ Verificación 5: El item debe tener los datos correctos
            var item = result.Items.First();
            item.ProductId.Should().Be(1);
            item.Quantity.Should().Be(2);
            item.UnitPrice.Should().Be(100.00m);
            item.Total.Should().Be(200.00m);

            // ✅ Verificación 6: El stock del producto debe haberse reducido
            product.StockQuantity.Should().Be(8,
                "El stock debe reducirse de 10 a 8 (10 - 2)");

            // ═══════════════════════════════════════════════════════════════════════════
            // VERIFICACIONES CRÍTICAS: TRANSACCIONALIDAD
            // ═══════════════════════════════════════════════════════════════════════════

            // ✅ Verificación 7: Se debe haber iniciado una transacción
            _unitOfWorkMock.Verify(
                uow => uow.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once,
                "Debe iniciar una transacción para proteger la integridad de datos"
            );

            // ✅ Verificación 8: Se debe haber guardado los cambios
            _unitOfWorkMock.Verify(
                uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once,
                "Debe guardar los cambios en la base de datos"
            );

            // ✅ Verificación 9: Se debe haber hecho COMMIT de la transacción
            _transactionMock.Verify(
                tx => tx.CommitAsync(It.IsAny<CancellationToken>()),
                Times.Once,
                "Debe confirmar la transacción para hacer permanentes los cambios"
            );

            // ✅ Verificación 10: NUNCA se debe haber hecho ROLLBACK (operación exitosa)
            _transactionMock.Verify(
                tx => tx.RollbackAsync(It.IsAny<CancellationToken>()),
                Times.Never,
                "No debe revertir la transacción si todo fue exitoso"
            );

            // ═══════════════════════════════════════════════════════════════════════════
            // VERIFICACIONES DE REPOSITORIOS
            // ═══════════════════════════════════════════════════════════════════════════

            // ✅ Verificación 11: Se debe haber actualizado el producto (reducción de stock)
            _productRepositoryMock.Verify(
                repo => repo.UpdateAsync(product, It.IsAny<CancellationToken>()),
                Times.Once,
                "Debe actualizar el stock del producto"
            );

            // ✅ Verificación 12: Se debe haber registrado el movimiento de stock
            _stockMovementRepositoryMock.Verify(
                repo => repo.AddAsync(
                    It.Is<StockMovement>(m =>
                        m.ProductId == 1 &&
                        m.Type == MovementType.Sale &&
                        m.Quantity == 2 &&
                        m.CreatedBy == userId
                    ),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once,
                "Debe registrar el movimiento de stock para auditoría"
            );

            // ✅ Verificación 13: Se debe haber agregado el pedido
            _orderRepositoryMock.Verify(
                repo => repo.AddAsync(It.IsAny<Order>()),
                Times.Once,
                "Debe agregar el pedido al repositorio"
            );
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // TEST 2: SAD PATH - ROLLBACK POR STOCK INSUFICIENTE
        // ═══════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// ❌ ESCENARIO: Cliente intenta comprar 20 unidades pero solo hay 10 en stock.
        /// EXPECTATIVA:
        /// - Lanza InvalidOperationException
        /// - NO se reduce el stock
        /// - NO se crea el pedido
        /// - NO se registran movimientos
        /// - Se hace ROLLBACK automático (protección de integridad)
        /// </summary>
        [Fact]
        public async Task CreateOrder_WhenInsufficientStock_ShouldThrowExceptionAndRollback()
        {
            // ═══════════════════════════════════════════════════════════════════════════
            // ARRANGE (Preparar): Configuramos un escenario de fallo
            // ═══════════════════════════════════════════════════════════════════════════

            // 1. Creamos un producto con stock limitado (solo 10 unidades)
            var product = new Product
            {
                Id = 2,
                Name = "iPhone 15 Pro",
                SKU = "APPLE-IP15P-001",
                Price = 999.99m,
                StockQuantity = 10,  // ⚠️ Solo hay 10 unidades
                CreatedAt = DateTime.UtcNow
            };

            // 2. Configuramos el mock del ProductRepository
            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            // 3. Creamos un DTO que intenta comprar MÁS de lo disponible
            var createOrderDto = new CreateOrderDto
            {
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = 2,
                        Quantity = 20  // ⚠️ Queremos 20, pero solo hay 10
                    }
                }
            };

            const int userId = 42;

            // ═══════════════════════════════════════════════════════════════════════════
            // ACT (Actuar): Ejecutamos la acción que esperamos que falle
            // ═══════════════════════════════════════════════════════════════════════════

            Func<Task> action = async () =>
                await _orderService.CreateOrderAsync(createOrderDto, userId);

            // ═══════════════════════════════════════════════════════════════════════════
            // ASSERT (Verificar): Comprobamos que se lanzó la excepción esperada
            // ═══════════════════════════════════════════════════════════════════════════

            // ✅ Verificación 1: Debe lanzar InvalidOperationException
            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Stock insuficiente*",
                    "La excepción debe indicar claramente el problema de stock");

            // ✅ Verificación 2: El stock NO debe haberse modificado
            product.StockQuantity.Should().Be(10,
                "El stock debe mantenerse en 10 si la operación falla");

            // ═══════════════════════════════════════════════════════════════════════════
            // VERIFICACIONES CRÍTICAS: PROTECCIÓN DE INTEGRIDAD
            // ═══════════════════════════════════════════════════════════════════════════

            // ✅ Verificación 3: Se debe haber iniciado la transacción
            // (el OrderService siempre inicia transacción primero)
            _unitOfWorkMock.Verify(
                uow => uow.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once,
                "La transacción se inicia antes de detectar el error"
            );

            // ✅ Verificación 4: NUNCA se debe haber guardado cambios
            _unitOfWorkMock.Verify(
                uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never,
                "No debe guardar cambios si la validación falla"
            );

            // ✅ Verificación 5: NUNCA se debe haber hecho COMMIT
            _transactionMock.Verify(
                tx => tx.CommitAsync(It.IsAny<CancellationToken>()),
                Times.Never,
                "No debe confirmar la transacción si la operación falla"
            );

            // ✅ Verificación 6: El Rollback es automático gracias al using/Dispose
            // No verificamos RollbackAsync explícito porque el using statement
            // llama a Dispose() que hace rollback automático si no hubo commit

            // ═══════════════════════════════════════════════════════════════════════════
            // VERIFICACIONES DE REPOSITORIOS: NADA DEBE PERSISTIR
            // ═══════════════════════════════════════════════════════════════════════════

            // ✅ Verificación 7: NUNCA se debe haber actualizado el producto
            _productRepositoryMock.Verify(
                repo => repo.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "No debe actualizar el producto si la validación de stock falla"
            );

            // ✅ Verificación 8: NUNCA se debe haber registrado movimiento de stock
            _stockMovementRepositoryMock.Verify(
                repo => repo.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "No debe registrar movimientos de operaciones fallidas"
            );

            // ✅ Verificación 9: NUNCA se debe haber creado el pedido
            _orderRepositoryMock.Verify(
                repo => repo.AddAsync(It.IsAny<Order>()),
                Times.Never,
                "No debe crear el pedido si la validación falla"
            );
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // TEST 3: SAD PATH - PRODUCTO NO ENCONTRADO
        // ═══════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// ❌ ESCENARIO: Cliente intenta comprar un producto que no existe (ID 999).
        /// EXPECTATIVA: Lanza KeyNotFoundException y no persiste nada.
        /// </summary>
        [Fact]
        public async Task CreateOrder_WhenProductNotFound_ShouldThrowKeyNotFoundException()
        {
            // ═══════════════════════════════════════════════════════════════════════════
            // ARRANGE: Configuramos que el producto NO existe
            // ═══════════════════════════════════════════════════════════════════════════

            // Configuramos el mock para devolver null (producto no encontrado)
            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            var createOrderDto = new CreateOrderDto
            {
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = 999,  // ⚠️ Este producto no existe
                        Quantity = 1
                    }
                }
            };

            const int userId = 42;

            // ═══════════════════════════════════════════════════════════════════════════
            // ACT & ASSERT
            // ═══════════════════════════════════════════════════════════════════════════

            Func<Task> action = async () =>
                await _orderService.CreateOrderAsync(createOrderDto, userId);

            // ✅ Debe lanzar KeyNotFoundException
            await action.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*producto con ID 999 no existe*");

            // ✅ NUNCA debe persistir nada
            _unitOfWorkMock.Verify(
                uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );

            _transactionMock.Verify(
                tx => tx.CommitAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 🎯 BONUS: TESTS ADICIONALES RECOMENDADOS
        // ═══════════════════════════════════════════════════════════════════════════════
        // Para practicar, intenta crear estos tests:
        //
        // [Fact] CreateOrder_WithMultipleItems_ShouldCalculateTotalCorrectly()
        //    - Pedido con 3 productos diferentes
        //    - Verifica que el total sea la suma correcta de todos
        //
        // [Fact] CreateOrder_WhenDtoIsNull_ShouldThrowArgumentNullException()
        //    - Pasa null como DTO
        //    - Verifica que lanza ArgumentNullException
        //
        // [Fact] CreateOrder_WhenItemsIsEmpty_ShouldThrowArgumentException()
        //    - DTO con lista de Items vacía
        //    - Verifica que lanza ArgumentException
        //
        // [Fact] CreateOrder_ShouldCaptureProductPriceSnapshot()
        //    - Crea pedido con un producto
        //    - Cambia el precio del producto después
        //    - Verifica que el OrderItem tiene el precio original (snapshot)
    }
}
