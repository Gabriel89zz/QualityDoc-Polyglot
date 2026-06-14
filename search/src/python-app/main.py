import os
import re
import fitz  # PyMuPDF para PDFs
import docx  # python-docx para Word
import pymongo
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from motor.motor_asyncio import AsyncIOMotorClient
from datetime import datetime
import subprocess
from fastapi.responses import FileResponse
from fastapi import UploadFile, File
import shutil
import uuid

app = FastAPI(
    title="QualityDoc Polyglot - Motor de Búsqueda",
    root_path="/api/search" 
)

# ==========================================
# 1. CREDENCIALES Y CONEXIÓN
# ==========================================
MONGO_USER = os.getenv("MONGO_INITDB_ROOT_USERNAME", "admin_mongo")
MONGO_PASS = os.getenv("MONGO_INITDB_ROOT_PASSWORD", "TuPasswordSeguroMongo123")
MONGO_URL = f"mongodb://{MONGO_USER}:{MONGO_PASS}@mongo-db:27017"

client = AsyncIOMotorClient(MONGO_URL)
db = client.qualitydoc_metadata
coleccion_docs = db.documentos_aprobados

@app.on_event("startup")
async def startup_db_client():
    await coleccion_docs.create_index([
        ("contenido_texto", pymongo.TEXT),
        ("titulo", pymongo.TEXT),
        ("codigo", pymongo.TEXT)
    ])
    print("Índice de Full-Text Search creado exitosamente.")

# ==========================================
# 2. DEFINICIÓN DEL MOLDE
# ==========================================
class DocumentoAprobado(BaseModel):
    documento_id: int 
    codigo: str
    titulo: str
    version: str
    etiquetas: list[str] 
    url_archivo: str
    aprobado_por: str
    empresa_id: int
    departamento_id: int
    archivo_fisico: str = "" 

# ==========================================
# 🚀 3. FUNCIONES HELPER
# ==========================================
def limpiar_ruta_archivo(ruta_cruda: str) -> str:
    """
    Toma cualquier cosa que envíe C# (URL, path absoluto, etc.) y extrae 
    exactamente lo que sigue después de 'uploads/' para buscar en Docker.
    """
    if not ruta_cruda: return ""
    
    # Buscamos la palabra clave "uploads" para cortar el string
    if "uploads/" in ruta_cruda:
        ruta_relativa = ruta_cruda.split("uploads/")[-1]
    elif "uploads\\" in ruta_cruda:
        ruta_relativa = ruta_cruda.split("uploads\\")[-1]
    else:
        # Plan B de contingencia: agarramos solo el nombre del archivo
        ruta_relativa = os.path.basename(ruta_cruda.replace("\\", "/"))
        
    # Quitamos slash inicial si quedó
    if ruta_relativa.startswith("/"):
        ruta_relativa = ruta_relativa[1:]
        
    # Armamos la ruta perfecta para el volumen de Docker
    return os.path.join("/shared_uploads", ruta_relativa)


def extraer_texto(nombre_archivo: str) -> str:
    """Lee el archivo físico compartido por Docker y extrae el texto puro."""
    if not nombre_archivo:
        return ""
        
    # 🚀 FIX: Limpiamos la ruta antes de buscar
    ruta_completa = limpiar_ruta_archivo(nombre_archivo)
    
    if not os.path.exists(ruta_completa):
        print(f"Advertencia: No se encontró el archivo físico en {ruta_completa}")
        return ""

    texto = ""
    try:
        if ruta_completa.lower().endswith('.pdf'):
            doc = fitz.open(ruta_completa)
            for pagina in doc:
                texto += pagina.get_text() + " "
        elif ruta_completa.lower().endswith(('.docx', '.doc')):
            doc_word = docx.Document(ruta_completa)
            texto = " ".join([parrafo.text for parrafo in doc_word.paragraphs])
    except Exception as e:
        print(f"Error extrayendo texto de {nombre_archivo}: {str(e)}")
        
    return re.sub(r'\s+', ' ', texto).strip()

def generar_snippet(texto_completo: str, query: str) -> str:
    if not texto_completo or not query:
        return "Coincidencia en metadatos."
        
    match = re.search(re.escape(query), texto_completo, re.IGNORECASE)
    if match:
        inicio = max(0, match.start() - 60)
        fin = min(len(texto_completo), match.end() + 60)
        
        fragmento = texto_completo[inicio:fin]
        resaltado = re.sub(
            re.escape(query), 
            f"<mark class='bg-amber-200 text-amber-900 font-bold px-1 rounded'>{match.group(0)}</mark>", 
            fragmento, 
            flags=re.IGNORECASE
        )
        return f"...{resaltado}..."
        
    return "Coincidencia en metadatos."

# ==========================================
# 4. RUTAS DE LA API
# ==========================================
@app.get("/")
async def root():
    return {"status": "online", "message": "Motor FastAPI con Extracción OCR online"}

@app.post("/api/docs/index")
async def indexar_documento(doc: DocumentoAprobado):
    try:
        doc_dict = doc.model_dump()
        doc_dict["fecha_indexacion"] = datetime.utcnow()
        
        # 🚀 La extracción pesada ahora usa rutas limpias
        texto_extraido = extraer_texto(doc.archivo_fisico)
        doc_dict["contenido_texto"] = texto_extraido
        
        resultado = await coleccion_docs.replace_one(
            {"documento_id": doc.documento_id}, 
            doc_dict,                           
            upsert=True                         
        )
        
        return {
            "success": True,
            "message": "Documento y texto interno indexados con éxito en MongoDB",
            "bytes_extraidos": len(texto_extraido) # Para debug visual
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error al guardar en Mongo: {str(e)}")

@app.get("/api/docs/approved")
async def obtener_documentos_aprobados(empresa_id: int = None, departamento_id: int = None, q: str = None):
    try:
        filtro = {}
        
        if empresa_id and empresa_id != 0:
            filtro["empresa_id"] = empresa_id
        if departamento_id and departamento_id != 0:
            filtro["departamento_id"] = departamento_id

        if q:
            filtro["$text"] = {"$search": q}

        cursor = coleccion_docs.find(filtro, {"_id": 0, "contenido_texto": 1, "titulo": 1, "codigo": 1, "version": 1, "etiquetas": 1, "aprobado_por": 1, "url_archivo": 1})
        documentos = await cursor.to_list(length=2000)
        
        resultados_finales = []
        for d in documentos:
            texto_interno = d.pop("contenido_texto", "") 
            
            if q:
                d["snippet"] = generar_snippet(texto_interno, q)
            else:
                d["snippet"] = ""
                
            resultados_finales.append(d)
        
        return {
            "success": True,
            "total_documentos": len(resultados_finales),
            "data": resultados_finales
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error al buscar en Mongo: {str(e)}")

@app.delete("/api/docs/index/{doc_id}")
async def eliminar_documento(doc_id: int):
    try:
        resultado = await coleccion_docs.delete_one({"documento_id": doc_id})
        if resultado.deleted_count == 1:
            return {"success": True, "message": f"Documento {doc_id} eliminado exitosamente."}
        else:
            return {"success": True, "message": f"El documento {doc_id} no estaba indexado."}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error al eliminar en Mongo: {str(e)}")

# 🚀 7. RUTA CORREGIDA: CONVERSIÓN DE OFFICE A PDF ON-THE-FLY
@app.get("/api/docs/preview")
async def previsualizar_documento(url_archivo: str):
    try:
        # 🚀 FIX: Usamos la función maestra para destruir el HTTP y encontrar el archivo
        ruta_completa = limpiar_ruta_archivo(url_archivo)
        
        if not os.path.exists(ruta_completa):
            raise HTTPException(status_code=404, detail=f"Archivo físico no encontrado en volumen: {ruta_completa}")

        ext = ruta_completa.split('.')[-1].lower()
        office_exts = ['doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx']

        if ext in office_exts:
            comando = [
                "libreoffice", "--headless", "--convert-to", "pdf",
                ruta_completa, "--outdir", "/tmp"
            ]
            subprocess.run(comando, check=True)
            
            nombre_base = os.path.basename(ruta_completa)
            nombre_pdf = os.path.splitext(nombre_base)[0] + ".pdf"
            ruta_pdf = os.path.join("/tmp", nombre_pdf)
            
            return FileResponse(ruta_pdf, media_type="application/pdf")
        
        return FileResponse(ruta_completa)
        
    except subprocess.CalledProcessError as sub_e:
         raise HTTPException(status_code=500, detail="Fallo interno de LibreOffice al convertir documento.")
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error al procesar documento: {str(e)}")

@app.post("/api/docs/preview-upload")
async def previsualizar_upload(file: UploadFile = File(...)):
    try:
        ext = file.filename.split('.')[-1].lower()
        office_exts = ['doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx']
        
        temp_id = str(uuid.uuid4())
        ruta_temp = f"/tmp/{temp_id}_{file.filename}"
        
        with open(ruta_temp, "wb") as buffer:
            shutil.copyfileobj(file.file, buffer)
            
        if ext in office_exts:
            comando = ["libreoffice", "--headless", "--convert-to", "pdf", ruta_temp, "--outdir", "/tmp"]
            subprocess.run(comando, check=True)
            
            nombre_base = os.path.basename(ruta_temp)
            nombre_pdf = os.path.splitext(nombre_base)[0] + ".pdf"
            ruta_pdf = os.path.join("/tmp", nombre_pdf)
            
            return FileResponse(ruta_pdf, media_type="application/pdf")
            
        return FileResponse(ruta_temp)
        
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error al convertir upload temporal: {str(e)}")