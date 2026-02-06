using SmartInventory.Application.DTOs.Stock;
using SmartInventory.Application.Interfaces;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.Application.Services
{
    /// <summary>
    /// Implementación del servicio de gestión de inventario.
    /// </summary>
    /// <remarks>
    /// ARQUITECTURA CLEAN:
    /// - Capa de Aplicación: Orquesta lógica de negocio entre repositorios.
    /// - Aplica el principio de Separación de Responsabilidades (SRP).
    /// - NO conoce detalles de infraestructura (SQL, Entity Framework, etc.).
    /// 
    /// PATRÓN DE DISEÑO:
    /// - Dependency Injection: Los repositorios se inyectan vía constructor.
    /// - Single Responsibility: Solo gestiona operaciones de stock.
    /// 
    /// REGLAS DE NEGOCIO IMPLEMENTADAS:
    /// 1. ✅ Validación de existencia del producto.
    /// 2. ✅ Cálculo correcto de stock según tipo de movimiento.
    /// 3. ✅ Validación de stock no negativo (integridad de datos).
    /// 4. ✅ Registro automático de auditoría (StockMovement).
    /// 5. ✅ Trazabilidad de usuario (CreatedBy).
    /// 
    /// MEJORAS FUTURAS (En siguiente sprint):
    /// - Transacciones explícitas (Unit of Work) para garantizar atomicidad.
    /// - Validaciones con FluentValidation antes de ejecutar.
    /// - Notificaciones cuando stock baja de umbral mínimo.
    /// - Soporte para reservas de stock (pedidos pendientes).
    /// </remarks>
    public sealed class StockService : IStockService
    {
        private readonly IProductRepository _productRepository;
        private readonly IStockMovementRepository _stockMovementRepository;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        /// <param name="productRepository">Repositorio de productos.</param>
        /// <param name="stockMovementRepository">Repositorio de movimientos de stock.</param>
        public StockService(
            IProductRepository productRepository,
            IStockMovementRepository stockMovementRepository)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _stockMovementRepository = stockMovementRepository ?? throw new ArgumentNullException(nameof(stockMovementRepository));
        }

        /// <inheritdoc />
        public async Task<StockMovementResponseDto> AdjustStockAsync(
            StockAdjustmentDto dto,
            int userId,
            CancellationToken cancellationToken = default)
        {
            // ========================================
            // PASO 1: VALIDAR EXISTENCIA DEL PRODUCTO
            // ========================================
            // Regla de Negocio: No se puede ajustar el stock de un producto inexistente.
            var product = await _productRepository.GetByIdAsync(dto.ProductId, cancellationToken);

            if (product is null)
            {
                throw new KeyNotFoundException(
                    $"El producto con ID {dto.ProductId} no existe en el sistema. " +
                    $"Verifica que el ID sea correcto.");
            }

            // ========================================
            // PASO 2: CALCULAR CAMBIO DE STOCK
            // ========================================
            // Lógica de Negocio según tipo de movimiento:
            // - Purchase (Entrada de compra): Suma al stock actual.
            // - Adjustment (Ajuste de inventario): Suma al stock (simplificación por ahora).
            // - Sale (Salida por venta): Resta del stock actual.
            int previousStock = product.StockQuantity;
            int stockChange = dto.Type switch
            {
                MovementType.Purchase => dto.Quantity,    // Entrada: +cantidad
                MovementType.Adjustment => dto.Quantity,  // Ajuste: +cantidad (simplificado)
                MovementType.Sale => -dto.Quantity,       // Salida: -cantidad
                _ => throw new InvalidOperationException(
                    $"Tipo de movimiento no reconocido: {dto.Type}. " +
                    $"Valores válidos: {nameof(MovementType.Purchase)}, " +
                    $"{nameof(MovementType.Sale)}, {nameof(MovementType.Adjustment)}.")
            };

            int newStock = previousStock + stockChange;

            // ========================================
            // PASO 3: VALIDACIÓN CRÍTICA - STOCK NO NEGATIVO
            // ========================================
            // Regla de Negocio CRÍTICA: El stock físico nunca puede ser negativo.
            // Si intentamos vender más de lo que tenemos, rechazamos la operación.
            if (newStock < 0)
            {
                throw new InvalidOperationException(
                    $"Stock insuficiente para realizar esta operación. " +
                    $"Stock actual: {previousStock} unidades. " +
                    $"Cantidad solicitada: {dto.Quantity} unidades. " +
                    $"Diferencia faltante: {Math.Abs(newStock)} unidades. " +
                    $"Por favor, verifica el inventario o reduce la cantidad.");
            }

            // ========================================
            // PASO 4: ACTUALIZAR STOCK DEL PRODUCTO
            // ========================================
            // Actualizamos la cantidad en memoria (EF Core rastreará el cambio).
            product.StockQuantity = newStock;

            // Persistimos el cambio en la base de datos.
            await _productRepository.UpdateAsync(product, cancellationToken);

            // ========================================
            // PASO 5: REGISTRAR MOVIMIENTO PARA AUDITORÍA
            // ========================================
            // Patrón Event Sourcing parcial: NUNCA modificamos/eliminamos movimientos.
            // Cada cambio de stock debe quedar registrado para trazabilidad completa.
            // Esto es crítico para auditorías, contabilidad y cumplimiento normativo.
            var stockMovement = new StockMovement
            {
                ProductId = product.Id,
                Quantity = dto.Quantity,  // Siempre positivo
                Type = dto.Type,
                Reason = dto.Reason,
                CreatedBy = userId,       // Auditoría: quién hizo el cambio
                CreatedAt = DateTime.UtcNow  // Timestamp en UTC (buena práctica)
            };

            // Persistimos el movimiento (inmutable, solo inserción).
            var createdMovement = await _stockMovementRepository.AddAsync(stockMovement, cancellationToken);

            // ========================================
            // PASO 6: RETORNAR RESPUESTA CON TODA LA INFORMACIÓN
            // ========================================
            // Proporcionamos datos completos para que el frontend pueda:
            // 1. Mostrar confirmación al usuario.
            // 2. Actualizar la UI con el nuevo stock sin recargar.
            // 3. Tener referencia del movimiento para consultas futuras.
            return new StockMovementResponseDto(
                MovementId: createdMovement.Id,
                ProductId: product.Id,
                ProductName: product.Name,
                PreviousStock: previousStock,
                NewStock: newStock,
                QuantityChanged: dto.Quantity,
                MovementType: dto.Type,
                Reason: dto.Reason,
                CreatedAt: createdMovement.CreatedAt,
                CreatedByUserId: userId
            );
        }
    }
}
