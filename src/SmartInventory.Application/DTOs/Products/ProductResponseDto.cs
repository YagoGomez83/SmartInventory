namespace SmartInventory.Application.DTOs.Products
{
    /// <summary>
    /// DTO de respuesta que representa un producto en las consultas.
    /// </summary>
    /// <remarks>
    /// ¿POR QUÉ UN DTO DE RESPUESTA SEPARADO?
    /// 
    /// 1. CONTROL DE EXPOSICIÓN:
    ///    - La entidad tiene propiedades internas (IsActive, fechas de auditoría).
    ///    - En algunos casos, no queremos exponerlas al cliente.
    ///    - Este DTO decide exactamente qué datos se envían al frontend.
    /// 
    /// 2. VERSIONADO DE API:
    ///    - Si agregamos un campo a la entidad Product (ej: CategoryId),
    ///      no rompe la API v1 si no lo incluimos en el DTO v1.
    ///    - Podemos tener ProductResponseDtoV1 y ProductResponseDtoV2.
    /// 
    /// 3. PERFORMANCE:
    ///    - En consultas complejas, podemos usar proyecciones de EF Core
    ///      y mapear directamente a este DTO sin cargar la entidad completa.
    ///    - Ejemplo: context.Products.Select(p => new ProductResponseDto(...))
    ///      genera SQL optimizado con solo los campos necesarios.
    /// 
    /// 4. ENRIQUECIMIENTO:
    ///    - Podemos agregar campos calculados que no existen en la BD.
    ///    - Ejemplo: FormattedPrice = "$1,299.99" (frontend no tiene que formatearlo).
    /// 
    /// EJEMPLO DE RESPUESTA JSON:
    /// {
    ///   "id": 1,
    ///   "name": "Laptop Dell XPS 15",
    ///   "description": "Laptop de alto rendimiento...",
    ///   "sku": "LAPTOP-2024-001",
    ///   "price": 1299.99,
    ///   "stockQuantity": 50,
    ///   "createdAt": "2024-01-15T10:30:00Z"
    /// }
    /// </remarks>
    /// <param name="Id">Identificador único del producto.</param>
    /// <param name="Name">Nombre del producto.</param>
    /// <param name="Description">Descripción del producto.</param>
    /// <param name="SKU">Stock Keeping Unit.</param>
    /// <param name="Price">Precio unitario.</param>
    /// <param name="StockQuantity">Cantidad disponible en inventario.</param>
    /// <param name="CreatedAt">Fecha de creación (útil para ordenamiento).</param>
    public record ProductResponseDto(
        int Id,
        string Name,
        string Description,
        string SKU,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt
    );
}
