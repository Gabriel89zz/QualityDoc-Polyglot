# 1. Usamos PHP 8.2 con FPM
FROM php:8.2-fpm

# 2. Instalamos dependencias
RUN apt-get update && apt-get install -y \
    libpq-dev libzip-dev zip unzip git curl

# 3. Instalamos las extensiones de PHP
RUN docker-php-ext-install pdo pdo_pgsql zip bcmath pcntl

# 4. Instalamos Composer
COPY --from=composer:latest /usr/bin/composer /usr/bin/composer

WORKDIR /var/www/html

# 🚀 NUEVO PARA PRODUCCIÓN: Copiamos todo el código de Laravel adentro de la imagen
COPY audit/src/php-app .

# 🚀 NUEVO PARA PRODUCCIÓN: Instalamos dependencias optimizadas (sin paquetes de pruebas)
RUN composer install --no-dev --optimize-autoloader

# Ajustamos permisos de seguridad para el servidor web
RUN chown -R www-data:www-data /var/www/html \
    && chmod -R 775 /var/www/html/storage \
    && chmod -R 775 /var/www/html/bootstrap/cache

# 🚀 NUEVO: Configuramos el script de arranque para producción
COPY audit/docker/php/entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh
ENTRYPOINT ["entrypoint.sh"]