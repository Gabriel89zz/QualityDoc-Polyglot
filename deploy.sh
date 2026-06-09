#!/bin/bash

# Detiene el script si hay un error crítico
set -e

echo "========================================================="
echo "🚀 PREPARANDO SERVIDOR PARA QUALITYDOC-POLYGLOT"
echo "========================================================="

# ---------------------------------------------------------
# PASO 1: ASIGNAR PERMISOS A SCRIPTS DE ARRANQUE
# ---------------------------------------------------------
echo "🔐 1/3 - Otorgando permisos de ejecución a los scripts..."
chmod +x docker/php/entrypoint.sh
chmod +x db/sql-server/entrypoint.sh
chmod -R 755 ./db/sql-server/scripts

# ---------------------------------------------------------
# PASO NUEVO: GENERAR JWT SECRET GLOBAL AUTOMÁTICO
# ---------------------------------------------------------
echo "🛡️ Verificando clave maestra JWT para los microservicios..."

# Busca si el .env todavía tiene el texto de ejemplo
if grep -q "LARAVEL_JWT_SECRET=tu_secreto_jwt_aqui" .env; then
    echo "🔑 Generando un nuevo JWT Secret criptográfico..."
    
    # Crea una cadena aleatoria súper segura de 32 bytes en base64
    NUEVO_JWT=$(openssl rand -base64 32)
    
    # Busca el texto de ejemplo en el .env y lo reemplaza por la nueva llave
    sed -i "s|LARAVEL_JWT_SECRET=tu_secreto_jwt_aqui|LARAVEL_JWT_SECRET=$NUEVO_JWT|g" .env
    
    echo "✅ JWT Secret inyectado con éxito en el archivo global."
else
    echo "⚡ El JWT Secret ya está configurado. Se conserva para no cerrar sesiones."
fi

# ---------------------------------------------------------
# PASO 2: LEVANTAR INFRAESTRUCTURA
# ---------------------------------------------------------
echo "🐳 2/3 - Levantando toda la infraestructura con Docker..."
sudo docker compose -f docker-compose.prod.yml up -d --build

# ---------------------------------------------------------
# PASO 3: CONFIGURACIÓN AUTOMÁTICA DE LARAVEL (SEGURIDAD)
# ---------------------------------------------------------
echo "🔑 3/3 - Configurando entorno y generando APP_KEY automática..."
echo "⏳ Esperando 5 segundos a que el contenedor de PHP inicialice..."
sleep 5

# 1. Creamos el .env interno clonando la plantilla limpia
sudo docker exec -i php_laravel_prod cp .env.example .env

# 2. Fabricamos la llave criptográfica maestra de forma segura
sudo docker exec -i php_laravel_prod php artisan key:generate --force

# 3. Limpiamos la caché interna para aplicar la llave al instante
sudo docker exec -i php_laravel_prod php artisan config:clear
sudo docker exec -i php_laravel_prod php artisan cache:clear

echo "========================================================="
echo "✅ ¡SISTEMA LEVANTADO CON ÉXITO!"
echo "🌐 Todo el equipo ya puede acceder desde sus navegadores."
echo "========================================================="