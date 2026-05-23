<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use Illuminate\Support\Facades\Http;
use App\Models\AccessLog;
use Exception;
use Carbon\Carbon;

class DashboardController extends Controller
{
    public function index(Request $request)
    {
        if (!session('is_authenticated')) {
            return redirect('http://127.0.0.1:5269/Auth/Login');
        }

        $searchTerm = $request->query('search');
        $documentos = [];
        $errorApi = null;

        try {
            $pythonApiUrl = env('PYTHON_API_URL', 'http://python-app:8000');
            
            // 🚀 EL ARREGLO DINÁMICO: Preparamos los parámetros base
            $queryParams = [
                'empresa_id' => session('company_id'),
                'q'          => $searchTerm 
            ];

            // 🚀 LA REGLA DE NEGOCIO: Si NO es Administrador ni Auditor, filtramos por su departamento
            $rolActual = session('role');
            if (!in_array($rolActual, ['Administrador', 'Auditor'])) {
                $queryParams['departamento_id'] = session('dept_id');
            }

            // Hacemos la petición a Python con los parámetros construidos
            $response = Http::timeout(5)->get($pythonApiUrl . '/api/docs/approved', $queryParams);

            if ($response->successful()) {
                $jsonData = $response->json();
                $documentos = $jsonData['data'] ?? [];
            }
        } catch (Exception $e) {
            $errorApi = "Error al conectar con MongoDB.";
        }

        // 🚀 LOG DE INGRESO (Solo una vez por sesión)
        if (!session()->has('ya_ingreso')) {
            try {
                $log = new AccessLog([
                    'document_code'  => 'DASHBOARD_VIEW', 
                    'document_title' => 'Consulta de Directorio General',
                    'version_num'    => 'N/A',
                    'user_id'        => session('user_id') ?? 0,
                    'user_name'      => session('name') ?? 'Usuario Desconocido',
                    'user_role'      => session('role') ?? 'Sin Rol',
                    'ip_address'     => $request->ip(),
                    'company_id'     => session('company_id') ?? 0
                ]);

                $log->created_at = Carbon::now('America/Monterrey');
                $log->updated_at = Carbon::now('America/Monterrey');
                $log->save();

                session(['ya_ingreso' => true]);
            } catch (Exception $e) {
                // Si falla aquí, al menos el dashboard carga
            }
        }

        return view('dashboard', [
            'documentos' => $documentos,
            'errorApi'   => $errorApi,
            'userName'   => session('name'),
            'userRole'   => session('role'),
            'searchTerm' => $searchTerm 
        ]);
    }

    // 🚀 MÉTODO CORREGIDO PARA ABRIR PDF Y REGISTRAR LOG
    public function logDocumentAccess(Request $request)
    {
        if (!session('is_authenticated')) {
            return redirect('http://127.0.0.1:5269/Auth/Login');
        }

        // Capturamos parámetros con valores por defecto para evitar errores de Postgres
        $codigo = $request->query('codigo', 'SIN-CODIGO');
        $titulo = $request->query('titulo', 'Sin Título');
        $version = $request->query('version', '1.0');
        $urlArchivo = $request->query('url', '');

        try {
            // Creamos el log manualmente para asegurar la hora y los datos
            $log = new AccessLog();
            $log->document_code  = $codigo;
            $log->document_title = $titulo;
            $log->version_num    = $version;
            $log->user_id        = session('user_id') ?? 0;
            $log->user_name      = session('name') ?? 'Usuario Desconocido';
            $log->user_role      = session('role') ?? 'Sin Rol';
            $log->ip_address     = $request->ip();
            $log->company_id     = session('company_id') ?? 0;
            
            // Forzamos la zona horaria de Coahuila
            $log->created_at = Carbon::now('America/Monterrey');
            $log->updated_at = Carbon::now('America/Monterrey');
            
            $log->save();
        } catch (Exception $e) {
            // Si el log falla, imprimimos el error en el log de Laravel para que lo veas
            \Log::error("Error al guardar log de PDF: " . $e->getMessage());
        }

        // Redirigimos a C# para mostrar el archivo
        $urlLimpia = str_replace('\\', '/', $urlArchivo);
        return redirect('http://127.0.0.1:5269' . $urlLimpia);
    }

    // 🚀 NUEVO MÉTODO: Firma de Enterado / Cumplimiento
    public function acuseLectura(Request $request)
    {
        if (!session('is_authenticated')) {
            return redirect('http://127.0.0.1:5269/Auth/Login');
        }

        $codigo = $request->input('codigo');
        $titulo = $request->input('titulo');
        $version = $request->input('version');

        try {
            $log = new AccessLog();
            $log->document_code  = $codigo;
            // 🚀 EL TRUCO: Le ponemos una etiqueta clara para que el Auditor lo vea distinto en el reporte
            $log->document_title = '[FIRMA DE ENTERADO] ' . $titulo;
            $log->version_num    = $version;
            $log->user_id        = session('user_id') ?? 0;
            $log->user_name      = session('name') ?? 'Usuario Desconocido';
            $log->user_role      = session('role') ?? 'Sin Rol';
            $log->ip_address     = $request->ip();
            $log->company_id     = session('company_id') ?? 0;
            
            // Forzamos la zona horaria
            $log->created_at = Carbon::now('America/Monterrey');
            $log->updated_at = Carbon::now('America/Monterrey');
            
            $log->save();

            // Regresamos a la vista con un mensaje de éxito
            return back()->with('success', "Has firmado de enterado la versión $version del documento $codigo exitosamente.");
        } catch (Exception $e) {
            \Log::error("Error al registrar acuse: " . $e->getMessage());
            return back()->with('errorApi', 'Hubo un problema al registrar tu firma de cumplimiento.');
        }
    }


    public function misCumplimientos(Request $request)
    {
        // 1. Validamos que esté logueado
        if (!session('is_authenticated')) {
            return redirect('http://127.0.0.1:5269/Auth/Login');
        }

        $myCompany = session('company_id');
        $myUserId = session('user_id');

        // 2. Buscamos solo los acuses de recibo ([FIRMA DE ENTERADO]) de ESTE usuario en SU empresa
        $cumplimientos = AccessLog::where('company_id', $myCompany)
            ->where('user_id', $myUserId)
            ->where('document_title', 'LIKE', '%[FIRMA DE ENTERADO]%')
            ->orderBy('created_at', 'desc')
            ->paginate(10); // Paginamos de 10 en 10 para mantener la vista limpia

        // 3. Retornamos la nueva vista que crearemos en el paso 3
        return view('compliances', [
            'cumplimientos' => $cumplimientos,
            'userName'      => session('name'),
            'userRole'      => session('role')
        ]);
    }
}