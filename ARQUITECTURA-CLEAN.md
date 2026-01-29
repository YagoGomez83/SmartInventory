# 🏗️ Clean Architecture - Fundamentos Teóricos

## 📚 Índice

1. [¿Qué es Clean Architecture?](#qué-es-clean-architecture)
2. [Principios SOLID Aplicados](#principios-solid-aplicados)
3. [Las 4 Capas del Sistema](#las-4-capas-del-sistema)
4. [Inversión de Dependencias (DIP)](#inversión-de-dependencias-dip)
5. [Patrones de Diseño Utilizados](#patrones-de-diseño-utilizados)
6. [Convenciones y Buenas Prácticas](#convenciones-y-buenas-prácticas)
7. [Decisiones Arquitectónicas](#decisiones-arquitectónicas)
8. [Referencias y Recursos](#referencias-y-recursos)

---

## 🎯 ¿Qué es Clean Architecture?

**Clean Architecture** (propuesta por Robert C. Martin - "Uncle Bob") es un patrón arquitectónico que busca crear sistemas:

- ✅ **Independientes de frameworks**: La lógica de negocio no depende de ASP.NET, Entity Framework, etc.
- ✅ **Testables**: Puedes probar la lógica sin base de datos, UI o servicios externos.
- ✅ **Independientes de la UI**: Puedes cambiar de React a Angular sin tocar la lógica.
- ✅ **Independientes de la Base de Datos**: Cambiar de PostgreSQL a MongoDB no afecta el negocio.
- ✅ **Independientes de agentes externos**: La lógica no conoce APIs, servicios de email, etc.

### El Principio de las Dependencias

> **"Las dependencias del código fuente solo pueden apuntar HACIA ADENTRO."**

```
┌─────────────────────────────────────────────┐
│   FRAMEWORKS & DRIVERS                       │  ← Más bajo nivel
│   (DB, Web, UI, Devices, External APIs)    │     (Detalles)
├─────────────────────────────────────────────┤
│   INTERFACE ADAPTERS                         │
│   (Controllers, Gateways, Presenters)       │
├─────────────────────────────────────────────┤
│   APPLICATION BUSINESS RULES                 │
│   (Use Cases, Services)                     │
├─────────────────────────────────────────────┤
│   ENTERPRISE BUSINESS RULES                  │  ← Más alto nivel
│   (Entities, Domain Logic)                  │     (Políticas)
└─────────────────────────────────────────────┘
        ↑ Las flechas solo apuntan hacia dentro
```

---

## 🔧 Principios SOLID Aplicados

### **S - Single Responsibility Principle (Responsabilidad Única)**

> "Una clase debe tener solo una razón para cambiar."

**✅ Implementado en:**
- `User.cs`: Solo representa el concepto de Usuario del dominio.
- `AuthService.cs`: Solo maneja autenticación, no hace persistencia directa.
- `IUserRepository.cs`: Solo define el contrato de persistencia de usuarios.

**❌ Antipatrón común:**
```csharp
// MAL: Clase que hace TODO
public class UserController
{
    public void Register(string email, string password)
    {
        // Valida
        if (string.IsNullOrEmpty(email)) throw new Exception();
        
        // Conecta a BD directamente
        var connection = new SqlConnection("...");
        connection.Execute("INSERT INTO Users...");
        
        // Envia email
        SmtpClient.Send(email, "Bienvenido");
    }
}
```

**✅ Correcto:**
```csharp
// Controller: Solo maneja HTTP
// AuthService: Solo lógica de autenticación
// IUserRepository: Solo persistencia
// IEmailService: Solo envío de emails
```

---

### **O - Open/Closed Principle (Abierto/Cerrado)**

> "Abierto para extensión, cerrado para modificación."

**✅ Implementado en:**

```csharp
// No necesitas modificar AuthService si cambias la BD
public class AuthService
{
    private readonly IUserRepository _repository; // Interfaz (abstracción)
    
    // Puedes extender creando nuevas implementaciones
    // sin tocar este código
}

// Extensión 1: PostgreSQL
public class PostgreSqlUserRepository : IUserRepository { }

// Extensión 2: MongoDB
public class MongoUserRepository : IUserRepository { }

// Extensión 3: Cache en memoria
public class InMemoryUserRepository : IUserRepository { }
```

---

### **L - Liskov Substitution Principle (Sustitución de Liskov)**

> "Los objetos de una subclase deben poder reemplazar objetos de la superclase."

**✅ Implementado en:**

Cualquier implementación de `IUserRepository` puede usarse en `AuthService`:

```csharp
// Todas estas líneas son válidas:
IUserRepository repo1 = new PostgreSqlUserRepository();
IUserRepository repo2 = new MongoUserRepository();
IUserRepository repo3 = new InMemoryUserRepository();

// AuthService funciona con cualquiera
var service = new AuthService(repo1); // ✓
var service = new AuthService(repo2); // ✓
var service = new AuthService(repo3); // ✓
```

---

### **I - Interface Segregation Principle (Segregación de Interfaces)**

> "Ningún cliente debe depender de métodos que no usa."

**✅ Implementado en:**

No creamos una interfaz gigante `IRepository<T>` con 50 métodos. Cada repositorio tiene solo lo que necesita:

```csharp
// ✓ Específico para User
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(...);  // User necesita esto
    Task<bool> ExistsByEmailAsync(...); // User necesita esto
}

// ✓ Específico para Product
public interface IProductRepository
{
    Task<Product?> GetBySkuAsync(...);  // Product necesita esto
    Task<IEnumerable<Product>> SearchByNameAsync(...); // Product necesita esto
}
```

**❌ Antipatrón:**
```csharp
// MAL: Interfaz genérica obliga a implementar métodos innecesarios
public interface IRepository<T>
{
    Task<T> GetById(int id);
    Task<T> GetByEmail(string email); // ❌ Product no tiene email
    Task<T> GetBySku(string sku);     // ❌ User no tiene SKU
    // ... 40 métodos más que no todos usan
}
```

---

### **D - Dependency Inversion Principle (Inversión de Dependencias)**

> "Depende de abstracciones, no de concreciones."

**✅ Implementado en:**

```csharp
// ❌ ANTES (Acoplamiento directo):
public class AuthService
{
    private PostgreSqlUserRepository _repo = new PostgreSqlUserRepository();
    //      ↑ Depende de la implementación concreta
}

// ✅ DESPUÉS (Inversión de dependencia):
public class AuthService
{
    private readonly IUserRepository _repo;
    //                ↑ Depende de la abstracción
    
    public AuthService(IUserRepository repo)
    {
        _repo = repo; // Se inyecta desde afuera
    }
}
```

**Beneficios:**
1. **Testabilidad**: Puedes inyectar un mock en tests.
2. **Flexibilidad**: Cambias la implementación sin tocar `AuthService`.
3. **Desacoplamiento**: `AuthService` no conoce PostgreSQL, Entity Framework, etc.

---

## 🏛️ Las 4 Capas del Sistema

### 1️⃣ **Domain Layer** (Capa de Dominio)

**Ubicación:** `SmartInventory.Domain`

**Responsabilidades:**
- Entidades de negocio (`User`, `Product`)
- Reglas de negocio puras
- Interfaces de repositorio (contratos)
- Enums y Value Objects
- Excepciones de dominio

**Dependencias:** 
- ❌ CERO. No conoce ninguna tecnología.
- ✅ Solo depende de C# estándar.

**Ejemplo:**
```csharp
// User.cs - Lógica de dominio pura
public sealed class User : BaseEntity
{
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    
    // Propiedad calculada (lógica de negocio)
    public string FullName => $"{FirstName} {LastName}".Trim();
}
```

---

### 2️⃣ **Application Layer** (Capa de Aplicación)

**Ubicación:** `SmartInventory.Application`

**Responsabilidades:**
- Casos de uso (Services)
- DTOs (Data Transfer Objects)
- Validadores
- Mappers
- Interfaces de servicios externos

**Dependencias:**
- ✅ Solo depende de `Domain`
- ❌ No conoce la infraestructura concreta

**Ejemplo:**
```csharp
// AuthService.cs - Orquesta la lógica
public class AuthService : IAuthService
{
    private readonly IUserRepository _repository;
    
    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        // 1. Validar reglas de negocio
        if (await _repository.ExistsByEmailAsync(dto.Email))
            throw new EmailAlreadyExistsException();
        
        // 2. Transformar DTO → Entidad
        var user = new User { /* ... */ };
        
        // 3. Persistir (sin saber cómo se hace)
        await _repository.AddAsync(user);
        
        // 4. Retornar respuesta
        return new AuthResponseDto(/* ... */);
    }
}
```

---

### 3️⃣ **Infrastructure Layer** (Capa de Infraestructura)

**Ubicación:** `SmartInventory.Infrastructure` *(pendiente)*

**Responsabilidades:**
- Implementación de repositorios (EF Core)
- DbContext y configuraciones
- Servicios externos (Email, Storage, APIs)
- Migraciones de base de datos

**Dependencias:**
- ✅ Depende de `Domain` y `Application`
- ✅ Usa frameworks concretos (EF Core, Npgsql)

**Ejemplo:**
```csharp
// UserRepository.cs - Implementación concreta
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }
}
```

---

### 4️⃣ **API/Presentation Layer** (Capa de Presentación)

**Ubicación:** `SmartInventory.API` *(pendiente)*

**Responsabilidades:**
- Controllers (REST API)
- Middleware
- Filtros de validación
- Configuración de DI (Dependency Injection)

**Dependencias:**
- ✅ Depende de `Application` e `Infrastructure`
- ✅ Usa ASP.NET Core

**Ejemplo:**
```csharp
// AuthController.cs
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return Ok(result);
    }
}
```

---

## 🔄 Inversión de Dependencias (DIP)

### El Flujo de Ejecución vs. Dependencias

```
┌─────────────┐
│ Controller  │  ← Usuario hace HTTP POST /api/auth/register
└──────┬──────┘
       │ Llama
       ↓
┌─────────────┐
│ AuthService │  ← Orquesta la lógica
└──────┬──────┘
       │ Usa
       ↓
┌──────────────────┐
│ IUserRepository  │  ← Interfaz (Contrato)
└──────────────────┘
       ↑ Implementa
┌──────────────────┐
│ UserRepository   │  ← Implementación concreta (EF Core)
└──────────────────┘
```

**Flujo de Ejecución:** Controller → Service → Repository → Base de Datos

**Dependencias de Compilación:**
```
Infrastructure → Application → Domain
    API → Application
    API → Infrastructure

Domain NO conoce a nadie ← CLAVE
```

---

## 🎨 Patrones de Diseño Utilizados

### 1. **Repository Pattern**
Abstrae el acceso a datos.

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User> AddAsync(User user);
}
```

### 2. **Service Pattern**
Encapsula lógica de negocio.

```csharp
public class AuthService : IAuthService
{
    // Orquesta múltiples operaciones
}
```

### 3. **DTO Pattern**
Separa la representación interna de la externa.

```csharp
// Entrada
public record RegisterUserDto(string Email, string Password);

// Salida
public record AuthResponseDto(string Token, string Email);
```

### 4. **Dependency Injection**
Inyecta dependencias en tiempo de ejecución.

```csharp
// Program.cs
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
```

---

## 📏 Convenciones y Buenas Prácticas

### Nomenclatura

| Tipo | Convención | Ejemplo |
|------|-----------|---------|
| **Entidades** | PascalCase, singular | `User`, `Product`, `Order` |
| **Interfaces** | `I` + PascalCase | `IUserRepository`, `IAuthService` |
| **DTOs** | Descriptivo + `Dto` | `RegisterUserDto`, `UpdateProductDto` |
| **Servicios** | Nombre + `Service` | `AuthService`, `ProductService` |
| **Repositorios** | Entidad + `Repository` | `UserRepository`, `ProductRepository` |

### Asincronía

✅ **Siempre usar `async/await` en operaciones I/O:**

```csharp
// ✓ CORRECTO
public async Task<User?> GetByIdAsync(int id)
{
    return await _context.Users.FindAsync(id);
}

// ✗ INCORRECTO
public User? GetById(int id)
{
    return _context.Users.Find(id); // Bloquea el hilo
}
```

### Nullabilidad

✅ **Usar Nullable Reference Types (C# 8.0+):**

```csharp
// Indica explícitamente que puede ser null
Task<User?> GetByIdAsync(int id);

// No puede ser null
Task<User> AddAsync(User user);
```

### Records para DTOs

✅ **Usar `record` en lugar de `class` para DTOs:**

```csharp
// ✓ Inmutable, equals por valor, sintaxis concisa
public record RegisterUserDto(string Email, string Password);

// ✗ Mutable, equals por referencia, más verboso
public class RegisterUserDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}
```

---

## 🎯 Decisiones Arquitectónicas

### ¿Por qué .NET 8?
- ✅ LTS (Long Term Support hasta 2026)
- ✅ Performance superior (benchmarks)
- ✅ Fuertemente tipado (menos errores en runtime)
- ✅ Ecosistema maduro

### ¿Por qué PostgreSQL?
- ✅ Open source y gratuito
- ✅ Soporte JSON nativo
- ✅ Mejor manejo de concurrencia que MySQL
- ✅ Cumplimiento ACID completo

### ¿Por qué Clean Architecture?
- ✅ Independencia de frameworks
- ✅ Testabilidad
- ✅ Mantenibilidad a largo plazo
- ✅ Facilita el trabajo en equipo

### ¿Por qué Entity Framework Core?
- ✅ ORM maduro y performante
- ✅ Code-First + Migrations
- ✅ LINQ (queries tipadas)
- ✅ Change Tracking automático

---

## 📖 Referencias y Recursos

### Libros
- **"Clean Architecture"** - Robert C. Martin
- **"Domain-Driven Design"** - Eric Evans
- **"Patterns of Enterprise Application Architecture"** - Martin Fowler

### Recursos Online
- [Microsoft Docs - ASP.NET Core](https://docs.microsoft.com/aspnet/core)
- [Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture)
- [SOLID Principles](https://www.digitalocean.com/community/conceptual_articles/s-o-l-i-d-the-first-five-principles-of-object-oriented-design)

### Benchmarks
- [TechEmpower Framework Benchmarks](https://www.techempower.com/benchmarks/)
- [.NET Performance](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-8/)

---

## 🎓 Conclusión

Clean Architecture no es solo "código bonito". Es una **inversión a largo plazo** que:

1. **Reduce costos de mantenimiento**: Cambios aislados, no en cascada.
2. **Facilita el testing**: Lógica de negocio testable sin infraestructura.
3. **Mejora el onboarding**: Nuevos desarrolladores entienden la estructura fácilmente.
4. **Permite escalar**: Puedes migrar a microservicios sin reescribir todo.

> **"El buen diseño arquitectónico es una inversión. El mal diseño es una deuda técnica con intereses compuestos."**

---

**Última actualización:** Enero 2026  
**Autor:** Arquitectura de SmartInventory  
**Versión:** 1.0
