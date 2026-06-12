#!/bin/bash

# Detiene el script si hay un error crítico
set -e

# =========================================================
# 🎨 CONFIGURACIÓN DE COLORES PARA LA TERMINAL
# =========================================================
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m' # Sin color (No Color)

# =========================================================
# 🚀 BANNER ESTÉTICO
# =========================================================
clear
echo -e "${CYAN}"
cat << "EOF"
  ____               _ _ _         ____             
 / __ \             | (_) |       |  _ \            
| |  | |_   _  __ _ | |_| |_ _   _| | | | ___   ___ 
| |  | | | | |/ _` || | | __| | | | | | |/ _ \ / __|
| |__| | |_| | (_| || | | |_| |_| | |__| | (_) | (__ 
 \___\_\\__,_|\__,_||_|_|\__|\__, |____/ \___/ \___|
                              __/ |                 
                             |___/ POLYGLOT DEPLOYER
EOF
echo -e "${NC}"
echo -e "${BLUE}=========================================================${NC}"
echo -e "${GREEN} 🚀 INICIANDO ORQUESTACIÓN DE MICROSERVICIOS ${NC}"
echo -e "${BLUE}=========================================================${NC}\n"

# ---------------------------------------------------------
# PASO 0: VERIFICACIÓN E INSTALACIÓN MULTIDISTRO DE DOCKER
# ---------------------------------------------------------
echo -e "${YELLOW}🔍 0/4 - Comprobando dependencias del sistema...${NC}"

if ! command -v docker &> /dev/null; then
    echo -e "${RED}   [!] Docker no está instalado en este sistema.${NC}"
    echo -e "${CYAN}   [+] Iniciando instalación automatizada multidistribución...${NC}"
    
    # Descarga y ejecuta el script universal oficial de Docker
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    rm get-docker.sh
    
    echo -e "${GREEN}   [✔] Docker instalado correctamente.${NC}"
else
    echo -e "${GREEN}   [✔] Docker ya está instalado.${NC}"
fi

# Verificar que el servicio de Docker esté corriendo
if ! sudo systemctl is-active --quiet docker; then
    echo -e "${YELLOW}   [!] El servicio de Docker está apagado. Encendiéndolo...${NC}"
    sudo systemctl start docker
    sudo systemctl enable docker
fi

# ---------------------------------------------------------
# PASO 1: ASIGNAR PERMISOS A SCRIPTS DE ARRANQUE
# ---------------------------------------------------------
echo -e "\n${YELLOW}🔐 1/4 - Otorgando permisos de ejecución a los scripts...${NC}"
chmod +x audit/docker/php/entrypoint.sh || true 
chmod +x admin/db/sql-server/entrypoint.sh
chmod -R 755 admin/db/sql-server/scripts
echo -e "${GREEN}   [✔] Permisos asignados.${NC}"

# ---------------------------------------------------------
# PASO NUEVO: GENERAR JWT SECRET GLOBAL AUTOMÁTICO
# ---------------------------------------------------------
echo -e "\n${YELLOW}🛡️ Verificando clave maestra JWT para los microservicios...${NC}"
if grep -q "LARAVEL_JWT_SECRET=tu_secreto_jwt_aqui" .env; then
    echo -e "${CYAN}   🔑 Generando un nuevo JWT Secret criptográfico...${NC}"
    NUEVO_JWT=$(openssl rand -base64 32)
    sed -i "s|LARAVEL_JWT_SECRET=tu_secreto_jwt_aqui|LARAVEL_JWT_SECRET=$NUEVO_JWT|g" .env
    echo -e "${GREEN}   [✔] JWT Secret inyectado con éxito en el archivo global.${NC}"
else
    echo -e "${BLUE}   ⚡ El JWT Secret ya está configurado. Se conserva para no cerrar sesiones.${NC}"
fi

# ---------------------------------------------------------
# PASO 2: PREPARAR RED DE MICROSERVICIOS
# ---------------------------------------------------------
echo -e "\n${YELLOW}🌐 2/4 - Verificando red compartida (quality-net)...${NC}"
if ! sudo docker network ls | grep -q "quality-net"; then
    echo -e "${CYAN}   Creando red externa 'quality-net'...${NC}"
    sudo docker network create quality-net
    echo -e "${GREEN}   [✔] Red creada exitosamente.${NC}"
else
    echo -e "${GREEN}   [✔] La red 'quality-net' ya existe.${NC}"
fi

# ---------------------------------------------------------
# PASO 3: LEVANTAR INFRAESTRUCTURA MODULAR (EN ORDEN)
# ---------------------------------------------------------
echo -e "\n${YELLOW}🐳 3/4 - Levantando módulos con Docker en orden estratégico...${NC}"

echo -e "${CYAN}   -> Levantando Módulo de Administración (SQL Server + C#)...${NC}"
sudo docker compose --env-file .env -f admin/docker-compose.admin.yml up -d --build

echo -e "${CYAN}   -> Levantando Módulo de Búsqueda (MongoDB + FastAPI)...${NC}"
sudo docker compose --env-file .env -f search/docker-compose.search.yml up -d --build

echo -e "${CYAN}   -> Levantando Módulo de Auditoría (PostgreSQL + Laravel)...${NC}"
sudo docker compose --env-file .env -f audit/docker-compose.audit.yml up -d --build

echo -e "${CYAN}   -> Levantando Módulo Enrutador (Nginx Proxy)...${NC}"
sudo docker compose --env-file .env -f proxy/docker-compose.proxy.yml up -d --build

# ---------------------------------------------------------
# PASO 4: CONFIGURACIÓN AUTOMÁTICA DE LARAVEL (SEGURIDAD)
# ---------------------------------------------------------
echo -e "\n${YELLOW}🔑 4/4 - Configurando entorno interno de Laravel...${NC}"
echo -e "${CYAN}⏳ Esperando 10 segundos a que PostgreSQL y PHP inicialicen por completo...${NC}"
sleep 10

sudo docker exec -i php_laravel_prod cp .env.example .env
sudo docker exec -i php_laravel_prod php artisan key:generate --force
sudo docker exec -i php_laravel_prod php artisan config:clear
sudo docker exec -i php_laravel_prod php artisan cache:clear

echo -e "${CYAN}📦 Ejecutando migraciones de auditoría en PostgreSQL...${NC}"
sudo docker exec -i php_laravel_prod php artisan migrate --force

echo -e "\n${BLUE}=========================================================${NC}"
echo -e "${GREEN} ✅ ¡SISTEMA MODULAR LEVANTADO CON ÉXITO! ${NC}"
echo -e "${GREEN} 🌐 Todo el equipo ya puede acceder desde sus navegadores. ${NC}"
echo -e "${BLUE}=========================================================${NC}"