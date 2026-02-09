using FluentAssertions;
using Moq;
using SmartInventory.Application.DTOs.Stock;
using SmartInventory.Application.Services;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.UnitTests.Services
{
    /// <summary>
    /// 🧪 UNIT TESTS PARA STOCKSERVICE
    /// ═══════════════════════════════════════════════════════════════════════════════
    /// OBJETIVO:
    /// Probar la lógica de negocio PURA del StockService sin tocar la base de datos real.
    /// 
    /// TÉCNICAS UTILIZADAS:
    /// - Mocking (Moq): Simulamos repositorios para aislar la lógica del servicio.
    /// - AAA Pattern: Arrange-Act-Assert (estructura estándar de tests).
    /// - FluentAssertions: Aserciones legibles como lenguaje natural.
    /// 
    /// POR QUÉ UNIT TESTS SON CRÍTICOS:
    /// 1. ✅ Detectan bugs ANTES de que lleguen a producción.
    /// 2. ✅ Documentan el comportamiento esperado del código.
    /// 3. ✅ Permiten refactorizar con confianza (si los tests pasan, no rompiste nada).
    /// 4. ✅ Ejecutan en milisegundos (vs. Integration Tests que tardan segundos).
    /// 5. ✅ Son tu red de seguridad cuando el equipo crece y múltiples personas tocan el código.
    /// 
    /// CASOS DE PRUEBA CUBIERTOS:
    /// ✓ Happy Path: Agregar stock (compra) funciona correctamente.
    /// ✓ Sad Path: Intentar vender más stock del disponible lanza excepción.
    /// </summary>
    public class StockServiceTests
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // SETUP: DEPENDENCIAS MOCKEADAS
        // ═══════════════════════════════════════════════════════════════════════════════
        // Creamos "dobles de acción" (mocks) de las dependencias del StockService.
        // Estos mocks NO tocan la base de datos real, solo simulan su comportamiento.

        private readonly Mock<IProductRepository> _productRepositoryMock;
        private readonly Mock<IStockMovementRepository> _stockMovementRepositoryMock;
        private readonly StockService _stockService;

        /// <summary>
        /// Constructor que se ejecuta ANTES de cada test.
        /// Inicializa los mocks y el servicio en estado limpio.
        /// </summary>
        public StockServiceTests()
        {
            // Creamos instancias nuevas de mocks (cada test tiene su propio set limpio).
            _productRepositoryMock = new Mock<IProductRepository>();
            _stockMovementRepositoryMock = new Mock<IStockMovementRepository>();

            // Instanciamos el servicio REAL, pero con repositorios FALSOS (mocks).
            // Esto es Dependency Injection manual para testing.
            _stockService = new StockService(
                _productRepositoryMock.Object,
                _stockMovementRepositoryMock.Object
            );
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // TEST 1: HAPPY PATH - AGREGAR STOCK (COMPRA)
        // ═══════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// ✅ ESCENARIO: Un producto tiene 10 unidades y compramos 5 más.
        /// EXPECTATIVA: El stock debe quedar en 15 unidades y registrar el movimiento.
        /// </summary>
        [Fact]
        public async Task AdjustStock_WhenAddingStock_ShouldIncreaseProductStock()
        {
            // ═══════════════════════════════════════════════════════════════════════════
            // ARRANGE (Preparar): Configuramos el escenario del test
            // ═══════════════════════════════════════════════════════════════════════════

            // 1. Creamos un producto con stock inicial de 10 unidades
            var product = new Product
            {
                Id = 1,
                Name = "Laptop Dell XPS 15",
                SKU = "DELL-XPS-001",
                StockQuantity = 10,  // ⭐ Stock inicial
                Price = 1500.00m,
                CreatedAt = DateTime.UtcNow
            };

            // 2. Configuramos el mock del repositorio para que devuelva nuestro producto
            //    cuando se llame a GetByIdAsync(1)
            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            // 3. Configuramos el mock para que cuando se registre un movimiento,
            //    lo devuelva con un ID generado (simulando la BD)
            _stockMovementRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockMovement movement, CancellationToken _) =>
                {
                    movement.Id = 100; // Simulamos el ID auto-generado por la BD
                    return movement;
                });

            // 4. Creamos el DTO de entrada (lo que enviaría el frontend)
            var adjustmentDto = new StockAdjustmentDto(
                ProductId: 1,
                Quantity: 5,  // ⭐ Queremos agregar 5 unidades
                Type: MovementType.Purchase,  // Tipo: Compra
                Reason: "Compra a proveedor TechSupplies"
            );

            const int userId = 42;  // Usuario que hace la operación

            // ═══════════════════════════════════════════════════════════════════════════
            // ACT (Actuar): Ejecutamos el método que queremos probar
            // ═══════════════════════════════════════════════════════════════════════════

            var result = await _stockService.AdjustStockAsync(adjustmentDto, userId);

            // ═══════════════════════════════════════════════════════════════════════════
            // ASSERT (Verificar): Comprobamos que todo funcionó como esperábamos
            // ═══════════════════════════════════════════════════════════════════════════

            // ✅ Verificación 1: El resultado no debe ser null
            result.Should().NotBeNull();

            // ✅ Verificación 2: El stock anterior debe ser 10
            result.PreviousStock.Should().Be(10);

            // ✅ Verificación 3: El nuevo stock debe ser 15 (10 + 5)
            result.NewStock.Should().Be(15);

            // ✅ Verificación 4: La cantidad cambiada debe ser 5
            result.QuantityChanged.Should().Be(5);

            // ✅ Verificación 5: El producto debe haber sido actualizado en memoria
            product.StockQuantity.Should().Be(15);

            // ✅ Verificación 6: Se debe haber llamado a UpdateAsync exactamente 1 vez
            _productRepositoryMock.Verify(
                repo => repo.UpdateAsync(product, It.IsAny<CancellationToken>()),
                Times.Once,
                "El producto debe ser actualizado en la base de datos"
            );

            // ✅ Verificación 7: Se debe haber registrado el movimiento exactamente 1 vez
            _stockMovementRepositoryMock.Verify(
                repo => repo.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
                Times.Once,
                "El movimiento de stock debe ser registrado para auditoría"
            );

            // ✅ Verificación 8: El movimiento debe tener la información correcta
            _stockMovementRepositoryMock.Verify(
                repo => repo.AddAsync(
                    It.Is<StockMovement>(m =>
                        m.ProductId == 1 &&
                        m.Quantity == 5 &&
                        m.Type == MovementType.Purchase &&
                        m.CreatedBy == userId
                    ),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once,
                "El movimiento debe contener los datos correctos"
            );
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // TEST 2: SAD PATH - STOCK INSUFICIENTE (VENTA IMPOSIBLE)
        // ═══════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// ❌ ESCENARIO: Un producto tiene 10 unidades y queremos vender 20.
        /// EXPECTATIVA: Debe lanzar InvalidOperationException sin modificar la BD.
        /// </summary>
        [Fact]
        public async Task AdjustStock_WhenSellingMoreThanAvailable_ShouldThrowException()
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
                StockQuantity = 10,  // ⭐ Solo tenemos 10 unidades
                Price = 999.99m,
                CreatedAt = DateTime.UtcNow
            };

            // 2. Configuramos el mock para devolver nuestro producto
            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            // 3. Creamos un DTO que intenta vender MÁS de lo disponible
            var adjustmentDto = new StockAdjustmentDto(
                ProductId: 2,
                Quantity: 20,  // ⚠️ Queremos vender 20, pero solo hay 10
                Type: MovementType.Sale,  // Tipo: Venta
                Reason: "Pedido urgente cliente VIP"
            );

            const int userId = 42;

            // ═══════════════════════════════════════════════════════════════════════════
            // ACT (Actuar): Envolvemos la llamada en una función para poder testear la excepción
            // ═══════════════════════════════════════════════════════════════════════════

            // Creamos una función que ejecutará el código que esperamos que falle
            Func<Task> action = async () =>
                await _stockService.AdjustStockAsync(adjustmentDto, userId);

            // ═══════════════════════════════════════════════════════════════════════════
            // ASSERT (Verificar): Comprobamos que SE LANZÓ la excepción esperada
            // ═══════════════════════════════════════════════════════════════════════════

            // ✅ Verificación 1: Debe lanzar InvalidOperationException
            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Stock insuficiente*",
                    "La excepción debe indicar claramente que no hay suficiente stock");

            // ✅ Verificación 2: El stock del producto NO debe haber cambiado
            product.StockQuantity.Should().Be(10,
                "El stock no debe modificarse si la operación falla");

            // ✅ Verificación 3: NUNCA se debe haber llamado a UpdateAsync
            //    (protección de integridad de datos)
            _productRepositoryMock.Verify(
                repo => repo.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "No se debe actualizar la base de datos si la operación falla la validación"
            );

            // ✅ Verificación 4: NUNCA se debe haber registrado el movimiento
            //    (no registramos operaciones inválidas)
            _stockMovementRepositoryMock.Verify(
                repo => repo.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "No se debe crear registro de auditoría de operaciones fallidas"
            );
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 🎯 BONUS: TESTS ADICIONALES QUE PODRÍAS AGREGAR
        // ═══════════════════════════════════════════════════════════════════════════════
        // Para practicar, intenta crear estos tests por tu cuenta:
        // 
        // [Fact] AdjustStock_WhenProductNotFound_ShouldThrowKeyNotFoundException()
        //    - Simula que GetByIdAsync devuelve null
        //    - Verifica que lanza KeyNotFoundException
        //
        // [Fact] AdjustStock_WhenAdjustmentTypeIsAdjustment_ShouldCorrectStock()
        //    - Prueba el tipo MovementType.Adjustment
        //    - Verifica que suma correctamente (ajuste positivo)
        //
        // [Theory]
        // [InlineData(10, 5, 15)]  // Compra 5, stock pasa de 10 a 15
        // [InlineData(20, 3, 23)]  // Compra 3, stock pasa de 20 a 23
        // AdjustStock_WithVariousQuantities_ShouldCalculateCorrectly(int initial, int qty, int expected)
        //    - Tests parametrizados para probar múltiples casos en un solo test
    }
}
