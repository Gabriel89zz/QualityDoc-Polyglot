<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Portal Operativo | QualityDoc</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">
</head>
<body class="bg-gray-50 min-h-screen">
    <nav class="bg-indigo-900 text-white shadow-lg border-b-4 border-indigo-500">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div class="flex justify-between h-16 items-center">
                <div class="flex items-center">
                    <i class="fa-solid fa-book-open-reader text-2xl mr-3 text-indigo-400"></i>
                    <span class="font-bold text-xl tracking-wider">QualityDoc <span class="text-indigo-400">Operativos</span></span>
                </div>
                <div class="flex items-center space-x-6">
                    <div class="text-right hidden md:block">
                        <p class="text-sm font-bold text-indigo-100">{{ $userName }}</p>
                        <p class="text-xs text-indigo-300"><i class="fa-solid fa-user-helmet mr-1"></i> {{ $userRole }}</p>
                    </div>
                    <a href="{{ route('logout') }}" class="bg-red-500 hover:bg-red-600 px-4 py-2 rounded text-sm font-bold shadow transition">
                        Salir <i class="fa-solid fa-right-from-bracket ml-1"></i>
                    </a>
                </div>
            </div>
        </div>
    </nav>

    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
        <div class="mb-8 flex justify-between items-end">
            <div>
                <h1 class="text-3xl font-black text-gray-800">Documentos Vigentes</h1>
                <p class="text-gray-500 mt-1">Directorio de normativas y procedimientos aprobados para planta.</p>
            </div>
            <div class="hidden sm:block text-indigo-600 bg-indigo-100 px-4 py-2 rounded-lg font-bold">
                <i class="fa-solid fa-database mr-2"></i> MongoDB Engine
            </div>
        </div>

        @if($errorApi)
            <div class="bg-red-50 border-l-4 border-red-500 text-red-700 p-4 mb-6 rounded shadow-sm">
                <p class="font-bold"><i class="fa-solid fa-triangle-exclamation mr-2"></i>Error de Conexión</p>
                <p>{{ $errorApi }}</p>
            </div>
        @endif

        <div class="bg-white rounded-xl shadow-md overflow-hidden border border-gray-200">
            <table class="min-w-full divide-y divide-gray-200">
                <thead class="bg-gray-100">
                    <tr>
                        <th class="px-6 py-4 text-left text-xs font-bold text-gray-500 uppercase tracking-wider">Código</th>
                        <th class="px-6 py-4 text-left text-xs font-bold text-gray-500 uppercase tracking-wider">Título del Documento</th>
                        <th class="px-6 py-4 text-left text-xs font-bold text-gray-500 uppercase tracking-wider">Metadatos (Búsqueda Rápida)</th>
                        <th class="px-6 py-4 text-center text-xs font-bold text-gray-500 uppercase tracking-wider">Archivo</th>
                    </tr>
                </thead>
                <tbody class="bg-white divide-y divide-gray-100">
                    @forelse($documentos as $doc)
                        <tr class="hover:bg-indigo-50 transition duration-150">
                            <td class="px-6 py-4 whitespace-nowrap">
                                <span class="text-sm font-black text-indigo-700 bg-indigo-100 px-2 py-1 rounded">{{ $doc['codigo'] }}</span>
                                <span class="text-gray-400 font-bold text-xs ml-2">v{{ $doc['version'] }}</span>
                            </td>
                            <td class="px-6 py-4">
                                <div class="text-sm text-gray-900 font-bold">{{ $doc['titulo'] }}</div>
                                <div class="text-xs text-gray-400 mt-1"><i class="fa-solid fa-check-double text-green-500 mr-1"></i> Aprobado por: {{ $doc['aprobado_por'] }}</div>
                            </td>
                            <td class="px-6 py-4">
                                <div class="flex flex-wrap gap-2">
                                    @foreach($doc['etiquetas'] as $etiqueta)
                                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-50 text-blue-700 border border-blue-200 shadow-sm">
                                            {{ $etiqueta }}
                                        </span>
                                    @endforeach
                                </div>
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-center">
                                <a href="{{ route('log.document', ['codigo' => $doc['codigo'], 'titulo' => $doc['titulo'], 'version' => $doc['version'], 'url' => $doc['url_archivo']]) }}" target="_blank" class="inline-flex items-center text-indigo-600 hover:text-white bg-indigo-50 hover:bg-indigo-600 border border-indigo-200 px-4 py-2 rounded-lg font-bold transition">
    <i class="fa-solid fa-file-pdf mr-2 text-red-500"></i> Abrir PDF
</a>
                            </td>
                        </tr>
                    @empty
                        <tr>
                            <td colspan="4" class="px-6 py-12 text-center">
                                <div class="flex flex-col items-center">
                                    <i class="fa-solid fa-folder-open text-5xl mb-4 text-gray-300"></i>
                                    <h3 class="text-lg font-bold text-gray-700">Sin documentos vigentes</h3>
                                    <p class="text-gray-500 text-sm">No hay normativas aprobadas en la base de datos.</p>
                                </div>
                            </td>
                        </tr>
                    @endforelse
                </tbody>
            </table>
        </div>
    </main>
</body>
</html>