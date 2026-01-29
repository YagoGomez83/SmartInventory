# 🏢 Smart Inventory & Orders Platform

[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue.svg)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Clean Architecture](https://img.shields.io/badge/architecture-clean-brightgreen.svg)](ARQUITECTURA-CLEAN.md)

Sistema distribuido de gestión de inventario y pedidos construido con **Clean Architecture**, **.NET 8** y **PostgreSQL**.

---

## 🎯 Visión del Proyecto

**Smart Inventory** es una plataforma enterprise-grade diseñada para gestionar:

- 🔐 **Autenticación y Autorización** basada en roles (JWT)
- 📦 **Gestión de Productos** con control de stock en tiempo real
- 📊 **Inventario Inteligente** con entradas/salidas y trazabilidad completa
- 🛒 **Sistema de Pedidos** con transacciones ACID
- 🤖 **Predicción de Stock** mediante análisis estadístico
- 🐳 **Cloud-Ready** con Docker y Kubernetes

---

## 🏗️ Arquitectura

Este proyecto implementa **Clean Architecture** (Uncle Bob), garantizando:

- ✅ Independencia de frameworks
- ✅ Testabilidad completa
- ✅ Independencia de UI y Base de Datos
- ✅ Mantenibilidad a largo plazo

### Estructura de Capas

```
┌─────────────────────────────────────┐
│   SmartInventory.API                │  ← Presentación (Controllers, Middleware)
├─────────────────────────────────────┤
│   SmartInventory.Infrastructure     │  ← Infraestructura (EF Core, Repositorios)
├─────────────────────────────────────┤
│   SmartInventory.Application        │  ← Aplicación (Services, DTOs, Validadores)
├─────────────────────────────────────┤
│   SmartInventory.Domain             │  ← Dominio (Entidades, Interfaces)
└─────────────────────────────────────┘

Dependencias: API → Infrastructure → Application → Domain
              API → Application
```

📖 **[Ver documentación completa de arquitectura](ARQUITECTURA-CLEAN.md)**

---

## 🚀 Tech Stack

| Categoría | Tecnología | Versión | Justificación |
|-----------|-----------|---------|---------------|
| **Framework** | .NET | 9.0 | LTS, performance, fuertemente tipado |
| **Lenguaje** | C# | 12.0 | Nullable reference types, records, pattern matching |
| **Base de Datos** | PostgreSQL | 16 | Open source, JSON nativo, concurrencia avanzada |
| **ORM** | Entity Framework Core | 9.0 | Migrations, LINQ, change tracking |
| **Autenticación** | JWT | - | Stateless, escalable, estándar |
| **Containerización** | Docker | - | Portabilidad, reproducibilidad |
| **Orquestación** | Kubernetes | - | Escalabilidad horizontal, self-healing |

---

## 📁 Estructura del Proyecto

```
SmartInventory/
├── src/
│   ├── SmartInventory.Domain/           # ← Corazón del negocio
│   │   ├── Entities/                    # User, Product, Order
│   │   ├── Enums/                       # UserRole, OrderStatus
│   │   ├── Interfaces/                  # IUserRepository, IProductRepository
│   │   └── Common/                      # BaseEntity
│   │
│   ├── SmartInventory.Application/      # ← Lógica de aplicación
│   │   ├── Services/                    # AuthService, ProductService
│   │   ├── DTOs/                        # RegisterUserDto, ProductResponseDto
│   │   ├── Interfaces/                  # IAuthService
│   │   └── Validators/                  # FluentValidation
│   │
│   ├── SmartInventory.Infrastructure/   # ← Implementación técnica
│   │   ├── Data/                        # ApplicationDbContext, Migrations
│   │   ├── Repositories/                # UserRepository, ProductRepository
│   │   └── Services/                    # JwtTokenGenerator, PasswordHasher
│   │
│   └── SmartInventory.API/              # ← Punto de entrada HTTP
│       ├── Controllers/                 # AuthController, ProductsController
│       ├── Middleware/                  # ExceptionHandler, JwtMiddleware
│       └── Program.cs                   # Configuración DI y pipeline
│
├── tests/
│   ├── SmartInventory.UnitTests/        # Tests unitarios
│   └── SmartInventory.IntegrationTests/ # Tests de integración
│
├── docs/                                 # Documentación adicional
├── docker-compose.yml                    # Entorno de desarrollo
├── ARQUITECTURA-CLEAN.md                 # Fundamentos teóricos
├── PRODUCT-BACKLOG.md                    # Gestión de proyecto
└── README.md                             # Este archivo
```

---

## 🛠️ Requisitos Previos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download) o superior
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (para PostgreSQL)
- [Git](https://git-scm.com/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) o [VS Code](https://code.visualstudio.com/) + extensión C#

---

## 🚀 Inicio Rápido

### 1️⃣ Clonar el repositorio

```bash
git clone https://github.com/YagoGomez83/SmartInventory.git
cd SmartInventory
```

### 2️⃣ Restaurar dependencias

```bash
dotnet restore
```

### 3️⃣ Compilar el proyecto

```bash
dotnet build
```

### 4️⃣ Levantar la base de datos (Docker)

```bash
# Próximamente - Pendiente configurar docker-compose.yml
docker-compose up -d
```

### 5️⃣ Aplicar migraciones

```bash
# Próximamente - Pendiente crear migraciones
dotnet ef database update --project src/SmartInventory.Infrastructure
```

### 6️⃣ Ejecutar la API

```bash
cd src/SmartInventory.API
dotnet run
```

La API estará disponible en: `https://localhost:5001`

---

## 📚 Documentación

| Documento | Descripción |
|-----------|-------------|
| [ARQUITECTURA-CLEAN.md](ARQUITECTURA-CLEAN.md) | Fundamentos teóricos de Clean Architecture |
| [PRODUCT-BACKLOG.md](PRODUCT-BACKLOG.md) | Product Backlog, Sprints y métricas |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Guía para contribuir al proyecto *(pendiente)* |
| [API.md](docs/API.md) | Documentación de endpoints REST *(pendiente)* |

---

## 🎯 Roadmap

### ✅ Sprint 1 - El Cimiento (Enero 2026)
- [x] Configuración de Clean Architecture
- [x] Entidades de dominio (User, Product)
- [x] Interfaces de repositorio
- [x] DTOs y servicios de aplicación
- [ ] Docker + PostgreSQL
- [ ] Autenticación JWT completa

### 📋 Sprint 2 - La Infraestructura (Febrero 2026)
- [ ] Entity Framework Core + Migraciones
- [ ] Implementación de repositorios
- [ ] API REST completa
- [ ] Validación con FluentValidation

### 📋 Sprint 3 - El Negocio (Febrero 2026)
- [ ] Gestión de stock (entradas/salidas)
- [ ] Sistema de pedidos transaccional
- [ ] Tests unitarios e integración

### 📋 Release 1.0 - MVP (Marzo 2026)
- [ ] Dashboard analítico
- [ ] Reportes de inventario
- [ ] CI/CD con GitHub Actions
- [ ] Despliegue en Azure/AWS

---

## 🧪 Testing

```bash
# Ejecutar todos los tests
dotnet test

# Con cobertura de código
dotnet test --collect:"XPlat Code Coverage"

# Solo tests unitarios
dotnet test --filter "FullyQualifiedName~UnitTests"

# Solo tests de integración
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

---

## 🤝 Contribución

Las contribuciones son bienvenidas. Por favor:

1. Haz un fork del proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

**Convenciones de commits:**
```
feat: nueva funcionalidad
fix: corrección de bugs
docs: cambios en documentación
style: formato, punto y coma faltantes, etc
refactor: refactorización de código
test: agregar tests
chore: actualizar dependencias, configuración, etc
```

---

## 📝 Licencia

Este proyecto está bajo la licencia MIT. Ver el archivo [LICENSE](LICENSE) para más detalles.

---

## 👥 Autores

- **Yago Gómez** - *Arquitecto y Desarrollador Principal* - [@YagoGomez83](https://github.com/YagoGomez83)

---

## 🙏 Agradecimientos

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) por Robert C. Martin
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core/)
- Comunidad de .NET y Open Source

---

## 📧 Contacto

¿Preguntas? ¿Sugerencias? Abre un [issue](https://github.com/YagoGomez83/SmartInventory/issues) o contacta al equipo.

---

**⭐ Si este proyecto te resulta útil, considera darle una estrella en GitHub ⭐**
