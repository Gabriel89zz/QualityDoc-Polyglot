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
                <table class="min-w-full divide-y divide-gray-100">
                    <thead class="bg-white sticky top-0 shadow-sm z-10">
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
                                    {{ $log->created_at->format('d/m/Y H:i') }}
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
                                    <i class="fa-solid fa-clipboard-check text-4xl mb-3 block text-gray-200"></i>
                                    No hay registros de auditoría recientes.
                                </td>
                            </tr>
                        @endforelse
                    </tbody>
                </table>
            </div>
        </div>

    </div>
@endsection