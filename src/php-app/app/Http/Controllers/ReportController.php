<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use App\Models\AccessLog;
use Illuminate\Support\Facades\DB;

class ReportController extends Controller
{
    public function index()
    {
        // 1. Validamos que esté logueado
        if (!session('is_authenticated')) {
            return redirect('http://127.0.0.1:5269/Auth/Login');
        }

        // 🚀 2. EL CADENERO: Validamos el rol del usuario
        $rolActual = session('role');
        $rolesPermitidos = ['Administrador', 'Auditor']; 

        if (!in_array($rolActual, $rolesPermitidos)) {
            // Si no tiene permiso, lo pateamos de vuelta al dashboard
            return redirect()->route('dashboard'); 
        }

        // 🚀 Atrapamos el ID de la empresa del usuario actual
        $myCompany = session('company_id');

        // 1. KPIs filtrados por empresa (Dejamos solo los 2 que pediste)
        $totalLecturas = AccessLog::where('company_id', $myCompany)
            ->where('document_code', '!=', 'DASHBOARD_VIEW')
            ->count();

        $usuariosUnicos = AccessLog::where('company_id', $myCompany)
            ->distinct('user_id')
            ->count('user_id');

        // 2. Top Documentos filtrados por empresa
        $topDocumentos = AccessLog::select('document_code', 'document_title', DB::raw('COUNT(*) as total_vistas'))
            ->where('company_id', $myCompany)
            ->where('document_code', '!=', 'DASHBOARD_VIEW')
            ->groupBy('document_code', 'document_title')
            ->orderByDesc('total_vistas')
            ->limit(5)
            ->get();

        // 3. Historial filtrado por empresa (🚀 Reducido a los 5 más recientes)
        $historial = AccessLog::where('company_id', $myCompany)
            ->orderBy('created_at', 'desc')
            ->limit(5) 
            ->get();

        $userName = session('name');
        $userRole = session('role');
        
        return view('reports', compact(
            'totalLecturas', 
            'usuariosUnicos', 
            'topDocumentos', 
            'historial', 
            'userName', 
            'userRole'
        ));
    }

    // 🚀 Vista de Historial Completo con Paginación
    public function historialDetallado()
    {
        if (!in_array(session('role'), ['Administrador', 'Auditor'])) {
            return redirect()->route('dashboard'); 
        }

        $myCompany = session('company_id');

        // Usamos paginate() en lugar de get() para que si hay 10,000 registros, no se congele la pantalla
        $historial = AccessLog::where('company_id', $myCompany)
            ->orderBy('created_at', 'desc')
            ->paginate(50);

        return view('history', compact('historial'));
    }

    // 🚀 Exportación Nativa a Excel (CSV con formato UTF-8)
    public function exportarExcel()
    {
        if (!in_array(session('role'), ['Administrador', 'Auditor'])) {
            return redirect()->route('dashboard'); 
        }

        $myCompany = session('company_id');
        $logs = AccessLog::where('company_id', $myCompany)->orderBy('created_at', 'desc')->get();

        $fileName = 'Auditoria_QualityDoc_' . date('Ymd_His') . '.csv';

        $headers = array(
            "Content-type"        => "text/csv; charset=UTF-8",
            "Content-Disposition" => "attachment; filename=$fileName",
            "Pragma"              => "no-cache",
            "Cache-Control"       => "must-revalidate, post-check=0, pre-check=0",
            "Expires"             => "0"
        );

        $columns = array('Fecha y Hora', 'Usuario', 'Rol', 'IP', 'Tipo de Accion', 'Codigo de Documento', 'Titulo', 'Version');

        $callback = function() use($logs, $columns) {
            $file = fopen('php://output', 'w');
            // 🚀 Este BOM es crucial para que Excel en Windows lea los acentos de México sin romper el texto
            fputs($file, $bom =( chr(0xEF) . chr(0xBB) . chr(0xBF) ));
            fputcsv($file, $columns);

            foreach ($logs as $log) {
                // Clasificamos la acción para que el Excel sea fácil de filtrar
                $tipoAccion = 'Lectura';
                if ($log->document_code == 'DASHBOARD_VIEW') $tipoAccion = 'Ingreso al Sistema';
                if (str_contains($log->document_title, '[FIRMA DE ENTERADO]')) $tipoAccion = 'Firma de Acuse';

                $tituloLimpio = str_replace('[FIRMA DE ENTERADO] ', '', $log->document_title);

                $row = [
                    $log->created_at->format('d/m/Y H:i:s'),
                    $log->user_name,
                    $log->user_role,
                    $log->ip_address,
                    $tipoAccion,
                    $log->document_code,
                    $tituloLimpio,
                    $log->version_num
                ];

                fputcsv($file, $row);
            }

            fclose($file);
        };

        return response()->stream($callback, 200, $headers);
    }
}