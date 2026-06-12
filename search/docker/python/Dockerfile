FROM python:3.11-slim
WORKDIR /app

RUN pip install --no-cache-dir fastapi uvicorn motor pymongo

# 🚀 CORRECCIÓN PARA PRODUCCIÓN: Apuntamos a la nueva ruta modular dentro del contexto global
COPY search/src/python-app/ .

EXPOSE 8000

# 🚀 NUEVO PARA PRODUCCIÓN: Sin "--reload" para exprimir el máximo rendimiento en el servidor
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]