using Microsoft.EntityFrameworkCore;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Interfaces;
using SmartInventory.Infrastructure.Data;

namespace SmartInventory.Infrastructure.Repositories
{
    /// <summary>
    /// Implementación del repositorio de productos utilizando Entity Framework Core.
    /// </summary>
    /// <remarks>
    /// MEJORES PRÁCTICAS IMPLEMENTADAS:
    /// 
    /// 1. AsNoTracking() en lecturas:
    ///    - Mejora el rendimiento al no cargar entidades en el change tracker de EF Core.
    ///    - Usado en GetAllAsync, GetByIdAsync (lectura), GetBySkuAsync, SearchByNameAsync.
    ///    - NO usado en UpdateAsync o DeleteAsync porque necesitamos tracking para actualizar.
    /// 
    /// 2. SaveChangesAsync en escrituras:
    ///    - AddAsync, UpdateAsync, DeleteAsync persisten cambios en la BD.
    ///    - Patrón Unit of Work implícito de EF Core.
    /// 
    /// 3. Soft Delete:
    ///    - DeleteAsync marca IsActive = false en lugar de eliminar físicamente.
    ///    - Mantiene integridad referencial y auditoría.
    /// 
    /// 4. Paginación:
    ///    - GetAllAsync usa Skip/Take para cargar solo la página solicitada.
    ///    - Evita cargar miles de registros en memoria.
    /// 
    /// 5. CancellationToken:
    ///    - Todos los métodos async soportan cancelación para escenarios de timeout.
    /// </remarks>
    public sealed class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor con inyección de dependencias del contexto de base de datos.
        /// </summary>
        /// <param name="context">Contexto de Entity Framework Core.</param>
        public ProductRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Obtiene un producto por su ID.
        /// </summary>
        public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking() // No necesitamos tracking para lecturas
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
        }

        /// <summary>
        /// Obtiene un producto por su SKU.
        /// </summary>
        public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return null;

            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SKU == sku && p.IsActive, cancellationToken);
        }

        /// <summary>
        /// Obtiene todos los productos con paginación.
        /// </summary>
        /// <remarks>
        /// PAGINACIÓN:
        /// - Skip((pageNumber - 1) * pageSize): Salta los registros de páginas anteriores.
        /// - Take(pageSize): Toma solo los registros de la página actual.
        /// 
        /// Ejemplo: pageNumber=2, pageSize=10
        ///   Skip((2-1)*10) = Skip(10) -> Salta los primeros 10 registros (página 1)
        ///   Take(10) -> Toma los siguientes 10 registros (página 2)
        /// 
        /// En SQL se traduce a: OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY
        /// </remarks>
        public async Task<IEnumerable<Product>> GetAllAsync(
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Validación de parámetros
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100; // Límite máximo para evitar abuso

            return await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive) // Solo productos activos
                .OrderByDescending(p => p.CreatedAt) // Más recientes primero
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Obtiene el total de productos activos.
        /// </summary>
        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .CountAsync(p => p.IsActive, cancellationToken);
        }

        /// <summary>
        /// Agrega un nuevo producto al inventario.
        /// </summary>
        public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            // Aseguramos que el producto esté activo al crearse
            product.IsActive = true;

            await _context.Products.AddAsync(product, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return product;
        }

        /// <summary>
        /// Actualiza un producto existente.
        /// </summary>
        /// <remarks>
        /// IMPORTANTE: El producto debe estar siendo trackeado por EF Core
        /// o se debe usar _context.Products.Update(product) si viene de fuera del contexto.
        /// </remarks>
        public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            // Verificamos que el producto exista
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == product.Id, cancellationToken);

            if (existingProduct == null)
                throw new InvalidOperationException($"Product with ID {product.Id} not found.");

            // Actualizamos las propiedades
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.SKU = product.SKU;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.MinimumStockLevel = product.MinimumStockLevel;
            existingProduct.Category = product.Category;
            existingProduct.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Elimina un producto (soft delete).
        /// </summary>
        /// <remarks>
        /// SOFT DELETE:
        /// - NO eliminamos físicamente el registro de la base de datos.
        /// - Solo marcamos IsActive = false para mantener historial.
        /// - Ventajas: auditoría, posibilidad de restauración, integridad referencial.
        /// </remarks>
        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (product == null)
                throw new InvalidOperationException($"Product with ID {id} not found.");

            // Soft delete: marcamos como inactivo
            product.IsActive = false;
            product.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Verifica si existe un producto con el SKU especificado.
        /// </summary>
        /// <remarks>
        /// VALIDACIÓN CRÍTICA:
        /// Este método es vital para evitar duplicados de SKU.
        /// Debe usarse en la capa de aplicación antes de crear/actualizar productos.
        /// </remarks>
        public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return false;

            return await _context.Products
                .AsNoTracking()
                .AnyAsync(p => p.SKU == sku && p.IsActive, cancellationToken);
        }

        /// <summary>
        /// Busca productos por nombre (búsqueda parcial).
        /// </summary>
        /// <remarks>
        /// Implementa búsqueda tipo LIKE con EF.Functions.Like o Contains.
        /// Para PostgreSQL, EF Core traduce Contains() a ILIKE (case-insensitive).
        /// </remarks>
        public async Task<IEnumerable<Product>> SearchByNameAsync(
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Array.Empty<Product>();

            return await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.Name.Contains(searchTerm))
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }
    }
}
