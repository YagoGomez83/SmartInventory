namespace SmartInventory.Domain.Enums
{
    /// <summary>
    /// Define los tipos de movimientos de inventario (Kardex).
    /// </summary>
    /// <remarks>
    /// CRITERIO DE NEGOCIO:
    /// - Purchase: Entrada de mercancía (compras a proveedores).
    /// - Sale: Salida de mercancía (ventas a clientes).
    /// - Adjustment: Correcciones manuales (inventario físico, mermas, roturas, devoluciones).
    /// 
    /// TRAZABILIDAD:
    /// - Cada movimiento debe tener una razón (Reason) que explique el cambio.
    /// - Esto es crítico para auditorías y cumplimiento normativo.
    /// </remarks>
    public enum MovementType
    {
        /// <summary>
        /// Entrada de mercancía al inventario (compras, devoluciones de clientes).
        /// Incrementa el stock.
        /// </summary>
        Purchase = 1,

        /// <summary>
        /// Salida de mercancía del inventario (ventas, devoluciones a proveedor).
        /// Decrementa el stock.
        /// </summary>
        Sale = 2,

        /// <summary>
        /// Ajuste manual de inventario (conteo físico, corrección de errores, mermas, robos).
        /// Puede incrementar o decrementar el stock según la cantidad vs stock actual.
        /// </summary>
        Adjustment = 3
    }
}
