using SmartInventory.Domain.Entities;

namespace SmartInventory.Domain.Interfaces
{
    /// <summary>
    /// Contrato para operaciones de persistencia de movimientos de stock.
    /// </summary>
    /// <remarks>
    /// PATRÓN REPOSITORY:
    /// - Abstrae la lógica de acceso a datos de la lógica de negocio.
    /// - Permite cambiar la fuente de datos sin afectar la capa de aplicación.
    /// - Facilita el testing (podemos usar repositorios en memoria para tests).
    /// 
    /// EVENT SOURCING PARCIAL:
    /// - Los movimientos de stock son INMUTABLES (solo inserción, nunca actualización).
    /// - Esto garantiza una auditoría completa del histórico de cambios.
    /// - El stock actual se calcula como: Σ (Entradas) - Σ (Salidas) + Σ (Ajustes)
    /// </remarks>
    public interface IStockMovementRepository
    {
        /// <summary>
        /// Registra un nuevo movimiento de stock.
        /// </summary>
        /// <param name="stockMovement">Movimiento de stock a registrar.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>El movimiento de stock registrado con su ID asignado.</returns>
        /// <remarks>
        /// REGLAS DE NEGOCIO:
        /// - La cantidad siempre debe ser positiva.
        /// - El tipo de movimiento determina si suma o resta del stock.
        /// - CreatedBy es obligatorio para auditoría.
        /// - Reason es obligatorio para ajustes, opcional para compras/ventas.
        /// </remarks>
        Task<StockMovement> AddAsync(StockMovement stockMovement, CancellationToken cancellationToken = default);
    }
}
