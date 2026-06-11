@extends('layouts.app')

@section('title', 'Mis Cumplimientos')

@section('content')
<div class="w-full">
    
    <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 mb-8 animate-fade-up">
        <div>
            <h1 class="text-2xl font-bold text-slate-800 dark:text-white">Mis Cumplimientos y Acuses</h1>
            <p class="text-slate-500 dark:text-slate-400 text-sm mt-1">Historial personal de normativas operativas firmadas de enterado en planta.</p>
        </div>
        
        <div class="flex items-center">
            <form method="GET" action="{{ route('dashboard.compliances') }}" id="filterForm" class="flex items-center bg-white dark:bg-[#2a2b2e] border border-slate-200 dark:border-darkbg-border rounded-lg shadow-sm h-10 text-sm transition-colors">
                
                <div class="relative h-full flex items-center">
                    <select name="rango" id="rangoSelect" class="bg-transparent border-none focus:ring-0 text-slate-600 dark:text-slate-300 py-0 pl-3 pr-8 h-full cursor-pointer font-medium appearance-none outline-none text-xs sm:text-sm">
                        <option value="all" {{ $rango == 'all' ? 'selected' : '' }}>Histórico completo</option>
                        <option value="7days" {{ $rango == '7days' ? 'selected' : '' }}>Últimos 7 días</option>
                        <option value="30days" {{ $rango == '30days' ? 'selected' : '' }}>Últimos 30 días</option>
                        <option value="custom" {{ $rango == 'custom' ? 'selected' : '' }}>Personalizado</option>
                    </select>
                    <i class="fa-solid fa-chevron-down absolute right-3 text-[10px] text-slate-400 pointer-events-none"></i>
                </div>

                <div class="h-6 w-px bg-slate-200 dark:bg-darkbg-border"></div>

                <div class="flex items-center px-3 h-full">
                    <i class="fa-regular fa-calendar text-slate-400 dark:text-slate-500 mr-2"></i>
                    <input type="date" name="fecha_inicio" id="fechaInicio" value="{{ $fechaInicio ?? '' }}" class="bg-transparent border-none focus:ring-0 text-slate-600 dark:text-slate-300 p-0 text-xs sm:text-sm outline-none cursor-pointer">
                    <span class="mx-2 text-slate-300 dark:text-slate-500">-</span>
                    <input type="date" name="fecha_fin" id="fechaFin" value="{{ $fechaFin ?? '' }}" class="bg-transparent border-none focus:ring-0 text-slate-600 dark:text-slate-300 p-0 text-xs sm:text-sm outline-none cursor-pointer">
                </div>
            </form>

            @if($rango != 'all' || !empty($fechaInicio) || !empty($fechaFin))
                <a href="{{ route('dashboard.compliances') }}" class="ml-2 h-10 w-10 flex items-center justify-center bg-white dark:bg-[#2a2b2e] border border-slate-200 dark:border-darkbg-border rounded-lg text-slate-400 hover:text-red-500 transition-colors shadow-sm" title="Limpiar filtros">
                    <i class="fa-solid fa-xmark"></i>
                </a>
            @endif
        </div>
    </div>

    <div class="bg-white dark:bg-darkbg-card rounded-2xl shadow-soft dark:shadow-none border border-slate-200 dark:border-darkbg-border overflow-hidden animate-fade-up delay-100">
        
        <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse min-w-full">
                <thead class="bg-slate-50 dark:bg-[#2a2b2e] text-slate-500 dark:text-slate-400 text-xs uppercase font-semibold border-b border-slate-100 dark:border-darkbg-border">
                    <tr>
                        <th class="p-5">Fecha y Hora</th>
                        <th class="p-5">Código</th>
                        <th class="p-5">Documento / Procedimiento</th>
                        <th class="p-5">Versión</th>
                        <th class="p-5 text-center">Estado de Cumplimiento</th>
                    </tr>
                </thead>
                <tbody class="divide-y divide-slate-100 dark:divide-darkbg-border text-sm">
                    @forelse($cumplimientos as $item)
                        <tr class="hover:bg-slate-50 dark:hover:bg-[#2a2b2e] transition-colors">
                            
                            <td class="p-5 whitespace-nowrap">
                                <div class="font-bold text-slate-700 dark:text-slate-300">
                                    {{ $item->created_at->format('d M Y') }}
                                </div>
                                <div class="text-xs text-slate-400">
                                    {{ $item->created_at->format('H:i') }} hrs
                                </div>
                            </td>
                            
                            <td class="p-5 whitespace-nowrap">
                                <span class="text-sm font-black text-brand bg-brand/10 border border-brand/20 px-2.5 py-1 rounded-lg">{{ $item->document_code }}</span>
                            </td>
                            
                            <td class="p-5">
                                <div class="text-sm text-slate-800 dark:text-white font-bold">
                                    {{ str_replace('[FIRMA DE ENTERADO] ', '', $item->document_title) }}
                                </div>
                            </td>
                            
                            <td class="p-5 whitespace-nowrap">
                                <span class="text-slate-500 dark:text-slate-400 font-bold text-xs">v{{ $item->version_num }}</span>
                            </td>
                            
                            <td class="p-5 whitespace-nowrap text-center">
                                <span class="inline-flex items-center px-2.5 py-1 rounded-md text-[11px] font-bold bg-emerald-50 dark:bg-emerald-900/20 text-emerald-600 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-800/30 shadow-sm">
                                    <i class="fa-solid fa-circle-check mr-1.5"></i> Conforme de Enterado
                                </span>
                            </td>
                        </tr>
                    @empty
                        <tr>
                            <td colspan="5" class="px-6 py-16 text-center">
                                <div class="w-20 h-20 mx-auto bg-slate-50 dark:bg-[#2a2b2e] rounded-full flex items-center justify-center mb-4 border border-slate-100 dark:border-darkbg-border">
                                    <i class="fa-solid fa-file-signature text-4xl text-slate-300 dark:text-slate-600"></i>
                                </div>
                                @if(!empty($fechaInicio) || !empty($fechaFin))
                                    <h3 class="text-lg font-bold text-slate-700 dark:text-slate-300">Sin resultados en estas fechas</h3>
                                    <p class="text-slate-500 text-sm mt-1">Intenta ampliar el rango de búsqueda.</p>
                                @else
                                    <h3 class="text-lg font-bold text-slate-700 dark:text-slate-300">Sin firmas registradas</h3>
                                    <p class="text-slate-500 text-sm mt-1">Aún no cuentas con registros de acuses firmados en el sistema.</p>
                                @endif
                            </td>
                        </tr>
                    @endforelse
                </tbody>
            </table>
        </div>

        @if($cumplimientos->hasPages())
    <div class="bg-slate-50 dark:bg-[#2a2b2e] px-5 py-4 border-t border-slate-200 dark:border-darkbg-border flex flex-col sm:flex-row items-center justify-between transition-colors duration-300">
        <div class="text-sm font-medium text-slate-500 dark:text-slate-400 mb-4 sm:mb-0">
            Página <span class="font-bold text-slate-800 dark:text-white">{{ $cumplimientos->currentPage() }}</span> de <span class="font-bold text-slate-800 dark:text-white">{{ $cumplimientos->lastPage() }}</span>
        </div>
        
        <div class="flex space-x-2">
            {{-- Botón Anterior --}}
            <a href="{{ $cumplimientos->previousPageUrl() }}" 
               class="px-4 py-2 text-xs font-bold rounded-lg border transition-colors shadow-sm {{ $cumplimientos->onFirstPage() ? 'opacity-50 cursor-not-allowed bg-slate-100 dark:bg-[#2a2b2e] border-slate-200 dark:border-darkbg-border text-slate-400' : 'bg-white dark:bg-[#323338] border-slate-300 dark:border-darkbg-border text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-[#3a3b40] hover:text-brand' }}">
                <i class="fa-solid fa-chevron-left mr-1"></i> Anterior
            </a>

            {{-- Botón Siguiente --}}
            <a href="{{ $cumplimientos->nextPageUrl() }}" 
               class="px-4 py-2 text-xs font-bold rounded-lg border transition-colors shadow-sm {{ !$cumplimientos->hasMorePages() ? 'opacity-50 cursor-not-allowed bg-slate-100 dark:bg-[#2a2b2e] border-slate-200 dark:border-darkbg-border text-slate-400' : 'bg-white dark:bg-[#323338] border-slate-300 dark:border-darkbg-border text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-[#3a3b40] hover:text-brand' }}">
                Siguiente <i class="fa-solid fa-chevron-right ml-1"></i>
            </a>
        </div>
    </div>
@endif
    </div>
</div>
@endsection
@section('scripts')
<script>
    document.addEventListener('DOMContentLoaded', function() {
        const form = document.getElementById('filterForm');
        const rangoSelect = document.getElementById('rangoSelect');
        const fechaInicio = document.getElementById('fechaInicio');
        const fechaFin = document.getElementById('fechaFin');

        // 1. Si el usuario cambia el selector de rango (ej. "Últimos 7 días")
        rangoSelect.addEventListener('change', () => {
            if(rangoSelect.value !== 'custom') {
                // Limpiamos las fechas manuales si elige un rango predefinido
                fechaInicio.value = '';
                fechaFin.value = '';
            }
            form.submit(); // Dispara la búsqueda automáticamente
        });

        // 2. Si el usuario mueve las fechas manualmente
        const handleDateChange = () => {
            // Cambiamos el select a "Personalizado" silenciosamente
            rangoSelect.value = 'custom';
            // Solo disparamos la búsqueda si ambas fechas están llenas
            if(fechaInicio.value && fechaFin.value) {
                form.submit(); 
            }
        };

        fechaInicio.addEventListener('change', handleDateChange);
        fechaFin.addEventListener('change', handleDateChange);
    });
</script>
@endsection