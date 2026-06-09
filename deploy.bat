@echo off
:: =========================================================
:: 🚀 SCRIPT DE DESPLIEGUE AUTOMÁTICO PARA WINDOWS
:: PROYECTO: QualityDoc-Polyglot
:: =========================================================
chcp 65001 >nul
echo =========================================================
echo 🚀 PREPARANDO ENTORNO WINDOWS PARA QUALITYDOC-POLYGLOT
echo =========================================================

:: ---------------------------------------------------------
:: PASO 1: GENERAR JWT SECRET GLOBAL AUTOMÁTICO
:: ---------------------------------------------------------
echo 🛡️ Verificando clave maestra JWT para los microservicios...
findstr /C:"LARAVEL_JWT_SECRET=tu_secreto_jwt_aqui" .env >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo 🔑 Generando un nuevo JWT Secret criptográfico...
    powershell -Command "$bytes = New-Object byte[] 32; (New-Object Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes); $secret = [Convert]::ToBase64String($bytes); (Get-Content .env) -replace 'LARAVEL_JWT_SECRET=tu_secreto_jwt_aqui', ('LARAVEL_JWT_SECRET='+$secret) | Set-Content .env" >nul
    echo [OK] JWT Secret inyectado con éxito en el archivo global.
) else (
    echo ⚡ El JWT Secret ya está configurado. Se conserva para no cerrar sesiones.
)

:: ---------------------------------------------------------
:: PASO 2: LEVANTAR INFRAESTRUCTURA CON DOCKER
:: ---------------------------------------------------------
echo 🐳 Levantando toda la infraestructura con Docker...
docker compose -f docker-compose.prod.yml up -d --build
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

:: ---------------------------------------------------------
:: PASO 3: CONFIGURACIÓN AUTOMÁTICA DE LARAVEL
:: ---------------------------------------------------------
echo 🔑 Configurando entorno y generando APP_KEY automática...
echo ⏳ Esperando 5 segundos a que el contenedor de PHP inicialice...
timeout /t 5 /nobreak >nul

docker exec -i php_laravel_prod cp .env.example .env
docker exec -i php_laravel_prod php artisan key:generate --force
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%
docker exec -i php_laravel_prod php artisan config:clear
docker exec -i php_laravel_prod php artisan cache:clear

echo =========================================================
echo ✅ ¡SISTEMA LEVANTADO CON ÉXITO!
echo 🌐 Todo el equipo ya puede acceder desde sus navegadores.
echo =========================================================
pause
exit /b 0