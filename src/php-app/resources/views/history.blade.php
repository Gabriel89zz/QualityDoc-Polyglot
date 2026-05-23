@extends('layouts.app')

@section('title', 'Auditoría Detallada')
@section('header_title', 'Registro Extendido de Trazabilidad')

@section('content')
    <div class="mb-6 flex flex-col sm:flex-row justify-between sm:items-end gap-4">
        <div>
            <p class="text-gray-500">Historial completo de accesos, lecturas y firmas de la planta.</p>
        </div>
        
        <a href="{{ route('reports.export') }}" class="inline-flex items-center bg-green-600 hover:bg-green-700 text-white px-5 py-2.5 rounded-lg font-bold transition shadow-sm border border-green-700">
            <i class="fa-solid fa-file-excel mr-2 text-xl"></i> Exportar a Excel (CSV)
        </a>
    </div>

    <div class="bg-white rounded-xl shadow-sm overflow-hidden border border-gray-100">
        <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-100">
                <thead class="bg-gray-50">
                    <tr>
                        <th class="px-6 py-4 text-left text-xs font-black text-gray-400 uppercase tracking-wider">Fecha y Hora</th>
                        <th class="px-6 py-4 text-left text-xs font-black text-gray-400 uppercase tracking-wider">Usuario / IP</th>
                        <th class="px-6 py-4 text-left text-xs font-black text-gray-400 uppercase tracking-wider">Acción Registrada</th>
                    </tr>
                </thead>
                <tbody class="bg-white divide-y divide-gray-50">
                    @forelse($historial as $log)
                        <tr class="hover:bg-indigo-50/50 transition">
                            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-500">
                                {{ $log->created_at->format('d/m/Y H:i:s') }}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap">
                                <p class="text-sm font-bold text-gray-800">{{ $log->user_name }}</p>
                                <p class="text-xs text-gray-400 font-mono">{{ $log->user_role }} | {{ $log->ip_address }}</p>
                            </td>
                            <td class="px-6 py-4">
    @if($log->document_code == 'DASHBOARD_VIEW')
        <div class="flex items-center gap-2 mt-1">
            <span class="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-bold bg-blue-50 text-blue-700 border border-blue-100 shadow-sm">
                <i class="fa-solid fa-right-to-bracket mr-1.5"></i> Inicio de Sesión
            </span>
            <span class="text-xs text-gray-500 font-medium">Acceso al portal operativo</span>
        </div>
    @elseif(str_contains($log->document_title, '[FIRMA DE ENTERADO]'))
        <div class="mb-1.5">
            <span class="text-sm font-black text-gray-800">{{ $log->document_code }}</span>
        </div>
        <div class="flex items-center gap-2">
            <span class="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-bold bg-green-50 text-green-700 border border-green-100 shadow-sm">
                <i class="fa-solid fa-file-signature mr-1.5"></i> Acuse de Lectura
            </span>
            <span class="text-xs text-gray-500 truncate max-w-xs font-medium" title="{{ str_replace('[FIRMA DE ENTERADO] ', '', $log->document_title) }}">
                v{{ $log->version_num }} - {{ str_replace('[FIRMA DE ENTERADO] ', '', $log->document_title) }}
            </span>
        </div>
    @else
        <div class="mb-1.5">
            <span class="text-sm font-black text-gray-800">{{ $log->document_code }}</span>
        </div>
        <div class="flex items-center gap-2">
            <span class="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-bold bg-indigo-50 text-indigo-700 border border-indigo-100 shadow-sm">
                <i class="fa-solid fa-eye mr-1.5"></i> Consulta
            </span>
            <span class="text-xs text-gray-500 truncate max-w-xs font-medium" title="{{ $log->document_title }}">
                v{{ $log->version_num }} - {{ $log->document_title }}
            </span>
        </div>
    @endif
</td>
                        </tr>
                    @empty
                        <tr>
                            <td colspan="3" class="px-6 py-12 text-center text-gray-400 text-sm">
                                <i class="fa-solid fa-database text-4xl mb-3 block text-gray-200"></i>
                                No hay registros de auditoría.
                            </td>
                        </tr>
                    @endforelse
                </tbody>
            </table>
        </div>
        
        <div class="px-6 py-4 border-t border-gray-100 bg-gray-50">
            {{ $historial->links('pagination::tailwind') }}
        </div>
    </div>
@endsection