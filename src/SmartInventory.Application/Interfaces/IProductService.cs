using SmartInventory.Application.DTOs.Products;

namespace SmartInventory.Application.Interfaces
{
    /// <summary>
    /// Contrato de servicio para la gestión de productos.
    /// </summary>
    /// <remarks>
    /// RESPONSABILIDADES DE LA CAPA DE APLICACIÓN:
    /// 
    /// 1. ORQUESTACIÓN:
    ///    - Coordina las operaciones entre repositorios y otros servicios.
    ///    - Ejemplo: Al crear un producto, puede validar con ExistsBySkuAsync
    ///      antes de llamar a AddAsync.
    /// 
    /// 2. VALIDACIÓN DE NEGOCIO:
    ///    - FluentValidation para validar DTOs.
    ///    - Reglas de negocio complejas (ej: "no permitir precio negativo").
    /// 
    /// 3. MAPEO DTO ↔ ENTIDAD:
    ///    - Convierte CreateProductDto → Product (entidad).
    ///    - Convierte Product → ProductResponseDto.
    ///    - Usa AutoMapper o mapeo manual según preferencia del equipo.
    /// 
    /// 4. TRANSACCIONES:
    ///    - Si una operación requiere múltiples cambios en BD,
    ///      el servicio gestiona la transacción (Unit of Work).
    /// 
    /// EJEMPLO DE FLUJO (CreateAsync):
    /// 1. Controller recibe CreateProductDto.
    /// 2. Controller llama a productService.CreateAsync(dto).
    /// 3. ProductService valida con FluentValidation.
    /// 4. ProductService verifica que el SKU no exista (ExistsBySkuAsync).
    /// 5. ProductService mapea DTO → entidad Product.
    /// 6. ProductService llama a productRepository.AddAsync(product).
    /// 7. ProductService mapea Product → ProductResponseDto.
    /// 8. ProductService retorna ProductResponseDto al Controller.
    /// 9. Controller retorna respuesta HTTP 201 Created con el DTO.
    /// 
    /// PATRONES APLICADOS:
    /// - Service Layer Pattern: Encapsula lógica de negocio.
    /// - DTO Pattern: Desacopla la API de las entidades de dominio.
    /// - Dependency Inversion: Depende de IProductRepository (abstracción).
    /// </remarks>
    public interface IProductService
    {
        /// <summary>
        /// Crea un nuevo producto en el inventario.
        /// </summary>
        /// <param name="dto">DTO con los datos del producto a crear.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>DTO del producto creado con su ID generado.</returns>
        /// <remarks>
        /// VALIDACIONES QUE DEBE REALIZAR LA IMPLEMENTACIÓN:
        /// - Validar que el SKU no esté duplicado (ExistsBySkuAsync).
        /// - Validar que el precio sea mayor a 0.
        /// - Validar que el stock inicial sea >= 0.
        /// - Validar formato del SKU (alfanumérico, sin espacios).
        /// 
        /// EXCEPCIONES ESPERADAS:
        /// - ValidationException: Si algún campo no cumple las reglas.
        /// - InvalidOperationException: Si el SKU ya existe.
        /// </remarks>
        Task<ProductResponseDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un producto por su ID.
        /// </summary>
        /// <param name="id">ID del producto.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>DTO del producto o null si no existe.</returns>
        /// <remarks>
        /// IMPORTANTE: Solo retorna productos activos (IsActive = true).
        /// Los productos eliminados (soft delete) no son visibles.
        /// </remarks>
        Task<ProductResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene una lista paginada de productos.
        /// </summary>
        /// <param name="pageNumber">Número de página (base 1).</param>
        /// <param name="pageSize">Cantidad de elementos por página.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>Lista de DTOs de productos de la página solicitada.</returns>
        /// <remarks>
        /// PAGINACIÓN:
        /// - pageNumber mínimo: 1
        /// - pageSize recomendado: 10, 20, 50 o 100
        /// - pageSize máximo: Limitado en el repositorio (típicamente 100)
        /// 
        /// MEJORA FUTURA:
        /// Retornar un objeto PagedResult<ProductResponseDto> con metadatos:
        /// {
        ///   "items": [...],
        ///   "pageNumber": 1,
        ///   "pageSize": 20,
        ///   "totalCount": 150,
        ///   "totalPages": 8,
        ///   "hasNextPage": true,
        ///   "hasPreviousPage": false
        /// }
        /// 
        /// Esto mejora la experiencia del usuario en el frontend (paginadores, scroll infinito).
        /// </remarks>
        Task<IEnumerable<ProductResponseDto>> GetAllAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un producto existente.
        /// </summary>
        /// <param name="id">ID del producto a actualizar.</param>
        /// <param name="dto">DTO con los nuevos datos del producto.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <remarks>
        /// VALIDACIONES:
        /// - Verificar que el producto con el ID especificado existe.
        /// - Validar que el DTO.Id coincida con el parámetro id (seguridad).
        /// - Si se cambia el SKU, validar que el nuevo SKU no exista en otro producto.
        /// - Validar restricciones de negocio (precio > 0, stock >= 0).
        /// 
        /// EXCEPCIONES ESPERADAS:
        /// - InvalidOperationException: Si el ID no existe o no coincide.
        /// - ValidationException: Si algún campo no cumple las reglas.
        /// 
        /// NOTA SOBRE CONCURRENCIA:
        /// En sistemas de alto tráfico, dos usuarios podrían actualizar el mismo
        /// producto simultáneamente. Considera implementar:
        /// - Optimistic concurrency control (RowVersion en EF Core).
        /// - Locks distribuidos (Redis) para operaciones críticas.
        /// </remarks>
        Task UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina un producto (soft delete).
        /// </summary>
        /// <param name="id">ID del producto a eliminar.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <remarks>
        /// SOFT DELETE:
        /// - No elimina físicamente el registro de la base de datos.
        /// - Marca IsActive = false para ocultarlo de las consultas normales.
        /// 
        /// VENTAJAS DEL SOFT DELETE:
        /// 1. Auditoría completa (¿quién eliminó qué y cuándo?).
        /// 2. Posibilidad de restaurar productos eliminados por error.
        /// 3. Integridad referencial: Si el producto está en pedidos históricos,
        ///    no podemos eliminarlo físicamente sin romper esas referencias.
        /// 
        /// EXCEPCIONES ESPERADAS:
        /// - InvalidOperationException: Si el producto no existe.
        /// 
        /// MEJORA FUTURA:
        /// Para restaurar productos eliminados, agrega un método:
        /// Task RestoreAsync(int id, CancellationToken cancellationToken = default);
        /// </remarks>
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
