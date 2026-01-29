using SmartInventory.Domain.Entities;

namespace SmartInventory.Domain.Interfaces
{
    /// <summary>
    /// Contrato para operaciones de persistencia de usuarios.
    /// </summary>
    /// <remarks>
    /// PRINCIPIO DE INVERSIÓN DE DEPENDENCIA (DIP):
    /// - Esta interfaz vive en el Domain (capa de alto nivel).
    /// - La implementación concreta estará en Infrastructure (capa de bajo nivel).
    /// - Application y Domain dependen de esta abstracción, NO de la implementación.
    /// 
    /// DISEÑO ASÍNCRONO:
    /// - Todos los métodos retornan Task<T> para operaciones no bloqueantes.
    /// - CancellationToken permite cancelar operaciones largas (buena práctica en APIs web).
    /// 
    /// NULLABILIDAD:
    /// - User? indica que puede retornar null (C# 8.0+ Nullable Reference Types).
    /// - Esto previene NullReferenceException en tiempo de compilación.
    /// </remarks>
    public interface IUserRepository
    {
        /// <summary>
        /// Agrega un nuevo usuario al sistema.
        /// </summary>
        /// <param name="user">Usuario a agregar.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>El usuario creado con su ID generado.</returns>
        Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un usuario por su dirección de correo electrónico.
        /// </summary>
        /// <param name="email">Email del usuario a buscar.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>El usuario encontrado o null si no existe.</returns>
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un usuario por su ID.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>El usuario encontrado o null si no existe.</returns>
        Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si existe un usuario con el email especificado.
        /// </summary>
        /// <param name="email">Email a verificar.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>True si existe, false en caso contrario.</returns>
        /// <remarks>
        /// Este método es más eficiente que GetByEmailAsync cuando solo necesitas
        /// verificar existencia, ya que no carga toda la entidad en memoria.
        /// En SQL: SELECT EXISTS(SELECT 1 FROM Users WHERE Email = @email)
        /// </remarks>
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un usuario existente.
        /// </summary>
        /// <param name="user">Usuario con los datos actualizados.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina un usuario (soft delete - marca IsActive = false).
        /// </summary>
        /// <param name="id">ID del usuario a eliminar.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <remarks>
        /// IMPORTANTE: No eliminamos físicamente (DELETE FROM). 
        /// Implementamos soft delete por:
        /// 1. Cumplimiento legal (trazabilidad de quién hizo qué).
        /// 2. Integridad referencial (usuarios asociados a pedidos históricos).
        /// 3. Posibilidad de restaurar datos accidentalmente eliminados.
        /// </remarks>
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
