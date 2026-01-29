namespace SmartInventory.Domain.Enums
{
    /// <summary>
    /// Define los roles de usuario en el sistema.
    /// </summary>
    /// <remarks>
    /// CRITERIO DE SEGURIDAD:
    /// - Los roles determinan permisos mediante políticas de autorización (Policy-Based Authorization).
    /// - En .NET, esto se implementa con [Authorize(Policy = "RequireAdminRole")].
    /// - Para sistemas más complejos, considera implementar Claims-Based o RBAC completo.
    /// </remarks>
    public enum UserRole
    {
        /// <summary>
        /// Usuario estándar con permisos de solo lectura.
        /// Puede consultar inventario y pedidos, pero no modificar.
        /// </summary>
        Employee = 1,

        /// <summary>
        /// Gestor con permisos de escritura.
        /// Puede crear/editar productos, ajustar stock y gestionar pedidos.
        /// </summary>
        Manager = 2,

        /// <summary>
        /// Administrador del sistema con acceso total.
        /// Puede gestionar usuarios, roles, configuración y acceso a todas las funcionalidades.
        /// </summary>
        Admin = 3
    }
}
