using SmartInventory.Domain.Common;
using SmartInventory.Domain.Enums;

namespace SmartInventory.Domain.Entities
{
    /// <summary>
    /// Entidad que representa un pedido/orden de compra (cabecera).
    /// </summary>
    /// <remarks>
    /// PATRÓN MAESTRO-DETALLE:
    /// Order es el "Maestro" (Aggregate Root en DDD). Contiene la colección de OrderItems (Detalle).
    /// 
    /// AGGREGATE ROOT (Domain-Driven Design):
    /// - Order es la raíz del agregado. Para crear/modificar OrderItems, se opera sobre Order.
    /// - Garantiza consistencia transaccional: Order + OrderItems se salvan en una sola transacción.
    /// - Evita el "anemic domain model": Order tiene lógica de negocio (ej: CalcularTotal()).
    /// 
    /// CÁLCULO DE TOTAL:
    /// TotalAmount = Suma de OrderItems.Total. Se recalcula al agregar/quitar items.
    /// Alternativas:
    /// 1. Computed Property: Itera OrderItems en runtime. Más costoso si hay +100 items.
    /// 2. Persistido: Guardado en BD. Requiere lógica para mantener sincronizado.
    /// Aquí persiste porque las órdenes son "write-once, read-many" (escritura única, muchas lecturas).
    /// 
    /// SEGURIDAD:
    /// UserId es obligatorio. Nunca permitir pedidos anónimos (fraude, abuso).
    /// </remarks>
    public sealed class Order : BaseEntity
    {
        /// <summary>
        /// ID del usuario que realizó el pedido.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Navegación al usuario propietario del pedido.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// Fecha y hora de creación del pedido (UTC).
        /// </summary>
        /// <remarks>
        /// SIEMPRE UTC. Las conversiones a zona local se hacen en el frontend.
        /// Antipatrón: Guardar DateTime.Now (hora local del servidor). 
        /// Problema: Si el servidor está en Virginia (UTC-5) y el cliente en Tokio (UTC+9),
        /// verá horarios incorrectos. UTC es la fuente de verdad.
        /// </remarks>
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Estado actual del pedido.
        /// </summary>
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        /// <summary>
        /// Monto total del pedido (suma de OrderItems.Total).
        /// </summary>
        /// <remarks>
        /// PERSISTIDO en BD para performance (evita SUM en queries de reportes).
        /// Debe recalcularse al agregar/modificar items. Ver método CalculateTotal().
        /// </remarks>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Colección de items/líneas del pedido.
        /// </summary>
        /// <remarks>
        /// NAVEGACIÓN INVERSA:
        /// EF Core configura automáticamente la relación bidireccional Order <--> OrderItems.
        /// Inicializada como List vacía para evitar NullReferenceException.
        /// 
        /// EAGER LOADING OBLIGATORIO:
        /// Sin 'virtual' no hay lazy loading (mejor rendimiento, más predecible).
        /// SIEMPRE usar .Include(o => o.OrderItems) en las queries del repositorio.
        /// Ventaja: Elimina N+1 queries, control explícito sobre qué se carga.
        /// </remarks>
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        /// <summary>
        /// Recalcula el total del pedido sumando los subtotales de los items.
        /// </summary>
        /// <remarks>
        /// LÓGICA DE DOMINIO:
        /// Este método encapsula la regla de negocio del cálculo del total.
        /// Debe llamarse antes de persistir la orden en BD.
        /// </remarks>
        public void CalculateTotal()
        {
            TotalAmount = OrderItems.Sum(item => item.Total);
        }
    }
}
