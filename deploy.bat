@echo off
:: =========================================================
:: 🚀 SCRIPT DE DESPLIEGUE AUTOMÁTICO PARA WINDOWS (MODULAR)
:: PROYECTO: QualityDoc-Polyglot
:: =========================================================
chcp 65001 >nul
echo =========================================================
echo 🚀 PREPARANDO ENTORNO WINDOWS PARA QUALITYDOC-POLYGLOT
echo =========================================================

:: ---------------------------------------------------------
:: PASO 1: GENERAR JWT SECRET GLOBAL AUTOMÁTICO
:: ---------------------------------------------------------
echo 🛡️ 1/4 - Verificando clave maestra JWT para los microservicios...
findstr /C:"LARAVEL_JWT_SECRET=tu_secreto_jwt_aqui" .env >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo 🔑 Generando un nuevo JWT Secret criptográfico...
    powershell -Command "$bytes = New-Object byte[] 32; (New-Object Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes); $secret = [Convert]::ToBase64String($bytes); (Get-Content .env) -replace 'LARAVEL_JWT_SECRET=tu_secreto_jwt_aqui', ('LARAVEL_JWT_SECRET='+$secret) | Set-Content .env" >nul
    echo ✅ [OK] JWT Secret inyectado con éxito en el archivo global.
) else (
    echo ⚡ El JWT Secret ya está configurado. Se conserva para no cerrar sesiones.
)

:: ---------------------------------------------------------
:: PASO 2: PREPARAR RED DE MICROSERVICIOS
:: ---------------------------------------------------------
echo 🌐 2/4 - Verificando red compartida (quality-net)...
docker network inspect quality-net >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo    Creando red externa 'quality-net'...
    docker network create quality-net >nul
) else (
    echo    La red 'quality-net' ya existe.
)

:: ---------------------------------------------------------
:: PASO 3: LEVANTAR INFRAESTRUCTURA MODULAR (EN ORDEN)
:: ---------------------------------------------------------
echo 🐳 3/4 - Levantando módulos con Docker en orden estratégico...

echo    -^> Levantando Módulo de Administración (SQL Server + C#)...
docker compose --env-file .env -f admin/docker-compose.admin.yml up -d --build
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

echo    -^> Levantando Módulo de Búsqueda (MongoDB + FastAPI)...
docker compose --env-file .env -f search/docker-compose.search.yml up -d --build
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

echo    -^> Levantando Módulo de Auditoría (PostgreSQL + Laravel)...
docker compose --env-file .env -f audit/docker-compose.audit.yml up -d --build
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

echo    -^> Levantando Módulo Enrutador (Nginx Proxy)...
docker compose --env-file .env -f proxy/docker-compose.proxy.yml up -d --build
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

:: ---------------------------------------------------------
:: PASO 4: CONFIGURACIÓN AUTOMÁTICA DE LARAVEL (SEGURIDAD)
:: ---------------------------------------------------------
echo 🔑 4/4 - Configurando entorno interno de Laravel...
echo ⏳ Esperando 10 segundos a que PostgreSQL y PHP inicialicen por completo...
timeout /t 10 /nobreak >nul

:: 1. Creamos el .env interno clonando la plantilla limpia
docker exec -i php_laravel_prod cp .env.example .env

:: 2. Fabricamos la llave criptográfica maestra de forma segura
docker exec -i php_laravel_prod php artisan key:generate --force
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

:: 3. Limpiamos la caché interna para aplicar la llave al instante
docker exec -i php_laravel_prod php artisan config:clear
docker exec -i php_laravel_prod php artisan cache:clear

:: 4. Corremos migraciones de base de datos
echo 📦 Ejecutando migraciones de auditoría en PostgreSQL...
docker exec -i php_laravel_prod php artisan migrate --force

echo =========================================================
echo ✅ ¡SISTEMA MODULAR LEVANTADO CON ÉXITO!
echo 🌐 Todo el equipo ya puede acceder desde sus navegadores locales.
echo =========================================================
pause
exit /b 0