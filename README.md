# 🚀 QualityDoc-Polyglot

## Visión General
QualityDoc-Polyglot es una plataforma de gestión documental y cumplimiento normativo basada en una arquitectura de microservicios políglota (Polyglot Microservices Architecture).

La solución utiliza múltiples tecnologías especializadas para aprovechar las fortalezas de cada ecosistema:

* **ASP.NET Core 10.0 MVC** para procesos administrativos y flujos de aprobación complejos.
* **PHP Laravel 10** para la interacción rápida de operadores y usuarios finales.
* **Python FastAPI** para indexación, búsqueda avanzada y recuperación eficiente de documentos.
* **Nginx** como Gateway y Reverse Proxy centralizado.
* **SQL Server**, **PostgreSQL** y **MongoDB** como estrategia de persistencia políglota.

---

# 📋 Guía de Instalación y Despliegue

Sigue los pasos descritos a continuación para clonar, configurar y desplegar toda la infraestructura de **QualityDoc-Polyglot** de forma automatizada.

## 📥 Paso 1: Clonar el Repositorio

Abre una terminal en tu servidor Debian, Ubuntu o equipo local y ejecuta:

```bash
git clone https://github.com/Gabriel89zz/QualityDoc-Polyglot.git
cd QualityDoc-Polyglot
```

---

## 📄 Paso 2: Crear el Archivo de Entorno Global

Genera el archivo `.env` a partir de la plantilla incluida en el proyecto:

```bash
cp .env.example .env
```

---

## ⚙️ Paso 3: Configurar las Variables de Entorno

Edita el archivo `.env` con tu editor favorito:

```bash
nano .env
```

Es importante configurar correctamente:

* La IP del servidor donde se ejecutará el sistema.
* El puerto de acceso principal.
* Las credenciales de las bases de datos.
* Los datos del servicio de correo electrónico.

### Ejemplo de configuración

```env
# ==========================================
# CONFIGURACIÓN GENERAL
# ==========================================

PROYECTO_NOMBRE=QualityDoc-Polyglot

# IP física o pública del servidor
IP_DEL_SERVIDOR=192.168.1.184

# Puerto externo utilizado por Nginx
APP_PORT=8080

# ==========================================
# SQL SERVER
# ==========================================

SQL_SERVER_USER=sa
SQL_SERVER_PASSWORD=TuPasswordSeguro123!

# ==========================================
# POSTGRESQL (AUDITORÍA)
# ==========================================

POSTGRES_DB=quality_audit
POSTGRES_USER=admin
POSTGRES_PASSWORD=tu_password_postgres_seguro

# ==========================================
# MONGODB (BÚSQUEDA)
# ==========================================

MONGO_INITDB_ROOT_USERNAME=mongo_admin
MONGO_INITDB_ROOT_PASSWORD=tu_password_mongo_seguro

# ==========================================
# SERVICIO DE CORREO (.NET)
# ==========================================

SMTP_EMAIL=tu_correo_emisor@gmail.com
SMTP_PASSWORD=tu_token_de_aplicacion_smtp
```

> ⚠️ **Importante:** Verifica que el puerto configurado no esté siendo utilizado por otro servicio en el servidor.

---

## ⚡ Paso 4: Ejecutar el Despliegue Automático

El proyecto cuenta con orquestadores inteligentes que detectan tu sistema operativo, verifican dependencias (instalando Docker si falta en Linux), crean redes virtuales y levantan los módulos en cascada.

Si estás en Linux/Servidor Ubuntu (Bash):

```bash
bash deploy.sh
```

Si estás en Windows (CMD):
Solo dale doble clic al archivo deploy.bat desde el explorador de archivos, o ejecútalo en la terminal:
```bash
deploy.bat
```

Este script realizará automáticamente:

* ✅ Configuración de permisos necesarios.
* ✅ Generación de llaves y recursos de seguridad.
* ✅ Creación de redes Docker.
* ✅ Construcción de imágenes.
* ✅ Despliegue de los microservicios.
* ✅ Inicio de los contenedores en segundo plano.

---

# 🏗️ Arquitectura Desplegada

El proceso de despliegue levantará los siguientes componentes:

| Servicio                   | Tecnología   |
| -------------------------- | ------------ |
| Gateway Web                | Nginx        |
| Backend Principal          | ASP.NET Core |
| Base de Datos Principal    | SQL Server   |
| Servicio de Auditoría      | PostgreSQL   |
| Motor de Búsqueda          | MongoDB      |
| API de Búsqueda            | FastAPI      |
| Servicio de Notificaciones | .NET SMTP    |

---

# 🌐 Accesos al Sistema

Una vez finalizado el despliegue, podrás acceder a los distintos servicios mediante la IP y puerto configurados.

## Portal Principal de Calidad y Administración

```text
http://<IP_DEL_SERVIDOR>:<APP_PORT>/admin/
```

### Ejemplo

```text
http://192.168.1.184:8080/admin/
```

---

## Documentación Interactiva de la API de Búsqueda

```text
http://<IP_DEL_SERVIDOR>:8000/docs
```

### Ejemplo

```text
http://192.168.1.184:8000/docs
```

---

# 🔍 Verificación del Estado de los Contenedores

Para verificar que todos los servicios estén funcionando correctamente:

```bash
docker ps
```

Para visualizar los logs de un servicio específico:

```bash
docker logs -f <nombre_del_contenedor>
```

---

# 🛑 Detener el Sistema

Para detener todos los servicios:

Si estás en Linux/Servidor Ubuntu (Bash):

```bash
bash shutdown.sh
```

Si estás en Windows (CMD):
Solo dale doble clic al archivo shutdown.bat desde el explorador de archivos, o ejecútalo en la terminal:
```bash
shutdowm.bat
```

---

# 🔄 Actualizar el Proyecto

Si existen cambios en el repositorio:

```bash
git pull origin main
bash deploy.sh
```

---

# 📚 Tecnologías Utilizadas

* ASP.NET Core
* SQL Server
* PostgreSQL
* MongoDB
* FastAPI
* Docker
* Docker Compose
* Nginx
* JWT Authentication

---

# 👨‍💻 Equipo de Desarrollo

**QualityDoc-Polyglot**

Proyecto académico orientado a la gestión documental, auditoría y búsqueda inteligente mediante una arquitectura moderna basada en microservicios.

---

# 🏛️ Arquitectura del Sistema

```text
                    ┌─────────────┐
                    │    Usuario   │
                    └──────┬──────┘
                           │
                           ▼
                  ┌─────────────────┐
                  │  NGINX GATEWAY  │
                  │ Reverse Proxy   │
                  └─────┬─────┬─────┘
                        │     │
          ┌─────────────┘     └─────────────┐
          ▼                                 ▼

 ┌─────────────────┐             ┌─────────────────┐
 │   ASP.NET Core  │             │ PHP Laravel 10  │
 │  Portal Admin   │             │ Portal Operador │
 └────────┬────────┘             └────────┬────────┘
          │                               │
          ▼                               ▼

   ┌─────────────┐               ┌─────────────┐
   │ SQL Server  │               │ PostgreSQL │
   └─────────────┘               └─────────────┘

                 ┌────────────────────┐
                 │ Python FastAPI API │
                 │ Search Engine      │
                 └─────────┬──────────┘
                           │
                           ▼
                     ┌─────────┐
                     │ MongoDB │
                     └─────────┘
```

---

# 🌐 Gateway Centralizado

Todas las solicitudes ingresan a través de Nginx, el cual actúa como punto único de entrada para la plataforma.

### Rutas principales

| Ruta           | Servicio                           |
| -------------- | ---------------------------------- |
| `/`            | Portal Operador Laravel            |
| `/admin/`      | Portal Administrativo ASP.NET Core |
| `/api/search/` | API de Búsqueda FastAPI            |

Gracias a este enfoque, el usuario interactúa con una única dirección mientras Nginx enruta internamente cada solicitud al microservicio correspondiente.

---

# 🔷 Portal Administrativo (.NET)

## Responsabilidad

Es el núcleo del sistema y representa la fuente oficial de información (Source of Truth).

### Funcionalidades

* Gestión de empresas.
* Gestión de departamentos.
* Administración de usuarios.
* Gestión de roles y permisos.
* Control documental.
* Flujo de aprobación.
* Versionamiento de documentos.
* Firmas electrónicas.
* Gestión de normativas.
* Indicadores de cumplimiento.

### Tecnología

* ASP.NET Core 10 MVC
* Entity Framework Core
* SQL Server

---

# 🟢 Portal Operador (Laravel)

## Responsabilidad

Proporcionar una experiencia ligera y rápida para los usuarios operativos.

### Funcionalidades

* Consulta de documentos vigentes.
* Directorio documental.
* Firma de enterado.
* Confirmación de lectura.
* Historial de cumplimiento.
* Consulta rápida de normativas.

### Tecnología

* PHP 8.2
* Laravel 10
* PostgreSQL

---

# 🟡 Motor de Búsqueda (FastAPI)

## Responsabilidad

Permitir búsquedas rápidas y desacopladas del sistema principal.

### Funcionalidades

* Indexación documental.
* Búsqueda por texto.
* Filtrado avanzado.
* Recuperación de documentos.
* API REST para consultas.

### Tecnología

* Python 3.11
* FastAPI
* MongoDB

---

# 🗄️ Estrategia de Persistencia Políglota

El sistema utiliza tres motores de bases de datos especializados.

## SQL Server

Almacena:

* Empresas
* Departamentos
* Usuarios
* Roles
* Documentos
* Versiones
* Aprobaciones

## PostgreSQL

Almacena:

* Firmas de enterado
* Confirmaciones de lectura
* Historial de cumplimiento
* Actividad de operadores

## MongoDB

Almacena:

* Índices documentales
* Metadatos
* Información optimizada para búsquedas

---

# 🔐 Seguridad

## JWT Compartido

La plataforma implementa un mecanismo Single Sign-On (SSO) entre .NET y Laravel mediante JWT.

### Flujo

1. Usuario inicia sesión en ASP.NET Core.
2. ASP.NET genera un JWT firmado.
3. Laravel valida el token.
4. Se crea una sesión local automáticamente.
5. El usuario navega entre portales sin volver a autenticarse.

---

## Endurecimiento de Seguridad

Nginx implementa medidas adicionales de protección:

* X-Frame-Options
* X-Content-Type-Options
* X-XSS-Protection
* Aislamiento de red Docker
* Bases de datos no expuestas públicamente

---

# 🐳 Infraestructura Docker

Todos los servicios se ejecutan dentro de contenedores Docker conectados mediante una red privada:

```text
quality-net
```

Beneficios:

* Aislamiento de servicios.
* Comunicación segura interna.
* Despliegue reproducible.
* Escalabilidad futura.
* Portabilidad entre entornos.

---

# 🚀 Beneficios de la Arquitectura

* Separación clara de responsabilidades.
* Escalabilidad independiente por servicio.
* Mayor rendimiento en búsquedas.
* Mejor mantenibilidad.
* Persistencia especializada según el tipo de información.
* Despliegue simplificado mediante Docker Compose.
* Posibilidad de evolucionar cada componente de forma independiente.

---

# Para ver la documentacion detallada y resolver dudas aqui:
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/Gabriel89zz/QualityDoc-Polyglot)