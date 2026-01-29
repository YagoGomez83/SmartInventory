using SmartInventory.Domain.Entities;

namespace SmartInventory.Domain.Interfaces
{
    /// <summary>
    /// Contrato para operaciones de persistencia de productos.
    /// </summary>
    /// <remarks>
    /// PATRÓN REPOSITORY:
    /// - Abstrae la lógica de acceso a datos de la lógica de negocio.
    /// - Permite cambiar la fuente de datos sin afectar la capa de aplicación.
    /// - Facilita el testing (podemos usar repositorios en memoria para tests).
    /// 
    /// PAGINACIÓN:
    /// - GetAllAsync incluye parámetros de paginación para evitar cargar miles
    ///   de productos en memoria (anti-patrón común en aplicaciones novatas).
    /// - Ejemplo: En producción con 100K productos, sin paginación colapsarías el servidor.
    /// </remarks>
    public interface IProductRepository
    {
        /// <summary>
        /// Obtiene un producto por su ID.
        /// </summary>
        /// <param name="id">ID del producto.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>El producto encontrado o null si no existe.</returns>
        Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un producto por su SKU (Stock Keeping Unit).
        /// </summary>
        /// <param name="sku">SKU del producto.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>El producto encontrado o null si no existe.</returns>
        /// <remarks>
        /// En sistemas reales, el SKU suele usarse más que el ID para búsquedas,
        /// ya que es el identificador de negocio (no técnico).
        /// Ejemplo: Un escáner de código de barras envía el SKU, no el ID de BD.
        /// </remarks>
        Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene todos los productos con paginación.
        /// </summary>
        /// <param name="pageNumber">Número de página (base 1).</param>
        /// <param name="pageSize">Cantidad de elementos por página.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>Lista paginada de productos activos.</returns>
        /// <remarks>
        /// PAGINACIÓN: Evita cargar toda la tabla en memoria.
        /// - pageNumber: 1 = primera página, 2 = segunda página, etc.
        /// - pageSize: Típicamente 10, 20, 50 o 100.
        /// 
        /// En SQL esto se traduce a OFFSET y LIMIT:
        ///   SELECT * FROM Products 
        ///   WHERE IsActive = true 
        ///   ORDER BY CreatedAt DESC
        ///   OFFSET (@pageNumber - 1) * @pageSize ROWS 
        ///   FETCH NEXT @pageSize ROWS ONLY;
        /// 
        /// MEJORA FUTURA: Devolver un objeto PagedResult<Product> que incluya:
        /// - Items (lista de productos)
        /// - TotalCount (total de registros sin paginar)
        /// - PageNumber, PageSize, TotalPages
        /// </remarks>
        Task<IEnumerable<Product>> GetAllAsync(
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene el total de productos activos en el sistema.
        /// </summary>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>Cantidad total de productos.</returns>
        /// <remarks>
        /// Útil para calcular el número total de páginas en la paginación.
        /// TotalPages = Math.Ceiling((double)TotalCount / PageSize)
        /// </remarks>
        Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Agrega un nuevo producto al inventario.
        /// </summary>
        /// <param name="product">Producto a agregar.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>El producto creado con su ID generado.</returns>
        Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un producto existente.
        /// </summary>
        /// <param name="product">Producto con los datos actualizados.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <remarks>
        /// CONCURRENCIA: En EF Core, considera usar [ConcurrencyCheck] o RowVersion
        /// para evitar que dos usuarios actualicen el mismo producto simultáneamente
        /// y uno sobrescriba los cambios del otro (lost update problem).
        /// </remarks>
        Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina un producto (soft delete - marca IsActive = false).
        /// </summary>
        /// <param name="id">ID del producto a eliminar.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <remarks>
        /// SOFT DELETE: No eliminamos físicamente productos porque:
        /// 1. Pueden estar referenciados en pedidos históricos.
        /// 2. Auditoría y trazabilidad (¿quién eliminó qué y cuándo?).
        /// 3. Posibilidad de restaurar productos eliminados por error.
        /// </remarks>
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si existe un producto con el SKU especificado.
        /// </summary>
        /// <param name="sku">SKU a verificar.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>True si existe, false en caso contrario.</returns>
        Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca productos por nombre (búsqueda parcial).
        /// </summary>
        /// <param name="searchTerm">Término de búsqueda.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>Lista de productos que coinciden con el término de búsqueda.</returns>
        /// <remarks>
        /// Implementa búsqueda tipo "LIKE %término%".
        /// NOTA: Para sistemas grandes, considera usar Full-Text Search (PostgreSQL FTS)
        /// o servicios externos como Elasticsearch/Azure Cognitive Search para
        /// búsquedas más sofisticadas (ranking, typo tolerance, sinónimos, etc).
        /// </remarks>
        Task<IEnumerable<Product>> SearchByNameAsync(
            string searchTerm,
            CancellationToken cancellationToken = default);
    }
}
