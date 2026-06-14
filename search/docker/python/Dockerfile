# 1. Imagen base ligera de Python
FROM python:3.11-slim

# 2. Directorio de trabajo
WORKDIR /app

# 🚀 FIX: Instalamos LibreOffice (versión sin interfaz gráfica) para convertir archivos a PDF
RUN apt-get update && apt-get install -y libreoffice --no-install-recommends && rm -rf /var/lib/apt/lists/*

# 3. Copiamos y ejecutamos dependencias
COPY search/src/python-app/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# 4. Exponemos puerto
EXPOSE 8000

# 5. Comando de inicio
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000", "--reload"]