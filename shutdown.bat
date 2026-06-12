@echo off
:: =========================================================
:: 🛑 SCRIPT DE APAGADO SEGURO PARA WINDOWS
:: PROYECTO: QualityDoc-Polyglot
:: =========================================================
chcp 65001 >nul
cls

:: Configura la consola con color base elegante
color 0B

echo =========================================================
echo   ____  _           _      _                     
echo  / ___^\^| ^|__  _   _^| ^|_ __^| ^| _____      ___ __  
echo  \___ \^| '_ \^| ^| ^| ^| __/ _` ^|/ _ \ \ /\ / / '_ \ 
echo   ___) ^| ^| ^| ^| ^|_^| ^| ^|^| (_^| ^| (_) \ V  V /^| ^| ^| ^|
echo  ^|____/^|_^| ^|_^|\__,_^|\__\__,_^|\___/ \_/\_/ ^|_^| ^|_^|
echo                               POLYGLOT SHUTDOWN
echo =========================================================
echo 🛑 DETENIENDO ORQUESTACION DE MICROSERVICIOS
echo =========================================================
echo.

echo    -^> Deteniendo Módulo Enrutador (Nginx Proxy)...
docker compose -f proxy/docker-compose.prod.yml down

echo    -^> Deteniendo Módulo de Auditoría (PostgreSQL + Laravel)...
docker compose -f audit/docker-compose.prod.yml down

echo    -^> Deteniendo Módulo de Búsqueda (MongoDB + FastAPI)...
docker compose -f search/docker-compose.prod.yml down

echo    -^> Deteniendo Módulo de Administración (SQL Server + C#)...
docker compose -f admin/docker-compose.prod.yml down

:: Cambia a verde al finalizar con éxito
color 0A
echo.
echo =========================================================
echo ✅ ¡SISTEMA APAGADO DE FORMA SEGURA!
echo 💾 Tus datos, bases de datos y archivos estan a salvo.
echo =========================================================
pause
exit /b 0