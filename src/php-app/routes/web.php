<?php

use Illuminate\Support\Facades\Route;
use App\Http\Controllers\AuthController;
use App\Http\Controllers\DashboardController;
use App\Http\Controllers\ReportController;

// ==========================================
// 1. ZONA PÚBLICA / AUTENTICACIÓN
// ==========================================
Route::get('/', function () {
    return redirect('http://127.0.0.1:5269/Auth/Login');
});

Route::get('/auth/token', [AuthController::class, 'verifyToken'])->name('auth.token');
Route::get('/logout', [AuthController::class, 'logout'])->name('logout');


// ==========================================
// 2. ZONA DEL OPERARIO (Consulta y Cumplimiento)
// ==========================================
Route::prefix('operario')->group(function () {
    // Mantenemos los nombres de ruta ('name') iguales para no romper tus vistas actuales
    Route::get('/documentos', [DashboardController::class, 'index'])->name('dashboard'); 
    Route::get('/log-access', [DashboardController::class, 'logDocumentAccess'])->name('log.document');
    Route::post('/documentos/acuse', [DashboardController::class, 'acuseLectura'])->name('document.acuse');
    Route::get('/mis-cumplimientos', [DashboardController::class, 'misCumplimientos'])->name('dashboard.compliances');
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