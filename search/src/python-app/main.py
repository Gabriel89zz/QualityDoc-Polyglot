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

# 🚀 NUEVO: Evento de arranque para crear el Índice de Texto Completo
@app.on_event("startup")
async def startup_db_client():
    # Esto le dice a Mongo que prepare estos campos para búsquedas súper rápidas
    await coleccion_docs.create_index([
        ("contenido_texto", pymongo.TEXT),
        ("titulo", pymongo.TEXT),
        ("codigo", pymongo.TEXT)
    ])
    print("Índice de Full-Text Search creado exitosamente.")

# ==========================================
# 2. DEFINICIÓN DEL MOLDE (Pydantic)
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
    archivo_fisico: str = "" # 🚀 NUEVO: Recibe el nombre del archivo desde C#

# ==========================================
# 🚀 3. FUNCIONES HELPER
# ==========================================
def extraer_texto(nombre_archivo: str) -> str:
    """Lee el archivo físico compartido por Docker y extrae el texto puro."""
    if not nombre_archivo:
        return ""
        
    # /shared_uploads es el volumen que configuramos en docker-compose
    ruta_completa = os.path.join("/shared_uploads", nombre_archivo)
    
    if not os.path.exists(ruta_completa):
        print(f"Advertencia: No se encontró el archivo físico en {ruta_completa}")
        return ""

    texto = ""
    try:
        if nombre_archivo.lower().endswith('.pdf'):
            doc = fitz.open(ruta_completa)
            for pagina in doc:
                texto += pagina.get_text() + " "
        elif nombre_archivo.lower().endswith('.docx'):
            doc_word = docx.Document(ruta_completa)
            texto = " ".join([parrafo.text for parrafo in doc_word.paragraphs])
    except Exception as e:
        print(f"Error extrayendo texto de {nombre_archivo}: {str(e)}")
        
    # Limpiamos saltos de línea excesivos para no ensuciar Mongo
    return re.sub(r'\s+', ' ', texto).strip()

def generar_snippet(texto_completo: str, query: str) -> str:
    """Busca la palabra en el texto gigante y devuelve solo un pedacito formateado para UI."""
    if not texto_completo or not query:
        return "Coincidencia en metadatos."
        
    # Buscamos la palabra ignorando mayúsculas/minúsculas
    match = re.search(re.escape(query), texto_completo, re.IGNORECASE)
    if match:
        inicio = max(0, match.start() - 60) # 60 letras antes
        fin = min(len(texto_completo), match.end() + 60) # 60 letras después
        
        fragmento = texto_completo[inicio:fin]
        # Envolvemos la coincidencia exacta en HTML (Tailwind) para PHP
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
        
        # 🚀 1. Ejecutamos la extracción de texto pesada
        texto_extraido = extraer_texto(doc.archivo_fisico)
        doc_dict["contenido_texto"] = texto_extraido
        
        # 2. Guardamos en MongoDB
        resultado = await coleccion_docs.replace_one(
            {"documento_id": doc.documento_id}, 
            doc_dict,                           
            upsert=True                         
        )
        
        return {
            "success": True,
            "message": "Documento y texto interno indexados con éxito en MongoDB"
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
            # 🚀 Usamos el índice de Full-Text Search de Mongo (Es rapidísimo)
            filtro["$text"] = {"$search": q}

        # Traemos los documentos (EXCLUYENDO el contenido gigante para no trabar la red hacia PHP)
        # Traemos los documentos (EXCLUYENDO el contenido gigante para no trabar la red hacia PHP)
        cursor = coleccion_docs.find(filtro, {"_id": 0, "contenido_texto": 1, "titulo": 1, "codigo": 1, "version": 1, "etiquetas": 1, "aprobado_por": 1, "url_archivo": 1})
        documentos = await cursor.to_list(length=2000)
        
        # 🚀 Procesamos los resultados para agregarles el "Snippet" elegante a cada uno
        resultados_finales = []
        for d in documentos:
            texto_interno = d.pop("contenido_texto", "") # Lo sacamos del dict para no enviarlo completo
            
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
    

# 🚀 7. NUEVA RUTA GET: CONVERSIÓN DE OFFICE A PDF ON-THE-FLY
@app.get("/api/docs/preview")
async def previsualizar_documento(url_archivo: str):
    try:
        # 1. Limpiamos la ruta igual que en C# para que empate con el volumen de Docker
        ruta_limpia = url_archivo.replace("/uploads/", "").replace("\\uploads\\", "")
        if ruta_limpia.startswith("/"):
            ruta_limpia = ruta_limpia[1:]
            
        ruta_completa = os.path.join("/shared_uploads", ruta_limpia)
        
        if not os.path.exists(ruta_completa):
            raise HTTPException(status_code=404, detail="Archivo físico no encontrado en el servidor")

        ext = ruta_completa.split('.')[-1].lower()
        office_exts = ['doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx']

        if ext in office_exts:
            # 2. Magia Negra: Usamos LibreOffice de Linux para convertir a PDF y guardarlo en la carpeta temporal /tmp
            comando = [
                "libreoffice", "--headless", "--convert-to", "pdf",
                ruta_completa, "--outdir", "/tmp"
            ]
            subprocess.run(comando, check=True)
            
            # 3. Calculamos el nombre del nuevo PDF generado
            nombre_base = os.path.basename(ruta_completa)
            nombre_pdf = os.path.splitext(nombre_base)[0] + ".pdf"
            ruta_pdf = os.path.join("/tmp", nombre_pdf)
            
            # 4. Devolvemos el PDF directo al navegador para que lo pinte el iframe
            return FileResponse(ruta_pdf, media_type="application/pdf")
        
        # Si mandan otra cosa por error, devolvemos el archivo original
        return FileResponse(ruta_completa)
        
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error al convertir documento: {str(e)}")
    

# 🚀 8. NUEVA RUTA POST: CONVERSIÓN DE OFFICE AL VUELO DESDE EL CLIENTE (UPLOAD)
@app.post("/api/docs/preview-upload")
async def previsualizar_upload(file: UploadFile = File(...)):
    try:
        ext = file.filename.split('.')[-1].lower()
        office_exts = ['doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx']
        
        # 1. Generamos un nombre temporal único para no chocar con otros archivos
        temp_id = str(uuid.uuid4())
        ruta_temp = f"/tmp/{temp_id}_{file.filename}"
        
        # 2. Guardamos el archivo físico temporalmente en el contenedor de Linux
        with open(ruta_temp, "wb") as buffer:
            shutil.copyfileobj(file.file, buffer)
            
        # 3. Si es de Office, lo convertimos a PDF
        if ext in office_exts:
            comando = ["libreoffice", "--headless", "--convert-to", "pdf", ruta_temp, "--outdir", "/tmp"]
            import subprocess
            subprocess.run(comando, check=True)
            
            # Armamos la ruta del nuevo PDF
            import os
            nombre_base = os.path.basename(ruta_temp)
            nombre_pdf = os.path.splitext(nombre_base)[0] + ".pdf"
            ruta_pdf = os.path.join("/tmp", nombre_pdf)
            
            # Devolvemos el PDF al navegador
            from fastapi.responses import FileResponse
            return FileResponse(ruta_pdf, media_type="application/pdf")
            
        # Si no es de office, devolvemos el archivo tal cual
        from fastapi.responses import FileResponse
        return FileResponse(ruta_temp)
        
    except Exception as e:
        from fastapi import HTTPException
        raise HTTPException(status_code=500, detail=f"Error al convertir upload temporal: {str(e)}")