# 1. Usamos PHP 8.2 con FPM (Fast Process Manager) para mejor rendimiento
FROM php:8.2-fpm

# 2. Instalamos dependencias del sistema (Agregamos libzip-dev)
RUN apt-get update && apt-get install -y \
    libpq-dev \
    libzip-dev \
    zip \
    unzip \
    git \
    curl

# 3. Instalamos las extensiones de PHP para PostgreSQL y utilidades de Laravel
RUN docker-php-ext-install pdo pdo_pgsql zip bcmath pcntl

# 4. Instalamos Composer de forma oficial y global
COPY --from=composer:latest /usr/bin/composer /usr/bin/composer

# 5. Definimos el directorio de trabajo
WORKDIR /var/www/html

# 6. Ajustamos permisos (Nota: al montar el volumen, los permisos del host sobrescriben esto, 
# pero es buena práctica dejarlo en la imagen base)
RUN chown -R www-data:www-data /var/www/html