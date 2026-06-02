@extends('layouts.app')

@section('title', 'Auditoría Detallada')

@section('content')
<div class="w-full">
    
    <!-- 🚀 Encabezado Minimalista (Estilo Popover) -->
    <!-- FIX: Agregamos 'relative z-50' al contenedor principal para elevarlo sobre la tabla -->
    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-8 animate-fade-up relative z-50">
        <div>
            <h1 class="text-2xl font-bold text-slate-800 dark:text-white">Registro de Trazabilidad</h1>
            <p class="text-slate-500 dark:text-slate-400 text-sm mt-1">Historial completo de accesos, firmas y reportes operativos.</p>
        </div>
        
        <div class="flex items-center gap-3 w-full sm:w-auto">
            
            <!-- 🚀 BOTÓN Y MENÚ DE FILTRO -->
            <div class="relative w-full sm:w-auto">
                <button type="button" id="btnFilterToggle" class="w-full sm:w-auto flex items-center justify-center gap-2 bg-white dark:bg-[#323338] border border-slate-200 dark:border-darkbg-border text-slate-700 dark:text-slate-300 px-4 py-2 rounded-xl text-sm font-bold hover:bg-slate-50 dark:hover:bg-[#2a2b2e] hover:text-brand dark:hover:text-brand shadow-sm transition-all focus:outline-none focus:ring-2 focus:ring-brand/20">
                    <i class="fa-solid fa-filter text-brand/70"></i> Filtrar
                    @if(($rango ?? 'all') != 'all' || !empty($fechaInicio) || !empty($fechaFin))
                        <span class="absolute -top-1 -right-1 w-3 h-3 bg-brand rounded-full border-2 border-white dark:border-darkbg-card"></span>
                    @endif
                </button>

                <!-- Menú Flotante de Filtros -->
                <div id="filterMenu" class="hidden absolute right-0 sm:right-0 left-0 sm:left-auto mt-2 w-full sm:w-72 bg-white dark:bg-[#323338] border border-slate-200 dark:border-[#4a4b50] rounded-2xl shadow-xl z-50 p-5 transform origin-top-right transition-all">
                    <div class="flex justify-between items-center mb-4">
                        <h3 class="text-sm font-bold text-slate-800 dark:text-white">Añadir Filtro</h3>
                        @if(($rango ?? 'all') != 'all' || !empty($fechaInicio) || !empty($fechaFin))
                            <a href="{{ route('reports.history') }}" class="text-[10px] font-bold text-red-500 hover:text-red-600 bg-red-50 dark:bg-red-500/10 px-2 py-1 rounded-md transition-colors">Limpiar</a>
                        @endif
                    </div>
                    
                    <form method="GET" action="{{ route('reports.history') }}" id="filterForm" class="space-y-4">
                        <div>
                            <label class="block text-[11px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider mb-1.5">Rango de Tiempo</label>
                            <select name="rango" id="rangoSelect" class="w-full bg-slate-50 dark:bg-[#2a2b2e] border border-slate-200 dark:border-darkbg-border rounded-xl px-3 py-2 text-sm text-slate-700 dark:text-slate-300 focus:border-brand focus:ring-1 focus:ring-brand outline-none transition-colors cursor-pointer">
                                <option value="all" {{ ($rango ?? 'all') == 'all' ? 'selected' : '' }}>Histórico completo</option>
                                <option value="7days" {{ ($rango ?? '') == '7days' ? 'selected' : '' }}>Últimos 7 días</option>
                                <option value="30days" {{ ($rango ?? '') == '30days' ? 'selected' : '' }}>Últimos 30 días</option>
                                <option value="custom" {{ ($rango ?? '') == 'custom' ? 'selected' : '' }}>Fechas exactas</option>
                            </select>
                        </div>

                        <div id="customDateGroup" class="{{ ($rango ?? '') == 'custom' ? 'block' : 'hidden' }} space-y-3 pt-2 border-t border-slate-100 dark:border-darkbg-border">
                            <div>
                                <label class="block text-[11px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider mb-1.5">Desde</label>
                                <input type="date" name="fecha_inicio" id="fechaInicio" value="{{ $fechaInicio ?? '' }}" class="w-full bg-slate-50 dark:bg-[#2a2b2e] border border-slate-200 dark:border-darkbg-border rounded-xl px-3 py-2 text-sm text-slate-700 dark:text-slate-300 focus:border-brand focus:ring-1 focus:ring-brand outline-none transition-colors cursor-pointer">
                            </div>
                            <div>
                                <label class="block text-[11px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider mb-1.5">Hasta</label>
                                <input type="date" name="fecha_fin" id="fechaFin" value="{{ $fechaFin ?? '' }}" class="w-full bg-slate-50 dark:bg-[#2a2b2e] border border-slate-200 dark:border-darkbg-border rounded-xl px-3 py-2 text-sm text-slate-700 dark:text-slate-300 focus:border-brand focus:ring-1 focus:ring-brand outline-none transition-colors cursor-pointer">
                            </div>
                        </div>

                        <button type="submit" class="w-full bg-brand hover:bg-brand-hover text-white text-sm font-bold py-2.5 rounded-xl transition-colors shadow-sm mt-2">
                            Aplicar Filtros
                        </button>
                    </form>
                </div>
            </div>

            <!-- 🚀 BOTÓN Y MENÚ DE EXPORTAR -->
            <div class="relative w-full sm:w-auto">
                <button type="button" id="btnExportToggle" class="w-full sm:w-auto flex items-center justify-center gap-2 bg-white dark:bg-[#323338] border border-slate-200 dark:border-darkbg-border text-slate-700 dark:text-slate-300 px-4 py-2 rounded-xl text-sm font-bold hover:bg-slate-50 dark:hover:bg-[#2a2b2e] shadow-sm transition-all focus:outline-none focus:ring-2 focus:ring-slate-200 dark:focus:ring-slate-700">
                    Export <i class="fa-solid fa-cloud-arrow-down ml-1"></i>
                </button>

                <!-- Menú Flotante de Exportación -->
                <div id="exportMenu" class="hidden absolute right-0 mt-2 w-48 bg-white dark:bg-[#323338] border border-slate-200 dark:border-[#4a4b50] rounded-2xl shadow-xl z-50 overflow-hidden py-1.5 transform origin-top-right transition-all">
                    <!-- Exportar CSV -->
                    <a href="{{ route('reports.export', array_merge(request()->all(), ['format' => 'csv'])) }}" class="flex items-center px-4 py-2.5 text-sm font-bold text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-[#2a2b2e] hover:text-emerald-600 dark:hover:text-emerald-400 transition-colors group">
                        <div class="w-8 h-8 rounded-lg bg-emerald-50 dark:bg-emerald-500/10 text-emerald-500 flex items-center justify-center mr-3 group-hover:scale-110 transition-transform">
                            <i class="fa-solid fa-file-csv"></i>
                        </div>
                        Excel CSV
                    </a>
                    
                    <!-- Exportar PDF -->
                    <a href="{{ route('reports.export', array_merge(request()->all(), ['format' => 'pdf'])) }}" class="flex items-center px-4 py-2.5 text-sm font-bold text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-[#2a2b2e] hover:text-red-500 transition-colors group">
                        <div class="w-8 h-8 rounded-lg bg-red-50 dark:bg-red-500/10 text-red-500 flex items-center justify-center mr-3 group-hover:scale-110 transition-transform">
                            <i class="fa-solid fa-file-pdf"></i>
                        </div>
                        Archivo PDF
                    </a>
                </div>
            </div>

        </div>
    </div>

    <!-- 🚀 Tarjeta de la Tabla Rediseñada (4 Columnas) -->
    <div class="bg-white dark:bg-darkbg-card rounded-2xl shadow-soft dark:shadow-none border border-slate-200 dark:border-darkbg-border overflow-hidden animate-fade-up delay-100">
        <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse table-fixed min-w-[800px]">
                <thead class="bg-slate-50 dark:bg-[#2a2b2e] text-slate-500 dark:text-slate-400 text-xs uppercase font-semibold border-b border-slate-100 dark:border-darkbg-border">
                    <tr>
                        <th class="p-5 w-[15%]">Fecha y Hora</th>
                        <th class="p-5 w-[20%]">Usuario / IP</th>
                        <th class="p-5 w-[45%]">Documento Afectado</th>
                        <th class="p-5 w-[20%]">Acción Registrada</th>
                    </tr>
                </thead>
                <tbody class="divide-y divide-slate-100 dark:divide-darkbg-border text-sm">
                    @forelse($historial as $log)
                        <tr class="hover:bg-slate-50 dark:hover:bg-[#2a2b2e] transition-colors">
                            
                            <td class="p-5 align-top">
                                <div class="font-bold text-slate-700 dark:text-slate-300">{{ $log->created_at->format('d M Y') }}</div>
                                <div class="text-xs text-slate-400">{{ $log->created_at->format('H:i:s') }} hrs</div>
                            </td>
                            
                            <td class="p-5 align-top">
                                <p class="text-sm font-bold text-slate-800 dark:text-white truncate" title="{{ $log->user_name }}">{{ $log->user_name }}</p>
                                <p class="text-[11px] text-slate-400 dark:text-slate-500 font-mono mt-0.5 flex items-center">
                                    <i class="fa-solid fa-desktop mr-1.5 opacity-70"></i> <span class="truncate">{{ $log->ip_address }}</span>
                                </p>
                            </td>
                            
                            <td class="p-5 align-top">
                                @if($log->document_code == 'DASHBOARD_VIEW')
                                    <span class="text-xs text-slate-400 dark:text-slate-500 font-medium italic">-- Sistema Central --</span>
                                @else
                                    <div class="flex flex-col items-start gap-1.5 overflow-hidden">
                                        <span class="text-xs font-black text-brand bg-brand/5 dark:bg-brand/10 border border-brand/20 dark:border-brand/20 px-2 py-0.5 rounded truncate max-w-full" title="{{ $log->document_code }}">{{ $log->document_code }}</span>
                                        
                                        @if(str_contains($log->document_title, '[REPORTE DE INCIDENCIA]'))
                                            <span class="text-[11px] text-slate-400 dark:text-slate-500 font-medium truncate max-w-full">Revisión de calidad / Incidencia</span>
                                        @else
                                            <span class="text-[11px] text-slate-500 dark:text-slate-400 font-medium truncate max-w-full" title="{{ str_replace('[FIRMA DE ENTERADO] ', '', $log->document_title) }}">
                                                v{{ $log->version_num }} - {{ str_replace('[FIRMA DE ENTERADO] ', '', $log->document_title) }}
                                            </span>
                                        @endif
                                    </div>
                                @endif
                            </td>
                            
                            <td class="p-5 align-top">
                                @if($log->document_code == 'DASHBOARD_VIEW')
                                    <span class="inline-flex items-center px-2 py-1 rounded-md text-[11px] font-bold bg-slate-100 dark:bg-[#323338] text-slate-600 dark:text-slate-300 border border-slate-200 dark:border-darkbg-border shadow-sm">
                                        <i class="fa-solid fa-right-to-bracket mr-1.5 opacity-70"></i> Ingreso al Portal
                                    </span>

                                @elseif(str_contains($log->document_title, '[REPORTE DE INCIDENCIA]'))
                                    <div class="flex flex-col items-start gap-1.5 overflow-hidden w-full">
                                        <span class="inline-flex items-center px-2 py-1 rounded-md text-[11px] font-bold bg-amber-50 dark:bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-200/50 dark:border-amber-500/20 shadow-sm shrink-0">
                                            <i class="fa-solid fa-flag mr-1.5 opacity-80"></i> Reporte de Error
                                        </span>
                                        <div class="flex items-center text-[11px] text-slate-500 dark:text-slate-400 font-medium w-full" title="{{ str_replace('[REPORTE DE INCIDENCIA] ', '', $log->document_title) }}">
                                            <div class="w-1 h-1 rounded-full bg-slate-300 dark:bg-slate-600 mr-1.5 shrink-0"></div>
                                            <span class="truncate">{{ str_replace('[REPORTE DE INCIDENCIA] ', '', $log->document_title) }}</span>
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
                        </tr>
                    @empty
                        <tr>
                            <td colspan="4" class="px-6 py-16 text-center">
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

        @if($historial->hasPages())
            <div class="bg-slate-50 dark:bg-[#2a2b2e] px-5 py-4 flex flex-col sm:flex-row items-center justify-between transition-colors duration-300 border-t border-slate-200 dark:border-darkbg-border">
                <div class="text-sm font-medium text-slate-500 dark:text-slate-400 mb-4 sm:mb-0">
                    Mostrando página <span class="font-bold text-slate-800 dark:text-white">{{ $historial->currentPage() }}</span> de <span class="font-bold text-slate-800 dark:text-white">{{ $historial->lastPage() }}</span>
                    <span class="ml-1 text-xs hidden md:inline">({{ $historial->total() }} registros en total)</span>
                </div>
                
                <div class="flex space-x-2">
                    @if ($historial->onFirstPage())
                        <span class="px-4 py-2 text-xs font-bold rounded-lg border shadow-sm opacity-50 cursor-not-allowed bg-slate-100 dark:bg-[#2a2b2e] border-slate-200 dark:border-darkbg-border text-slate-400 flex items-center">
                            <i class="fa-solid fa-chevron-left mr-1"></i> Anterior
                        </span>
                    @else
                        <a href="{{ $historial->previousPageUrl() }}" class="px-4 py-2 text-xs font-bold rounded-lg border transition-colors shadow-sm bg-white dark:bg-[#323338] border-slate-300 dark:border-darkbg-border text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-[#3a3b40] hover:text-brand flex items-center">
                            <i class="fa-solid fa-chevron-left mr-1"></i> Anterior
                        </a>
                    @endif

                    @if (!$historial->hasMorePages())
                        <span class="px-4 py-2 text-xs font-bold rounded-lg border shadow-sm opacity-50 cursor-not-allowed bg-slate-100 dark:bg-[#2a2b2e] border-slate-200 dark:border-darkbg-border text-slate-400 flex items-center">
                            Siguiente <i class="fa-solid fa-chevron-right ml-1"></i>
                        </span>
                    @else
                        <a href="{{ $historial->nextPageUrl() }}" class="px-4 py-2 text-xs font-bold rounded-lg border transition-colors shadow-sm bg-white dark:bg-[#323338] border-slate-300 dark:border-darkbg-border text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-[#3a3b40] hover:text-brand flex items-center">
                            Siguiente <i class="fa-solid fa-chevron-right ml-1"></i>
                        </a>
                    @endif
                </div>
            </div>
        @endif
    </div>
</div>
@endsection

@section('scripts')
<script>
    document.addEventListener('DOMContentLoaded', function() {
        // Elementos del Filtro
        const btnFilterToggle = document.getElementById('btnFilterToggle');
        const filterMenu = document.getElementById('filterMenu');
        const rangoSelect = document.getElementById('rangoSelect');
        const customDateGroup = document.getElementById('customDateGroup');

        // Elementos de Exportar
        const btnExportToggle = document.getElementById('btnExportToggle');
        const exportMenu = document.getElementById('exportMenu');

        // Función para mostrar fechas custom
        rangoSelect.addEventListener('change', (e) => {
            if(e.target.value === 'custom') {
                customDateGroup.classList.remove('hidden');
            } else {
                customDateGroup.classList.add('hidden');
                document.getElementById('fechaInicio').value = '';
                document.getElementById('fechaFin').value = '';
            }
        });

        // Toggle Menú Filtro
        btnFilterToggle.addEventListener('click', (e) => {
            e.stopPropagation();
            exportMenu.classList.add('hidden'); // Cierra el otro
            filterMenu.classList.toggle('hidden');
        });

        // Toggle Menú Exportar
        btnExportToggle.addEventListener('click', (e) => {
            e.stopPropagation();
            filterMenu.classList.add('hidden'); // Cierra el otro
            exportMenu.classList.toggle('hidden');
        });

        // Cerrar menús al hacer clic fuera
        document.addEventListener('click', (e) => {
            if (!filterMenu.contains(e.target) && !btnFilterToggle.contains(e.target)) {
                filterMenu.classList.add('hidden');
            }
            if (!exportMenu.contains(e.target) && !btnExportToggle.contains(e.target)) {
                exportMenu.classList.add('hidden');
            }
        });
        
        // Evitar que el clic DENTRO del menú lo cierre
        filterMenu.addEventListener('click', (e) => e.stopPropagation());
    });
</script>
@endsection