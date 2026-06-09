# 🚀 QualityDoc-Polyglot

Sistema integral para la gestión documental, auditoría, búsqueda inteligente y administración de calidad mediante una arquitectura basada en microservicios y contenedores Docker.

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
IP_DEL_SERVIDOR_DEL_PROFE=192.168.1.184

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

Una vez guardados los cambios en el archivo `.env`, ejecuta:

```bash
bash deploy.sh
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
http://<IP_DEL_SERVIDOR_DEL_PROFE>:<APP_PORT>
```

### Ejemplo

```text
http://192.168.1.184:8080
```

---

## Documentación Interactiva de la API de Búsqueda

```text
http://<IP_DEL_SERVIDOR_DEL_PROFE>:8000/docs
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

```bash
docker compose down
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




[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/Gabriel89zz/QualityDoc-Polyglot)