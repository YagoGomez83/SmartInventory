namespace SmartInventory.Application.DTOs.Products
{
    /// <summary>
    /// DTO para la creación de un nuevo producto.
    /// </summary>
    /// <remarks>
    /// SEPARACIÓN DE RESPONSABILIDADES:
    /// - NO incluye propiedades autogeneradas: Id, CreatedAt, LastModifiedAt, IsActive.
    /// - Esas propiedades se asignan automáticamente en el backend.
    /// 
    /// VALIDACIÓN (a implementar con FluentValidation):
    /// - Name: Requerido, longitud mínima 3, máxima 200 caracteres.
    /// - Description: Opcional, máximo 1000 caracteres.
    /// - SKU: Requerido, único, formato alfanumérico (ej: "PROD-2024-001").
    /// - Price: Requerido, mayor que 0, máximo 2 decimales.
    /// - StockQuantity: Requerido, >= 0 (puede ser 0 si el producto está agotado).
    /// 
    /// EJEMPLO DE USO EN FRONTEND (React):
    /// const newProduct = {
    ///   name: "Laptop Dell XPS 15",
    ///   description: "Laptop de alto rendimiento con procesador Intel i7",
    ///   sku: "LAPTOP-2024-001",
    ///   price: 1299.99,
    ///   stockQuantity: 50
    /// };
    /// 
    /// await fetch('/api/products', {
    ///   method: 'POST',
    ///   headers: { 'Content-Type': 'application/json' },
    ///   body: JSON.stringify(newProduct)
    /// });
    /// </remarks>
    /// <param name="Name">Nombre del producto.</param>
    /// <param name="Description">Descripción detallada del producto.</param>
    /// <param name="SKU">Stock Keeping Unit (código único de identificación).</param>
    /// <param name="Price">Precio unitario en la moneda del sistema.</param>
    /// <param name="StockQuantity">Cantidad inicial en inventario.</param>
    public record CreateProductDto(
        string Name,
        string Description,
        string SKU,
        decimal Price,
        int StockQuantity,
        int MinimumStockLevel,
        string Category
    );
}
