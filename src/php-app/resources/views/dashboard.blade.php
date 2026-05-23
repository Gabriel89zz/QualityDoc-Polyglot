@extends('layouts.app')

@section('title', 'Directorio Operativo')
@section('header_title', 'Documentos Vigentes')

@section('content')
    <div class="mb-6">
        <p class="text-gray-500">Directorio de normativas y procedimientos aprobados para planta.</p>
    </div>

    <div class="mb-8 bg-white p-4 rounded-xl shadow-sm border border-gray-100">
        <form method="GET" action="{{ route('dashboard') }}" class="flex flex-col sm:flex-row gap-4">
            <div class="relative flex-grow">
                <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <i class="fa-solid fa-magnifying-glass text-gray-400"></i>
                </div>
                <input 
                    type="text" 
                    name="search" 
                    value="{{ $searchTerm ?? '' }}" 
                    class="block w-full pl-10 pr-3 py-3 border border-gray-200 rounded-lg focus:ring-indigo-500 focus:border-indigo-500 text-sm text-gray-900 placeholder-gray-400 transition" 
                    placeholder="Buscar por código, título o etiqueta (Ej. ISO, Calidad)..."
                >
            </div>
            <div class="flex gap-2">
                <button type="submit" class="bg-indigo-600 hover:bg-indigo-700 text-white px-6 py-3 rounded-lg font-bold shadow-sm transition flex items-center">
                    Buscar
                </button>
                @if(!empty($searchTerm))
                    <a href="{{ route('dashboard') }}" class="bg-gray-50 hover:bg-gray-100 text-gray-700 px-4 py-3 rounded-lg font-bold shadow-sm transition flex items-center border border-gray-200 text-sm">
                        <i class="fa-solid fa-xmark mr-1"></i> Limpiar
                    </a>
                @endif
            </div>
        </form>
    </div>

    @if($errorApi)
        <div class="bg-red-50 border-l-4 border-red-500 text-red-700 p-4 mb-6 rounded shadow-sm flex items-center">
            <i class="fa-solid fa-triangle-exclamation text-2xl mr-4 text-red-400"></i>
            <div>
                <p class="font-bold">Error de Conexión</p>
                <p class="text-sm">{{ $errorApi }}</p>
            </div>
        </div>
    @endif

    @if(session('success'))
        <div class="bg-green-50 border-l-4 border-green-500 text-green-700 p-4 mb-6 rounded shadow-sm flex items-center justify-between">
            <div>
                <p class="font-bold"><i class="fa-solid fa-circle-check mr-2"></i>Cumplimiento Registrado</p>
                <p class="text-sm">{{ session('success') }}</p>
            </div>
            <i class="fa-solid fa-file-signature text-3xl text-green-200"></i>
        </div>
    @endif

    <div class="bg-white rounded-xl shadow-sm overflow-hidden border border-gray-100">
        <table class="min-w-full divide-y divide-gray-100">
            <thead class="bg-gray-50">
                <tr>
                    <th class="px-6 py-4 text-left text-xs font-black text-gray-400 uppercase tracking-wider">Código</th>
                    <th class="px-6 py-4 text-left text-xs font-black text-gray-400 uppercase tracking-wider">Título del Documento</th>
                    <th class="px-6 py-4 text-left text-xs font-black text-gray-400 uppercase tracking-wider">Metadatos (Búsqueda)</th>
                    <th class="px-6 py-4 text-center text-xs font-black text-gray-400 uppercase tracking-wider">Acciones Operativas</th>
                </tr>
            </thead>
            <tbody class="bg-white divide-y divide-gray-50">
                @forelse($documentos as $doc)
                    <tr class="hover:bg-indigo-50/50 transition duration-150">
                        <td class="px-6 py-4 whitespace-nowrap">
                            <span class="text-sm font-black text-indigo-700 bg-indigo-50 border border-indigo-100 px-2 py-1 rounded">{{ $doc['codigo'] }}</span>
                            <span class="text-gray-400 font-bold text-xs ml-2">v{{ $doc['version'] }}</span>
                        </td>
                        <td class="px-6 py-4">
                            <div class="text-sm text-gray-800 font-bold">{{ $doc['titulo'] }}</div>
                            <div class="text-xs text-gray-400 mt-1"><i class="fa-solid fa-check-double text-green-500 mr-1"></i> Aprobado por: {{ $doc['aprobado_por'] }}</div>
                        </td>
                        <td class="px-6 py-4">
                            <div class="flex flex-wrap gap-2">
                                @foreach($doc['etiquetas'] as $etiqueta)
                                    <span class="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-bold bg-blue-50 text-blue-600 border border-blue-100 shadow-sm">
                                        {{ $etiqueta }}
                                    </span>
                                @endforeach
                            </div>
                        </td>
                        <td class="px-6 py-4 whitespace-nowrap text-center">
                            <div class="flex justify-center space-x-2 items-center">
                                
                                <a href="{{ route('log.document', ['codigo' => $doc['codigo'], 'titulo' => $doc['titulo'], 'version' => $doc['version'], 'url' => $doc['url_archivo']]) }}" 
                                   target="_blank" 
                                   onclick="desbloquearFirma('{{ $doc['codigo'] }}')"
                                   class="inline-flex items-center text-indigo-600 hover:text-white bg-indigo-50 hover:bg-indigo-600 border border-indigo-200 px-3 py-2 rounded-lg font-bold transition text-sm shadow-sm">
                                    <i class="fa-solid fa-file-pdf mr-1 text-red-500"></i> Ver
                                </a>
                                
                                <form action="{{ route('document.acuse') }}" method="POST" onsubmit="return confirm('¿Declaras formalmente haber leído y comprendido los lineamientos de este documento normativo?');">
                                    @csrf
                                    <input type="hidden" name="codigo" value="{{ $doc['codigo'] }}">
                                    <input type="hidden" name="titulo" value="{{ $doc['titulo'] }}">
                                    <input type="hidden" name="version" value="{{ $doc['version'] }}">
                                    
                                    <button type="submit" 
                                            id="btn-firma-{{ $doc['codigo'] }}" 
                                            disabled
                                            class="inline-flex items-center bg-gray-50 text-gray-400 border border-gray-200 px-3 py-2 rounded-lg font-bold text-sm shadow-sm cursor-not-allowed transition-all duration-300">
                                        <i class="fa-solid fa-lock mr-1" id="icono-firma-{{ $doc['codigo'] }}"></i> Firmar
                                    </button>
                                </form>

                            </div>
                        </td>
                    </tr>
                @empty
                    <tr>
                        <td colspan="4" class="px-6 py-12 text-center">
                            <div class="flex flex-col items-center">
                                <i class="fa-solid fa-folder-open text-5xl mb-4 text-gray-200"></i>
                                @if(!empty($searchTerm))
                                    <h3 class="text-lg font-bold text-gray-700">Sin resultados para "{{ $searchTerm }}"</h3>
                                    <p class="text-gray-500 text-sm mt-1">Intenta buscar con otra palabra clave o etiqueta.</p>
                                @else
                                    <h3 class="text-lg font-bold text-gray-700">Sin documentos vigentes</h3>
                                    <p class="text-gray-500 text-sm mt-1">No hay normativas aprobadas en la base de datos.</p>
                                @endif
                            </div>
                        </td>
                    </tr>
                @endforelse
            </tbody>
        </table>
    </div>

    <script>
        function desbloquearFirma(codigo) {
            const btnFirma = document.getElementById('btn-firma-' + codigo);
            const iconoFirma = document.getElementById('icono-firma-' + codigo);

            if (btnFirma) {
                // Desbloqueo y cambio a verde
                btnFirma.removeAttribute('disabled');
                btnFirma.className = "inline-flex items-center text-green-600 hover:text-white bg-green-50 hover:bg-green-600 border border-green-200 px-3 py-2 rounded-lg font-bold transition text-sm shadow-sm cursor-pointer";
                
                // Cambio de icono
                if (iconoFirma) {
                    iconoFirma.className = "fa-solid fa-pen-nib mr-1";
                }
            }
        }
    </script>
@endsection