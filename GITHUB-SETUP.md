# 🚀 Guía de Configuración de GitHub

## Paso 1: Crear el Repositorio en GitHub

1. Ve a [GitHub](https://github.com) e inicia sesión
2. Haz clic en el botón **"+"** (arriba a la derecha) → **"New repository"**
3. Configura el repositorio:
   - **Repository name:** `SmartInventory`
   - **Description:** "Sistema de gestión de inventario y pedidos con Clean Architecture y .NET 8"
   - **Visibility:** Public (o Private si prefieres)
   - ⚠️ **NO marques:** "Initialize this repository with a README" (ya tenemos uno)
   - ⚠️ **NO agregues:** .gitignore ni LICENSE (ya tenemos .gitignore)
4. Haz clic en **"Create repository"**

---

## Paso 2: Conectar el Repositorio Local con GitHub

GitHub te mostrará comandos similares a estos. **Copia tu URL real** del repositorio:

```bash
# Agregar el remoto (REEMPLAZA con tu URL real de GitHub)
git remote add origin https://github.com/YagoGomez83/SmartInventory.git

# Renombrar la rama principal a 'main' (convención moderna de GitHub)
git branch -M main

# Push del código al repositorio remoto
git push -u origin main
```

### Si tienes autenticación de dos factores (2FA)

Necesitarás un **Personal Access Token (PAT)** en lugar de tu contraseña:

1. Ve a GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Clic en "Generate new token (classic)"
3. Selecciona scopes: `repo` (full control)
4. Genera y copia el token
5. Usa el token como contraseña cuando Git te lo pida

---

## Paso 3: Verificar la Conexión

```bash
# Ver los remotos configurados
git remote -v

# Debería mostrar:
# origin  https://github.com/YagoGomez83/SmartInventory.git (fetch)
# origin  https://github.com/YagoGomez83/SmartInventory.git (push)
```

---

## Paso 4: Flujo de Trabajo Diario

### Agregar cambios y hacer commit

```bash
# Ver estado de archivos modificados
git status

# Agregar archivos específicos
git add src/SmartInventory.Domain/Entities/Category.cs

# O agregar todos los cambios
git add .

# Hacer commit con mensaje descriptivo
git commit -m "feat(domain): add Category entity with hierarchical support"

# Push al repositorio remoto
git push
```

---

## Convenciones de Commits (Conventional Commits)

Usamos prefijos semánticos para facilitar el seguimiento:

| Prefijo | Uso | Ejemplo |
|---------|-----|---------|
| `feat` | Nueva funcionalidad | `feat(auth): add JWT token refresh` |
| `fix` | Corrección de bugs | `fix(product): resolve SKU uniqueness constraint` |
| `docs` | Documentación | `docs: update API endpoints documentation` |
| `style` | Formato (no afecta código) | `style: format code with prettier` |
| `refactor` | Refactorización | `refactor(service): simplify authentication logic` |
| `test` | Tests | `test(user): add unit tests for registration` |
| `chore` | Mantenimiento | `chore: update dependencies` |
| `perf` | Mejoras de rendimiento | `perf(query): optimize product search query` |
| `ci` | CI/CD | `ci: add GitHub Actions workflow` |

### Formato completo:

```
<tipo>(<scope>): <descripción>

[cuerpo opcional]

[footer opcional]
```

**Ejemplo completo:**
```
feat(orders): implement order creation with transaction support

- Add Order and OrderItem entities
- Implement transactional order creation
- Reduce stock automatically on order creation
- Add validation for insufficient stock

Implements: PB-08
Closes: #42
```

---

## Estrategia de Branches

### Main Branch (Protegida)
- `main`: Código en producción o listo para desplegar
- Siempre debe estar funcional y pasar todos los tests

### Development Branch
- `develop`: Integración de features para el próximo release

### Feature Branches
```bash
# Crear branch para nueva funcionalidad
git checkout -b feature/user-profile

# Trabajar en la feature...
git add .
git commit -m "feat(user): add user profile endpoint"

# Push del branch
git push -u origin feature/user-profile

# En GitHub: Crear Pull Request hacia 'develop'
```

### Hotfix Branches
```bash
# Para bugs críticos en producción
git checkout -b hotfix/critical-login-bug main
# Fix...
git commit -m "fix(auth): resolve token expiration issue"
git push -u origin hotfix/critical-login-bug
# Merge a 'main' y 'develop'
```

---

## Comandos Útiles

### Ver historial
```bash
# Historial completo
git log

# Historial compacto
git log --oneline --graph --all

# Último commit
git log -1
```

### Revertir cambios
```bash
# Descartar cambios en archivo (antes de staging)
git checkout -- archivo.cs

# Quitar archivo del staging area
git reset HEAD archivo.cs

# Revertir último commit (mantiene cambios)
git reset --soft HEAD~1

# Revertir último commit (descarta cambios) ⚠️
git reset --hard HEAD~1
```

### Sincronizar con remoto
```bash
# Descargar cambios sin fusionar
git fetch origin

# Descargar y fusionar
git pull origin main

# Ver diferencias con remoto
git diff origin/main
```

---

## Tags para Releases

```bash
# Crear tag para release
git tag -a v1.0.0 -m "Release 1.0.0 - MVP"

# Push del tag
git push origin v1.0.0

# Push de todos los tags
git push --tags

# Listar tags
git tag -l
```

---

## GitHub Actions (CI/CD) - Próximamente

Archivo `.github/workflows/dotnet.yml`:

```yaml
name: .NET Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
```

---

## Protección de Branches en GitHub

Para `main` branch:
1. Ir a Settings → Branches → Add rule
2. Branch name pattern: `main`
3. Marcar:
   - ✓ Require pull request reviews before merging
   - ✓ Require status checks to pass before merging
   - ✓ Require branches to be up to date before merging
   - ✓ Include administrators

---

## Recursos

- [Conventional Commits](https://www.conventionalcommits.org/)
- [GitHub Flow](https://docs.github.com/en/get-started/quickstart/github-flow)
- [Git Branching Strategy](https://nvie.com/posts/a-successful-git-branching-model/)

---

**¿Problemas?** Abre un issue en el repositorio o consulta la documentación de Git/GitHub.
