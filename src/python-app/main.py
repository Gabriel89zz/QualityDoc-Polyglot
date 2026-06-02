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

# 2. Definimos el "Molde" 
class DocumentoAprobado(BaseModel):
    documento_id: int 
    codigo: str
    titulo: str
    version: str
    etiquetas: list[str] 
    url_archivo: str
    aprobado_por: str
    # 🚀 AGREGAMOS ESTOS DOS CAMPOS
    empresa_id: int
    departamento_id: int

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
    

# 🚀 5. NUEVA RUTA GET CON FILTROS
@app.get("/api/docs/approved")
async def obtener_documentos_aprobados(empresa_id: int = None, departamento_id: int = None, q: str = None):
    try:
        filtro = {}
        
        # 1. Filtro estricto de seguridad (Multitenant)
        if empresa_id and empresa_id != 0:
            filtro["empresa_id"] = empresa_id
        if departamento_id and departamento_id != 0:
            filtro["departamento_id"] = departamento_id

        # 🚀 2. LA MAGIA DEL BUSCADOR: Si el usuario escribió algo
        if q:
            # $or significa "Que coincida con CUALQUIERA de estas 3 opciones"
            # $options: "i" significa "Ignorar mayúsculas y minúsculas" (Insensitive)
            filtro["$or"] = [
                {"titulo": {"$regex": q, "$options": "i"}},
                {"codigo": {"$regex": q, "$options": "i"}},
                {"etiquetas": {"$regex": q, "$options": "i"}}
            ]

        # Buscamos en Mongo aplicando ambos filtros
        cursor = coleccion_docs.find(filtro, {"_id": 0})
        documentos = await cursor.to_list(length=2000)
        
        return {
            "success": True,
            "total_documentos": len(documentos),
            "data": documentos
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error al leer de Mongo: {str(e)}")
    
    
# 🚀 6. NUEVA RUTA DELETE: Para "des-publicar" documentos revocados
@app.delete("/api/docs/index/{doc_id}")
async def eliminar_documento(doc_id: int):
    try:
        # Buscamos y eliminamos el documento usando el ID de SQL Server
        resultado = await coleccion_docs.delete_one({"documento_id": doc_id})
        
        # Verificamos si realmente se borró algo
        if resultado.deleted_count == 1:
            return {
                "success": True,
                "message": f"Documento {doc_id} eliminado exitosamente de MongoDB. Ya no es público."
            }
        else:
            # Si Python no lo encuentra, no hay problema, significa que ya no estaba publicado
            return {
                "success": True,
                "message": f"El documento {doc_id} no estaba indexado en MongoDB."
            }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error al eliminar en Mongo: {str(e)}")