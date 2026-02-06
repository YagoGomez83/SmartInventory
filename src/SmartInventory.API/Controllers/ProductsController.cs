using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInventory.Application.DTOs.Products;
using SmartInventory.Application.Interfaces;

namespace SmartInventory.API.Controllers
{
    /// <summary>
    /// Controlador REST para la gestión de productos del inventario.
    /// </summary>
    /// <remarks>
    /// SEGURIDAD Y AUTORIZACIÓN:
    /// - [Authorize] a nivel de clase: Todos los endpoints requieren autenticación JWT.
    /// - Lectura (GET): Accesible para cualquier usuario autenticado.
    /// - Escritura (POST/PUT/DELETE): Requiere rol "Admin" explícitamente.
    /// 
    /// EJEMPLO DE USO EN FRONTEND:
    /// 
    /// // Usuario normal (Customer) puede ver productos:
    /// fetch('/api/products', {
    ///   headers: { 'Authorization': 'Bearer {token}' }
    /// });
    /// 
    /// // Solo Admin puede crear:
    /// fetch('/api/products', {
    ///   method: 'POST',
    ///   headers: { 
    ///     'Authorization': 'Bearer {tokenAdmin}',
    ///     'Content-Type': 'application/json'
    ///   },
    ///   body: JSON.stringify(newProduct)
    /// });
    /// 
    /// CÓDIGOS DE ESTADO HTTP:
    /// - 200 OK: GET exitoso.
    /// - 201 Created: POST exitoso (recurso creado).
    /// - 204 No Content: PUT/DELETE exitoso (sin cuerpo en respuesta).
    /// - 400 Bad Request: Error de validación o lógica de negocio.
    /// - 401 Unauthorized: No autenticado (falta token JWT).
    /// - 403 Forbidden: Autenticado pero sin permisos (no es Admin).
    /// - 404 Not Found: Recurso no encontrado.
    /// - 500 Internal Server Error: Error no controlado en el servidor.
    /// 
    /// MANEJO DE EXCEPCIONES:
    /// - KeyNotFoundException → 404 Not Found
    /// - InvalidOperationException → 400 Bad Request
    /// - ValidationException → 400 Bad Request (si usamos FluentValidation)
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Protege TODOS los endpoints por defecto
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        /// <param name="productService">Servicio de lógica de negocio de productos.</param>
        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Obtiene una lista paginada de productos.
        /// </summary>
        /// <param name="pageNumber">Número de página (predeterminado: 1).</param>
        /// <param name="pageSize">Tamaño de página (predeterminado: 10).</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Lista de productos de la página solicitada.</returns>
        /// <remarks>
        /// GET /api/products?pageNumber=1&amp;pageSize=20
        /// 
        /// ACCESO: Cualquier usuario autenticado.
        /// 
        /// PARÁMETROS OPCIONALES:
        /// - Si no se envían, usa valores predeterminados.
        /// - Validación básica: pageNumber >= 1, pageSize <= 100.
        /// 
        /// MEJORA FUTURA:
        /// Agregar filtros opcionales:
        /// - ?search=laptop (búsqueda por nombre/SKU)
        /// - ?category=electronics (filtro por categoría)
        /// - ?minPrice=100&amp;maxPrice=500
        /// </remarks>
        /// <response code="200">Lista de productos obtenida exitosamente.</response>
        /// <response code="401">No autenticado (token JWT faltante o inválido).</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllAsync(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validación básica de parámetros
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100; // Límite de seguridad

                var products = await _productService.GetAllAsync(pageNumber, pageSize, cancellationToken);
                return Ok(products);
            }
            catch (InvalidOperationException ex)
            {
                // Error de lógica de negocio
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un producto específico por su ID.
        /// </summary>
        /// <param name="id">ID del producto.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Datos del producto solicitado.</returns>
        /// <remarks>
        /// GET /api/products/5
        /// 
        /// ACCESO: Cualquier usuario autenticado.
        /// 
        /// IMPORTANTE:
        /// - Solo retorna productos activos (IsActive = true).
        /// - Productos eliminados (soft delete) devuelven 404.
        /// </remarks>
        /// <response code="200">Producto encontrado.</response>
        /// <response code="401">No autenticado.</response>
        /// <response code="404">Producto no encontrado o eliminado.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _productService.GetByIdAsync(id, cancellationToken);

                if (product == null)
                {
                    return NotFound(new { message = $"Producto con ID {id} no encontrado." });
                }

                return Ok(product);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo producto en el inventario.
        /// </summary>
        /// <param name="dto">Datos del producto a crear.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Producto creado con su ID generado.</returns>
        /// <remarks>
        /// POST /api/products
        /// Content-Type: application/json
        /// Body:
        /// {
        ///   "name": "Laptop Dell XPS 15",
        ///   "description": "Laptop de alto rendimiento",
        ///   "sku": "LAPTOP-2024-001",
        ///   "price": 1299.99,
        ///   "stockQuantity": 50,
        ///   "minimumStockLevel": 10,
        ///   "category": "Electronics"
        /// }
        /// 
        /// ACCESO: SOLO usuarios con rol "Admin".
        /// 
        /// VALIDACIONES:
        /// - SKU único (no duplicado).
        /// - Precio &gt; 0.
        /// - StockQuantity &gt;= 0.
        /// - FluentValidation (si está implementado).
        /// </remarks>
        /// <response code="201">Producto creado exitosamente.</response>
        /// <response code="400">Error de validación o SKU duplicado.</response>
        /// <response code="401">No autenticado.</response>
        /// <response code="403">Autenticado pero sin rol Admin.</response>
        [HttpPost]
        [Authorize(Roles = "Admin")] // SOLO ADMIN
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateAsync(
            [FromBody] CreateProductDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var createdProduct = await _productService.CreateAsync(dto, cancellationToken);

                // Genera la URL del nuevo recurso
                // Ejemplo: Location: /api/products/5
                var locationUri = Url.Action(
                    nameof(GetByIdAsync),
                    new { id = createdProduct.Id });

                return Created(locationUri ?? $"/api/products/{createdProduct.Id}", createdProduct);
            }
            catch (InvalidOperationException ex)
            {
                // Ejemplo: "El SKU 'LAPTOP-2024-001' ya existe."
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza un producto existente.
        /// </summary>
        /// <param name="id">ID del producto a actualizar.</param>
        /// <param name="dto">Nuevos datos del producto.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Sin contenido (204).</returns>
        /// <remarks>
        /// PUT /api/products/5
        /// Content-Type: application/json
        /// Body:
        /// {
        ///   "id": 5,
        ///   "name": "Laptop Dell XPS 15 (actualizado)",
        ///   "description": "Descripción actualizada",
        ///   "sku": "LAPTOP-2024-001",
        ///   "price": 1199.99,
        ///   "stockQuantity": 45,
        ///   "minimumStockLevel": 10,
        ///   "category": "Electronics"
        /// }
        /// 
        /// ACCESO: SOLO usuarios con rol "Admin".
        /// 
        /// VALIDACIÓN IMPORTANTE:
        /// - El DTO.Id debe coincidir con el parámetro id de la URL.
        /// - Esto previene actualizaciones accidentales o maliciosas.
        /// - Ejemplo: PUT /api/products/5 con body.id=10 → BadRequest
        /// 
        /// SEGURIDAD:
        /// Validar siempre que el ID de la URL coincida con el ID del body.
        /// </remarks>
        /// <response code="204">Producto actualizado exitosamente.</response>
        /// <response code="400">Error de validación o IDs no coinciden.</response>
        /// <response code="401">No autenticado.</response>
        /// <response code="403">Autenticado pero sin rol Admin.</response>
        /// <response code="404">Producto no encontrado.</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")] // SOLO ADMIN
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAsync(
            int id,
            [FromBody] UpdateProductDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // SEGURIDAD: Validar que el ID de la URL coincida con el ID del DTO
                if (id != dto.Id)
                {
                    return BadRequest(new
                    {
                        message = "El ID de la URL no coincide con el ID del producto en el cuerpo de la petición."
                    });
                }

                await _productService.UpdateAsync(id, dto, cancellationToken);
                return NoContent(); // 204: Actualización exitosa, sin cuerpo de respuesta
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un producto (soft delete).
        /// </summary>
        /// <param name="id">ID del producto a eliminar.</param>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Sin contenido (204).</returns>
        /// <remarks>
        /// DELETE /api/products/5
        /// 
        /// ACCESO: SOLO usuarios con rol "Admin".
        /// 
        /// SOFT DELETE:
        /// - NO elimina físicamente el registro de la base de datos.
        /// - Marca IsActive = false en la entidad Product.
        /// - El producto desaparece de las consultas normales (GetAll, GetById).
        /// 
        /// VENTAJAS:
        /// 1. Auditoría completa (se mantiene el historial).
        /// 2. Posibilidad de restaurar productos eliminados por error.
        /// 3. Integridad referencial (si hay pedidos históricos que referencian este producto).
        /// 
        /// MEJORA FUTURA:
        /// Implementar endpoint de restauración:
        /// POST /api/products/5/restore (requiere rol Admin)
        /// </remarks>
        /// <response code="204">Producto eliminado exitosamente.</response>
        /// <response code="401">No autenticado.</response>
        /// <response code="403">Autenticado pero sin rol Admin.</response>
        /// <response code="404">Producto no encontrado.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")] // SOLO ADMIN
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _productService.DeleteAsync(id, cancellationToken);
                return NoContent(); // 204: Eliminación exitosa
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
