@extends('layouts.app')

@section('title', 'Mis Cumplimientos')
@section('header_title', 'Mis Cumplimientos y Acuses')

@section('content')
    <div class="mb-6">
        <p class="text-gray-500">Historial personal de documentos y normativas operativas firmadas de enterado en planta.</p>
    </div>

    <div class="bg-white rounded-xl shadow-sm overflow-hidden border border-gray-100 flex flex-col">
        <div class="bg-gray-50 px-6 py-4 border-b border-gray-100 flex justify-between items-center">
            <div class="flex items-center">
                <h3 class="font-bold text-gray-700 text-sm uppercase tracking-wider">
                    <i class="fa-solid fa-file-signature text-indigo-500 mr-2"></i>Mis Firmas Registradas
                </h3>
            </div>
        </div>
        
        <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-100">
                <thead class="bg-gray-50">
                    <tr>
                        <th class="px-6 py-4 text-left text-xs font-black text-gray-400 uppercase tracking-wider">Fecha y Hora</th>
                        <th class="px-6 py-4 text-left text-xs font-black text-gray-400 uppercase tracking-wider">Código</th>
                        <th class="px-6 py-4 text-left text-xs font-black text-gray-400 uppercase tracking-wider">Documento / Procedimiento</th>
                        <th class="px-6 py-4 text-left text-xs font-black text-gray-400 uppercase tracking-wider">Versión</th>
                        <th class="px-6 py-4 text-left text-xs font-black text-gray-400 uppercase tracking-wider">Estado de Cumplimiento</th>
                    </tr>
                </thead>
                <tbody class="bg-white divide-y divide-gray-50">
                    @forelse($cumplimientos as $item)
                        <tr class="hover:bg-indigo-50/50 transition">
                            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-500">
                                {{ $item->created_at->format('d/m/Y H:i') }} hs
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm font-black text-gray-800">
                                {{ $item->document_code }}
                            </td>
                            <td class="px-6 py-4 text-sm font-medium text-gray-700">
                                {{ str_replace('[FIRMA DE ENTERADO] ', '', $item->document_title) }}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-500">
                                v{{ $item->version_num }}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap">
                                <span class="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-bold bg-green-50 text-green-700 border border-green-100 shadow-sm">
                                    <i class="fa-solid fa-circle-check mr-1.5"></i> Conforme de Enterado
                                </span>
                            </td>
                        </tr>
                    @empty
                        <tr>
                            <td colspan="5" class="px-6 py-16 text-center text-gray-400 text-sm">
                                <i class="fa-solid fa-signature text-5xl mb-3 block text-gray-200"></i>
                                Aún no cuentas con registros de acuses firmados en el sistema.
                            </td>
                        </tr>
                    @endforelse
                </tbody>
            </table>
        </div>

        @if($cumplimientos->hasPages())
            <div class="px-6 py-4 bg-gray-50 border-t border-gray-100">
                {{ $cumplimientos->links() }}
            </div>
        @endif
    </div>
@endsection