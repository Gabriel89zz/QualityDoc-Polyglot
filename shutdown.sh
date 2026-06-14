#!/bin/bash

# =========================================================
# 🎨 CONFIGURACIÓN DE COLORES
# =========================================================
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # Sin color

clear
echo -e "${CYAN}"
cat << "EOF"
  ____  _           _      _                     
 / ___|| |__  _   _| |_ __| | _____      ___ __  
 \___ \| '_ \| | | | __/ _` |/ _ \ \ /\ / / '_ \ 
  ___) | | | | |_| | || (_| | (_) \ V  V /| | | |
 |____/|_| |_|\__,_|\__\__,_|\___/ \_/\_/ |_| |_|
                            POLYGLOT SHUTDOWN
EOF
echo -e "${NC}"
echo -e "${BLUE}=========================================================${NC}"
echo -e "${YELLOW} 🛑 DETENIENDO ORQUESTACIÓN DE MICROSERVICIOS ${NC}"
echo -e "${BLUE}=========================================================${NC}\n"

echo -e "${CYAN}   -> Deteniendo Módulo Enrutador (Nginx Proxy)...${NC}"
sudo docker compose -f proxy/docker-compose.proxy.yml down

echo -e "${CYAN}   -> Deteniendo Módulo de Auditoría (PostgreSQL + Laravel)...${NC}"
sudo docker compose -f audit/docker-compose.audit.yml down

echo -e "${CYAN}   -> Deteniendo Módulo de Búsqueda (MongoDB + FastAPI)...${NC}"
sudo docker compose -f search/docker-compose.search.yml down

echo -e "${CYAN}   -> Deteniendo Módulo de Administración (SQL Server + C#)...${NC}"
sudo docker compose -f admin/docker-compose.admin.yml down

echo -e "\n${BLUE}=========================================================${NC}"
echo -e "${GREEN} ✅ ¡SISTEMA APAGADO DE FORMA SEGURA! ${NC}"
echo -e "${YELLOW} 💾 Tus datos, bases de datos y archivos están a salvo. ${NC}"
echo -e "${BLUE}=========================================================${NC}"