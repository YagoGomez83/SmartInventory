using Microsoft.EntityFrameworkCore;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Interfaces;
using SmartInventory.Infrastructure.Data;

namespace SmartInventory.Infrastructure.Repositories
{
    /// <summary>
    /// Implementación concreta del repositorio de usuarios usando Entity Framework Core.
    /// </summary>
    /// <remarks>
    /// RESPONSABILIDAD ÚNICA (SRP):
    /// - Esta clase SOLO maneja persistencia de datos de usuarios.
    /// - NO contiene lógica de negocio (eso va en Application/Services).
    /// - NO maneja autenticación (eso va en AuthService).
    /// 
    /// OPTIMIZACIONES:
    /// - AsNoTracking(): Desactiva el Change Tracker de EF Core en consultas de solo lectura.
    ///   * Reduce consumo de memoria (no almacena snapshots de entidades).
    ///   * Mejora rendimiento hasta 30% en consultas grandes.
    ///   * Úsalo cuando no vayas a modificar la entidad después de consultarla.
    /// 
    /// - AnyAsync vs Count: Para verificar existencia, AnyAsync es más eficiente.
    ///   * AnyAsync: SELECT EXISTS(...) → Retorna true/false sin cargar datos.
    ///   * CountAsync: SELECT COUNT(*) → Cuenta todas las filas (más costoso).
    /// 
    /// DECISIÓN DE DISEÑO:
    /// - SaveChangesAsync() se llama en cada operación de escritura (Add, Update, Delete).
    /// - En aplicaciones reales, podrías usar el patrón Unit of Work para transacciones complejas.
    /// - Para simplificar este sprint, guardamos cambios directamente aquí.
    /// </remarks>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        /// <param name="context">Contexto de base de datos inyectado por ASP.NET Core DI.</param>
        public UserRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Agrega un nuevo usuario a la base de datos.
        /// </summary>
        public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            // EF Core detecta que es una entidad nueva (Id = 0) y la marca como Added
            await _context.Users.AddAsync(user, cancellationToken);

            // Persiste los cambios en la base de datos
            // PostgreSQL asignará automáticamente el ID (columna SERIAL/IDENTITY)
            await _context.SaveChangesAsync(cancellationToken);

            // Retornamos el usuario con el ID generado por la base de datos
            return user;
        }

        /// <summary>
        /// Obtiene un usuario por su dirección de correo electrónico.
        /// </summary>
        /// <remarks>
        /// OPTIMIZACIÓN:
        /// - AsNoTracking(): No necesitamos rastrear cambios, solo leer datos.
        /// - SingleOrDefaultAsync(): Lanza excepción si hay duplicados (previene bugs).
        ///   Alternativas:
        ///   * FirstOrDefaultAsync(): Retorna el primero si hay duplicados (menos seguro).
        ///   * SingleOrDefaultAsync(): Asume unicidad (configurada en base de datos).
        /// </remarks>
        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El email no puede estar vacío.", nameof(email));

            return await _context.Users
                .AsNoTracking() // Solo lectura, no rastrear cambios
                .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        /// <summary>
        /// Obtiene un usuario por su ID.
        /// </summary>
        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
                throw new ArgumentException("El ID debe ser mayor que cero.", nameof(id));

            return await _context.Users
                .AsNoTracking() // Solo lectura
                .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        /// <summary>
        /// Verifica si existe un usuario con el email especificado.
        /// </summary>
        /// <remarks>
        /// OPTIMIZACIÓN:
        /// - AnyAsync() es más eficiente que GetByEmailAsync() != null.
        /// - SQL generado: SELECT EXISTS(SELECT 1 FROM Users WHERE Email = @p0)
        /// - No carga la entidad en memoria, solo retorna true/false.
        /// </remarks>
        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El email no puede estar vacío.", nameof(email));

            return await _context.Users
                .AsNoTracking() // Solo lectura
                .AnyAsync(u => u.Email == email, cancellationToken);
        }

        /// <summary>
        /// Actualiza un usuario existente.
        /// </summary>
        /// <remarks>
        /// EF Core detecta cambios automáticamente si la entidad está siendo rastreada.
        /// Si la entidad proviene de AsNoTracking, debes marcarla explícitamente:
        ///   _context.Entry(user).State = EntityState.Modified;
        /// </remarks>
        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            // Marca la entidad como modificada
            _context.Users.Update(user);

            // Persiste los cambios
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Elimina un usuario (soft delete - marca IsActive = false).
        /// </summary>
        /// <remarks>
        /// SOFT DELETE:
        /// - No ejecutamos DELETE FROM Users.
        /// - Solo marcamos IsActive = false.
        /// - Ventajas:
        ///   * Cumplimiento legal (GDPR requiere trazabilidad).
        ///   * Integridad referencial (no rompe relaciones con otras tablas).
        ///   * Posibilidad de restaurar datos.
        /// </remarks>
        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
                throw new ArgumentException("El ID debe ser mayor que cero.", nameof(id));

            // Buscamos el usuario (sin AsNoTracking porque vamos a modificarlo)
            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
                throw new InvalidOperationException($"Usuario con ID {id} no encontrado.");

            // Soft delete: marcamos como inactivo
            user.IsActive = false;

            // EF Core detecta los cambios automáticamente
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
