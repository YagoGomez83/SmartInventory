using FluentAssertions;
using Moq;
using SmartInventory.Application.DTOs.Products;
using SmartInventory.Application.Services;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.UnitTests.Services
{
    /// <summary>
    /// 🧪 UNIT TESTS PARA PRODUCTSERVICE - OPERACIONES CRUD
    /// ═══════════════════════════════════════════════════════════════════════════════
    /// OBJETIVO:
    /// Validar las operaciones CRUD (Create, Read, Update, Delete) del catálogo de productos.
    /// 
    /// COBERTURA:
    /// ✓ Crear producto exitoso
    /// ✓ Validar SKU único (no duplicados)
    /// ✓ Obtener producto por ID
    /// ✓ Listar productos con paginación
    /// ✓ Actualizar producto
    /// ✓ Eliminar producto (soft delete)
    /// ✓ Manejo de productos inexistentes
    /// 
    /// IMPORTANCIA:
    /// El catálogo de productos es el núcleo del sistema de inventario.
    /// Sin productos correctamente gestionados, no hay ventas, no hay stock, no hay negocio.
    /// </summary>
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _productRepositoryMock;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _productRepositoryMock = new Mock<IProductRepository>();
            _productService = new ProductService(_productRepositoryMock.Object);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // TESTS DE CREACIÓN (CREATE)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// ✅ HAPPY PATH: Crear un producto nuevo con todos los datos válidos.
        /// </summary>
        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnProductResponse()
        {
            // Arrange
            var createDto = new CreateProductDto(
                Name: "Laptop Dell XPS 15",
                Description: "Laptop de alto rendimiento",
                SKU: "DELL-XPS-001",
                Price: 1500.00m,
                StockQuantity: 10,
                MinimumStockLevel: 2,
                Category: "Electrónica"
            );

            // Configuramos que el SKU NO existe (validación pasa)
            _productRepositoryMock
                .Setup(repo => repo.ExistsBySkuAsync(createDto.SKU, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Configuramos que AddAsync retorna el producto con ID generado
            _productRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product p, CancellationToken _) =>
                {
                    p.Id = 1; // Simulamos ID auto-generado
                    return p;
                });

            // Act
            var result = await _productService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Name.Should().Be(createDto.Name);
            result.SKU.Should().Be(createDto.SKU);
            result.Price.Should().Be(createDto.Price);
            result.StockQuantity.Should().Be(createDto.StockQuantity);

            // Verifica que se validó el SKU
            _productRepositoryMock.Verify(
                repo => repo.ExistsBySkuAsync(createDto.SKU, It.IsAny<CancellationToken>()),
                Times.Once,
                "Debe validar que el SKU no exista antes de crear"
            );

            // Verifica que se agregó el producto
            _productRepositoryMock.Verify(
                repo => repo.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
                Times.Once,
                "Debe agregar el producto al repositorio"
            );
        }

        /// <summary>
        /// ❌ SAD PATH: Intentar crear un producto con SKU duplicado.
        /// </summary>
        [Fact]
        public async Task CreateAsync_WithDuplicateSKU_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var createDto = new CreateProductDto(
                Name: "iPhone 15 Pro",
                Description: "Smartphone premium",
                SKU: "APPLE-IP15P-001", // SKU que ya existe
                Price: 999.99m,
                StockQuantity: 5,
                MinimumStockLevel: 1,
                Category: "Smartphones"
            );

            // Configuramos que el SKU YA EXISTE
            _productRepositoryMock
                .Setup(repo => repo.ExistsBySkuAsync(createDto.SKU, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            Func<Task> action = async () => await _productService.CreateAsync(createDto);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"*SKU '{createDto.SKU}' ya existe*",
                    "Debe lanzar excepción cuando el SKU está duplicado");

            // Verifica que NUNCA se intentó agregar el producto
            _productRepositoryMock.Verify(
                repo => repo.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "No debe agregar el producto si el SKU está duplicado"
            );
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // TESTS DE LECTURA (READ)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// ✅ HAPPY PATH: Obtener un producto existente por su ID.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnProduct()
        {
            // Arrange
            var product = new Product
            {
                Id = 1,
                Name = "Laptop Dell XPS 15",
                Description = "Laptop de alto rendimiento",
                SKU = "DELL-XPS-001",
                Price = 1500.00m,
                StockQuantity = 10,
                MinimumStockLevel = 2,
                Category = "Electrónica",
                CreatedAt = DateTime.UtcNow
            };

            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            // Act
            var result = await _productService.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(product.Id);
            result.Name.Should().Be(product.Name);
            result.SKU.Should().Be(product.SKU);
            result.Price.Should().Be(product.Price);
        }

        /// <summary>
        /// ❌ SAD PATH: Intentar obtener un producto que no existe.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Arrange
            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _productService.GetByIdAsync(999);

            // Assert
            result.Should().BeNull("No debe retornar nada si el producto no existe");
        }

        /// <summary>
        /// ✅ HAPPY PATH: Obtener lista paginada de productos.
        /// </summary>
        [Fact]
        public async Task GetAllAsync_WithPagination_ShouldReturnProductList()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Producto 1", SKU = "SKU-001", Price = 100m, StockQuantity = 10, CreatedAt = DateTime.UtcNow },
                new Product { Id = 2, Name = "Producto 2", SKU = "SKU-002", Price = 200m, StockQuantity = 20, CreatedAt = DateTime.UtcNow },
                new Product { Id = 3, Name = "Producto 3", SKU = "SKU-003", Price = 300m, StockQuantity = 30, CreatedAt = DateTime.UtcNow }
            };

            _productRepositoryMock
                .Setup(repo => repo.GetAllAsync(1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);

            // Act
            var result = await _productService.GetAllAsync(pageNumber: 1, pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.First().Id.Should().Be(1);
            result.Last().Id.Should().Be(3);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // TESTS DE ACTUALIZACIÓN (UPDATE)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// ✅ HAPPY PATH: Actualizar un producto existente.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldUpdateProduct()
        {
            // Arrange
            var existingProduct = new Product
            {
                Id = 1,
                Name = "Laptop Dell XPS 15 (Antiguo)",
                Description = "Descripción antigua",
                SKU = "DELL-XPS-001",
                Price = 1500.00m,
                StockQuantity = 10,
                MinimumStockLevel = 2,
                Category = "Electrónica",
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow.AddDays(-10)
            };

            var updateDto = new UpdateProductDto(
                Id: 1,
                Name: "Laptop Dell XPS 15 (Actualizado)",
                Description: "Nueva descripción",
                SKU: "DELL-XPS-001",
                Price: 1600.00m, // Precio actualizado
                StockQuantity: 15, // Stock actualizado
                MinimumStockLevel: 3,
                Category: "Computadoras",
                LastModifiedAt: DateTime.UtcNow
            );

            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingProduct);

            // Act
            await _productService.UpdateAsync(1, updateDto);

            // Assert
            existingProduct.Name.Should().Be(updateDto.Name, "El nombre debe actualizarse");
            existingProduct.Price.Should().Be(updateDto.Price, "El precio debe actualizarse");
            existingProduct.StockQuantity.Should().Be(updateDto.StockQuantity, "El stock debe actualizarse");

            _productRepositoryMock.Verify(
                repo => repo.UpdateAsync(existingProduct, It.IsAny<CancellationToken>()),
                Times.Once,
                "Debe llamar al repositorio para persistir los cambios"
            );
        }

        /// <summary>
        /// ❌ SAD PATH: Intentar actualizar con ID inconsistente.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithMismatchedId_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var updateDto = new UpdateProductDto(
                Id: 5, // DTO dice ID 5
                Name: "Producto",
                Description: "Descripción",
                SKU: "SKU-001",
                Price: 100m,
                StockQuantity: 10,
                MinimumStockLevel: 1,
                Category: "Categoría",
                LastModifiedAt: DateTime.UtcNow
            );

            // Act
            Func<Task> action = async () => await _productService.UpdateAsync(1, updateDto); // Pero llamamos con ID 1

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*ID del parámetro*no coincide*",
                    "Debe lanzar excepción cuando los IDs no coinciden");

            // No debe intentar actualizar nada
            _productRepositoryMock.Verify(
                repo => repo.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        /// <summary>
        /// ❌ SAD PATH: Intentar actualizar un producto que no existe.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithNonExistingProduct_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var updateDto = new UpdateProductDto(
                Id: 999,
                Name: "Producto Inexistente",
                Description: "Descripción",
                SKU: "SKU-999",
                Price: 100m,
                StockQuantity: 10,
                MinimumStockLevel: 1,
                Category: "Categoría",
                LastModifiedAt: DateTime.UtcNow
            );

            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            // Act
            Func<Task> action = async () => await _productService.UpdateAsync(999, updateDto);

            // Assert
            await action.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*producto con ID 999 no fue encontrado*");
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // TESTS DE ELIMINACIÓN (DELETE)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// ✅ HAPPY PATH: Eliminar un producto existente (soft delete).
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WithExistingProduct_ShouldDeleteSuccessfully()
        {
            // Arrange
            var product = new Product
            {
                Id = 1,
                Name = "Producto a Eliminar",
                SKU = "SKU-001",
                Price = 100m,
                StockQuantity = 10,
                CreatedAt = DateTime.UtcNow
            };

            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            // Act
            await _productService.DeleteAsync(1);

            // Assert
            _productRepositoryMock.Verify(
                repo => repo.DeleteAsync(1, It.IsAny<CancellationToken>()),
                Times.Once,
                "Debe llamar al repositorio para eliminar (soft delete)"
            );
        }

        /// <summary>
        /// ❌ SAD PATH: Intentar eliminar un producto que no existe.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WithNonExistingProduct_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            // Act
            Func<Task> action = async () => await _productService.DeleteAsync(999);

            // Assert
            await action.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*producto con ID 999 no fue encontrado*");

            // No debe intentar eliminar nada
            _productRepositoryMock.Verify(
                repo => repo.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "No debe intentar eliminar si el producto no existe"
            );
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // 🎯 BONUS: TEST DE MAPEO
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// ✅ VERIFICACIÓN: El mapeo Product → ProductResponseDto conserva todos los datos.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldMapAllPropertiesCorrectly()
        {
            // Arrange
            var product = new Product
            {
                Id = 1,
                Name = "Test Product",
                Description = "Test Description",
                SKU = "TEST-SKU-001",
                Price = 99.99m,
                StockQuantity = 50,
                MinimumStockLevel = 5,
                Category = "Test Category",
                CreatedAt = new DateTime(2024, 1, 1),
                LastModifiedAt = new DateTime(2024, 2, 1)
            };

            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            // Act
            var result = await _productService.GetByIdAsync(1);

            // Assert - Verifica que TODOS los campos se mapearon correctamente
            result.Should().NotBeNull();
            result!.Id.Should().Be(product.Id);
            result.Name.Should().Be(product.Name);
            result.Description.Should().Be(product.Description);
            result.SKU.Should().Be(product.SKU);
            result.Price.Should().Be(product.Price);
            result.StockQuantity.Should().Be(product.StockQuantity);
            result.MinimumStockLevel.Should().Be(product.MinimumStockLevel);
            result.Category.Should().Be(product.Category);
            result.CreatedAt.Should().Be(product.CreatedAt);
        }
    }
}
