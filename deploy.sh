#!/bin/bash

# Detiene el script si hay un error crítico
set -e

echo "========================================================="
echo "🚀 PREPARANDO SERVIDOR PARA QUALITYDOC-POLYGLOT"
echo "========================================================="

# ---------------------------------------------------------
# PASO 1: ASIGNAR PERMISOS A SCRIPTS DE ARRANQUE
# ---------------------------------------------------------
echo "🔐 1/2 - Otorgando permisos de ejecución a los scripts..."
chmod +x docker/php/entrypoint.sh
chmod +x db/sql-server/entrypoint.sh
chmod -R 755 ./db/sql-server/scripts

# ---------------------------------------------------------
# PASO 2: LEVANTAR INFRAESTRUCTURA
# ---------------------------------------------------------
echo "🐳 2/2 - Levantando toda la infraestructura con Docker..."
sudo docker compose -f docker-compose.prod.yml up -d --build

echo "========================================================="
echo "✅ ¡SISTEMA LEVANTADO CON ÉXITO!"
echo "🌐 Todo el equipo ya puede acceder desde sus navegadores."
echo "========================================================="