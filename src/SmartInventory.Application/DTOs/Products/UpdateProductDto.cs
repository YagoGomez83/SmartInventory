namespace SmartInventory.Application.DTOs.Products
{
    /// <summary>
    /// DTO para la actualización de un producto existente.
    /// </summary>
    /// <remarks>
    /// DISEÑO DE API - PUT vs PATCH:
    /// - PUT: Reemplaza el recurso completo (todos los campos requeridos).
    /// - PATCH: Actualiza solo campos específicos (campos opcionales).
    /// 
    /// Este DTO está diseñado para PUT (actualización completa).
    /// Para PATCH, considera usar JsonPatchDocument o un DTO con propiedades nullable.
    /// 
    /// IDENTIFICACIÓN:
    /// - Incluye Id para identificar qué producto actualizar.
    /// - En REST, típicamente el Id viene en la URL: PUT /api/products/{id}
    ///   y el DTO en el body NO debería tener Id (redundancia).
    ///   Aquí lo incluimos para flexibilidad, pero en el Controller
    ///   validaremos que el Id del DTO coincida con el de la URL.
    /// 
    /// VALIDACIÓN:
    /// - Id: Requerido, debe existir en BD.
    /// - Name: Requerido, longitud mínima 3, máxima 200.
    /// - SKU: Requerido, único (excepto para el mismo producto).
    /// - Price: Mayor que 0.
    /// - StockQuantity: >= 0.
    /// 
    /// CONCURRENCIA:
    /// Para evitar el "lost update problem", considera agregar:
    /// - RowVersion (byte[]): EF Core detecta cambios concurrentes.
    /// - LastModifiedAt: Validación optimista (si cambió, rechazar).
    /// 
    /// Ejemplo del problema:
    /// 1. Usuario A lee Producto (Price = $100).
    /// 2. Usuario B lee Producto (Price = $100).
    /// 3. Usuario A actualiza a $120.
    /// 4. Usuario B actualiza a $90.
    /// 5. Resultado: $90 (se perdió el cambio de A). ❌
    /// 
    /// Solución: RowVersion o LastModifiedAt en el DTO.
    /// </remarks>
    /// <param name="Id">ID del producto a actualizar.</param>
    /// <param name="Name">Nombre actualizado del producto.</param>
    /// <param name="Description">Descripción actualizada.</param>
    /// <param name="SKU">SKU actualizado (debe seguir siendo único).</param>
    /// <param name="Price">Precio actualizado.</param>
    /// <param name="StockQuantity">Cantidad actualizada en inventario.</param>
    public record UpdateProductDto(
        int Id,
        string Name,
        string Description,
        string SKU,
        decimal Price,
        int StockQuantity,
        int MinimumStockLevel,
        string Category,
        DateTime LastModifiedAt
    );
}
