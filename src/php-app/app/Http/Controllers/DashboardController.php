<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use Illuminate\Support\Facades\Http;
use App\Models\AccessLog;
use Exception;

class DashboardController extends Controller
{
    public function index(Request $request)
    {
        // 1. Validamos que nadie se cuele sin sesión
        if (!session('is_authenticated')) {
            return redirect('http://127.0.0.1:5269/Auth/Login');
        }

        $documentos = [];
        $errorApi = null;

        try {
            // 2. Leemos la URL del .env (Apunta a la red interna de Docker)
            $pythonApiUrl = env('PYTHON_API_URL', 'http://python-app:8000');
            
            // 3. Disparamos la petición GET hacia Node/Python
            $response = Http::timeout(5)->get($pythonApiUrl . '/api/docs/approved');

            if ($response->successful()) {
                $jsonData = $response->json();
                $documentos = $jsonData['data'] ?? [];
            } else {
                $errorApi = "El motor de búsqueda devolvió un error interno.";
            }
        } catch (Exception $e) {
            $errorApi = "No se pudo conectar con el microservicio de indexación de MongoDB.";
        }

        // 🚀 NUEVO: Registro de Auditoría en PostgreSQL
        // Registramos que el usuario acaba de entrar a consultar el directorio
        try {
            AccessLog::create([
                'document_code'  => 'DASHBOARD_VIEW', 
                'document_title' => 'Consulta de Directorio General',
                'version_num'    => 'N/A',
                'user_id'        => session('user_id') ?? 0, // Sacamos el ID de la sesión que guardó el JWT
                'user_name'      => session('name') ?? 'Usuario Desconocido',
                'user_role'      => session('role') ?? 'Sin Rol',
                'ip_address'     => $request->ip(),
            ]);
        } catch (Exception $e) {
            // Si la base de datos de Postgres está apagada o falla, 
            // el catch evita que la página se caiga, simplemente no guarda el log.
        }

        // 4. Mandamos los datos y la información del usuario a la Vista
        return view('dashboard', [
            'documentos' => $documentos,
            'errorApi'   => $errorApi,
            'userName'   => session('name'),
            'userRole'   => session('role')
        ]);
    }

    // 🚀 NUEVO: Método para registrar cuando abren un PDF específico
    public function logDocumentAccess(Request $request)
    {
        if (!session('is_authenticated')) {
            return redirect('http://127.0.0.1:5269/Auth/Login');
        }

        // Recibimos los datos del documento por la URL
        $codigo = $request->query('codigo');
        $titulo = $request->query('titulo');
        $version = $request->query('version');
        $urlArchivo = $request->query('url');

        try {
            // Guardamos el log exacto en PostgreSQL
            AccessLog::create([
                'document_code'  => $codigo, 
                'document_title' => $titulo,
                'version_num'    => $version,
                'user_id'        => session('user_id') ?? 0,
                'user_name'      => session('name') ?? 'Usuario Desconocido',
                'user_role'      => session('role') ?? 'Sin Rol',
                'ip_address'     => $request->ip(),
            ]);
        } catch (Exception $e) {
            // Si falla el log, no evitamos que vea el documento
        }

        // Redirigimos al servidor de C# para que el navegador descargue/muestre el PDF
        return redirect('http://127.0.0.1:5269' . $urlArchivo);
    }
}