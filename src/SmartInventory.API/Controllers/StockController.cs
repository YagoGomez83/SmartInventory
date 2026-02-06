using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInventory.Application.DTOs.Stock;
using SmartInventory.Application.Interfaces;
using System.Security.Claims;

namespace SmartInventory.API.Controllers
{
    /// <summary>
    /// Controlador REST para la gestión de movimientos de inventario (stock).
    /// </summary>
    /// <remarks>
    /// SEGURIDAD Y AUTORIZACIÓN:
    /// - [Authorize] a nivel de clase: Todos los endpoints requieren autenticación JWT.
    /// - Todos los movimientos de stock quedan registrados con el UserId para auditoría.
    /// 
    /// CASOS DE USO:
    /// 1. POST /api/stock/adjustment → Registrar entrada, salida o ajuste de inventario.
    /// 
    /// EJEMPLO DE USO EN FRONTEND:
    /// 
    /// // Registrar compra (entrada de mercancía):
    /// fetch('/api/stock/adjustment', {
    ///   method: 'POST',
    ///   headers: { 
    ///     'Authorization': 'Bearer {token}',
    ///     'Content-Type': 'application/json'
    ///   },
    ///   body: JSON.stringify({
    ///     productId: 1,
    ///     quantity: 50,
    ///     type: "Purchase",
    ///     reason: "Compra a proveedor ABC"
    ///   })
    /// });
    /// 
    /// // Registrar venta (salida):
    /// fetch('/api/stock/adjustment', {
    ///   method: 'POST',
    ///   headers: { 
    ///     'Authorization': 'Bearer {token}',
    ///     'Content-Type': 'application/json'
    ///   },
    ///   body: JSON.stringify({
    ///     productId: 1,
    ///     quantity: 10,
    ///     type: "Sale",
    ///     reason: "Venta a cliente XYZ"
    ///   })
    /// });
    /// 
    /// CÓDIGOS DE ESTADO HTTP:
    /// - 200 OK: Ajuste registrado exitosamente.
    /// - 400 Bad Request: Stock insuficiente o error de validación.
    /// - 401 Unauthorized: No autenticado (falta token JWT).
    /// - 404 Not Found: Producto no encontrado.
    /// - 500 Internal Server Error: Error no controlado en el servidor.
    /// 
    /// MANEJO DE EXCEPCIONES:
    /// - KeyNotFoundException → 404 Not Found (producto no existe).
    /// - InvalidOperationException → 400 Bad Request (stock negativo o error de negocio).
    /// </remarks>
    [ApiController]
    [Route("api/stock")]
    [Authorize]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StockController> _logger;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        /// <param name="stockService">Servicio de gestión de stock.</param>
        /// <param name="logger">Logger para auditoría y diagnóstico.</param>
        public StockController(IStockService stockService, ILogger<StockController> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        /// <summary>
        /// Ajusta el stock de un producto (entrada, salida o corrección).
        /// </summary>
        /// <param name="dto">Datos del ajuste: ProductId, Quantity, Type, Reason.</param>
        /// <param name="cancellationToken">Token para cancelar la operación.</param>
        /// <returns>Información del movimiento registrado y el nuevo stock.</returns>
        /// <response code="200">Ajuste registrado exitosamente.</response>
        /// <response code="400">Stock insuficiente o error de validación.</response>
        /// <response code="401">Usuario no autenticado.</response>
        /// <response code="404">Producto no encontrado.</response>
        /// <remarks>
        /// EJEMPLO DE REQUEST:
        /// 
        /// POST /api/stock/adjustment
        /// {
        ///   "productId": 1,
        ///   "quantity": 100,
        ///   "type": "Purchase",
        ///   "reason": "Reabastecimiento mensual"
        /// }
        /// 
        /// TIPOS DE MOVIMIENTO:
        /// - Purchase: Entrada de mercancía (compra a proveedor).
        /// - Sale: Salida de mercancía (venta a cliente).
        /// - Adjustment: Corrección por inventario físico.
        /// 
        /// VALIDACIONES:
        /// - El producto debe existir.
        /// - La cantidad debe ser mayor a 0.
        /// - El stock resultante NO puede ser negativo.
        /// </remarks>
        [HttpPost("adjustment")]
        [ProducesResponseType(typeof(StockMovementResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AdjustStock(
            [FromBody] StockAdjustmentDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Obtener el UserId del usuario autenticado desde los Claims del token JWT
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("Intento de ajuste de stock sin UserId en el token JWT");
                    return Unauthorized(new { Message = "Token JWT inválido: falta el UserId" });
                }

                var userId = int.Parse(userIdClaim);

                // Registrar el movimiento de stock
                _logger.LogInformation(
                    "Usuario {UserId} ajustando stock del producto {ProductId}: {Quantity} unidades ({Type})",
                    userId, dto.ProductId, dto.Quantity, dto.Type);

                var result = await _stockService.AdjustStockAsync(dto, userId, cancellationToken);

                _logger.LogInformation(
                    "Ajuste de stock exitoso. Movimiento ID: {MovementId}, Nuevo stock: {NewStock}",
                    result.MovementId, result.NewStock);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Producto no encontrado: {ProductId}", dto.ProductId);
                return NotFound(new { Message = $"El producto con ID {dto.ProductId} no existe" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validación en ajuste de stock: {Message}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al ajustar stock del producto {ProductId}", dto.ProductId);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "Error interno del servidor al procesar el ajuste de stock" });
            }
        }
    }
}
