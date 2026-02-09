namespace SmartInventory.Domain.Enums
{
    /// <summary>
    /// Estados del ciclo de vida de un pedido.
    /// </summary>
    /// <remarks>
    /// FLUJO DE ESTADOS:
    /// 1. Pending: Pedido creado, stock reservado. Usuario aún no ha pagado.
    ///    - Acción: Reserva temporal de inventario (StockMovement tipo 'Reserved').
    /// 2. Paid: Pago confirmado por pasarela (Stripe, PayPal, MercadoPago).
    ///    - Acción: Descuento definitivo de stock (StockMovement tipo 'Sale').
    /// 3. Shipped: Pedido enviado al cliente (integración con courier).
    ///    - Acción: Notificación por email + tracking number.
    /// 4. Cancelled: Usuario cancela o pago rechazado.
    ///    - Acción: Devolución de stock (StockMovement tipo 'Return').
    /// 
    /// REGLA DE NEGOCIO:
    /// Los pedidos Pending expiran tras 15 minutos sin pago (Job de fondo).
    /// Esto evita el "cart abandonment" que bloquee stock indefinidamente.
    /// </remarks>
    public enum OrderStatus
    {
        /// <summary>
        /// Pedido creado, esperando pago. Stock reservado temporalmente.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Pago confirmado. Stock descontado definitivamente.
        /// </summary>
        Paid = 1,

        /// <summary>
        /// Pedido enviado al cliente.
        /// </summary>
        Shipped = 2,

        /// <summary>
        /// Pedido cancelado. Stock devuelto al inventario.
        /// </summary>
        Cancelled = 3
    }
}
