<?php

use Illuminate\Support\Facades\Route;
use App\Http\Controllers\AuthController;
use App\Http\Controllers\DashboardController; // 🚀 1. Agregamos el Use del nuevo controlador

Route::get('/', function () {
    return redirect('http://localhost:5269/Auth/Login');
});

Route::get('/auth/token', [AuthController::class, 'verifyToken'])->name('auth.token');

// 🚀 2. Reemplazamos la ruta estática por el controlador
Route::get('/dashboard', [DashboardController::class, 'index'])->name('dashboard');

Route::get('/log-access', [DashboardController::class, 'logDocumentAccess'])->name('log.document');

Route::get('/logout', [AuthController::class, 'logout'])->name('logout');