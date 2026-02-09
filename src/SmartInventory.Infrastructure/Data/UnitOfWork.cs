using Microsoft.EntityFrameworkCore.Storage;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.Infrastructure.Data
{
    /// <summary>
    /// Implementación del patrón Unit of Work usando Entity Framework Core.
    /// </summary>
    /// <remarks>
    /// RESPONSABILIDAD:
    /// - Coordinar transacciones explícitas entre múltiples repositorios.
    /// - Encapsular el ApplicationDbContext para mantener Clean Architecture.
    /// - Proporcionar SaveChanges centralizado.
    /// 
    /// PATRÓN:
    /// - Wrapper sobre DbContext que implementa IUnitOfWork (interfaz del Domain).
    /// - Permite que la capa Application controle transacciones sin depender de EF Core.
    /// </remarks>
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            return new TransactionWrapper(dbTransaction);
        }

        /// <inheritdoc />
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        /// <summary>
        /// Wrapper que adapta IDbContextTransaction a ITransaction.
        /// </summary>
        private sealed class TransactionWrapper : ITransaction
        {
            private readonly IDbContextTransaction _transaction;

            public TransactionWrapper(IDbContextTransaction transaction)
            {
                _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            }

            public async Task CommitAsync(CancellationToken cancellationToken = default)
            {
                await _transaction.CommitAsync(cancellationToken);
            }

            public async Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                await _transaction.RollbackAsync(cancellationToken);
            }

            public async ValueTask DisposeAsync()
            {
                await _transaction.DisposeAsync();
            }
        }
    }
}
