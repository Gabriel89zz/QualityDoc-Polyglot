
# 📄 QualityDoc-Polyglot
### Sistema de Gestión de Calidad Documental — Arquitectura Políglota con Microservicios
[![.NET](https://img.shields.io/badge/.NET_9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Laravel](https://img.shields.io/badge/Laravel_12-FF2D20?style=for-the-badge&logo=laravel&logoColor=white)](https://laravel.com/)
[![FastAPI](https://img.shields.io/badge/FastAPI-009688?style=for-the-badge&logo=fastapi&logoColor=white)](https://fastapi.tiangolo.com/)
[![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![SQL Server](https://img.shields.io/badge/SQL_Server_2022-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL_15-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![MongoDB](https://img.shields.io/badge/MongoDB-47A248?style=for-the-badge&logo=mongodb&logoColor=white)](https://www.mongodb.com/)
[![Nginx](https://img.shields.io/badge/Nginx-009639?style=for-the-badge&logo=nginx&logoColor=white)](https://nginx.org/)
---
**QualityDoc-Polyglot** es un sistema empresarial de gestión de calidad documental construido sobre una arquitectura de **microservicios políglota**. El proyecto integra tres tecnologías de backend distintas (C#, PHP y Python), tres motores de base de datos especializados (SQL Server, PostgreSQL y MongoDB) y un reverse proxy Nginx, todo orquestado mediante Docker Compose.
---
## 📑 Tabla de Contenidos
- [Arquitectura del Sistema](#-arquitectura-del-sistema)
- [Estructura del Proyecto](#-estructura-del-proyecto)
- [Microservicios](#-microservicios)
  - [Administración — .NET Core MVC (C#)](#1--administración--net-core-mvc-c)
  - [Auditoría — Laravel (PHP)](#2--auditoría--laravel-php)
  - [Motor de Búsqueda — FastAPI (Python)](#3--motor-de-búsqueda--fastapi-python)
- [Bases de Datos](#-bases-de-datos)
  - [SQL Server — Base de Datos Principal](#1--sql-server--base-de-datos-principal)
  - [PostgreSQL — Registro de Auditoría](#2--postgresql--registro-de-auditoría)
  - [MongoDB — Índice de Búsqueda](#3--mongodb--índice-de-búsqueda)
- [Infraestructura](#-infraestructura)
  - [Docker Compose](#docker-compose)
  - [Nginx Reverse Proxy](#nginx-reverse-proxy)
- [Comunicación entre Microservicios](#-comunicación-entre-microservicios)
- [Requisitos Previos](#-requisitos-previos)
- [Instalación y Despliegue](#-instalación-y-despliegue)
- [Variables de Entorno](#-variables-de-entorno)
- [Puertos y Accesos](#-puertos-y-accesos)
- [Tecnologías Utilizadas](#-tecnologías-utilizadas)
---
## 🏗 Arquitectura del Sistema
El sistema sigue una **arquitectura de microservicios** donde cada servicio tiene una responsabilidad específica y utiliza la tecnología más adecuada para su función:
```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLIENTE (Navegador)                         │
└──────────────────────────────┬──────────────────────────────────────┘
                               │ Puerto 80
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     NGINX REVERSE PROXY                            │
│  ┌──────────────┐  ┌──────────────────┐  ┌───────────────────┐    │
│  │  /           │  │  /dotnet/        │  │  /api/search/     │    │
│  │  → PHP App   │  │  → .NET App      │  │  → Python App     │    │
│  └──────┬───────┘  └────────┬─────────┘  └─────────┬─────────┘    │
└─────────┼──────────────────┼───────────────────────┼──────────────┘
          │                  │                       │
          ▼                  ▼                       ▼
┌──────────────┐   ┌──────────────────┐   ┌──────────────────┐
│  PHP Laravel │   │  .NET Core MVC   │   │  Python FastAPI  │
│  (Auditoría) │   │ (Administración) │   │   (Búsqueda)     │
│  Puerto 9000 │   │   Puerto 8080    │   │   Puerto 8000    │
└──────┬───────┘   └───┬──────────┬───┘   └────────┬─────────┘
       │               │          │                 │
       ▼               ▼          │                 ▼
┌──────────────┐ ┌───────────┐    │          ┌───────────┐
│ PostgreSQL   │ │ SQL Server│    │          │  MongoDB   │
│ (Auditoría)  │ │ (Principal│    │          │ (Búsqueda) │
│ Puerto 5432  │ │ Puerto    │    │          │ Puerto     │
│              │ │    14333) │    │          │   27017    │
└──────────────┘ └───────────┘    │          └───────────┘
                                  │
                    ┌─────────────┴─────────────┐
                    │  Comunicación Interna      │
                    │  → Python (Indexar/Buscar) │
                    │  → PHP (Registrar Auditoría)│
                    └───────────────────────────┘
```
### Flujo de Datos Principal
1. **El usuario** accede al sistema a través del puerto `80` (Nginx).
2. **Nginx** enruta las peticiones al microservicio correspondiente según la URL.
3. **La app .NET** es el núcleo del sistema: gestiona usuarios, documentos y orquesta la comunicación con los otros microservicios.
4. **Cada operación CRUD** en .NET genera dos acciones secundarias:
   - Envía un evento de auditoría a la **app PHP/Laravel** (vía API REST).
   - Indexa/actualiza/elimina metadatos del documento en la **app Python/FastAPI** (vía API REST).
5. **Las bases de datos** están especializadas: SQL Server para datos transaccionales, PostgreSQL para auditoría, y MongoDB para búsqueda full-text.
---
## 📁 Estructura del Proyecto
```
QualityDoc-Polyglot/
│
├── 📄 docker-compose.yml           # Orquestación de todos los servicios
├── 📄 .env.example                 # Plantilla de variables de entorno
├── 📄 QualityDoc-Polyglot.sln      # Solución de Visual Studio
│
├── 📂 src/                         # Código fuente de los microservicios
│   ├── 📂 dotnet-app/              # 🟣 Microservicio de Administración (C# MVC)
│   │   ├── Controllers/            #    Controladores MVC
│   │   ├── Models/                 #    Modelos de datos
│   │   ├── ViewModels/             #    ViewModels para las vistas
│   │   ├── Views/                  #    Vistas Razor (.cshtml)
│   │   ├── Services/               #    Servicios (Auditoría, Email, Búsqueda)
│   │   ├── Data/                   #    DbContext de Entity Framework
│   │   ├── wwwroot/                #    Archivos estáticos (CSS, JS, uploads)
│   │   ├── Program.cs              #    Punto de entrada y configuración DI
│   │   └── dotnet-app.csproj       #    Archivo de proyecto
│   │
│   ├── 📂 php-app/                 # 🔴 Microservicio de Auditoría (Laravel)
│   │   ├── app/
│   │   │   ├── Http/Controllers/   #    Controladores
│   │   │   ├── Http/Middleware/     #    Middleware de autenticación API
│   │   │   ├── Models/             #    Modelos Eloquent
│   │   │   └── Services/           #    Servicio de comunicación con .NET
│   │   ├── routes/                 #    Definición de rutas (web + API)
│   │   ├── resources/views/        #    Vistas Blade
│   │   ├── database/migrations/    #    Migraciones de base de datos
│   │   └── composer.json           #    Dependencias PHP
│   │
│   └── 📂 python-app/              # 🟢 Motor de Búsqueda (FastAPI)
│       ├── app/
│       │   ├── config/             #    Configuración de MongoDB
│       │   ├── models/             #    Esquemas Pydantic
│       │   ├── repositories/       #    Capa de acceso a datos
│       │   ├── routers/            #    Definición de endpoints
│       │   └── services/           #    Lógica de negocio
│       ├── main.py                 #    Punto de entrada FastAPI
│       └── requirements.txt        #    Dependencias Python
│
├── 📂 db/                          # Scripts de inicialización de bases de datos
│   ├── 📂 sql-server/
│   │   ├── entrypoint.sh           #    Script de arranque
│   │   └── scripts/
│   │       ├── 01_schema.sql       #    Esquema de tablas
│   │       ├── 02_seed.sql         #    Datos iniciales
│   │       └── 03_triggers.sql     #    Triggers automáticos
│   ├── 📂 postgresql/
│   │   └── scripts/
│   │       └── 01_schema.sql       #    Esquema de auditoría
│   └── 📂 mongodb/
│       └── scripts/
│           └── 01_init_search.js   #    Colección e índices de búsqueda
│
└── 📂 docker/                      # Archivos de configuración Docker
    ├── 📂 dotnet/
    │   └── Dockerfile              #    Multi-stage build para .NET
    ├── 📂 php/
    │   └── Dockerfile              #    Imagen PHP-FPM con extensiones
    ├── 📂 python/
    │   └── Dockerfile              #    Imagen Python slim
    └── 📂 nginx/
        └── default.conf            #    Configuración del reverse proxy
```
---
## 🔌 Microservicios
### 1. 🟣 Administración — .NET Core MVC (C#)
> **Contenedor**: `dotnet_core_mvc` | **Puerto interno**: `8080` | **Puerto externo**: `5269`
Este es el **microservicio principal** del sistema. Gestiona usuarios, documentos, autenticación y orquesta la comunicación con los demás servicios.
#### Tecnologías
| Componente | Tecnología |
|---|---|
| Framework | ASP.NET Core 9.0 MVC |
| ORM | Entity Framework Core 9.0 |
| Base de datos | SQL Server 2022 |
| Autenticación | Cookie Authentication + BCrypt |
| Motor de vistas | Razor Pages (.cshtml) |
| UI | Bootstrap 5.3 (tema oscuro) |
#### Paquetes NuGet
| Paquete | Versión | Propósito |
|---|---|---|
| `BCrypt.Net-Next` | 4.0.3 | Hashing seguro de contraseñas |
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.6 | Proveedor EF Core para SQL Server |
| `Microsoft.EntityFrameworkCore.Tools` | 9.0.6 | Herramientas CLI de EF Core |
#### Modelos de Datos
##### `Usuario` — Gestión de usuarios del sistema
| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `int` | Clave primaria auto-incremental |
| `Nombre` | `string` | Nombre completo del usuario (máx. 100 caracteres) |
| `Email` | `string` | Correo electrónico único (máx. 150 caracteres) |
| `PasswordHash` | `string` | Contraseña hasheada con BCrypt |
| `Rol` | `string` | Rol del usuario (`Admin` o `Usuario`) |
| `DepartamentoId` | `int?` | FK al departamento asignado |
| `Activo` | `bool` | Estado activo/inactivo (soft delete) |
| `FechaRegistro` | `DateTime` | Fecha de registro |
| `ResetToken` | `string?` | Token para restablecimiento de contraseña |
| `ResetTokenExpiry` | `DateTime?` | Expiración del token de reset |
| `EmailConfirmed` | `bool` | Confirmación de correo electrónico |
| `ConfirmationToken` | `string?` | Token de confirmación de email |
##### `Documento` — Documentos de calidad
| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `int` | Clave primaria auto-incremental |
| `Codigo` | `string` | Código único del documento (ej: `PROC-CAL-001`) |
| `Titulo` | `string` | Título del documento (máx. 200 caracteres) |
| `TipoDocumentoId` | `int` | FK al tipo de documento |
| `DepartamentoId` | `int` | FK al departamento propietario |
| `Version` | `int` | Número de versión actual (inicia en 1) |
| `FechaVigencia` | `DateTime?` | Fecha de vigencia del documento |
| `Estado` | `string` | Estado del documento (`Borrador`, `Vigente`, `Obsoleto`) |
| `ArchivoUrl` | `string?` | Ruta del archivo subido |
| `CreadoPor` | `int` | FK al usuario creador |
| `FechaCreacion` | `DateTime` | Fecha de creación |
##### `Departamento` — Departamentos de la organización
| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `int` | Clave primaria auto-incremental |
| `Nombre` | `string` | Nombre del departamento (máx. 100 caracteres) |
| `Descripcion` | `string?` | Descripción opcional |
##### `TipoDocumento` — Clasificación de documentos
| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `int` | Clave primaria auto-incremental |
| `Nombre` | `string` | Nombre del tipo (ej: Procedimiento, Instructivo, Formato, Manual) |
| `Descripcion` | `string?` | Descripción opcional |
##### `HistorialVersion` — Historial de cambios de documentos
| Propiedad | Tipo | Descripción |
|---|---|---|
| `Id` | `int` | Clave primaria auto-incremental |
| `DocumentoId` | `int` | FK al documento (CASCADE en eliminación) |
| `VersionAnterior` | `int` | Número de versión anterior |
| `VersionNueva` | `int` | Número de versión nueva |
| `ArchivoUrlAnterior` | `string?` | URL del archivo de la versión anterior |
| `ModificadoPor` | `int` | FK al usuario que realizó la modificación |
| `FechaModificacion` | `DateTime` | Fecha de la modificación |
| `Comentario` | `string?` | Comentario del cambio |
#### Controladores y Endpoints
##### `AccountController` — Autenticación y gestión de cuentas
| Acción | Método | Ruta | Descripción |
|---|---|---|---|
| `Login` | GET | `/Account/Login` | Muestra formulario de inicio de sesión |
| `Login` | POST | `/Account/Login` | Autentica al usuario, verifica confirmación de email, crea claims de sesión, envía evento de auditoría a PHP |
| `Register` | GET | `/Account/Register` | Muestra formulario de registro con lista de departamentos |
| `Register` | POST | `/Account/Register` | Crea usuario con hash BCrypt, envía correo de confirmación, registra auditoría |
| `ConfirmEmail` | GET | `/Account/ConfirmEmail` | Confirma email mediante token en la URL |
| `Logout` | POST | `/Account/Logout` | Cierra sesión y registra auditoría |
| `ForgotPassword` | GET/POST | `/Account/ForgotPassword` | Envía correo con enlace de restablecimiento de contraseña |
| `ResetPassword` | GET/POST | `/Account/ResetPassword` | Restablece la contraseña usando token válido |
##### `DashboardController` — Panel principal
| Acción | Método | Ruta | Descripción |
|---|---|---|---|
| `Index` | GET | `/Dashboard` | Muestra panel con estadísticas: total de documentos, usuarios activos, departamentos, documentos pendientes de revisión, últimos 5 documentos, gráfica de distribución por tipo |
##### `DocumentosController` — CRUD de documentos
| Acción | Método | Ruta | Descripción |
|---|---|---|---|
| `Index` | GET | `/Documentos` | Lista documentos con búsqueda por texto y filtro por estado. Paginación de 10 elementos |
| `Details` | GET | `/Documentos/Details/{id}` | Muestra detalle completo del documento con su historial de versiones |
| `Create` | GET | `/Documentos/Create` | Formulario de creación con selectores de tipo y departamento |
| `Create` | POST | `/Documentos/Create` | Crea documento con subida de archivo, indexa metadatos en Python, registra auditoría |
| `Edit` | GET | `/Documentos/Edit/{id}` | Formulario de edición del documento |
| `Edit` | POST | `/Documentos/Edit/{id}` | Actualiza documento, maneja reemplazo de archivo, incrementa versión, registra auditoría |
| `Delete` | POST | `/Documentos/Delete/{id}` | Elimina documento, lo desindexia del motor de búsqueda, borra archivo físico, registra auditoría |
| `Download` | GET | `/Documentos/Download/{id}` | Descarga el archivo adjunto del documento |
##### `AdminController` — Panel de administración
| Acción | Método | Ruta | Descripción |
|---|---|---|---|
| `Index` | GET | `/Admin` | Panel de administración con lista de todos los usuarios |
| `Edit` | GET/POST | `/Admin/Edit/{id}` | Editar usuario (nombre, email, rol, departamento, estado activo) |
| `Delete` | POST | `/Admin/Delete/{id}` | Desactivación lógica del usuario (soft delete) |
| `GetUsers` | GET | `/api/admin/users` | **API JSON**: Retorna lista de usuarios (consumido por PHP) |
| `GetDocuments` | GET | `/api/admin/documents` | **API JSON**: Retorna lista de documentos (consumido por PHP) |
#### Servicios
##### `AuditService` — Comunicación con el servicio de auditoría (PHP)
Envía eventos de auditoría a la aplicación Laravel vía HTTP POST.
| Método | Descripción | Endpoint Destino |
|---|---|---|
| `LogActionAsync(userId, email, action, entityType, entityId, details, ipAddress)` | Registra una acción del usuario | `POST http://nginx_gateway/api/audits` |
| `LogLoginAttemptAsync(email, success, ipAddress, failureReason)` | Registra un intento de login | `POST http://nginx_gateway/api/login-attempts` |
- **Autenticación**: Header `X-API-KEY` con clave compartida.
##### `EmailService` — Servicio de correo electrónico
Envío de correos mediante SMTP (Gmail).
| Método | Descripción |
|---|---|
| `SendConfirmationEmailAsync(toEmail, userName, confirmationLink)` | Envía correo HTML de confirmación de cuenta |
| `SendPasswordResetEmailAsync(toEmail, userName, resetLink)` | Envía correo HTML de restablecimiento de contraseña |
- **Configuración SMTP**: `smtp.gmail.com`, puerto `587`, SSL habilitado.
- **Credenciales**: Se leen de las variables de entorno `EmailSettings__SenderEmail` y `EmailSettings__Password`.
##### `SearchService` — Comunicación con el motor de búsqueda (Python)
Cliente HTTP para el microservicio de búsqueda FastAPI.
| Método | Descripción | Endpoint Destino |
|---|---|---|
| `IndexDocumentAsync(code, title, type, department, revision, effectiveDate, status)` | Indexa metadatos del documento en MongoDB | `POST http://python-app:8000/api/search/index` |
| `SearchDocumentsAsync(query)` | Busca documentos por texto | `GET http://python-app:8000/api/search/?q={query}` |
| `DeleteDocumentAsync(documentCode)` | Elimina documento del índice | `DELETE http://python-app:8000/api/search/delete/{code}` |
#### Vistas Principales
| Vista | Descripción |
|---|---|
| `_Layout.cshtml` | Layout maestro con Bootstrap 5.3 tema oscuro, navbar responsiva con enlaces condicionales según rol |
| `Home/Index.cshtml` | Página de inicio / landing page del sistema |
| `Dashboard/Index.cshtml` | Panel con tarjetas de resumen, gráfica Chart.js (doughnut) de distribución por tipo, tabla de documentos recientes |
| `Documentos/Index.cshtml` | Listado con barra de búsqueda, pestañas de filtro por estado (Todos/Borrador/Vigente/Obsoleto), tabla paginada |
| `Documentos/Create.cshtml` | Formulario de creación con campos y subida de archivo |
| `Documentos/Edit.cshtml` | Formulario de edición con reemplazo de archivo |
| `Documentos/Details.cshtml` | Vista detallada con timeline de historial de versiones |
| `Admin/Index.cshtml` | Tabla de gestión de usuarios con badges de rol y estado |
| `Account/Login.cshtml` | Formulario de inicio de sesión |
| `Account/Register.cshtml` | Formulario de registro con selector de departamento |
---
### 2. 🔴 Auditoría — Laravel (PHP)
> **Contenedor**: `php_laravel_audit` | **Puerto interno**: `9000` (PHP-FPM) | **Acceso vía Nginx**: Puerto `80`
Microservicio dedicado al **registro y visualización de auditoría**. Recibe eventos desde la aplicación .NET y los almacena en PostgreSQL.
#### Tecnologías
| Componente | Tecnología |
|---|---|
| Framework | Laravel 12 (PHP 8.2) |
| Base de datos | PostgreSQL 15 |
| Motor de vistas | Blade Templates |
| UI | Bootstrap 5.3 (tema oscuro) |
| HTTP Client | Guzzle (integrado) |
#### Modelos de Datos
##### `AuditLog` — Registro de auditoría
| Propiedad | Tipo | Descripción |
|---|---|---|
| `id` | `int` | Clave primaria auto-incremental |
| `timestamp` | `timestamptz` | Marca de tiempo del evento (auto-generada) |
| `user_id` | `int` | ID del usuario que realizó la acción |
| `user_email` | `string` | Email del usuario |
| `action` | `string` | Acción realizada (CREATE, UPDATE, DELETE, LOGIN, etc.) |
| `entity_type` | `string` | Tipo de entidad afectada (Documento, Usuario, etc.) |
| `entity_id` | `int?` | ID de la entidad afectada |
| `details` | `jsonb` | Detalles adicionales en formato JSON |
| `ip_address` | `string?` | Dirección IP del usuario |
| `dotnet_source_url` | `string` | URL de origen del servicio .NET |
##### `LoginAttempt` — Intentos de inicio de sesión
| Propiedad | Tipo | Descripción |
|---|---|---|
| `id` | `int` | Clave primaria auto-incremental |
| `timestamp` | `timestamptz` | Marca de tiempo del intento |
| `email` | `string` | Email utilizado en el intento |
| `success` | `boolean` | Si el intento fue exitoso |
| `ip_address` | `string?` | Dirección IP |
| `failure_reason` | `string?` | Razón del fallo (si aplica) |
#### Rutas Web
| Método | Ruta | Controlador | Acción | Descripción |
|---|---|---|---|---|
| GET | `/` | `DashboardController` | `index` | Dashboard de auditoría con estadísticas |
| GET | `/audits` | `AuditController` | `index` | Listado de eventos de auditoría con filtros |
| GET | `/audits/{audit}` | `AuditController` | `show` | Detalle de un evento de auditoría |
#### Rutas API (protegidas con API Key)
| Método | Ruta | Controlador | Acción | Descripción |
|---|---|---|---|---|
| POST | `/api/audits` | `AuditController` | `store` | Crear nuevo registro de auditoría |
| POST | `/api/login-attempts` | `AuditController` | `storeLoginAttempt` | Registrar intento de login |
#### Middleware
##### `ApiKeyMiddleware`
- Verifica el header `X-API-KEY` en las peticiones API.
- Compara contra la variable de entorno `AUDIT_API_KEY`.
- Retorna `401 Unauthorized` si la clave es inválida o falta.
#### Servicios
##### `DotnetApiService` — Consulta de datos desde .NET
| Método | Descripción | Endpoint |
|---|---|---|
| `getUsers()` | Obtiene lista de usuarios del sistema | `GET http://dotnet-app:8080/api/admin/users` |
| `getDocuments()` | Obtiene lista de documentos del sistema | `GET http://dotnet-app:8080/api/admin/documents` |
#### Controladores
##### `DashboardController`
| Método | Descripción |
|---|---|
| `index()` | Renderiza dashboard con: total de eventos, eventos de hoy, intentos de login fallidos, últimos 10 eventos |
##### `AuditController`
| Método | Descripción |
|---|---|
| `index(Request)` | Lista eventos con filtros por `action`, `entity_type`, `user_email`. Paginación de 20 elementos |
| `show(AuditLog)` | Muestra detalle completo de un evento con JSON formateado |
| `store(Request)` | **API**: Valida y crea nuevo registro de auditoría. Retorna `201 Created` |
| `storeLoginAttempt(Request)` | **API**: Valida y registra intento de login. Retorna `201 Created` |
#### Vistas Blade
| Vista | Descripción |
|---|---|
| `layouts/app.blade.php` | Layout maestro con Bootstrap 5.3 tema oscuro, navbar con iconos Font Awesome |
| `dashboard.blade.php` | 3 tarjetas de estadísticas + tabla de actividad reciente con badges de color por acción |
| `audits/index.blade.php` | Formulario de filtros + tabla paginada de eventos de auditoría |
| `audits/show.blade.php` | Vista detallada con tarjetas de información del evento, usuario y datos técnicos |
---
### 3. 🟢 Motor de Búsqueda — FastAPI (Python)
> **Contenedor**: `python_fastapi_search` | **Puerto interno**: `8000` | **Puerto externo**: `8000`
Microservicio de **búsqueda full-text** de documentos. Utiliza MongoDB para indexar metadatos y permite búsquedas por texto mediante expresiones regulares.
#### Tecnologías
| Componente | Tecnología |
|---|---|
| Framework | FastAPI 0.115.12 |
| Base de datos | MongoDB (Motor async driver 3.7.1) |
| Validación | Pydantic 2.11.3 |
| Servidor ASGI | Uvicorn 0.34.3 |
#### Dependencias Python
| Paquete | Versión | Propósito |
|---|---|---|
| `fastapi` | 0.115.12 | Framework web asíncrono |
| `uvicorn` | 0.34.3 | Servidor ASGI de alto rendimiento |
| `motor` | 3.7.1 | Driver asíncrono para MongoDB |
| `pydantic` | 2.11.3 | Validación y serialización de datos |
| `python-dotenv` | 1.1.0 | Carga de variables de entorno |
#### Arquitectura del Servicio
```
Routers (endpoints) → Services (lógica) → Repositories (datos) → MongoDB
```
#### Modelos / Esquemas Pydantic
##### `DocumentMetadata` — Metadatos de un documento
| Campo | Tipo | Descripción |
|---|---|---|
| `document_code` | `str` | Código único del documento |
| `document_title` | `str` | Título del documento |
| `document_type` | `str` | Tipo de documento |
| `department` | `str` | Departamento propietario |
| `revision_number` | `int` | Número de revisión actual |
| `effective_date` | `str` | Fecha de vigencia |
| `status` | `str` | Estado del documento |
| `indexed_at` | `str` | Marca de tiempo de indexación (auto-generada) |
##### `DocumentResponse` — Respuesta con ID (extiende `DocumentMetadata`)
| Campo | Tipo | Descripción |
|---|---|---|
| `id` | `str` | ID del documento en MongoDB |
#### Endpoints API
Todos los endpoints usan el prefijo `/api/search` y están etiquetados como `"Búsqueda de Documentos"`.
| Método | Ruta | Descripción | Parámetros |
|---|---|---|---|
| GET | `/` | Endpoint raíz de salud | — |
| GET | `/api/search/` | Busca documentos por texto | Query param `q` (string) |
| POST | `/api/search/index` | Indexa nuevos metadatos de documento | Body: `DocumentMetadata` |
| GET | `/api/search/all` | Obtiene todos los documentos indexados | — |
| DELETE | `/api/search/delete/{document_code}` | Elimina documento por código | Path param `document_code` |
#### Capa de Repositorio (`DocumentRepository`)
| Método | Descripción |
|---|---|
| `search(query)` | Búsqueda full-text con regex case-insensitive en campos: `document_code`, `document_title`, `document_type`, `department`, `status` |
| `create(document)` | Inserta nuevo registro de metadatos en MongoDB |
| `get_all()` | Obtiene todos los registros de metadatos |
| `delete_by_code(document_code)` | Elimina registro por código de documento |
#### Capa de Servicio (`SearchService`)
| Método | Descripción |
|---|---|
| `search_documents(query)` | Delega búsqueda al repositorio |
| `index_document(metadata)` | Convierte a dict, agrega timestamp `indexed_at`, delega creación |
| `get_all_documents()` | Delega obtención de todos los documentos |
| `delete_document(document_code)` | Delega eliminación por código |
#### Configuración de MongoDB
- **Host**: `mongo-db:27017` (nombre del servicio Docker)
- **Base de datos**: `quality_search_db`
- **Colección**: `documents_metadata`
- **Credenciales**: Variables de entorno `MONGO_INITDB_ROOT_USERNAME` y `MONGO_INITDB_ROOT_PASSWORD`
- **CORS**: Habilitado para todos los orígenes (`*`)
---
## 🗄 Bases de Datos
### 1. 💎 SQL Server — Base de Datos Principal
> **Contenedor**: `sql_server_admin` | **Puerto externo**: `14333` | **Base de datos**: `QualityDocDB`
Base de datos transaccional principal utilizada por el microservicio .NET.
#### Diagrama Entidad-Relación
```
┌──────────────────┐     ┌──────────────────┐
│   Departamentos  │     │  TiposDocumento   │
│──────────────────│     │──────────────────│
│ PK Id            │     │ PK Id            │
│    Nombre        │     │    Nombre (UQ)   │
│    Descripcion   │     │    Descripcion   │
└────────┬─────────┘     └────────┬─────────┘
         │ 1:N                    │ 1:N
         │                        │
         ▼                        ▼
┌────────────────────────────────────────────┐
│               Documentos                   │
│────────────────────────────────────────────│
│ PK  Id                                     │
│ UQ  Codigo                                 │
│     Titulo                                 │
│ FK  TipoDocumentoId → TiposDocumento       │
│ FK  DepartamentoId  → Departamentos        │
│     Version (default 1)                    │
│     FechaVigencia                          │
│     Estado (default 'Borrador')            │
│     ArchivoUrl                             │
│ FK  CreadoPor → Usuarios                  │
│     FechaCreacion                          │
└────────────────────┬───────────────────────┘
                     │ 1:N (CASCADE)
                     ▼
┌────────────────────────────────────────────┐
│          HistorialVersiones                │
│────────────────────────────────────────────│
│ PK  Id                                     │
│ FK  DocumentoId → Documentos               │
│     VersionAnterior                        │
│     VersionNueva                           │
│     ArchivoUrlAnterior                     │
│ FK  ModificadoPor → Usuarios               │
│     FechaModificacion                      │
│     Comentario                             │
└────────────────────────────────────────────┘
┌──────────────────────────────────────┐
│            Usuarios                  │
│──────────────────────────────────────│
│ PK  Id                               │
│     Nombre                           │
│ UQ  Email                            │
│     PasswordHash (BCrypt)            │
│     Rol ('Admin' | 'Usuario')        │
│ FK  DepartamentoId → Departamentos   │
│     Activo (default 1)               │
│     FechaRegistro                    │
│     ResetToken                       │
│     ResetTokenExpiry                 │
│     EmailConfirmed (default 0)       │
│     ConfirmationToken                │
└──────────────────────────────────────┘
```
#### Trigger Automático
**`trg_Documentos_HistorialVersiones`**: Se dispara en `UPDATE` sobre la tabla `Documentos` cuando cambia `Version`, `ArchivoUrl` o `Estado`. Inserta automáticamente un registro en `HistorialVersiones` con la versión anterior, la nueva, y la URL del archivo anterior.
#### Datos Semilla
- **Departamentos**: Calidad, Producción, Recursos Humanos, Ingeniería
- **Tipos de Documento**: Procedimiento, Instructivo, Formato, Manual
- **Usuario Admin**: `admin@qualitydoc.com` (contraseña: `Admin123!`, rol: `Admin`)
---
### 2. 🐘 PostgreSQL — Registro de Auditoría
> **Contenedor**: `postgres_audit` | **Puerto externo**: `5432` | **Base de datos**: `quality_audit`
Base de datos dedicada al almacenamiento de registros de auditoría, utilizada por el microservicio PHP/Laravel.
#### Tablas
##### `audit_logs`
| Columna | Tipo | Descripción |
|---|---|---|
| `id` | `SERIAL` (PK) | ID auto-incremental |
| `timestamp` | `TIMESTAMPTZ` | Marca de tiempo del evento |
| `user_id` | `INT` | ID del usuario que realizó la acción |
| `user_email` | `VARCHAR(255)` | Email del usuario |
| `action` | `VARCHAR(100)` | Acción realizada |
| `entity_type` | `VARCHAR(100)` | Tipo de entidad afectada |
| `entity_id` | `INT` | ID de la entidad |
| `details` | `JSONB` | Detalles adicionales en JSON |
| `ip_address` | `VARCHAR(45)` | Dirección IP |
| `dotnet_source_url` | `VARCHAR(500)` | URL de origen del servicio .NET |
##### `login_attempts`
| Columna | Tipo | Descripción |
|---|---|---|
| `id` | `SERIAL` (PK) | ID auto-incremental |
| `timestamp` | `TIMESTAMPTZ` | Marca de tiempo del intento |
| `email` | `VARCHAR(255)` | Email utilizado |
| `success` | `BOOLEAN` | Si el intento fue exitoso |
| `ip_address` | `VARCHAR(45)` | Dirección IP |
| `failure_reason` | `VARCHAR(255)` | Razón del fallo |
#### Índices de Rendimiento
- `idx_audit_logs_timestamp` en `audit_logs(timestamp)`
- `idx_audit_logs_user_id` en `audit_logs(user_id)`
- `idx_audit_logs_action` en `audit_logs(action)`
- `idx_login_attempts_timestamp` en `login_attempts(timestamp)`
- `idx_login_attempts_email` en `login_attempts(email)`
- `idx_login_attempts_success` en `login_attempts(success)`
---
### 3. 🍃 MongoDB — Índice de Búsqueda
> **Contenedor**: `mongo_metadata` | **Puerto externo**: `27017` | **Base de datos**: `quality_search_db`
Base de datos NoSQL utilizada para almacenar metadatos de documentos e índices de búsqueda full-text.
#### Colección: `documents_metadata`
| Campo | Tipo | Descripción |
|---|---|---|
| `document_code` | `string` | Código único del documento |
| `document_title` | `string` | Título del documento |
| `document_type` | `string` | Tipo de documento |
| `department` | `string` | Departamento |
| `revision_number` | `int` | Número de revisión |
| `effective_date` | `string` | Fecha de vigencia |
| `status` | `string` | Estado del documento |
| `indexed_at` | `string` | Fecha de indexación |
#### Índice de Texto
Índice de texto compuesto sobre los campos: `document_code`, `document_title`, `document_type`, `department`, `status`.
#### Datos Semilla
| Código | Título | Departamento | Estado |
|---|---|---|---|
| `PROC-CAL-001` | Procedimiento de Control de Documentos | Calidad | Vigente |
| `INST-PROD-001` | Instructivo de Operación de Maquinaria | Producción | Vigente |
| `FMT-RRHH-001` | Formato de Evaluación de Desempeño | Recursos Humanos | Borrador |
| `MAN-ING-001` | Manual de Diseño de Producto | Ingeniería | En Revisión |
---
## 🔧 Infraestructura
### Docker Compose
El archivo `docker-compose.yml` define **7 servicios** organizados en una red Docker compartida (`quality-net`):
| Servicio | Imagen / Build | Contenedor | Función |
|---|---|---|---|
| `nginx-proxy` | `nginx:alpine` | `nginx_gateway` | Reverse proxy y punto de entrada |
| `sql-server` | `mcr.microsoft.com/mssql/server:2022-latest` | `sql_server_admin` | Base de datos principal |
| `postgres-db` | `postgres:15` | `postgres_audit` | Base de datos de auditoría |
| `mongo-db` | `mongo:latest` | `mongo_metadata` | Base de datos de búsqueda |
| `dotnet-app` | Build desde `docker/dotnet/Dockerfile` | `dotnet_core_mvc` | App de administración |
| `php-app` | Build desde `docker/php/Dockerfile` | `php_laravel_audit` | App de auditoría |
| `python-app` | Build desde `docker/python/Dockerfile` | `python_fastapi_search` | Motor de búsqueda |
#### Volúmenes Persistentes
| Volumen | Contenedor | Punto de Montaje |
|---|---|---|
| `sql_data` | SQL Server | `/var/opt/mssql/data` |
| `postgres_data` | PostgreSQL | `/var/lib/postgresql/data` |
| `mongo_data` | MongoDB | `/data/db` |
#### Dependencias de Arranque
```
nginx-proxy ──depends on──► dotnet-app ──depends on──► sql-server (healthy)
                                       ──depends on──► python-app ──depends on──► mongo-db
             ──depends on──► php-app ──depends on──► postgres-db (healthy)
             ──depends on──► python-app
```
### Nginx Reverse Proxy
Configuración de ruteo basado en URL:
| Ruta de Entrada | Servicio Destino | Protocolo |
|---|---|---|
| `/` (raíz, por defecto) | `php-app:9000` | FastCGI (PHP-FPM) |
| `/dotnet/` | `http://dotnet-app:8080/` | HTTP Reverse Proxy |
| `/api/search/` | `http://python-app:8000/api/search/` | HTTP Reverse Proxy |
### Dockerfiles
#### .NET (Multi-stage build)
1. **Etapa de build**: `mcr.microsoft.com/dotnet/sdk:9.0` — Restaura dependencias y publica la aplicación.
2. **Etapa de runtime**: `mcr.microsoft.com/dotnet/aspnet:9.0` — Imagen ligera para ejecutar la app. Expone puerto `8080`.
#### PHP (PHP-FPM)
- Base: `php:8.2-fpm`
- Instala extensiones: `pdo_pgsql`, `zip`
- Instala Composer y ejecuta `composer install`
- Configura permisos en `storage/` y `bootstrap/cache/`
- Genera clave de Laravel y ejecuta migraciones
- Expone puerto `9000`
#### Python (Slim)
- Base: `python:3.11-slim`
- Instala dependencias desde `requirements.txt`
- Ejecuta Uvicorn con `--reload` para desarrollo
- Expone puerto `8000`
---
## 🔄 Comunicación entre Microservicios
El sistema utiliza **comunicación síncrona** mediante **API REST** sobre HTTP dentro de la red Docker.
```
┌─────────────┐        HTTP POST              ┌─────────────┐
│   .NET App  │ ──── /api/audits ───────────► │  PHP/Laravel │
│  (Principal)│ ──── /api/login-attempts ───► │ (Auditoría)  │
│             │        Header: X-API-KEY       │              │
│             │                                └─────────────┘
│             │
│             │        HTTP POST/GET/DELETE     ┌─────────────┐
│             │ ──── /api/search/index ──────► │   Python/    │
│             │ ──── /api/search/?q=... ─────► │   FastAPI    │
│             │ ──── /api/search/delete/... ──►│ (Búsqueda)   │
└─────────────┘                                └─────────────┘
┌─────────────┐        HTTP GET                ┌─────────────┐
│  PHP/Laravel│ ──── /api/admin/users ───────► │   .NET App   │
│ (Auditoría) │ ──── /api/admin/documents ───► │ (Principal)  │
└─────────────┘                                └─────────────┘
```
### Resumen de Comunicaciones
| Origen | Destino | Endpoint | Propósito |
|---|---|---|---|
| .NET → PHP | `POST /api/audits` | Registrar evento de auditoría |
| .NET → PHP | `POST /api/login-attempts` | Registrar intento de login |
| .NET → Python | `POST /api/search/index` | Indexar metadatos de documento |
| .NET → Python | `GET /api/search/?q={query}` | Buscar documentos |
| .NET → Python | `DELETE /api/search/delete/{code}` | Eliminar documento del índice |
| PHP → .NET | `GET /api/admin/users` | Obtener lista de usuarios |
| PHP → .NET | `GET /api/admin/documents` | Obtener lista de documentos |
---
## 📋 Requisitos Previos
- **Docker** v20.10 o superior
- **Docker Compose** v2.0 o superior
- **Git** para clonar el repositorio
- Mínimo **4 GB de RAM** disponible (SQL Server requiere al menos 2 GB)
---
## 🚀 Instalación y Despliegue
### 1. Clonar el repositorio
```bash
git clone https://github.com/Gabriel89zz/QualityDoc-Polyglot.git
cd QualityDoc-Polyglot
```
### 2. Configurar variables de entorno
```bash
cp .env.example .env
```
Edita el archivo `.env` con tus credenciales:
```env
# SQL Server
SQL_SERVER_PASSWORD=TuPasswordSeguro123!
# PostgreSQL
POSTGRES_DB=quality_audit
POSTGRES_USER=admin
POSTGRES_PASSWORD=tu_password_postgres
# MongoDB
MONGO_INITDB_ROOT_USERNAME=mongo_admin
MONGO_INITDB_ROOT_PASSWORD=tu_password_mongo
# Email (Gmail)
SMTP_EMAIL=tu_correo@gmail.com
SMTP_PASSWORD=tu_password_de_aplicacion
```
igual con el de `.env` en src/php-app
```env
APP_NAME=Laravel
APP_ENV=local
APP_KEY=
APP_DEBUG=true
APP_URL=http://localhost

APP_LOCALE=en
APP_FALLBACK_LOCALE=en
APP_FAKER_LOCALE=en_US

APP_MAINTENANCE_DRIVER=file
# APP_MAINTENANCE_STORE=database

# PHP_CLI_SERVER_WORKERS=4

BCRYPT_ROUNDS=12

LOG_CHANNEL=stack
LOG_STACK=single
LOG_DEPRECATIONS_CHANNEL=null
LOG_LEVEL=debug

DB_CONNECTION=pgsql
DB_HOST=postgres-db
DB_PORT=5432
DB_DATABASE=quality_audit
DB_USERNAME=admin
DB_PASSWORD=pon_aqui_el_password_de_postgres

SESSION_DRIVER=database
SESSION_LIFETIME=120
SESSION_ENCRYPT=false
SESSION_PATH=/
SESSION_DOMAIN=null

BROADCAST_CONNECTION=log
FILESYSTEM_DISK=local
QUEUE_CONNECTION=database

CACHE_STORE=database
# CACHE_PREFIX=

MEMCACHED_HOST=127.0.0.1

REDIS_CLIENT=phpredis
REDIS_HOST=127.0.0.1
REDIS_PASSWORD=null
REDIS_PORT=6379

MAIL_MAILER=log
MAIL_SCHEME=null
MAIL_HOST=127.0.0.1
MAIL_PORT=2525
MAIL_USERNAME=null
MAIL_PASSWORD=null
MAIL_FROM_ADDRESS="hello@example.com"
MAIL_FROM_NAME="${APP_NAME}"

AWS_ACCESS_KEY_ID=
AWS_SECRET_ACCESS_KEY=
AWS_DEFAULT_REGION=us-east-1
AWS_BUCKET=
AWS_USE_PATH_STYLE_ENDPOINT=false

VITE_APP_NAME="${APP_NAME}"

JWT_SECRET=TuClaveSecretaAqui
PYTHON_API_URL=http://python-app:8000
CSHARP_API_URL=http://dotnet-app:8080
```
Para configurar tu llave secreta modifica secretkey en `appsettings.Development.json` en src/dotnet-app

### 3. Construir y levantar los servicios
```bash
docker-compose up --build -d
```
### 4. Verificar que todos los contenedores estén corriendo
```bash
docker-compose ps
```
Deberías ver 7 contenedores en estado `Up`:
```
NAME                    STATUS
nginx_gateway           Up
dotnet_core_mvc         Up
php_laravel_audit       Up
python_fastapi_search   Up
sql_server_admin        Up (healthy)
postgres_audit          Up (healthy)
mongo_metadata          Up
```
### 5.Conectar las redes.
```bash
docker exec -it php_laravel_audit composer install

docker exec -it php_laravel_audit php artisan key:generate

docker exec -it php_laravel_audit php artisan migrate

```
### 5.1 Configurar Permisos.
```bash
sudo chmod -R 775 src/php-app/storage

sudo chmod -R 775 src/php-app/bootstrap/cache

sudo chown -R 33:33 src/php-app/storage

sudo chown -R 33:33 src/php-app/bootstrap/cache
```
### 5.2 Limpiar la cache de Laravel
```bash
docker exec -it php_laravel_audit php artisan cache:clear

docker exec -it php_laravel_audit php artisan view:clear
```

### 6. Acceder al sistema
- **Aplicación principal** (vía Nginx): [http://localhost](http://localhost)
- **Panel .NET** (directo): [http://localhost:5269](http://localhost:5269)

---
## 🔐 Variables de Entorno
| Variable | Servicio | Descripción | Valor por Defecto |
|---|---|---|---|
| `SQL_SERVER_PASSWORD` | SQL Server, .NET | Contraseña del usuario `sa` | `TuPasswordSeguro123!` |
| `POSTGRES_DB` | PostgreSQL | Nombre de la base de datos | `quality_audit` |
| `POSTGRES_USER` | PostgreSQL | Usuario de PostgreSQL | `admin` |
| `POSTGRES_PASSWORD` | PostgreSQL | Contraseña de PostgreSQL | — |
| `MONGO_INITDB_ROOT_USERNAME` | MongoDB, Python | Usuario root de MongoDB | `mongo_admin` |
| `MONGO_INITDB_ROOT_PASSWORD` | MongoDB, Python | Contraseña root de MongoDB | — |
| `SMTP_EMAIL` | .NET | Correo electrónico del remitente SMTP | — |
| `SMTP_PASSWORD` | .NET | Contraseña de aplicación de Gmail | — |
---
## 🌐 Puertos y Accesos
| Puerto | Servicio | Descripción |
|---|---|---|
| `80` | Nginx | Punto de entrada principal (reverse proxy) |
| `5269` | .NET App | Acceso directo a la aplicación de administración |
| `8000` | Python App | API de búsqueda + documentación Swagger |
| `14333` | SQL Server | Acceso directo a SQL Server (puerto mapeado de 1433) |
| `5432` | PostgreSQL | Acceso directo a PostgreSQL |
| `27017` | MongoDB | Acceso directo a MongoDB |
---
## 🛠 Tecnologías Utilizadas
<div align="center">
### Backend
| Tecnología | Versión | Uso |
|---|---|---|
| ASP.NET Core MVC | 9.0 | Microservicio de administración |
| Laravel | 12.0 | Microservicio de auditoría |
| FastAPI | 0.115.12 | Motor de búsqueda |
| Entity Framework Core | 9.0.6 | ORM para .NET |
| Eloquent | (Laravel) | ORM para PHP |
| Motor (async) | 3.7.1 | Driver MongoDB para Python |
### Bases de Datos
| Tecnología | Versión | Uso |
|---|---|---|
| SQL Server | 2022 | Datos transaccionales principales |
| PostgreSQL | 15 | Registros de auditoría |
| MongoDB | Latest | Índice de búsqueda full-text |
### Infraestructura
| Tecnología | Uso |
|---|---|
| Docker & Docker Compose | Containerización y orquestación |
| Nginx | Reverse proxy y ruteo |
| BCrypt | Hashing de contraseñas |
| SMTP (Gmail) | Envío de correos electrónicos |
### Frontend
| Tecnología | Uso |
|---|---|
| Bootstrap 5.3 | Framework CSS (tema oscuro) |
| Chart.js | Gráficas en el dashboard |
| Blade Templates | Vistas en Laravel |
| Razor Pages | Vistas en .NET |
| Font Awesome | Iconografía |
</div>
---
<div align="center">
**QualityDoc-Polyglot** © 2025 — Sistema de Gestión de Calidad Documental
Desarrollado con ❤️ usando arquitectura de microservicios políglota
</div>
]]>
