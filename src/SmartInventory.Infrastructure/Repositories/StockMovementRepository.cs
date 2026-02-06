using Microsoft.EntityFrameworkCore;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Interfaces;
using SmartInventory.Infrastructure.Data;

namespace SmartInventory.Infrastructure.Repositories
{
    /// <summary>
    /// Implementación del repositorio de movimientos de stock utilizando Entity Framework Core.
    /// </summary>
    /// <remarks>
    /// MEJORES PRÁCTICAS IMPLEMENTADAS:
    /// 
    /// 1. INMUTABILIDAD:
    ///    - Los movimientos de stock son append-only (solo inserción).
    ///    - NUNCA se actualizan o eliminan, garantizando auditoría completa.
    ///    - Patrón Event Sourcing parcial para trazabilidad total.
    /// 
    /// 2. SaveChangesAsync:
    ///    - AddAsync persiste cambios en la BD inmediatamente.
    ///    - Patrón Unit of Work implícito de EF Core.
    /// 
    /// 3. CancellationToken:
    ///    - Todos los métodos async soportan cancelación para escenarios de timeout.
    /// 
    /// 4. AUDITORÍA:
    ///    - Cada movimiento registra quién, cuándo y por qué se realizó el cambio.
    ///    - Cumple con normativas (GDPR, SOX, ISO 9001).
    /// 
    /// ARQUITECTURA:
    /// - Este repositorio es la ÚNICA forma de registrar movimientos de stock.
    /// - El stock de un producto se calcula consultando todos sus movimientos,
    ///   no modificando directamente Product.StockQuantity.
    /// </remarks>
    public sealed class StockMovementRepository : IStockMovementRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor con inyección de dependencias del contexto de base de datos.
        /// </summary>
        /// <param name="context">Contexto de Entity Framework Core.</param>
        public StockMovementRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Registra un nuevo movimiento de stock.
        /// </summary>
        /// <param name="stockMovement">Movimiento de stock a registrar.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>El movimiento de stock registrado con su ID asignado.</returns>
        /// <remarks>
        /// FLUJO:
        /// 1. Validar que el movimiento no sea nulo.
        /// 2. Agregar el movimiento a la colección de EF Core.
        /// 3. Persistir cambios en la base de datos.
        /// 4. EF Core asigna automáticamente el ID generado por la BD.
        /// 
        /// IMPORTANTE:
        /// - NO actualizamos Product.StockQuantity aquí.
        /// - El stock actual se calcula mediante una consulta agregada de movimientos.
        /// - Esto garantiza que el histórico sea la única fuente de verdad.
        /// </remarks>
        public async Task<StockMovement> AddAsync(StockMovement stockMovement, CancellationToken cancellationToken = default)
        {
            if (stockMovement == null)
            {
                throw new ArgumentNullException(nameof(stockMovement), "El movimiento de stock no puede ser nulo.");
            }

            // Agregar el movimiento a la colección de EF Core
            await _context.StockMovements.AddAsync(stockMovement, cancellationToken);

            // Persistir cambios en la base de datos
            await _context.SaveChangesAsync(cancellationToken);

            // Retornar el movimiento con el ID asignado por la BD
            return stockMovement;
        }
    }
}
