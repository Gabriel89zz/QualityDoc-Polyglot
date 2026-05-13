import os
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from motor.motor_asyncio import AsyncIOMotorClient
from datetime import datetime

app = FastAPI(title="QualityDoc Polyglot - Motor de Búsqueda")

# 1. Credenciales y Conexión
MONGO_USER = os.getenv("MONGO_INITDB_ROOT_USERNAME", "admin_mongo")
MONGO_PASS = os.getenv("MONGO_INITDB_ROOT_PASSWORD", "TuPasswordSeguroMongo123")
MONGO_URL = f"mongodb://{MONGO_USER}:{MONGO_PASS}@mongo-db:27017"

client = AsyncIOMotorClient(MONGO_URL)
db = client.qualitydoc_metadata
coleccion_docs = db.documentos_aprobados # Así se llamará la "tabla" en Mongo

# 2. Definimos el "Molde" de los datos que esperamos recibir de C#
class DocumentoAprobado(BaseModel):
    documento_id: int # El ID original de SQL Server
    codigo: str
    titulo: str
    version: str
    etiquetas: list[str] # Lista de palabras clave para buscar rápido
    url_archivo: str
    aprobado_por: str

# 3. Ruta de prueba (la que ya tenías)
@app.get("/")
async def root():
    return {
        "status": "online",
        "message": "Microservicio FastAPI conectado a MongoDB SECURE exitosamente bro"
    }

# 🚀 4. NUEVA RUTA POST: Aquí C# mandará los datos
@app.post("/api/docs/index")
async def indexar_documento(doc: DocumentoAprobado):
    try:
        # Convertimos el molde a un diccionario de Python
        doc_dict = doc.model_dump()
        # Le agregamos la fecha exacta
        doc_dict["fecha_indexacion"] = datetime.utcnow()
        
        # 🚀 LA MAGIA ESTÁ AQUÍ: Usamos replace_one con upsert=True
        resultado = await coleccion_docs.replace_one(
            {"documento_id": doc.documento_id}, # 1. Buscamos si ya existe el ID de SQL Server
            doc_dict,                           # 2. Le pasamos todos los datos nuevos (la v2.0)
            upsert=True                         # 3. Si existe lo sobreescribe, si no existe lo inserta
        )
        
        return {
            "success": True,
            "message": "Documento indexado/actualizado con éxito en MongoDB"
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error al guardar en Mongo: {str(e)}")
    

# 🚀 5. NUEVA RUTA GET: PHP consumirá esta ruta para mostrar el dashboard
@app.get("/api/docs/approved")
async def obtener_documentos_aprobados():
    try:
        # Buscamos todos los documentos en Mongo
        # Ocultamos el campo _id nativo de Mongo para que el JSON salga limpio
        cursor = coleccion_docs.find({}, {"_id": 0})
        
        # Convertimos el resultado a una lista (máximo 100 para no saturar)
        documentos = await cursor.to_list(length=100)
        
        return {
            "success": True,
            "total_documentos": len(documentos),
            "data": documentos
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error al leer de Mongo: {str(e)}")