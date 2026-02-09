using FluentAssertions;

namespace SmartInventory.UnitTests;

/// <summary>
/// 🧪 TEST DUMMY DE VERIFICACIÓN
/// Este test simple verifica que el entorno de pruebas funciona correctamente.
/// Si este test pasa, significa que xUnit, FluentAssertions y la configuración están OK.
/// </summary>
public class UnitTest1
{
    [Fact]
    public void DummyTest_OnePlusOne_ShouldReturnTwo()
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // ESTRUCTURA AAA (Arrange-Act-Assert)
        // ═══════════════════════════════════════════════════════════════════════════════
        // Este es el patrón estándar para escribir tests:
        // - Arrange: Preparar los datos
        // - Act: Ejecutar la acción
        // - Assert: Verificar el resultado

        // Arrange (Preparar)
        int a = 1;
        int b = 1;
        int expectedResult = 2;

        // Act (Actuar)
        int actualResult = a + b;

        // Assert (Verificar)
        // FluentAssertions hace las aserciones más legibles:
        actualResult.Should().Be(expectedResult);

        // También puedes escribir aserciones tradicionales:
        // Assert.Equal(expectedResult, actualResult);
    }
}
