using SmartInventory.Application.DTOs.Products;
using SmartInventory.Application.Interfaces;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.Application.Services
{
    /// <summary>
    /// Implementación del servicio de gestión de productos.
    /// </summary>
    /// <remarks>
    /// RESPONSABILIDADES:
    /// 1. Orquestación: Coordina operaciones entre repositorio y lógica de negocio.
    /// 2. Validación: Implementa reglas de negocio (ej: SKU único, precio positivo).
    /// 3. Mapeo: Transforma DTOs ↔ Entidades.
    /// 4. Manejo de excepciones: Lanza excepciones específicas según el error.
    /// 
    /// MEJORAS FUTURAS:
    /// - Implementar AutoMapper para reducir código de mapeo manual.
    /// - Agregar FluentValidation para validaciones declarativas.
    /// - Implementar logging (ILogger<ProductService>) para auditoría.
    /// - Agregar caché distribuida (Redis) para GetByIdAsync de productos populares.
    /// </remarks>
    public sealed class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        /// <param name="productRepository">Repositorio de productos.</param>
        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        }

        /// <summary>
        /// Crea un nuevo producto en el inventario.
        /// </summary>
        /// <remarks>
        /// FLUJO:
        /// 1. Valida que el SKU no exista (negocio crítico).
        /// 2. Mapea CreateProductDto → Product (entidad).
        /// 3. Persiste en BD mediante el repositorio.
        /// 4. Mapea Product → ProductResponseDto.
        /// 5. Retorna el DTO con el ID generado.
        /// 
        /// VALIDACIONES:
        /// - SKU único: Si existe, lanza InvalidOperationException.
        /// - Futuro: Agregar validaciones adicionales con FluentValidation.
        /// </remarks>
        public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
        {
            // Validación: El SKU debe ser único
            var skuExists = await _productRepository.ExistsBySkuAsync(dto.SKU, cancellationToken);
            if (skuExists)
            {
                throw new InvalidOperationException($"El SKU '{dto.SKU}' ya existe en el sistema.");
            }

            // Mapeo manual: DTO → Entidad
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                SKU = dto.SKU,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                IsActive = true,
                MinimumStockLevel = dto.MinimumStockLevel,
                Category = dto.Category,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

            // Persistencia
            var createdProduct = await _productRepository.AddAsync(product, cancellationToken);

            // Mapeo: Entidad → DTO de respuesta
            return MapToResponseDto(createdProduct);
        }

        /// <summary>
        /// Obtiene un producto por su ID.
        /// </summary>
        /// <remarks>
        /// NOTA: Retorna null si el producto no existe o está inactivo (soft delete).
        /// </remarks>
        public async Task<ProductResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByIdAsync(id, cancellationToken);

            // Si no existe, retornamos null (pattern: Null Object)
            if (product == null)
            {
                return null;
            }

            return MapToResponseDto(product);
        }

        /// <summary>
        /// Obtiene una lista paginada de productos.
        /// </summary>
        /// <remarks>
        /// PERFORMANCE:
        /// - Usa LINQ .Select() para proyección eficiente.
        /// - El mapeo se ejecuta en memoria después de la consulta (ToListAsync).
        /// - Alternativa: Proyectar directamente en la query de EF Core:
        ///   context.Products.Select(p => new ProductResponseDto(...))
        ///   Esto genera SQL más eficiente (solo selecciona campos necesarios).
        /// </remarks>
        public async Task<IEnumerable<ProductResponseDto>> GetAllAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var products = await _productRepository.GetAllAsync(pageNumber, pageSize, cancellationToken);

            // Proyección: IEnumerable<Product> → IEnumerable<ProductResponseDto>
            return products.Select(MapToResponseDto);
        }

        /// <summary>
        /// Actualiza un producto existente.
        /// </summary>
        /// <remarks>
        /// VALIDACIONES:
        /// - El producto debe existir (si no, lanza KeyNotFoundException).
        /// - El ID del DTO debe coincidir con el parámetro id (seguridad).
        /// 
        /// FLUJO:
        /// 1. Busca el producto existente.
        /// 2. Actualiza sus propiedades con los valores del DTO.
        /// 3. Persiste los cambios mediante el repositorio.
        /// </remarks>
        public async Task UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default)
        {
            // Validación: El ID del parámetro debe coincidir con el del DTO
            if (id != dto.Id)
            {
                throw new InvalidOperationException(
                    $"El ID del parámetro ({id}) no coincide con el ID del DTO ({dto.Id}).");
            }

            // Busca el producto existente
            var existingProduct = await _productRepository.GetByIdAsync(id, cancellationToken);

            if (existingProduct == null)
            {
                throw new KeyNotFoundException($"El producto con ID {id} no fue encontrado.");
            }

            // Actualiza las propiedades
            existingProduct.Name = dto.Name;
            existingProduct.Description = dto.Description;
            existingProduct.SKU = dto.SKU;
            existingProduct.Price = dto.Price;
            existingProduct.StockQuantity = dto.StockQuantity;
            existingProduct.MinimumStockLevel = dto.MinimumStockLevel;
            existingProduct.Category = dto.Category;
            existingProduct.LastModifiedAt = DateTime.UtcNow;

            // Persiste los cambios
            await _productRepository.UpdateAsync(existingProduct, cancellationToken);
        }

        /// <summary>
        /// Elimina un producto (soft delete).
        /// </summary>
        /// <remarks>
        /// SOFT DELETE:
        /// - No elimina físicamente el registro.
        /// - El repositorio marca IsActive = false.
        /// - El producto queda oculto en consultas normales pero se mantiene para auditoría.
        /// </remarks>
        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            // Valida que el producto exista antes de eliminarlo
            var existingProduct = await _productRepository.GetByIdAsync(id, cancellationToken);

            if (existingProduct == null)
            {
                throw new KeyNotFoundException($"El producto con ID {id} no fue encontrado.");
            }

            // Soft delete mediante el repositorio
            await _productRepository.DeleteAsync(id, cancellationToken);
        }

        /// <summary>
        /// Mapea una entidad Product a ProductResponseDto.
        /// </summary>
        /// <remarks>
        /// PATRÓN: Mapeo manual simple.
        /// ALTERNATIVA: Usar AutoMapper para proyectos con muchos DTOs.
        /// 
        /// AutoMapper ejemplo:
        ///   CreateMap<Product, ProductResponseDto>();
        ///   var dto = _mapper.Map<ProductResponseDto>(product);
        /// </remarks>
        private static ProductResponseDto MapToResponseDto(Product product)
        {
            return new ProductResponseDto(
                Id: product.Id,
                Name: product.Name,
                Description: product.Description,
                SKU: product.SKU,
                Price: product.Price,
                StockQuantity: product.StockQuantity,
                CreatedAt: product.CreatedAt,
                MinimumStockLevel: product.MinimumStockLevel,
                Category: product.Category



            );
        }
    }
}
