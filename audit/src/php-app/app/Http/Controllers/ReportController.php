<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use App\Models\AccessLog;
use Illuminate\Support\Facades\DB;
use Barryvdh\DomPDF\Facade\Pdf;

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

    // ==========================================
    // 🚀 HISTORIAL DETALLADO CON FILTROS MINIMALISTAS
    // ==========================================
    public function historialDetallado(Request $request)
    {
        if (!in_array(session('role'), ['Administrador', 'Auditor'])) {
            return redirect()->route('dashboard'); 
        }

        $myCompany = session('company_id');
        $rango = $request->input('rango', 'all');
        $fechaInicio = $request->input('fecha_inicio');
        $fechaFin = $request->input('fecha_fin');

        $query = AccessLog::where('company_id', $myCompany);

        // LÓGICA DE RANGOS 
        if ($rango === '7days') {
            $fechaInicio = now()->subDays(7)->format('Y-m-d');
            $fechaFin = now()->format('Y-m-d');
            $query->whereDate('created_at', '>=', $fechaInicio)->whereDate('created_at', '<=', $fechaFin);
        } elseif ($rango === '30days') {
            $fechaInicio = now()->subDays(30)->format('Y-m-d');
            $fechaFin = now()->format('Y-m-d');
            $query->whereDate('created_at', '>=', $fechaInicio)->whereDate('created_at', '<=', $fechaFin);
        } else {
            if ($fechaInicio) $query->whereDate('created_at', '>=', $fechaInicio);
            if ($fechaFin) $query->whereDate('created_at', '<=', $fechaFin);
        }

        $historial = $query->orderBy('created_at', 'desc')->paginate(10);
        $historial->appends($request->all());

        return view('history', compact('historial', 'rango', 'fechaInicio', 'fechaFin'));
    }

   // ==========================================
    // 🚀 EXPORTACIÓN EXCEL Y PDF DINÁMICA
    // ==========================================
    public function exportarExcel(Request $request)
    {
        if (!in_array(session('role'), ['Administrador', 'Auditor'])) {
            return redirect()->route('dashboard'); 
        }

        $myCompany = session('company_id');
        $query = AccessLog::where('company_id', $myCompany);

        // 🚀 Aplicamos los mismos filtros que haya en la URL al exportar
        if ($request->input('fecha_inicio')) $query->whereDate('created_at', '>=', $request->input('fecha_inicio'));
        if ($request->input('fecha_fin')) $query->whereDate('created_at', '<=', $request->input('fecha_fin'));

        $logs = $query->orderBy('created_at', 'desc')->get();
        $fileName = 'Auditoria_QualityDoc_' . date('Ymd_His');

        // 🚀 CAPTURAMOS EL FORMATO SOLICITADO
        $formato = $request->input('format', 'csv'); 

        // ==========================================
        // 📄 RUTA A: EXPORTAR COMO PDF
        // ==========================================
        if ($formato === 'pdf') {
            // 🚀 Ahora VS Code ya sabe exactamente de dónde viene esta clase
            $pdf = Pdf::loadView('pdf.auditoria', [
                'logs' => $logs,
                'companyName' => session('company_name', 'Falcons Manufacturing')
            ])->setPaper('a4', 'landscape'); 

            return $pdf->download($fileName . '.pdf');
        }

        // ==========================================
        // 📊 RUTA B: EXPORTAR COMO CSV (EXCEL)
        // ==========================================
        $headers = [
            "Content-type"        => "text/csv; charset=UTF-8",
            "Content-Disposition" => "attachment; filename={$fileName}.csv",
            "Pragma"              => "no-cache",
            "Cache-Control"       => "must-revalidate, post-check=0, pre-check=0",
            "Expires"             => "0"
        ];

        $columns = ['Fecha y Hora', 'Usuario', 'Rol', 'IP', 'Tipo de Accion', 'Codigo de Documento', 'Titulo', 'Version'];

        $callback = function() use($logs, $columns) {
            $file = fopen('php://output', 'w');
            fputs($file, $bom = ( chr(0xEF) . chr(0xBB) . chr(0xBF) ));
            fputcsv($file, $columns);

            foreach ($logs as $log) {
                $tipoAccion = 'Lectura';
                $tituloLimpio = $log->document_title;

                if ($log->document_code == 'DASHBOARD_VIEW') {
                    $tipoAccion = 'Ingreso al Sistema';
                } elseif (str_contains($log->document_title, '[FIRMA DE ENTERADO]')) {
                    $tipoAccion = 'Firma de Acuse';
                    $tituloLimpio = str_replace('[FIRMA DE ENTERADO] ', '', $log->document_title);
                } elseif (str_contains($log->document_title, '[REPORTE DE INCIDENCIA]')) {
                    $tipoAccion = 'Reporte de Error';
                    $tituloLimpio = str_replace('[REPORTE DE INCIDENCIA] ', '', $log->document_title);
                }

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