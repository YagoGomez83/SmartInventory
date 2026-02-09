namespace SmartInventory.Domain.Interfaces
{
    /// <summary>
    /// Contrato para el patrón Unit of Work.
    /// </summary>
    /// <remarks>
    /// PATRÓN UNIT OF WORK:
    /// Coordina la escritura de múltiples repositorios en una sola transacción.
    /// Garantiza atomicidad (todo se guarda o nada se guarda).
    /// 
    /// PROPÓSITO:
    /// - Encapsular transacciones explícitas sin exponer el DbContext a Application.
    /// - Mantener Clean Architecture: Application no depende de Infrastructure.
    /// - Simplificar el código de servicios que necesitan transaccionalidad.
    /// 
    /// USO TÍPICO:
    /// await using var transaction = await _unitOfWork.BeginTransactionAsync();
    /// try
    /// {
    ///     // Operaciones con múltiples repositorios...
    ///     await _unitOfWork.SaveChangesAsync();
    ///     await transaction.CommitAsync();
    /// }
    /// catch
    /// {
    ///     await transaction.RollbackAsync();
    ///     throw;
    /// }
    /// </remarks>
    public interface IUnitOfWork : IAsyncDisposable
    {
        /// <summary>
        /// Inicia una transacción explícita en la base de datos.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Objeto de transacción para hacer commit o rollback.</returns>
        Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Guarda todos los cambios pendientes en la base de datos.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación.</param>
        /// <returns>Número de entidades afectadas.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Representa una transacción de base de datos.
    /// </summary>
    public interface ITransaction : IAsyncDisposable
    {
        /// <summary>
        /// Confirma la transacción, haciendo permanentes todos los cambios.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación.</param>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Revierte la transacción, descartando todos los cambios.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación.</param>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
