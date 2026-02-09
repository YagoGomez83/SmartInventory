# 📋 Product Backlog & Sprint Tracking

## 🎯 Visión del Producto

**Smart Inventory & Orders Platform** es un sistema distribuido de gestión de inventario y pedidos con las siguientes características clave:

- 🔐 Sistema de autenticación y autorización basado en roles
- 📦 Gestión completa de productos e inventario
- 📊 Control de stock con entradas/salidas
- 🛒 Sistema de pedidos transaccional
- 🤖 Predicción de stock mediante IA
- 🐳 Containerizado y cloud-ready

---

## 📊 Product Backlog

| ID | Módulo | Historia de Usuario / Tarea Técnica | Estado | Prioridad | Valor | Complejidad | Notas |
|----|--------|-------------------------------------|---------|-----------|-------|-------------|-------|
| **PB-01** | **Core** | Configuración inicial de Solución y Arquitectura Limpia | ✅ Completado | Alta | N/A | 3 | Creada estructura de 4 capas |
| **PB-02** | **Core** | Configuración de Docker y PostgreSQL | ✅ Completado | Alta | N/A | 3 | Docker Compose funcionando |
| **PB-03** | **Auth** | Diseño de Entidad User y Roles | ✅ Completado | Alta | Alto | 2 | Incluye enum UserRole |
| **PB-04** | **Auth** | Registro de Usuarios con Hash de contraseña | ✅ Completado | Alta | Alto | 5 | BCrypt implementado |
| **PB-05** | **Auth** | Login y generación de JWT Token | ✅ Completado | Alta | Crítico | 5 | JWT funcionando |
| **PB-06** | **Product** | CRUD de Categorías y Productos | 🔄 En Progreso | Media | Alto | 5 | Interfaces creadas |
| **PB-07** | **Stock** | Ajuste de inventario (Entradas/Salidas) | ✅ Completado | Alta | Crítico | 8 | Sprint 3 completado |
| **PB-08** | **Orders** | Creación de Pedidos (Transaccionalidad compleja) | ✅ Completado | Alta | Crítico | 13 | Sprint 4 completado |
| **PB-09** | **IA** | Servicio de Predicción de Stock (Cálculo estadístico) | 📋 Pendiente | Baja | Medio | 8 | |
| **PB-10** | **DevOps** | Containerización final y Manifests de Kubernetes | 📋 Pendiente | Media | Alto | 8 | |

### Leyenda de Estados
- ✅ **Completado**: Implementado y funcional
- 🔄 **En Progreso**: Iniciado pero no terminado
- 📋 **Pendiente**: No iniciado
- ⏸️ **Bloqueado**: Esperando dependencias
- ❌ **Cancelado**: Descartado

### Complejidad (Fibonacci)
- **1**: Muy simple (< 1 hora)
- **2**: Simple (1-2 horas)
- **3**: Medio (medio día)
- **5**: Complejo (1 día)
- **8**: Muy complejo (2-3 días)
- **13**: Épico (5+ días, considerar dividir)

---

## 🏃‍♂️ Sprint 1: "El Cimiento"

**Duración:** 1 Semana (7 días)  
**Inicio:** 29 Enero 2026  
**Fin:** 5 Febrero 2026  
**Objetivo:** Tener la arquitectura base funcionando, la base de datos conectada mediante Docker y el sistema de Registro/Login operativo.

### Sprint Backlog

| ID | Historia | Tareas Técnicas | Asignado | Estado | Horas Est. | Horas Real |
|----|----------|----------------|----------|--------|------------|------------|
| **PB-01** | Arquitectura Limpia | • Crear solución .NET<br>• Crear 4 proyectos<br>• Configurar referencias<br>• Configurar Git | - | ✅ | 3h | 3h |
| **PB-03** | Entidad User y Roles | • Crear BaseEntity<br>• Crear User entity<br>• Crear UserRole enum<br>• Crear interfaces de repositorio | - | ✅ | 2h | 2.5h |
| **PB-06** | Entidad Product | • Crear Product entity<br>• Crear IProductRepository<br>• Crear DTOs de Product | - | ✅ | 2h | 2h |
| **PB-04** | Registro de Usuarios | • Crear RegisterUserDto<br>• Crear IAuthService<br>• Implementar AuthService<br>• Implementar hashing BCrypt | - | ✅ | 4h | 4.5h |
| **PB-02** | Docker & PostgreSQL | • Crear docker-compose.yml<br>• Configurar PostgreSQL<br>• Configurar pgAdmin<br>• Instalar EF Core<br>• Crear DbContext<br>• Primera migración | - | ✅ | 5h | 5.5h |
| **PB-05** | Login y JWT | • Implementar IJwtTokenGenerator<br>• Configurar JWT en API<br>• Implementar LoginAsync<br>• Crear AuthController<br>• Probar autenticación | - | ✅ | 6h | 7h |

### Capacidad del Sprint
- **Horas disponibles:** 40h (1 persona full-time)
- **Horas planificadas:** 22h
- **Horas reales:** 24.5h
- **Buffer usado:** 2.5h (para debugging y ajustes)

### Definition of Done (DoD)
Para considerar una historia como "Completada", debe cumplir:

- [x] Código implementado siguiendo Clean Architecture
- [x] Código compilado sin errores ni warnings
- [ ] Tests unitarios escritos (min. 80% cobertura)
- [ ] Documentación XML en métodos públicos
- [ ] Code review realizado
- [ ] Integrado en rama `main`
- [ ] Funcionalidad probada manualmente

### Retrospectiva (Post-Sprint)
**Fecha:** 31/01/2026  
**¿Qué salió bien?**
- ✅ Arquitectura base sólida y bien documentada
- ✅ Interfaces claras siguiendo principios SOLID
- ✅ Documentación exhaustiva con comentarios educativos
- ✅ Sistema de autenticación JWT completamente funcional
- ✅ Docker Compose configurado correctamente
- ✅ EF Core con migraciones automáticas funcionando
- ✅ BCrypt implementado para seguridad de contraseñas

**¿Qué mejorar?**
- ⚠️ Conflictos de versiones con Swagger (pospuesto para siguiente sprint)
- ⚠️ Necesidad de agregar tests unitarios
- ⚠️ Documentación de API (Swagger) pendiente

**¿Qué aprendimos?**
- 💡 Clean Architecture facilita mucho la separación de responsabilidades
- 💡 Docker simplifica el setup de desarrollo
- 💡 EF Core Migrations automatiza muy bien la BD
- 💡 JWT es más simple de implementar de lo esperado

**Acción Items:**
- [ ] Implementar Swagger en Sprint 2 con versión compatible
- [ ] Iniciar tests unitarios en Sprint 2
- [ ] Documentar endpoints de API

---

## 🏃‍♂️ Sprint 2: "La Infraestructura"

**Duración:** 1 Semana  
**Inicio:** 6 Febrero 2026  
**Fin:** 12 Febrero 2026  
**Objetivo:** Implementar la capa de infraestructura completa con Entity Framework Core, repositorios concretos, y tener la API REST funcionando con autenticación JWT.

### Sprint Backlog (Planificado)

| ID | Historia | Tareas Técnicas | Estado |
|----|----------|----------------|--------|
| **PB-02** | Infraestructura de Datos | • Implementar UserRepository<br>• Implementar ProductRepository<br>• Configurar Entity Configurations<br>• Crear Seeders | 📋 |
| **PB-04** | Autenticación Completa | • Implementar BCrypt Password Hasher<br>• Implementar JWT Token Generator<br>• Middleware de autenticación | 📋 |
| **PB-05** | API REST | • Implementar AuthController<br>• Implementar ProductsController<br>• Configurar Swagger<br>• Validación con FluentValidation | 📋 |
| **PB-06** | CRUD Productos | • Endpoints GET/POST/PUT/DELETE<br>• Paginación en listados<br>• Búsqueda por nombre/SKU | 📋 |

---

## 🏃‍♂️ Sprint 3: "Gestión de Stock" ✅ COMPLETADO

**Duración:** 1 Semana  
**Inicio:** 6 Febrero 2026  
**Fin:** 12 Febrero 2026  
**Objetivo:** Implementar la gestión completa de stock con entradas/salidas y validaciones de negocio.

### Sprint Backlog

| ID | Historia | Tareas Técnicas | Estado |
|----|----------|----------------|--------|
| **PB-07** | Gestión de Stock | • Crear entidad StockMovement ✅<br>• Implementar StockMovementRepository ✅<br>• Implementar StockService (lógica de negocio) ✅<br>• Crear StockController ✅<br>• Validaciones de stock negativo ✅<br>• Actualizar migraciones de BD ✅ | ✅ |

### Capacidad del Sprint
- **Horas disponibles:** 40h
- **Horas planificadas:** 16h
- **Horas reales:** 18h
- **Puntos completados:** 8

### Definition of Done (DoD)
- [x] Entidad StockMovement creada en Domain
- [x] Repository implementado con EF Core
- [x] Service con validación de stock negativo
- [x] API Endpoint funcional
- [x] Migraciones aplicadas
- [x] Código sin errores ni warnings

### Retrospectiva
**Fecha:** 12/02/2026  
**¿Qué salió bien?**
- ✅ Lógica de negocio clara y bien implementada
- ✅ Validaciones de stock funcionando correctamente
- ✅ Integración con EF Core sin problemas

**¿Qué mejorar?**
- ⚠️ Agregar más tests unitarios
- ⚠️ Documentar mejor los endpoints

---

## 🏃‍♂️ Sprint 4: "Gestión de Pedidos (Orders)" ✅ COMPLETADO

**Duración:** 1 Semana  
**Inicio:** 13 Febrero 2026  
**Fin:** 19 Febrero 2026  
**Objetivo:** Implementar el sistema de pedidos con transaccionalidad completa y reducción automática de stock.

### Sprint Backlog

| ID | Historia | Tareas Técnicas | Estado |
|----|----------|----------------|--------|
| **PB-08** | Sistema de Pedidos | • Crear entidades Order y OrderItem ✅<br>• Implementar OrderRepository con Eager Loading ✅<br>• Implementar UnitOfWork pattern para transacciones ACID ✅<br>• Implementar OrderService (Transacciones atómicas: Crear Pedido + Descontar Stock) ✅<br>• Crear OrdersController ✅<br>• Validaciones de stock disponible ✅ | ✅ |

### Capacidad del Sprint
- **Horas disponibles:** 40h
- **Horas planificadas:** 20h
- **Horas reales:** 22h
- **Puntos completados:** 13

### Definition of Done (DoD)
- [x] Entidades Order y OrderItem creadas en Domain
- [x] Repository implementado con EF Core y Eager Loading
- [x] UnitOfWork pattern implementado para transaccionalidad
- [x] Service con validación de stock y reserva
- [x] API Endpoints funcionales (Create, Read)
- [x] Migraciones aplicadas
- [x] Código sin errores ni warnings

### Retrospectiva
**Fecha:** 9/02/2026  
**¿Qué salió bien?**
- ✅ Transaccionalidad implementada correctamente con UnitOfWork
- ✅ Eager Loading optimiza las consultas
- ✅ Validaciones de negocio robustas
- ✅ Integración completa entre módulos

**¿Qué mejorar?**
- ⚠️ Implementar Swagger para documentación de API
- ⚠️ Agregar tests unitarios con xUnit y Moq
- ⚠️ Implementar logging estructurado

---

## 🏃‍♂️ Sprint 5: "Calidad y Documentación" 🔄 EN PROGRESO

**Duración:** 1 Semana  
**Inicio:** 10 Febrero 2026  
**Fin:** 16 Febrero 2026  
**Objetivo:** Mejorar la calidad del código con tests, documentación API y logging profesional.

### Sprint Backlog

| ID | Historia | Tareas Técnicas | Estado |
|----|----------|----------------|--------|
| **PB-11** | Documentación API | • Implementar Swagger/OpenAPI 📋<br>• Configurar XML Documentation ✅<br>• Documentar todos los endpoints 📋<br>• Agregar ejemplos de requests 📋 | 🔄 |
| **PB-12** | Testing | • Implementar xUnit + Moq 📋<br>• Tests unitarios de StockService 📋<br>• Tests unitarios de OrderService 📋<br>• Coverage mínimo 70% 📋 | 📋 |
| **PB-13** | Logging | • Implementar Serilog 📋<br>• Configurar logs estructurados 📋<br>• Logs en archivos y consola 📋<br>• Integración con Application Insights 📋 | 📋 |

---

## 📈 Métricas del Proyecto

### Progreso General
- **Historias Completadas:** 7 / 10 (70%)
- **Puntos de Historia Completados:** 39 / 59 (66%)
- **Sprints Completados:** 3 / 5 (Sprint 1, Sprint 3 y Sprint 4 completados exitosamente)
- **Sprint Actual:** Sprint 5 - Calidad y Documentación (En Progreso)

### Velocidad del Equipo
- **Sprint 1 (completado):** 15 puntos completados (100% del sprint)
- **Sprint 3 (completado):** 8 puntos completados (100% del sprint)
- **Sprint 4 (completado):** 13 puntos completados (100% del sprint)
- **Velocidad promedio:** 12 puntos por sprint

### Cobertura de Código
- **Domain:** 0% (sin tests aún)
- **Application:** 0% (sin tests aún)
- **Infrastructure:** 0% (sin tests aún)
- **API:** 0% (sin tests aún)
- **Objetivo:** 80%

### Calidad del Código
- **Warnings:** 0
- **Errores de compilación:** 0
- **Code Smells (SonarQube):** Pendiente análisis
- **Deuda Técnica:** Baja (proyecto nuevo)

---

## 🎯 Roadmap de Releases

### Release 1.0 - MVP (Minimum Viable Product)
**Fecha Estimada:** Marzo 2026

**Incluye:**
- ✅ Autenticación JWT completa
- ✅ CRUD de Usuarios
- ✅ CRUD de Productos
- ✅ Gestión básica de inventario
- ✅ Sistema de pedidos básico
- ✅ Docker Compose para desarrollo

### Release 1.1 - Mejoras
**Fecha Estimada:** Abril 2026

**Incluye:**
- [ ] Categorías de productos
- [ ] Filtros avanzados
- [ ] Reportes de inventario
- [ ] API de búsqueda mejorada

### Release 2.0 - IA & Analytics
**Fecha Estimada:** Mayo 2026

**Incluye:**
- [ ] Predicción de stock con ML
- [ ] Dashboard analítico
- [ ] Alertas automáticas
- [ ] Exportación de reportes

---

## 📝 Notas de Desarrollo

### Decisiones Técnicas Importantes

**Fecha: 29/01/2026**
- ✅ Decidido usar Clean Architecture sobre N-Capas tradicional
- ✅ PostgreSQL elegido sobre SQL Server por costos y features
- ✅ EF Core Code-First para manejo de migraciones
- ✅ JWT para autenticación stateless
- ✅ Patrón Repository para abstracción de datos

### Riesgos Identificados

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|--------------|---------|------------|
| Complejidad de transacciones en pedidos | Media | Alto | Usar transacciones explícitas en EF Core |
| Performance con inventario grande | Baja | Medio | Implementar paginación desde el inicio |
| Curva de aprendizaje de Clean Arch | Media | Bajo | Documentación exhaustiva + pair programming |

### Deuda Técnica Identificada

| Item | Prioridad | Esfuerzo | Planificado para | Estado |
|------|-----------|----------|------------------|--------|
| Implementar BCrypt real | Alta | 1h | Sprint 1 | ✅ Completado |
| Implementar JWT real | Alta | 2h | Sprint 1 | ✅ Completado |
| Implementar Swagger/OpenAPI | Media | 2h | Sprint 2 | 📋 Pendiente |
| Tests unitarios | Media | 8h | Sprint 2 | 📋 Pendiente |
| Logging estructurado | Baja | 4h | Sprint 3 | 📋 Pendiente |
| Health checks | Baja | 2h | Sprint 3 | 📋 Pendiente |

---

## 🔗 Enlaces Útiles

- **Repositorio:** [GitHub](https://github.com/YagoGomez83/SmartInventory) *(configurar)*
- **Documentación:** [Wiki del Proyecto](./ARQUITECTURA-CLEAN.md)
- **Servidor Dev:** *(pendiente)*
- **Servidor QA:** *(pendiente)*
- **Producción:** *(pendiente)*
- **CI/CD:** *(pendiente)*

---

## 📞 Equipo y Contactos

| Rol | Nombre | Responsabilidades |
|-----|--------|-------------------|
| **Arquitecto/Lead** | - | Diseño arquitectónico, code reviews |
| **Backend Developer** | - | Implementación de APIs y lógica de negocio |
| **DevOps Engineer** | - | Docker, CI/CD, infraestructura |
| **QA Engineer** | - | Testing, automatización de pruebas |

---
**Última actualización:** 9 Febrero 2026  
**Sprint 1 Completado:** ✅ 31 Enero 2026  
**Sprint 3 Completado:** ✅ 12 Febrero 2026  
**Sprint 4 Completado:** ✅ 9 Febrero 2026  
**Sprint 3 Completado:** ✅ 12 Febrero 2026  
**Sprint Actual:** 🔄 Sprint 4 (En Progreso)  
**Próxima revisión:** 19 Febrero 2026 (Fin Sprint 4)  
**Versión del documento:** 1.3
