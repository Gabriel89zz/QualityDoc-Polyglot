@extends('layouts.app')

@section('title', 'Panel Gerencial')
@section('header_title', 'Dashboard de Auditoría y Cumplimiento')

@section('content')
    <div class="mb-6">
        <p class="text-gray-500">Estadísticas de visualización y registros de acceso operativo.</p>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
        <div class="bg-white rounded-xl shadow-sm p-6 border-l-4 border-blue-500 hover:shadow-md transition">
            <div class="flex items-center">
                <div class="p-3 rounded-full bg-blue-50 text-blue-600 mr-4 border border-blue-100">
                    <i class="fa-solid fa-file-invoice text-2xl"></i>
                </div>
                <div>
                    <p class="text-xs text-gray-400 font-bold uppercase tracking-wider">Lecturas Totales</p>
                    <p class="text-3xl font-black text-gray-800">{{ $totalLecturas }}</p>
                </div>
            </div>
        </div>

        <div class="bg-white rounded-xl shadow-sm p-6 border-l-4 border-green-500 hover:shadow-md transition">
            <div class="flex items-center">
                <div class="p-3 rounded-full bg-green-50 text-green-600 mr-4 border border-green-100">
                    <i class="fa-solid fa-users text-2xl"></i>
                </div>
                <div>
                    <p class="text-xs text-gray-400 font-bold uppercase tracking-wider">Usuarios Activos</p>
                    <p class="text-3xl font-black text-gray-800">{{ $usuariosUnicos }}</p>
                </div>
            </div>
        </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
        
        <div class="bg-white rounded-xl shadow-sm overflow-hidden border border-gray-100 col-span-1 flex flex-col">
            <div class="bg-gray-50 px-6 py-4 border-b border-gray-100">
                <h3 class="font-bold text-gray-700 text-sm uppercase tracking-wider"><i class="fa-solid fa-fire text-orange-500 mr-2"></i>Más Consultados</h3>
            </div>
            <ul class="divide-y divide-gray-50 flex-1 overflow-y-auto">
                @forelse($topDocumentos as $top)
                    <li class="px-6 py-4 flex justify-between items-center hover:bg-gray-50 transition">
                        <div class="overflow-hidden pr-4">
                            <p class="text-sm font-black text-gray-800 truncate">{{ $top->document_code }}</p>
                            <p class="text-xs text-gray-500 truncate w-40" title="{{ $top->document_title }}">{{ $top->document_title }}</p>
                        </div>
                        <span class="bg-indigo-50 text-indigo-700 border border-indigo-100 text-xs font-bold px-3 py-1 rounded-full whitespace-nowrap">
                            {{ $top->total_vistas }} <i class="fa-solid fa-eye ml-1"></i>
                        </span>
                    </li>
                @empty
                    <li class="px-6 py-8 text-center text-sm text-gray-400">
                        <i class="fa-solid fa-folder-open text-3xl mb-2 text-gray-200 block"></i>
                        Aún no hay lecturas.
                    </li>
                @endforelse
            </ul>
        </div>

        <div class="bg-white rounded-xl shadow-sm overflow-hidden border border-gray-100 col-span-1 lg:col-span-2 flex flex-col">
            <div class="bg-gray-50 px-6 py-4 border-b border-gray-100 flex justify-between items-center">
                <div class="flex items-center">
                    <h3 class="font-bold text-gray-700 text-sm uppercase tracking-wider"><i class="fa-solid fa-clock-rotate-left text-blue-500 mr-2"></i>Actividad Reciente</h3>
                    <span class="bg-gray-200 text-gray-600 text-[10px] font-bold px-2 py-0.5 rounded ml-3 uppercase tracking-wider">Últimos 5 registros</span>
                </div>
                <a href="{{ route('reports.history') }}" class="text-xs font-bold text-indigo-600 hover:text-indigo-800 transition">Ver todo <i class="fa-solid fa-arrow-right ml-1"></i></a>
            </div>
            <div class="overflow-x-auto">
                <!-- 🚀 FIX: Quitamos table-fixed y usamos table-auto con Columna Fantasma -->
            <table class="w-full text-left border-collapse table-auto min-w-[800px]">
                <thead class="bg-slate-50 dark:bg-[#2a2b2e] text-slate-500 dark:text-slate-400 text-xs uppercase font-semibold border-b border-slate-100 dark:border-darkbg-border">
                    <tr>
                        <!-- Anchos estrictos para mantener los datos agrupados -->
                        <th class="p-5 w-40 whitespace-nowrap">Fecha y Hora</th>
                        <th class="p-5 w-56 whitespace-nowrap">Usuario / IP</th>
                        <th class="p-5 w-80 whitespace-nowrap">Documento Afectado</th>
                        <th class="p-5 w-56 whitespace-nowrap">Acción Registrada</th>
                        <!-- 🚀 COLUMNA FANTASMA: Se expande y absorbe el espacio muerto -->
                        <th class="p-0 w-full"></th> 
                    </tr>
                </thead>
                <tbody class="divide-y divide-slate-100 dark:divide-darkbg-border text-sm">
                    @forelse($historial as $log)
                        <tr class="hover:bg-slate-50 dark:hover:bg-[#2a2b2e] transition-colors">
                            
                            <!-- 1. FECHA Y HORA -->
                            <td class="p-5 align-top whitespace-nowrap">
                                <div class="font-bold text-slate-700 dark:text-slate-300">{{ $log->created_at->format('d M Y') }}</div>
                                <div class="text-xs text-slate-400">{{ $log->created_at->format('H:i:s') }} hrs</div>
                            </td>
                            
                            <!-- 2. USUARIO -->
                            <td class="p-5 align-top whitespace-nowrap">
                                <p class="text-sm font-bold text-slate-800 dark:text-white truncate" title="{{ $log->user_name }}">{{ $log->user_name }}</p>
                                <p class="text-[11px] text-slate-400 dark:text-slate-500 font-mono mt-0.5 flex items-center">
                                    <i class="fa-solid fa-desktop mr-1.5 opacity-70"></i> {{ $log->ip_address }}
                                </p>
                            </td>
                            
                            <!-- 3. DOCUMENTO -->
                            <td class="p-5 align-top">
                                @if($log->document_code == 'DASHBOARD_VIEW')
                                    <span class="text-xs text-slate-400 dark:text-slate-500 font-medium italic">-- Sistema Central --</span>
                                @else
                                    <div class="flex flex-col items-start gap-1.5 overflow-hidden">
                                        <span class="text-xs font-black text-brand bg-brand/5 dark:bg-brand/10 border border-brand/20 dark:border-brand/20 px-2 py-0.5 rounded truncate max-w-full" title="{{ $log->document_code }}">{{ $log->document_code }}</span>
                                        
                                        @if(str_contains($log->document_title, '[REPORTE DE INCIDENCIA]'))
                                            <span class="text-[11px] text-slate-400 dark:text-slate-500 font-medium truncate max-w-full">Revisión de calidad / Incidencia</span>
                                        @else
                                            <!-- Truncamos el título largo para que no rompa la tabla -->
                                            <span class="text-[11px] text-slate-500 dark:text-slate-400 font-medium truncate max-w-[280px]" title="{{ str_replace('[FIRMA DE ENTERADO] ', '', $log->document_title) }}">
                                                v{{ $log->version_num }} - {{ str_replace('[FIRMA DE ENTERADO] ', '', $log->document_title) }}
                                            </span>
                                        @endif
                                    </div>
                                @endif
                            </td>
                            
                            <!-- 4. ACCIÓN REGISTRADA -->
                            <td class="p-5 align-top whitespace-nowrap">
                                @if($log->document_code == 'DASHBOARD_VIEW')
                                    <span class="inline-flex items-center px-2 py-1 rounded-md text-[11px] font-bold bg-slate-100 dark:bg-[#323338] text-slate-600 dark:text-slate-300 border border-slate-200 dark:border-darkbg-border shadow-sm">
                                        <i class="fa-solid fa-right-to-bracket mr-1.5 opacity-70"></i> Ingreso al Portal
                                    </span>

                                @elseif(str_contains($log->document_title, '[REPORTE DE INCIDENCIA]'))
                                    <!-- 🚀 DISEÑO MINIMALISTA PARA ERROR (Alineado perfecto) -->
                                    <div class="flex flex-col items-start gap-1.5 overflow-hidden">
                                        <span class="inline-flex items-center px-2 py-1 rounded-md text-[11px] font-bold bg-amber-50 dark:bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-200/50 dark:border-amber-500/20 shadow-sm shrink-0">
                                            <i class="fa-solid fa-flag mr-1.5 opacity-80"></i> Reporte de Error
                                        </span>
                                        <div class="flex items-center text-[11px] text-slate-500 dark:text-slate-400 font-medium w-full" title="{{ str_replace('[REPORTE DE INCIDENCIA] ', '', $log->document_title) }}">
                                            <div class="w-1 h-1 rounded-full bg-slate-300 dark:bg-slate-600 mr-1.5 shrink-0"></div>
                                            <span class="truncate max-w-[180px]">{{ str_replace('[REPORTE DE INCIDENCIA] ', '', $log->document_title) }}</span>
                                        </div>
                                    </div>

                                @elseif(str_contains($log->document_title, '[FIRMA DE ENTERADO]'))
                                    <span class="inline-flex items-center px-2 py-1 rounded-md text-[11px] font-bold bg-emerald-50 dark:bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-200/50 dark:border-emerald-500/20 shadow-sm shrink-0">
                                        <i class="fa-solid fa-file-signature mr-1.5 opacity-80"></i> Acuse Firmado
                                    </span>

                                @else
                                    <span class="inline-flex items-center px-2 py-1 rounded-md text-[11px] font-bold bg-blue-50 dark:bg-blue-500/10 text-blue-600 dark:text-blue-400 border border-blue-200/50 dark:border-blue-500/20 shadow-sm shrink-0">
                                        <i class="fa-solid fa-eye mr-1.5 opacity-80"></i> Lectura de Doc
                                    </span>
                                @endif
                            </td>

                            <!-- 5. COLUMNA FANTASMA DE RELLENO -->
                            <td class="p-0"></td>
                        </tr>
                    @empty
                        <tr>
                            <td colspan="5" class="px-6 py-16 text-center">
                                <div class="w-20 h-20 mx-auto bg-slate-50 dark:bg-[#2a2b2e] rounded-full flex items-center justify-center mb-4 border border-slate-100 dark:border-darkbg-border">
                                    <i class="fa-solid fa-database text-4xl text-slate-300 dark:text-slate-600"></i>
                                </div>
                                <h3 class="text-lg font-bold text-slate-700 dark:text-slate-300">Buzón Vacío</h3>
                                <p class="text-slate-500 text-sm mt-1">No hay registros de auditoría para estos filtros.</p>
                            </td>
                        </tr>
                    @endforelse
                </tbody>
            </table>
            </div>
        </div>

    </div>
@endsection