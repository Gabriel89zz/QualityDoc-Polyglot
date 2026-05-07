# 1. Imagen base ligera de Python
FROM python:3.11-slim

# 2. Directorio de trabajo dentro del contenedor
WORKDIR /app

# 3. Instalamos las librerías necesarias
# fastapi: El framework
# uvicorn: El servidor para correr la app
# motor: Driver asíncrono para MongoDB (el mejor para FastAPI)
RUN pip install --no-cache-dir fastapi uvicorn motor pymongo

# 4. Exponemos el puerto que usa FastAPI por defecto
EXPOSE 8000

# 5. Comando para iniciar el servicio en modo desarrollo (--reload)
# Esto hará que cada que guardes un cambio en tu código, el contenedor se reinicie solo
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000", "--reload"]