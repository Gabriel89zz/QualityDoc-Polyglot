<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use Illuminate\Support\Facades\Http;
use App\Models\AccessLog;
use Exception;
use Carbon\Carbon;

class DashboardController extends Controller
{
    // =========================================================
    // 🚀 1. VISTA DASHBOARD (Resumen Visual y KPIs Optimizados)
    // =========================================================
    public function dashboard(Request $request)
    {
        if (!session('is_authenticated')) {
            return redirect('/admin/Auth/Login');
        }

        $myCompany = session('company_id');
        $myUserId = session('user_id');
        $rolActual = session('role');

       // Variables para Operarios
        $kpiDocsLeidos = 0;
        $kpiPorFirmar = 0;
        $kpiCumplimiento = 100;

        // Variables para Auditores
        $kpiPersonalAuditado = 0;
        $kpiReportes = 0;
        $kpiErrores = 0;
        $chartLabels = [];
        $chartData = [];
        $actividadReciente = collect();
        $chartRange = '7days'; // 🚀 FIX: Declaramos la variable aquí para que exista siempre

        // 📊 CÁLCULO DE KPIS SEGÚN EL ROL
        if (in_array($rolActual, ['Administrador', 'Auditor'])) {
            
            // 1. KPI: Personal Auditado (Usuarios únicos)
            $kpiPersonalAuditado = AccessLog::where('company_id', $myCompany)
                                        ->distinct('user_id')
                                        ->count('user_id');

            // 2. KPI: Logs de Actividad (Movimientos totales en la plataforma)
            $kpiReportes = AccessLog::where('company_id', $myCompany)->count();

            // 3. KPI: Errores Reportados (No Conformidades)
            $kpiErrores = AccessLog::where('company_id', $myCompany)
                                   ->where('document_title', 'LIKE', '%[REPORTE DE INCIDENCIA]%')
                                   ->count();

            // 📈 DATOS PARA LA GRÁFICA DINÁMICA
            $chartRange = $request->input('chart_range', '7days');
            $daysToSubtract = 6; // Por defecto 7 días (0 a 6)
            
            if ($chartRange === '15days') $daysToSubtract = 14;
            if ($chartRange === '30days') $daysToSubtract = 29;

            for ($i = $daysToSubtract; $i >= 0; $i--) {
                $fecha = Carbon::now('America/Monterrey')->subDays($i);
                $chartLabels[] = $fecha->translatedFormat('d M'); // Ej: "01 Jun"
                
                // Contar movimientos por día
                $chartData[] = AccessLog::where('company_id', $myCompany)
                                        ->whereDate('created_at', $fecha->format('Y-m-d'))
                                        ->count();
            }

            // 📋 ÚLTIMOS 5 REGISTROS PARA LA LISTA DE ACTIVIDAD (La mitad derecha)
            $actividadReciente = AccessLog::where('company_id', $myCompany)
                                        ->orderBy('created_at', 'desc')
                                        ->limit(3)
                                        ->get();

        } else {
            // ==========================================
            // 🚀 LÓGICA DE OPERARIO OPTIMIZADA (V2)
            // ==========================================
            $kpiDocsLeidos = 0;
            $kpiPorFirmar = 0;
            $kpiCumplimiento = 100;
            
            try {
                $pythonApiUrl = env('PYTHON_API_URL', 'http://python-app:8000');
                $response = \Illuminate\Support\Facades\Http::timeout(5)->get($pythonApiUrl . '/api/docs/approved', [
                    'empresa_id' => $myCompany,
                    'departamento_id' => session('dept_id')
                ]);

                if ($response->successful()) {
                    $documentosVigentes = $response->json()['data'] ?? [];
                    $totalDocs = count($documentosVigentes);

                    if ($totalDocs > 0) {
                        // 1. Traemos TODAS las firmas históricas del usuario de un solo golpe (Ahorra memoria y consultas)
                        $firmasUsuario = AccessLog::where('company_id', $myCompany)
                                            ->where('user_id', $myUserId)
                                            ->where('document_title', 'LIKE', '%[FIRMA DE ENTERADO]%')
                                            ->get(['document_code', 'version_num']);

                        // 2. Evaluamos contra la FUENTE DE VERDAD (Los documentos actuales)
                        foreach ($documentosVigentes as $doc) {
                            $codigoDoc = $doc['codigo'] ?? '';
                            $versionActual = $doc['version'] ?? '';

                            // 3. Verificamos si existe la firma para esta versión exacta
                            $yaFirmo = $firmasUsuario->contains(function ($firma) use ($codigoDoc, $versionActual) {
                                return $firma->document_code === $codigoDoc && $firma->version_num === $versionActual;
                            });

                            if ($yaFirmo) {
                                $kpiDocsLeidos++;
                            } else {
                                $kpiPorFirmar++;
                            }
                        }

                        // 4. Calculamos el porcentaje real (tope a 100 para evitar errores matemáticos raros)
                        $kpiCumplimiento = min(100, round(($kpiDocsLeidos / $totalDocs) * 100));
                    }
                }
            } catch (Exception $e) {}
        }

        // 📝 LOG DE INGRESO INICIAL
        if (!session()->has('ya_ingreso')) {
            try {
                $log = new AccessLog([
                    'document_code'  => 'DASHBOARD_VIEW', 
                    'document_title' => 'Ingreso al Dashboard Principal',
                    'version_num'    => 'N/A',
                    'user_id'        => $myUserId ?? 0,
                    'user_name'      => session('name') ?? 'Usuario Desconocido',
                    'user_role'      => $rolActual ?? 'Sin Rol',
                    'ip_address'     => $request->ip(),
                    'company_id'     => $myCompany ?? 0
                ]);
                $log->created_at = Carbon::now('America/Monterrey');
                $log->updated_at = Carbon::now('America/Monterrey');
                $log->save();

                session(['ya_ingreso' => true]);
            } catch (Exception $e) { }
        }

       return view('dashboard', compact(
            'kpiDocsLeidos', 'kpiPorFirmar', 'kpiCumplimiento',
            'kpiPersonalAuditado', 'kpiReportes', 'kpiErrores',
            'chartLabels', 'chartData', 'chartRange', 'actividadReciente'
        ));
    }

    // =========================================================
    // 🚀 2. NUEVA VISTA: DIRECTORIO VIGENTE (Tabla Completa)
    // =========================================================
    public function directorio(Request $request)
    {
        if (!session('is_authenticated')) {
            return redirect('/admin/Auth/Login');
        }

        $searchTerm = $request->query('search');
        $documentos = [];
        $errorApi = null;

        try {
            $pythonApiUrl = env('PYTHON_API_URL', 'http://python-app:8000');

            // EL ARREGLO DINÁMICO: Preparamos los parámetros base
            $queryParams = [
                'empresa_id' => session('company_id'),
                'q'          => $searchTerm 
            ];

            // LA REGLA DE NEGOCIO: Si NO es Administrador ni Auditor, filtramos por su departamento
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

        // 🚀 NUEVO: Extraer todas las etiquetas únicas para los chips de filtro
        $allTags = [];
        foreach ($documentos as $doc) {
            if (isset($doc['etiquetas']) && is_array($doc['etiquetas'])) {
                foreach ($doc['etiquetas'] as $etiqueta) {
                    if (!empty(trim($etiqueta))) {
                        $allTags[] = trim($etiqueta);
                    }
                }
            }
        }
        $allTags = array_unique($allTags);
        sort($allTags); // Ordenamos alfabéticamente

        // Retornamos la nueva vista 'directorio'
        return view('directorio', [
            'documentos' => $documentos,
            'allTags'    => $allTags, // 🚀 Pasamos las etiquetas a la vista
            'errorApi'   => $errorApi,
            'searchTerm' => $searchTerm,
            'userName'   => session('name'),
            'userRole'   => session('role')
        ]);
    }

    // =========================================================
    // 🚀 3. LOG DE LECTURA DE PDF
    // =========================================================
    public function logDocumentAccess(Request $request)
    {
        if (!session('is_authenticated')) {
            return redirect('/admin/Auth/Login');
        }

        // Capturamos parámetros con valores por defecto para evitar errores de Postgres
        $codigo = $request->query('codigo', 'SIN-CODIGO');
        $titulo = $request->query('titulo', 'Sin Título');
        $version = $request->query('version', '1.0');
        $urlArchivo = $request->query('url', '');

        try {
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
            \Log::error("Error al guardar log de PDF: " . $e->getMessage());
        }

        // Redirigimos a C# para mostrar el archivo
        $urlLimpia = str_replace('\\', '/', $urlArchivo);
        return redirect('/admin' . $urlLimpia);
    }

    // =========================================================
    // 🚀 4. FIRMA DE CUMPLIMIENTO / ENTERADO
    // =========================================================
    public function acuseLectura(Request $request)
    {
        if (!session('is_authenticated')) {
            return redirect('/admin/Auth/Login');
        }

        $codigo = $request->input('codigo');
        $titulo = $request->input('titulo');
        $version = $request->input('version');

        try {
            $log = new AccessLog();
            $log->document_code  = $codigo;
            $log->document_title = '[FIRMA DE ENTERADO] ' . $titulo;
            $log->version_num    = $version;
            $log->user_id        = session('user_id') ?? 0;
            $log->user_name      = session('name') ?? 'Usuario Desconocido';
            $log->user_role      = session('role') ?? 'Sin Rol';
            $log->ip_address     = $request->ip();
            $log->company_id     = session('company_id') ?? 0;
            
            $log->created_at = Carbon::now('America/Monterrey');
            $log->updated_at = Carbon::now('America/Monterrey');
            
            $log->save();

            return back()->with('success', "Has firmado de enterado la versión $version del documento $codigo exitosamente.");
        } catch (Exception $e) {
            \Log::error("Error al registrar acuse: " . $e->getMessage());
            return back()->with('errorApi', 'Hubo un problema al registrar tu firma de cumplimiento.');
        }
    }

    // =========================================================
    // 🚀 5. MIS CUMPLIMIENTOS (Historial de firmas del usuario)
    // =========================================================
    public function misCumplimientos(Request $request)
    {
        if (!session('is_authenticated')) {
            return redirect('/admin/Auth/Login');
        }

        $myCompany = session('company_id');
        $myUserId = session('user_id');

        // 1. Capturar los filtros (Agregamos 'rango' por defecto 'all')
        $rango = $request->input('rango', 'all');
        $fechaInicio = $request->input('fecha_inicio');
        $fechaFin = $request->input('fecha_fin');

        $query = AccessLog::where('company_id', $myCompany)
            ->where('user_id', $myUserId)
            ->where('document_title', 'LIKE', '%[FIRMA DE ENTERADO]%');

        // 2. LÓGICA DE RANGOS MINIMALISTA
        if ($rango === '7days') {
            $fechaInicio = Carbon::now('America/Monterrey')->subDays(7)->format('Y-m-d');
            $fechaFin = Carbon::now('America/Monterrey')->format('Y-m-d');
            $query->whereDate('created_at', '>=', $fechaInicio)->whereDate('created_at', '<=', $fechaFin);
        } elseif ($rango === '30days') {
            $fechaInicio = Carbon::now('America/Monterrey')->subDays(30)->format('Y-m-d');
            $fechaFin = Carbon::now('America/Monterrey')->format('Y-m-d');
            $query->whereDate('created_at', '>=', $fechaInicio)->whereDate('created_at', '<=', $fechaFin);
        } else {
            // Rango "Personalizado" o "Todos"
            if ($fechaInicio) $query->whereDate('created_at', '>=', $fechaInicio);
            if ($fechaFin) $query->whereDate('created_at', '<=', $fechaFin);
        }

        $cumplimientos = $query->orderBy('created_at', 'desc')->paginate(8);
        
        // Mantiene los filtros en la paginación
        $cumplimientos->appends($request->all());

        return view('compliances', [
            'cumplimientos' => $cumplimientos,
            'userName'      => session('name'),
            'userRole'      => session('role'),
            'rango'         => $rango,
            'fechaInicio'   => $fechaInicio,
            'fechaFin'      => $fechaFin
        ]);
    }

    // =========================================================
    // 🚀 6. REPORTAR INCIDENCIA (Puente hacia C#)
    // =========================================================
    public function reportarError(Request $request)
    {
        if (!session('is_authenticated')) {
            return response()->json(['success' => false, 'message' => 'No autorizado'], 401);
        }

        try {
            // Usa el nombre de tu contenedor de C# que configuraste
            $csharpApiUrl = env('CSHARP_API_URL', 'http://dotnep-app:80');

            // 🚀 FIX 2: Forzamos el casteo a (int) para que el JSON sea perfecto para C#
            $response = Http::timeout(5)->post($csharpApiUrl . '/api/issues', [
                'CompanyId' => (int) session('company_id'),
                'UserId'    => (int) session('user_id'),
                'DocCode'   => $request->input('codigo'),
                'IssueType' => $request->input('tipo'),
                'Details'   => $request->input('detalles')
            ]);

            if ($response->successful()) {
                // 🚀 NUEVO: Dejamos la huella en el historial de Laravel
                AccessLog::create([
                    'company_id' => session('company_id'),
                    'user_id'    => session('user_id'),
                    'user_name'  => session('name'),
                    'user_role'  => session('role'),
                    'ip_address' => $request->ip(),
                    'document_code'  => $request->input('codigo'),
                    'document_title' => '[REPORTE DE INCIDENCIA] ' . $request->input('tipo'),
                    'version_num' => 'N/A'
                ]);

                return response()->json(['success' => true, 'message' => 'Reporte enviado con éxito']);
            } else {
                // 🚀 FIX 3: Ya no estamos ciegos. Extraemos el mensaje de rechazo de C#
                $errorBody = $response->body();
                return response()->json(['success' => false, 'message' => 'C# Rechazó la petición: ' . $errorBody]);
            }
        } catch (Exception $e) {
            return response()->json(['success' => false, 'message' => 'Error crítico de red: ' . $e->getMessage()]);
        }
    }

    // =========================================================
    // 🚀 7. OBTENER HISTORIAL DE VERSIONES (Puente a C#)
    // =========================================================
    public function historialDocumento($codigo)
    {
        try {
            // Reemplaza 'dotnet-app' por el nombre de tu contenedor de C#
            $csharpApiUrl = env('CSHARP_API_URL', 'http://dotnet-app:8080'); 
            
            $response = Http::timeout(5)->get("{$csharpApiUrl}/api/documents/{$codigo}/history");

            if ($response->successful()) {
                return response()->json([
                    'success' => true, 
                    'data' => $response->json()
                ]);
            }
            
            return response()->json(['success' => false, 'message' => 'C# rechazó la petición.']);
        } catch (Exception $e) {
            return response()->json(['success' => false, 'message' => 'Error de conexión con C#']);
        }
    }
}