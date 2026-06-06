#!/bin/bash

# Detiene el script si hay un error crítico
set -e

echo "========================================================="
echo "🚀 PREPARANDO SERVIDOR PARA QUALITYDOC-POLYGLOT"
echo "========================================================="

# ---------------------------------------------------------
# PASO 0: VERIFICACIÓN DE DEPENDENCIAS (Git y Docker)
# ---------------------------------------------------------
echo "🔍 Verificando herramientas del sistema..."

if ! command -v git &> /dev/null; then
    echo "⚙️  Git no encontrado. Instalando..."
    sudo apt update && sudo apt install git curl -y
else
    echo "✅ Git ya está instalado."
fi

if ! command -v docker &> /dev/null; then
    echo "⚙️  Docker no encontrado. Instalando versión oficial..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo usermod -aG docker $USER
    echo "✅ Docker instalado correctamente."
else
    echo "✅ Docker ya está instalado."
fi

# ---------------------------------------------------------
# PASO 1: CREAR BÓVEDA DE ARCHIVOS FÍSICOS
# ---------------------------------------------------------
echo "📂 1/3 - Creando bóveda segura para PDFs físicos en /var..."
sudo mkdir -p /var/qualitydoc_data/uploads
sudo chmod -R 777 /var/qualitydoc_data/uploads

# ---------------------------------------------------------
# PASO 2: ASIGNAR PERMISOS A SCRIPTS DE ARRANQUE
# ---------------------------------------------------------
echo "🔐 2/3 - Otorgando permisos de ejecución a los scripts..."
chmod +x docker/php/entrypoint.sh
chmod +x db/sql-server/entrypoint.sh

# ---------------------------------------------------------
# PASO 3: LEVANTAR INFRAESTRUCTURA
# ---------------------------------------------------------
echo "🐳 3/3 - Levantando toda la infraestructura con Docker..."
# Usamos sudo aquí por si Docker se acaba de instalar y los permisos de usuario aún no refrescan
sudo docker compose -f docker-compose.prod.yml up -d --build

echo "========================================================="
echo "✅ ¡SISTEMA LEVANTADO CON ÉXITO!"
echo "🌐 Todo el equipo ya puede acceder desde sus navegadores."
echo "========================================================="