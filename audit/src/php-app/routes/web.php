<?php

use Illuminate\Support\Facades\Route;
use App\Http\Controllers\AuthController;
use App\Http\Controllers\DashboardController;
use App\Http\Controllers\ReportController;

// ==========================================
// 1. ZONA PÚBLICA / AUTENTICACIÓN
// ==========================================
Route::get('/', function () {
    return redirect('/admin/Auth/Login');
});

Route::get('/auth/token', [AuthController::class, 'verifyToken'])->name('auth.token');
Route::get('/logout', [AuthController::class, 'logout'])->name('logout');


// ==========================================
// 2. ZONA DEL OPERARIO (Consulta y Cumplimiento)
// ==========================================
Route::prefix('operario')->group(function () {
    
    // 🚀 VISTAS PRINCIPALES
    Route::get('/dashboard', [DashboardController::class, 'dashboard'])->name('dashboard'); 
    Route::get('/directorio', [DashboardController::class, 'directorio'])->name('directorio'); 
    
    // ACCIONES Y LECTURAS
    Route::get('/log-access', [DashboardController::class, 'logDocumentAccess'])->name('log.document');
    Route::post('/documentos/acuse', [DashboardController::class, 'acuseLectura'])->name('document.acuse');
    Route::get('/mis-cumplimientos', [DashboardController::class, 'misCumplimientos'])->name('dashboard.compliances');

    // 🚀 NUEVA RUTA: Puente AJAX para enviar el reporte de error a C#
    Route::post('/reportar-error', [DashboardController::class, 'reportarError'])->name('reportar.error');
    Route::get('/directorio/historial/{codigo}', [DashboardController::class, 'historialDocumento'])->name('documento.historial');
});


// ==========================================
// 3. ZONA DEL AUDITOR / ADMINISTRADOR (Panel Gerencial)
// ==========================================
Route::prefix('auditor')->group(function () {
    Route::get('/panel', [ReportController::class, 'index'])->name('reports');
    
    // 🚀 NUEVAS RUTAS DESBLOQUEADAS
    Route::get('/historial', [ReportController::class, 'historialDetallado'])->name('reports.history');
    Route::get('/exportar', [ReportController::class, 'exportarExcel'])->name('reports.export');
});