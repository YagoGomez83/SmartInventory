using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInventory.Application.DTOs.Orders;
using SmartInventory.Application.Interfaces;

namespace SmartInventory.API.Controllers
{
    /// <summary>
    /// Controlador REST para la gestión de pedidos.
    /// </summary>
    /// <remarks>
    /// SEGURIDAD Y AUTORIZACIÓN:
    /// - [Authorize] a nivel de clase: Todos los endpoints requieren autenticación JWT.
    /// - UserId se obtiene del token JWT (previene IDOR attacks).
    /// 
    /// EJEMPLO DE USO EN FRONTEND:
    /// 
    /// // Crear pedido (cualquier usuario autenticado):
    /// fetch('/api/orders', {
    ///   method: 'POST',
    ///   headers: { 
    ///     'Authorization': 'Bearer {token}',
    ///     'Content-Type': 'application/json'
    ///   },
    ///   body: JSON.stringify({
    ///     items: [
    ///       { productId: 1, quantity: 2 },
    ///       { productId: 3, quantity: 1 }
    ///     ]
    ///   })
    /// });
    /// 
    /// CÓDIGOS DE ESTADO HTTP:
    /// - 201 Created: Pedido creado exitosamente.
    /// - 400 Bad Request: Stock insuficiente o error de validación.
    /// - 401 Unauthorized: No autenticado (falta token JWT).
    /// - 404 Not Found: Producto no encontrado.
    /// - 500 Internal Server Error: Error no controlado en el servidor.
    /// 
    /// MANEJO DE EXCEPCIONES:
    /// - InvalidOperationException → 400 Bad Request (stock insuficiente)
    /// - KeyNotFoundException → 404 Not Found (producto no existe)
    /// - Exception general → 500 Internal Server Error
    /// </remarks>
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        /// <param name="orderService">Servicio de gestión de pedidos.</param>
        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Crea un nuevo pedido para el usuario autenticado.
        /// </summary>
        /// <param name="dto">Datos del pedido (lista de items con ProductId y Quantity).</param>
        /// <param name="cancellationToken">Token de cancelación para operaciones asíncronas.</param>
        /// <returns>Detalles del pedido creado con Location header apuntando al recurso.</returns>
        /// <response code="201">Pedido creado exitosamente.</response>
        /// <response code="400">Stock insuficiente o error de validación.</response>
        /// <response code="404">Producto no encontrado.</response>
        /// <response code="401">Usuario no autenticado.</response>
        /// <response code="500">Error interno del servidor.</response>
        /// <remarks>
        /// TRANSACCIONALIDAD:
        /// Este endpoint ejecuta una transacción compleja que incluye:
        /// 1. Validar stock disponible
        /// 2. Crear el pedido
        /// 3. Reducir stock de productos
        /// 4. Registrar movimientos de stock
        /// 
        /// Si cualquier paso falla, se hace rollback automático.
        /// 
        /// SEGURIDAD:
        /// El UserId se extrae del token JWT (ClaimTypes.NameIdentifier).
        /// Un usuario solo puede crear pedidos para sí mismo.
        /// </remarks>
        [HttpPost]
        public async Task<IActionResult> CreateOrder(
            [FromBody] CreateOrderDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Obtener el UserId del token JWT
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "Usuario no autenticado o token inválido." });
                }

                var userId = int.Parse(userIdClaim);

                // Crear el pedido
                var result = await _orderService.CreateOrderAsync(dto, userId, cancellationToken);

                // Retornar 201 Created con Location header
                return Created($"/api/orders/{result.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                // Stock insuficiente u otra operación inválida
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                // Producto no existe
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Error general no controlado
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Error interno del servidor.", details = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene todos los pedidos del usuario autenticado con paginación.
        /// </summary>
        /// <param name="pageNumber">Número de página (1-based, default: 1).</param>
        /// <param name="pageSize">Cantidad de registros por página (default: 10, máximo: 100).</param>
        /// <param name="cancellationToken">Token de cancelación para operaciones asíncronas.</param>
        /// <returns>Lista paginada de pedidos del usuario.</returns>
        /// <response code="200">Lista de pedidos obtenida exitosamente.</response>
        /// <response code="400">Parámetros de paginación inválidos.</response>
        /// <response code="401">Usuario no autenticado.</response>
        /// <response code="500">Error interno del servidor.</response>
        /// <remarks>
        /// SEGURIDAD:
        /// El endpoint solo retorna pedidos del usuario autenticado (userId del token JWT).
        /// Un usuario no puede ver pedidos de otros usuarios (IDOR prevention).
        /// 
        /// PAGINACIÓN:
        /// Use los parámetros pageNumber y pageSize para controlar la cantidad de resultados.
        /// Ejemplo: GET /api/orders?pageNumber=1&pageSize=20
        /// </remarks>
        [HttpGet]
        public async Task<IActionResult> GetMyOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Obtener el UserId del token JWT
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "Usuario no autenticado o token inválido." });
                }

                var userId = int.Parse(userIdClaim);

                // Obtener pedidos paginados
                var orders = await _orderService.GetMyOrdersAsync(userId, pageNumber, pageSize, cancellationToken);

                return Ok(orders);
            }
            catch (ArgumentException ex)
            {
                // Parámetros de paginación inválidos
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Error general no controlado
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Error interno del servidor.", details = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un pedido específico por su ID.
        /// </summary>
        /// <param name="id">ID del pedido a obtener.</param>
        /// <param name="cancellationToken">Token de cancelación para operaciones asíncronas.</param>
        /// <returns>Detalles completos del pedido.</returns>
        /// <response code="200">Pedido obtenido exitosamente.</response>
        /// <response code="401">Usuario no autenticado.</response>
        /// <response code="403">Acceso denegado. El pedido no pertenece al usuario.</response>
        /// <response code="404">Pedido no encontrado.</response>
        /// <response code="500">Error interno del servidor.</response>
        /// <remarks>
        /// VALIDACIÓN DE SEGURIDAD CRÍTICA:
        /// El endpoint verifica que el pedido pertenezca al usuario autenticado.
        /// Si un usuario intenta acceder a un pedido que no es suyo, retorna 403 Forbidden.
        /// 
        /// EJEMPLO DE PREVENCIÓN DE IDOR:
        /// Usuario A (ID=5) intenta: GET /api/orders/123
        /// Si Order #123 pertenece a Usuario B (ID=8) → 403 Forbidden
        /// Si Order #123 pertenece a Usuario A (ID=5) → 200 OK con datos
        /// </remarks>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(
            [FromRoute] int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Obtener el UserId del token JWT
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "Usuario no autenticado o token inválido." });
                }

                var userId = int.Parse(userIdClaim);

                // Obtener el pedido con validación de ownership
                var order = await _orderService.GetOrderByIdAsync(id, userId, cancellationToken);

                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                // Pedido no encontrado
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                // El pedido no pertenece al usuario (IDOR attack prevention)
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Error general no controlado
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Error interno del servidor.", details = ex.Message });
            }
        }
    }
}
