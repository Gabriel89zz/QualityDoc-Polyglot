#!/bin/bash

# Detiene el script si hay un error crítico
set -e

echo "========================================================="
echo "🚀 PREPARANDO SERVIDOR PARA QUALITYDOC-POLYGLOT"
echo "========================================================="

# ---------------------------------------------------------
# PASO 1: CREAR BÓVEDA DE ARCHIVOS FÍSICOS
# ---------------------------------------------------------
echo "📂 1/4 - Creando bóveda segura para PDFs físicos en /var..."
sudo mkdir -p /var/qualitydoc_data/uploads
sudo chmod -R 777 /var/qualitydoc_data/uploads

# ---------------------------------------------------------
# PASO 1.5: CREAR BÓVEDA DE LLAVES CRIPTOGRÁFICAS
# ---------------------------------------------------------
echo "🔑 1.5/4 - Creando bóveda para llaves de seguridad..."
# Creamos la carpeta en la ruta donde está el proyecto
mkdir -p keys
# Asignamos permisos para que el contenedor pueda leer/escribir
sudo chmod -R 777 keys

# ---------------------------------------------------------
# PASO 2: ASIGNAR PERMISOS A SCRIPTS DE ARRANQUE
# ---------------------------------------------------------
echo "🔐 2/4 - Otorgando permisos de ejecución a los scripts..."
chmod +x docker/php/entrypoint.sh
chmod +x db/sql-server/entrypoint.sh

# ---------------------------------------------------------
# PASO 3: LEVANTAR INFRAESTRUCTURA
# ---------------------------------------------------------
echo "🐳 3/4 - Levantando toda la infraestructura con Docker..."
sudo docker compose -f docker-compose.prod.yml up -d --build

echo "========================================================="
echo "✅ ¡SISTEMA LEVANTADO CON ÉXITO!"
echo "🌐 Todo el equipo ya puede acceder desde sus navegadores."
echo "========================================================="