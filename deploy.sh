#!/bin/bash

# Detiene el script si hay un error crítico
set -e

echo "========================================================="
echo "🚀 PREPARANDO SERVIDOR PARA QUALITYDOC-POLYGLOT (MODULAR)"
echo "========================================================="

# ---------------------------------------------------------
# PASO 1: ASIGNAR PERMISOS A SCRIPTS DE ARRANQUE
# ---------------------------------------------------------
echo "🔐 1/4 - Otorgando permisos de ejecución a los scripts..."
# Ajustado a las nuevas rutas modulares. (El || true evita que falle si el archivo php no existe en esa ruta exacta)
chmod +x audit/docker/php/entrypoint.sh || true 
chmod +x admin/db/sql-server/entrypoint.sh
chmod -R 755 admin/db/sql-server/scripts

# ---------------------------------------------------------
# PASO NUEVO: GENERAR JWT SECRET GLOBAL AUTOMÁTICO
# ---------------------------------------------------------
echo "🛡️ Verificando clave maestra JWT para los microservicios..."
if grep -q "LARAVEL_JWT_SECRET=tu_secreto_jwt_aqui" .env; then
    echo "🔑 Generando un nuevo JWT Secret criptográfico..."
    NUEVO_JWT=$(openssl rand -base64 32)
    sed -i "s|LARAVEL_JWT_SECRET=tu_secreto_jwt_aqui|LARAVEL_JWT_SECRET=$NUEVO_JWT|g" .env
    echo "✅ JWT Secret inyectado con éxito en el archivo global."
else
    echo "⚡ El JWT Secret ya está configurado. Se conserva para no cerrar sesiones."
fi

# ---------------------------------------------------------
# PASO 2: PREPARAR RED DE MICROSERVICIOS
# ---------------------------------------------------------
echo "🌐 2/4 - Verificando red compartida (quality-net)..."
# Como nuestros archivos dicen 'external: true', la red debe existir antes de levantar nada
if ! sudo docker network ls | grep -q "quality-net"; then
    echo "   Creando red externa 'quality-net'..."
    sudo docker network create quality-net
else
    echo "   La red 'quality-net' ya existe."
fi

# ---------------------------------------------------------
# PASO 3: LEVANTAR INFRAESTRUCTURA MODULAR (EN ORDEN)
# ---------------------------------------------------------
echo "🐳 3/4 - Levantando módulos con Docker en orden estratégico..."

echo "   -> Levantando Módulo de Administración (SQL Server + C#)..."
sudo docker compose --env-file .env -f admin/docker-compose.admin.yml up -d --build

echo "   -> Levantando Módulo de Búsqueda (MongoDB + FastAPI)..."
sudo docker compose --env-file .env -f search/docker-compose.search.yml up -d --build

echo "   -> Levantando Módulo de Auditoría (PostgreSQL + Laravel)..."
sudo docker compose --env-file .env -f audit/docker-compose.audit.yml up -d --build

echo "   -> Levantando Módulo Enrutador (Nginx Proxy)..."
sudo docker compose --env-file .env -f proxy/docker-compose.proxy.yml up -d --build

# ---------------------------------------------------------
# PASO 4: CONFIGURACIÓN AUTOMÁTICA DE LARAVEL (SEGURIDAD)
# ---------------------------------------------------------
echo "🔑 4/4 - Configurando entorno interno de Laravel..."
echo "⏳ Esperando 10 segundos a que PostgreSQL y PHP inicialicen por completo..."
sleep 10

# 1. Creamos el .env interno clonando la plantilla limpia
sudo docker exec -i php_laravel_prod cp .env.example .env

# 2. Fabricamos la llave criptográfica maestra de forma segura
sudo docker exec -i php_laravel_prod php artisan key:generate --force

# 3. Limpiamos la caché interna para aplicar la llave al instante
sudo docker exec -i php_laravel_prod php artisan config:clear
sudo docker exec -i php_laravel_prod php artisan cache:clear

# 4. Corremos migraciones de producción (La bandera --force es obligatoria en producción)
echo "📦 Ejecutando migraciones de auditoría en PostgreSQL..."
sudo docker exec -i php_laravel_prod php artisan migrate --force

echo "========================================================="
echo "✅ ¡SISTEMA MODULAR LEVANTADO CON ÉXITO!"
echo "🌐 Todo el equipo ya puede acceder desde sus navegadores a la IP del servidor."
echo "========================================================="