# 1. Usamos PHP 8.2 con FPM (Fast Process Manager) para mejor rendimiento
FROM php:8.2-fpm

# 2. Instalamos dependencias del sistema
# zip/unzip y git son necesarios para que Composer funcione correctamente
RUN apt-get update && apt-get install -y \
    libpq-dev \
    zip \
    unzip \
    git \
    curl

# 3. Instalamos las extensiones de PHP para PostgreSQL
# Esto es vital para tu base de datos de auditoría
RUN docker-php-ext-install pdo pdo_pgsql

# 4. Instalamos Composer de forma oficial y global
# Copiamos el binario desde la imagen oficial de Composer
COPY --from=composer:latest /usr/bin/composer /usr/bin/composer

# 5. Definimos el directorio de trabajo
WORKDIR /var/www/html

# 6. Ajustamos permisos para que Laravel pueda escribir en carpetas de caché y logs
# Esto evitará errores de "Permission Denied" en tu HP Victus
RUN chown -R www-data:www-data /var/www/html