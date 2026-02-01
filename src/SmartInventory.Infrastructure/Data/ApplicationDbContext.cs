using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SmartInventory.Domain.Common;
using SmartInventory.Domain.Entities;

namespace SmartInventory.Infrastructure.Data
{
    /// <summary>
    /// Contexto de base de datos principal de la aplicación.
    /// </summary>
    /// <remarks>
    /// ARQUITECTURA LIMPIA:
    /// - Este DbContext reside en Infrastructure, NO en Domain.
    /// - Domain no debe tener dependencias de EF Core (principio de inversión de dependencias).
    /// 
    /// PATRÓN DE CONFIGURACIÓN:
    /// - ApplyConfigurationsFromAssembly: Busca automáticamente todas las clases que implementen
    ///   IEntityTypeConfiguration en el ensamblado actual.
    /// - VENTAJAS:
    ///   * No necesitas registrar manualmente cada configuración.
    ///   * Al agregar nuevas entidades, solo creas su configuración y se detecta automáticamente.
    ///   * Cumple Open/Closed Principle: abierto a extensión, cerrado a modificación.
    /// 
    /// CONVENCIONES:
    /// - DbSet<T>: Representa una tabla. T debe ser una entidad (clase con Id).
    /// - OnModelCreating: Se ejecuta una vez al iniciar la app. Aquí se aplican configuraciones.
    /// 
    /// PERFORMANCE TIPS:
    /// - Para consultas de solo lectura, usa AsNoTracking() para evitar overhead del Change Tracker.
    /// - Para operaciones masivas, considera ExecuteSqlRaw o bulk extensions.
    /// </remarks>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Constructor que recibe opciones de configuración.
        /// </summary>
        /// <param name="options">Opciones configuradas en Program.cs con la cadena de conexión.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// DbSet que representa la tabla de usuarios.
        /// </summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>
        /// DbSet que representa la tabla de productos.
        /// </summary>
        public DbSet<Product> Products => Set<Product>();

        /// <summary>
        /// Configuración del modelo de base de datos.
        /// </summary>
        /// <param name="modelBuilder">Constructor del modelo proporcionado por EF Core.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Aplicar todas las configuraciones automáticamente
            // Busca todas las clases que implementen IEntityTypeConfiguration<T>
            // en el ensamblado actual (SmartInventory.Infrastructure)
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // ALTERNATIVA MANUAL (NO USAR, solo como referencia):
            // modelBuilder.ApplyConfiguration(new UserConfiguration());
            // modelBuilder.ApplyConfiguration(new ProductConfiguration());
            // Problema: Cada nueva entidad requiere modificar esta clase (viola Open/Closed).
        }

        /// <summary>
        /// Sobrescribe SaveChanges para implementar auditoría automática.
        /// </summary>
        /// <remarks>
        /// AUDITORÍA AUTOMÁTICA:
        /// - Detecta entidades que heredan de BaseEntity.
        /// - Al agregar: establece CreatedAt.
        /// - Al modificar: actualiza LastModifiedAt.
        /// - No requiere código manual en cada repositorio.
        /// 
        /// SOFT DELETE:
        /// - En lugar de eliminar físicamente, marca IsActive = false.
        /// - Las consultas deben filtrar automáticamente con Query Filters (futuro).
        /// </remarks>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// Sobrescribe la versión asíncrona de SaveChanges.
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Actualiza automáticamente las propiedades de auditoría.
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }

                if (entry.State == EntityState.Modified)
                {
                    entity.LastModifiedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
