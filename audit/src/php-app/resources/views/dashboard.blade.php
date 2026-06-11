@extends('layouts.app')

@section('title', 'Dashboard Operativo')

@section('content')
@php 
    $rolActual = session('role'); 
    $companiaNombre = session('company_name', 'tu Empresa');
    $nombreUsuario = session('name', 'Usuario');
@endphp

<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>

<div class="xl:pr-80">
    <div class="space-y-8">
        
        <div class="flex flex-col md:flex-row justify-between items-center gap-4 animate-fade-up">
            <h1 class="text-2xl font-bold text-slate-800 dark:text-white hidden md:block">Mi Espacio de Trabajo</h1>
        </div>

        <div class="relative w-full bg-brand dark:bg-[#B8ACFF] rounded-[2rem] p-8 sm:p-10 overflow-hidden shadow-soft mt-6 mb-8 flex flex-col md:flex-row items-center justify-between animate-fade-up delay-100">
            <div class="absolute inset-0 z-0 pointer-events-none">
                <div class="absolute -top-[50%] left-[40%] w-full h-[200%] bg-white opacity-5 transform -skew-x-12"></div>
            </div>

            <div class="relative z-10 flex-1 w-full text-left">
                <div class="flex items-center gap-2 text-white/80 dark:text-white text-xs font-bold uppercase tracking-widest mb-4">
                    <i class="fa-regular fa-calendar"></i>
                    <span>{{ \Carbon\Carbon::now()->translatedFormat('d \d\e F \d\e Y') }}</span>
                </div>

                <h2 class="text-3xl md:text-4xl font-extrabold text-white dark:text-white mb-4 tracking-tight drop-shadow-sm">
                    ¡Hola, {{ session('name') }}!
                </h2>

                @if($rolActual === 'Auditor' || $rolActual === 'Administrador')
                    <p class="text-white/90 dark:text-white text-sm md:text-base font-medium leading-relaxed mb-6 max-w-xl">
                        Tu centro de auditoría y revisión en <strong class="font-extrabold text-white dark:text-white">{{ session('company_name') }}</strong>.<br />
                        Actualmente hay <span class="inline-block bg-white text-brand px-2 py-0.5 rounded-lg text-sm font-bold mx-0.5 shadow-sm">{{ $kpiReportes ?? 0 }}</span> movimientos registrados.
                    </p>
                    <div class="flex flex-wrap items-center gap-3">
                        <a href="{{ route('reports.history') }}" class="inline-flex items-center gap-2 bg-white text-brand font-bold text-sm px-6 py-3 rounded-xl shadow-sm hover:shadow-md hover:-translate-y-0.5 transition-all">
                            <i class="fa-solid fa-list-check"></i> Ver Trazabilidad
                        </a>
                    </div>
                @else
                    <p class="text-white/90 dark:text-white text-sm md:text-base font-medium leading-relaxed mb-6 max-w-xl">
                        Tu panel operativo en <strong class="font-extrabold text-white dark:text-white">{{ session('company_name') }}</strong>.<br />
                        Actualmente tienes <span class="inline-block bg-white text-brand px-2 py-0.5 rounded-lg text-sm font-bold mx-0.5 shadow-sm">{{ $kpiPorFirmar ?? 0 }}</span> firmas atrasadas.
                    </p>
                    <div class="flex flex-wrap items-center gap-3">
                        <a href="{{ route('directorio') }}" class="inline-flex items-center gap-2 bg-white text-brand font-bold text-sm px-6 py-3 rounded-xl shadow-sm hover:shadow-md hover:-translate-y-0.5 transition-all">
                            <i class="fa-solid fa-folder-open"></i> Explorar Directorio
                        </a>
                    </div>
                @endif
            </div>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-6 animate-fade-up delay-100 mb-8">
            @if($rolActual === 'Auditor' || $rolActual === 'Administrador')
                <div class="relative overflow-hidden rounded-2xl p-5 bg-[#459FFF] text-white shadow-soft flex items-center group transition-transform hover:-translate-y-1">
                    <div class="w-14 h-14 rounded-full bg-white flex items-center justify-center text-[#459FFF] shrink-0 z-10 shadow-sm group-hover:scale-110 transition-transform">
                        <i class="fa-solid fa-users text-2xl"></i>
                    </div>
                    <div class="ml-4 z-10">
                        <span class="block text-sm font-semibold text-white/90 mb-0.5 tracking-wide">Personal Auditado</span>
                        <span class="block text-3xl font-extrabold leading-none">{{ $kpiPersonalAuditado ?? 0 }}</span>
                    </div>
                    <i class="fa-solid fa-users absolute -bottom-6 -right-4 text-8xl opacity-10 transform rotate-12 group-hover:rotate-0 transition-transform duration-500"></i>
                </div>
                <div class="relative overflow-hidden rounded-2xl p-5 bg-[#00D2A6] text-white shadow-soft flex items-center group transition-transform hover:-translate-y-1">
                    <div class="w-14 h-14 rounded-full bg-white flex items-center justify-center text-[#00D2A6] shrink-0 z-10 shadow-sm group-hover:scale-110 transition-transform">
                        <i class="fa-solid fa-file-circle-check text-2xl"></i>
                    </div>
                    <div class="ml-4 z-10">
                        <span class="block text-sm font-semibold text-white/90 mb-0.5 tracking-wide">Logs de Actividad</span>
                        <span class="block text-3xl font-extrabold leading-none">{{ $kpiReportes ?? 0 }}</span>
                    </div>
                    <i class="fa-solid fa-file-circle-check absolute -bottom-6 -right-4 text-8xl opacity-10 transform -rotate-12 group-hover:rotate-0 transition-transform duration-500"></i>
                </div>
                <div class="relative overflow-hidden rounded-2xl p-5 bg-[#F43F5E] dark:bg-[#E11D48] text-white shadow-soft flex items-center group transition-transform hover:-translate-y-1">
                    <div class="w-14 h-14 rounded-full bg-white flex items-center justify-center text-[#F43F5E] dark:text-[#E11D48] shrink-0 z-10 shadow-sm group-hover:scale-110 transition-transform">
                        <i class="fa-solid fa-triangle-exclamation text-2xl"></i>
                    </div>
                    <div class="ml-4 z-10">
                        <span class="block text-sm font-semibold text-white/90 mb-0.5 tracking-wide">Errores Reportados</span>
                        <span class="block text-3xl font-extrabold leading-none">{{ $kpiErrores ?? 0 }}</span>
                    </div>
                    <i class="fa-solid fa-triangle-exclamation absolute -bottom-6 -right-4 text-8xl opacity-10 transform rotate-12 group-hover:rotate-0 transition-transform duration-500"></i>
                </div>
            @else
                <div class="relative overflow-hidden rounded-2xl p-5 bg-[#459FFF] text-white shadow-soft flex items-center group transition-transform hover:-translate-y-1">
                    <div class="w-14 h-14 rounded-full bg-white flex items-center justify-center text-[#459FFF] shrink-0 z-10 shadow-sm group-hover:scale-110 transition-transform">
                        <i class="fa-solid fa-book-open text-2xl"></i>
                    </div>
                    <div class="ml-4 z-10">
                        <span class="block text-sm font-semibold text-white/90 mb-0.5 tracking-wide">Docs. Leídos</span>
                        <span class="block text-3xl font-extrabold leading-none">{{ $kpiDocsLeidos ?? 0 }}</span>
                    </div>
                    <i class="fa-solid fa-book-open absolute -bottom-6 -right-4 text-8xl opacity-10 transform rotate-12 group-hover:rotate-0 transition-transform duration-500"></i>
                </div>
                <div class="relative overflow-hidden rounded-2xl p-5 bg-[#FF9A62] text-white shadow-soft flex items-center group transition-transform hover:-translate-y-1">
                    <div class="w-14 h-14 rounded-full bg-white flex items-center justify-center text-[#FF9A62] shrink-0 z-10 shadow-sm group-hover:scale-110 transition-transform">
                        <i class="fa-solid fa-signature text-2xl"></i>
                    </div>
                    <div class="ml-4 z-10">
                        <span class="block text-sm font-semibold text-white/90 mb-0.5 tracking-wide">Por Firmar</span>
                        <span class="block text-3xl font-extrabold leading-none">{{ $kpiPorFirmar ?? 0 }}</span>
                    </div>
                    <i class="fa-solid fa-pen-nib absolute -bottom-6 -right-4 text-8xl opacity-10 transform rotate-12 group-hover:rotate-0 transition-transform duration-500"></i>
                </div>
                <div class="relative overflow-hidden rounded-2xl p-5 bg-[#00D2A6] text-white shadow-soft flex items-center group transition-transform hover:-translate-y-1">
                    <div class="w-14 h-14 rounded-full bg-white flex items-center justify-center text-[#00D2A6] shrink-0 z-10 shadow-sm group-hover:scale-110 transition-transform">
                        <i class="fa-solid fa-circle-check text-2xl"></i>
                    </div>
                    <div class="ml-4 z-10">
                        <span class="block text-sm font-semibold text-white/90 mb-0.5 tracking-wide">Cumplimiento</span>
                        <span class="block text-3xl font-extrabold leading-none">{{ $kpiCumplimiento ?? 100 }}%</span>
                    </div>
                    <i class="fa-solid fa-circle-check absolute -bottom-6 -right-4 text-8xl opacity-10 transform -rotate-12 group-hover:rotate-0 transition-transform duration-500"></i>
                </div>
            @endif
        </div>

      <!-- 🚀 SECCIÓN INFERIOR DINÁMICA (Gráfica + Actividad Reciente Compacta) -->
        @if($rolActual === 'Auditor' || $rolActual === 'Administrador')
            <div class="grid grid-cols-1 xl:grid-cols-2 gap-6 animate-fade-up delay-200">
                
                <!-- MITAD IZQUIERDA: Gráfica para Auditores -->
                <div class="bg-white dark:bg-[#323338] p-6 rounded-2xl shadow-soft dark:shadow-none border border-slate-200 dark:border-[#4a4b50] flex flex-col">
                    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-5 gap-4">
                        <div>
                            <h3 class="text-lg font-bold text-slate-800 dark:text-white">Flujo de Actividad</h3>
                            <p class="text-sm text-slate-500 dark:text-slate-400 font-medium mt-0.5">Accesos, firmas y reportes</p>
                        </div>
                        
                        <div class="flex items-center gap-3">
                            <!-- MINI FILTRO -->
                            <div class="relative bg-slate-50 dark:bg-[#2a2b2e] border border-slate-200 dark:border-[#4a4b50] rounded-xl shadow-sm transition-colors">
                                <select onchange="window.location.href='?chart_range=' + this.value" class="bg-transparent border-none focus:ring-0 text-slate-600 dark:text-slate-300 py-1.5 pl-3 pr-8 text-xs font-bold cursor-pointer appearance-none outline-none">
                                    <option value="7days" {{ ($chartRange ?? '7days') == '7days' ? 'selected' : '' }}>Últimos 7 días</option>
                                    <option value="15days" {{ ($chartRange ?? '') == '15days' ? 'selected' : '' }}>Últimos 15 días</option>
                                    <option value="30days" {{ ($chartRange ?? '') == '30days' ? 'selected' : '' }}>Últimos 30 días</option>
                                </select>
                                <i class="fa-solid fa-chevron-down absolute right-3 top-1/2 -translate-y-1/2 text-[10px] text-slate-400 pointer-events-none"></i>
                            </div>
                        </div>
                    </div>
                    <!-- 🚀 FIX: Contenedor del Canvas más chaparrito (h-[200px]) -->
                    <div class="w-full flex-1 min-h-[200px] h-[200px] relative">
                        <canvas id="activityChart"></canvas>
                    </div>
                </div>

                <!-- MITAD DERECHA: Feed de Actividades Recientes -->
                <div class="bg-white dark:bg-[#323338] p-6 rounded-2xl shadow-soft dark:shadow-none border border-slate-200 dark:border-[#4a4b50] flex flex-col">
                    <div class="flex justify-between items-center mb-5">
                        <div>
                            <h3 class="text-lg font-bold text-slate-800 dark:text-white">Actividad Reciente</h3>
                            <p class="text-sm text-slate-500 dark:text-slate-400 font-medium mt-0.5">Últimos movimientos en la planta</p>
                        </div>
                        <!-- Botón Ver Todos -->
                        <a href="{{ route('reports.history') }}" class="text-xs font-bold text-brand bg-brand/10 hover:bg-brand hover:text-white px-3 py-2 rounded-xl transition-colors shadow-sm flex items-center">
                            Ver todos <i class="fa-solid fa-arrow-right ml-1.5"></i>
                        </a>
                    </div>

                    <!-- Lista de Registros Estilo "Píldora" -->
                    <div class="flex-1 space-y-3 overflow-y-auto pr-1">
                        @forelse($actividadReciente as $log)
                            <div class="flex items-center justify-between p-3.5 bg-slate-50 dark:bg-[#2a2b2e] rounded-[1rem] border border-slate-100 dark:border-darkbg-border hover:border-brand/30 dark:hover:border-brand/40 transition-colors shadow-sm group">
                                
                                <div class="flex items-center gap-4 overflow-hidden">
                                    <!-- Avatar Dinámico -->
                                    <img src="https://ui-avatars.com/api/?name={{ urlencode($log->user_name) }}&background=8772FE&color=fff&rounded=true&bold=true" class="w-10 h-10 rounded-full shadow-sm shrink-0">
                                    
                                    <div class="flex flex-col truncate">
                                        <h4 class="text-sm font-bold text-slate-800 dark:text-white truncate">{{ $log->user_name }}</h4>
                                        <div class="flex items-center gap-2 mt-0.5">
                                            <span class="text-[10px] text-slate-400 dark:text-slate-500 font-medium"><i class="fa-regular fa-clock mr-1"></i>{{ $log->created_at->diffForHumans() }}</span>
                                            
                                            <!-- Etiqueta de Acción -->
                                            @if($log->document_code == 'DASHBOARD_VIEW')
                                                <span class="text-[9px] font-bold text-slate-500 bg-slate-200 dark:bg-[#323338] px-1.5 py-0.5 rounded">Ingreso</span>
                                            @elseif(str_contains($log->document_title, '[REPORTE DE INCIDENCIA]'))
                                                <span class="text-[9px] font-bold text-amber-600 bg-amber-100 dark:bg-amber-500/20 px-1.5 py-0.5 rounded">Error</span>
                                            @elseif(str_contains($log->document_title, '[FIRMA DE ENTERADO]'))
                                                <span class="text-[9px] font-bold text-emerald-600 bg-emerald-100 dark:bg-emerald-500/20 px-1.5 py-0.5 rounded">Firma</span>
                                            @else
                                                <span class="text-[9px] font-bold text-blue-600 bg-blue-100 dark:bg-blue-500/20 px-1.5 py-0.5 rounded">Lectura</span>
                                            @endif
                                        </div>
                                    </div>
                                </div>

                                <!-- Código del Documento (Detalle útil y estético) -->
                                @if($log->document_code == 'DASHBOARD_VIEW')
                                    <div class="shrink-0 px-3 py-1.5 rounded-xl border border-slate-200 dark:border-[#4a4b50] bg-slate-100 dark:bg-[#323338] text-slate-400 dark:text-slate-500 text-[10px] font-bold uppercase tracking-wider shadow-sm select-none">
                                        Portal
                                    </div>
                                @else
                                    <div class="shrink-0 px-3 py-1.5 rounded-xl border border-brand/20 bg-brand/5 dark:bg-brand/10 text-brand dark:text-[#B8ACFF] text-[11px] font-black shadow-sm transition-colors cursor-default select-none group-hover:bg-brand group-hover:text-white dark:group-hover:text-white" title="{{ str_replace(['[FIRMA DE ENTERADO] ', '[REPORTE DE INCIDENCIA] '], '', $log->document_title) }}">
                                        {{ $log->document_code }}
                                    </div>
                                @endif
                            </div>
                        @empty
                            <div class="text-center py-6 flex flex-col items-center justify-center">
                                <div class="w-12 h-12 bg-slate-100 dark:bg-[#2a2b2e] rounded-full flex items-center justify-center text-slate-300 dark:text-slate-600 mb-2">
                                    <i class="fa-solid fa-ghost text-xl"></i>
                                </div>
                                <p class="text-slate-500 font-medium text-xs">No hay movimientos.</p>
                            </div>
                        @endforelse
                    </div>
                </div>
            </div>
        @else
            <!-- Botones Originales para Operarios (Se mantienen igual) -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6 animate-fade-up delay-200">
                <a href="{{ route('directorio') }}" class="bg-white dark:bg-[#323338] p-6 rounded-2xl shadow-soft dark:shadow-none border border-slate-200 dark:border-[#4a4b50] flex items-center justify-between hover:border-brand dark:hover:border-brand transition-colors cursor-pointer group">
                    <div class="flex items-center">
                        <div class="p-4 bg-brand/10 dark:bg-brand/20 text-brand dark:text-[#B8ACFF] rounded-xl mr-5">
                            <i class="fa-solid fa-book-open-reader text-2xl"></i>
                        </div>
                        <div>
                            <h3 class="text-lg font-bold text-slate-800 dark:text-white">Directorio Vigente</h3>
                            <p class="text-sm text-slate-500 dark:text-slate-400 font-medium mt-0.5">Consultar manuales y procedimientos</p>
                        </div>
                    </div>
                    <div class="w-10 h-10 rounded-full bg-slate-50 dark:bg-[#2a2b2e] flex items-center justify-center group-hover:bg-brand group-hover:text-white text-slate-400 transition-colors">
                        <i class="fa-solid fa-chevron-right text-sm"></i>
                    </div>
                </a>

                <a href="{{ route('dashboard.compliances') }}" class="bg-white dark:bg-[#323338] p-6 rounded-2xl shadow-soft dark:shadow-none border border-slate-200 dark:border-[#4a4b50] flex items-center justify-between hover:border-brand dark:hover:border-brand transition-colors cursor-pointer group">
                    <div class="flex items-center">
                        <div class="p-4 bg-slate-100 dark:bg-[#2A2B2E] text-slate-600 dark:text-slate-300 rounded-xl mr-5">
                            <i class="fa-solid fa-file-signature text-2xl"></i>
                        </div>
                        <div>
                            <h3 class="text-lg font-bold text-slate-800 dark:text-white">Mis Cumplimientos</h3>
                            <p class="text-sm text-slate-500 dark:text-slate-400 font-medium mt-0.5">Firma de acuses pendientes</p>
                        </div>
                    </div>
                    <div class="w-10 h-10 rounded-full bg-slate-50 dark:bg-[#2a2b2e] flex items-center justify-center group-hover:bg-brand group-hover:text-white text-slate-400 transition-colors">
                        <i class="fa-solid fa-chevron-right text-sm"></i>
                    </div>
                </a>
            </div>
        @endif
    </div>
</div>

<aside id="right-sidebar" class="hidden xl:flex flex-col fixed right-0 top-0 h-screen w-80 bg-white dark:bg-darkbg-card border-l border-slate-200 dark:border-darkbg-border overflow-y-auto z-30 shadow-[-4px_0_24px_rgba(0,0,0,0.02)]">
    <div class="p-6 pb-2 flex items-center justify-between sticky top-0 bg-white/90 dark:bg-darkbg-card/90 backdrop-blur-md z-20 border-b border-slate-100 dark:border-darkbg-border/50">
        <div class="flex gap-2">
            <button class="w-10 h-10 rounded-full border border-slate-200 dark:border-[#4a4b50] flex items-center justify-center text-slate-500 dark:text-slate-400 hover:text-brand hover:bg-brand/5 transition-all relative">
                <i class="fa-regular fa-bell"></i>
                <span class="absolute top-2 right-2.5 w-2 h-2 bg-red-500 rounded-full border-2 border-white dark:border-darkbg-card"></span>
            </button>
        </div>
        <div class="flex items-center gap-3 group text-right">
            <div class="flex flex-col justify-center">
                <span class="text-sm font-bold text-slate-800 dark:text-white group-hover:text-brand transition-colors leading-tight truncate max-w-[100px]">{{ $nombreUsuario }}</span>
                <span class="text-[10px] text-slate-500 uppercase tracking-wide">{{ $rolActual }}</span>
            </div>
            <img src="https://ui-avatars.com/api/?name={{ urlencode($nombreUsuario) }}&background=8772FE&color=fff&rounded=true&bold=true" alt="Perfil" class="w-10 h-10 rounded-full shadow-sm ring-2 ring-transparent group-hover:ring-brand/30 transition-all">
        </div>
    </div>

    <div class="p-6 space-y-8 pt-6">
        <div class="bg-gradient-to-br from-brand to-brand-hover dark:from-[#B8ACFF] dark:to-brand rounded-3xl p-5 text-white shadow-lg shadow-brand/20 dark:shadow-none relative overflow-hidden group">
            <i id="weatherIconBg" class="fa-solid fa-cloud-sun absolute -right-3 -bottom-4 text-7xl opacity-20 group-hover:scale-110 transition-transform duration-500"></i>
            <div class="relative z-10 flex justify-between items-center mb-4">
                <span class="text-sm font-medium opacity-90 tracking-wide">Monclova, MX</span>
                <span class="text-xs font-bold bg-white/20 px-2.5 py-1 rounded-lg backdrop-blur-sm">Hoy</span>
            </div>
            <div class="relative z-10 flex items-end gap-3">
                <h2 id="weatherTemp" class="text-5xl font-extrabold leading-none tracking-tighter">--°</h2>
                <span id="weatherDesc" class="text-sm font-medium mb-1.5 opacity-90">Cargando...</span>
            </div>
        </div>

        <div>
            <div class="flex justify-between items-center mb-4">
                <h3 class="font-bold text-slate-800 dark:text-white text-base">Calendario</h3>
                <span class="text-xs bg-slate-100 dark:bg-darkbg-base px-3 py-1.5 rounded-lg text-slate-600 dark:text-slate-400 font-medium">
                    <i class="fa-regular fa-calendar"></i> {{ \Carbon\Carbon::now()->translatedFormat('F') }}
                </span>
            </div>
            <div class="flex justify-between gap-2">
                @php
                    $diasSemana = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie'];
                    $today = \Carbon\Carbon::now();
                    $startOfWeek = $today->copy()->startOfWeek();
                @endphp
                @for ($i = 0; $i < 5; $i++)
                    @php
                        $currentDate = $startOfWeek->copy()->addDays($i);
                        $isToday = $currentDate->isSameDay($today);
                    @endphp
                    <div class="flex flex-col items-center justify-center py-2 px-3 rounded-2xl {{ $isToday ? 'bg-brand dark:bg-[#B8ACFF] text-white shadow-md scale-105' : 'bg-slate-50 dark:bg-[#2a2b2e] text-slate-500' }}">
                        <span class="text-[9px] uppercase font-bold mb-1">{{ $diasSemana[$i] }}</span>
                        <span class="text-sm font-bold">{{ $currentDate->day }}</span>
                    </div>
                @endfor
            </div>
        </div>
    </div>
</aside>
@endsection

@section('scripts')
<script>
    // 🚀 LÓGICA DE LA GRÁFICA (Solo se ejecuta si existen datos)
    @if(isset($chartLabels) && !empty($chartLabels))
        document.addEventListener("DOMContentLoaded", function() {
            const ctx = document.getElementById('activityChart').getContext('2d');
            
            // Creamos un gradiente suave azul para debajo de la curva
            let gradient = ctx.createLinearGradient(0, 0, 0, 300);
            gradient.addColorStop(0, 'rgba(135, 114, 254, 0.4)'); // Color primary (brand)
            gradient.addColorStop(1, 'rgba(135, 114, 254, 0.0)');

            new Chart(ctx, {
                type: 'line',
                data: {
                    labels: {!! json_encode($chartLabels) !!},
                    datasets: [{
                        label: 'Movimientos',
                        data: {!! json_encode($chartData) !!},
                        borderColor: '#8772FE', // Brand color
                        backgroundColor: gradient,
                        borderWidth: 3,
                        pointBackgroundColor: '#ffffff',
                        pointBorderColor: '#8772FE',
                        pointBorderWidth: 2,
                        pointRadius: 4,
                        pointRadius: {!! count($chartData) > 15 ? 1 : 4 !!},
                        pointHoverRadius: 6,
                        tension: 0.4 // Hace que la curva sea suave
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false }, // Ocultamos la leyenda
                        tooltip: {
                            backgroundColor: '#323338',
                            padding: 12,
                            titleFont: { size: 13, family: 'sans-serif' },
                            bodyFont: { size: 14, family: 'sans-serif', weight: 'bold' },
                            displayColors: false,
                            cornerRadius: 8,
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            grid: { 
                                borderDash: [4, 4], 
                                color: 'rgba(156, 163, 175, 0.2)', 
                                drawBorder: false 
                            },
                            ticks: { 
                                stepSize: 1, 
                                color: '#9CA3AF', 
                                font: { size: 11 } 
                            }
                        },
                        x: {
                            grid: { display: false, drawBorder: false },
                            ticks: { color: '#9CA3AF', font: { size: 11 } }
                        }
                    }
                }
            });
        });
    @endif

    // Clima
    async function fetchWeather() {
        const apiKey = 'c7861a95b153e45d21380f353d987af5';
        const city = 'Monclova,MX'; 
        const url = `https://api.openweathermap.org/data/2.5/weather?q=${city}&units=metric&lang=es&appid=${apiKey}`;
        
        try {
            const response = await fetch(url);
            if (!response.ok) throw new Error('Error clima');
            const data = await response.json();
            if(document.getElementById('weatherTemp')) {
                document.getElementById('weatherTemp').innerText = Math.round(data.main.temp) + '°';
                const desc = data.weather[0].description;
                document.getElementById('weatherDesc').innerText = desc.charAt(0).toUpperCase() + desc.slice(1);
                
                const iconCode = data.weather[0].icon;
                const iconElement = document.getElementById('weatherIconBg');
                iconElement.className = 'absolute -right-3 -bottom-4 text-7xl opacity-20 group-hover:scale-110 transition-transform duration-500 fa-solid ';
                
                if (iconCode.includes('01')) iconElement.classList.add('fa-sun');
                else if (iconCode.includes('02') || iconCode.includes('03') || iconCode.includes('04')) iconElement.classList.add('fa-cloud-sun');
                else if (iconCode.includes('09') || iconCode.includes('10')) iconElement.classList.add('fa-cloud-showers-heavy');
                else if (iconCode.includes('11')) iconElement.classList.add('fa-bolt');
                else iconElement.classList.add('fa-cloud');
            }
        } catch (error) {
            if(document.getElementById('weatherTemp')) {
                document.getElementById('weatherTemp').innerText = '28°';
                document.getElementById('weatherDesc').innerText = 'Parcialmente Soleado';
            }
        }
    }
    fetchWeather();
</script>
@endsection