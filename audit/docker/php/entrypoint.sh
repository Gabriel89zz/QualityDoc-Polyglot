#!/bin/bash
set -e

echo "🚀 Iniciando QualityDoc Audit (Laravel) en Producción..."

# 1. Optimizar velocidad (Caché de rutas, vistas y configuración)
php artisan config:cache
php artisan route:cache
php artisan view:cache

# 2. Correr migraciones automáticamente (crear tablas en Postgres)
# El flag --force es vital en producción para que no pida "Yes/No"
php artisan migrate --force

echo "✅ Migraciones listas. Levantando servidor web..."
# 3. Arrancar el proceso de PHP
exec php-fpm