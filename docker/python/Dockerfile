FROM python:3.11-slim
WORKDIR /app

RUN pip install --no-cache-dir fastapi uvicorn motor pymongo

# 🚀 NUEVO PARA PRODUCCIÓN: Copiamos el código de la API de búsqueda
COPY src/python-app .

EXPOSE 8000

# 🚀 NUEVO PARA PRODUCCIÓN: Quitamos el "--reload" para máximo rendimiento
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]