@echo off
:: =========================================================
:: 🚀 SCRIPT DE DESPLIEGUE AUTOMÁTICO PARA WINDOWS
:: PROYECTO: QualityDoc-Polyglot
:: =========================================================
chcp 65001 >nul
echo =========================================================
echo 🚀 PREPARANDO ENTORNO WINDOWS PARA QUALITYDOC-POLYGLOT
echo =========================================================

:: 1. Verificar si Docker está instalado y corriendo
echo 🔍 Verificando Docker...
docker --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Docker no está instalado o no está en el PATH de Windows.
    echo Descarga e instala Docker Desktop desde: https://www.docker.com/products/docker-desktop/
    goto error
)

:: 2. Crear carpeta local para PDFs si no existe
echo 📂 1/2 - Creando carpeta local para PDFs físicos...
if not exist "datos_produccion\uploads" (
    mkdir "datos_produccion\uploads"
    echo [OK] Carpeta "datos_produccion\uploads" creada con éxito.
) else (
    echo [OK] La carpeta "datos_produccion\uploads" ya existe.
)

:: 3. Levantar Infraestructura con Docker Compose
echo 🐳 2/2 - Levantando toda la infraestructura con Docker...
docker compose -f docker-compose.prod.yml up -d --build

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Error crítico al levantar los contenedores de Docker.
    echo Asegúrate de que Docker Desktop esté ENCENDIDO y ejecutándose.
    goto error
)

echo =========================================================
echo ✅ ¡SISTEMA LEVANTADO CON ÉXITO EN WINDOWS!
echo 🌐 Abre tu navegador e ingresa a tu localhost
echo =========================================================
pause
exit /b 0