namespace SmartInventory.Domain.Common
{
    /// <summary>
    /// Clase base abstracta para todas las entidades del dominio.
    /// Proporciona propiedades comunes de auditoría y estado.
    /// </summary>
    /// <remarks>
    /// CRITERIO ARQUITECTÓNICO:
    /// - Todas las entidades heredan de esta clase para garantizar consistencia.
    /// - DateTime.UtcNow: Siempre UTC en servidor. Las conversiones a zona horaria local
    ///   se realizan en el Frontend (React). Guardar hora local es un antipatrón.
    /// - IsActive: Implementa soft delete. Nunca eliminamos físicamente registros
    ///   en producción por trazabilidad y cumplimiento legal (GDPR, SOX, etc).
    /// </remarks>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Identificador único de la entidad.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Fecha y hora UTC de creación del registro.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha y hora UTC de la última modificación.
        /// Null si nunca ha sido modificado.
        /// </summary>
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>
        /// Indica si la entidad está activa (soft delete pattern).
        /// </summary>
        /// <remarks>
        /// En lugar de DELETE físico, hacemos UPDATE IsActive = false.
        /// Beneficios: auditoría, recuperación de datos, cumplimiento legal.
        /// </remarks>
        public bool IsActive { get; set; } = true;
    }
}
