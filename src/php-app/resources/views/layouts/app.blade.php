<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>@yield('title', 'QualityDoc') | Sistema Operativo</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">
    <script>
        // Inyectamos una paleta de colores personalizada para el Sidebar
        tailwind.config = {
            theme: {
                extend: {
                    colors: {
                        'qd-dark': '#141517',
                        'qd-darker': '#2a2b2e',
                    }
                }
            }
        }
    </script>
</head>
<body class="bg-gray-50 text-gray-800 font-sans antialiased flex h-screen overflow-hidden">

    <aside class="w-64 bg-qd-dark text-white flex flex-col shadow-2xl transition-all duration-300 z-20">
        <div class="h-16 flex items-center px-6 border-b border-gray-800 bg-qd-darker">
            <i class="fa-solid fa-layer-group text-2xl mr-3 text-indigo-400"></i>
            <span class="font-bold text-xl tracking-wider">Quality<span class="text-indigo-400">Doc</span></span>
        </div>

        <nav class="flex-1 px-4 py-6 space-y-2 overflow-y-auto">
             <p class="text-xs font-bold text-gray-500 uppercase tracking-wider mb-4 px-2">Navegación</p>

            @php
                $rolActual = session('role');
            @endphp

            @if(in_array($rolActual, ['Administrador', 'Auditor']))
                <a href="{{ route('reports') }}" class="{{ request()->routeIs('reports') ? 'bg-qd-darker border-l-4 border-indigo-500 text-white' : 'text-gray-400 hover:bg-qd-darker hover:text-white border-l-4 border-transparent' }} flex items-center px-4 py-3 rounded-r-lg shadow-sm transition">
                    <i class="fa-solid fa-chart-pie w-6"></i>
                    <span class="font-bold">Panel Gerencial</span>
                 </a>
                
                <a href="{{ route('reports.history') }}" class="{{ request()->routeIs('reports.history') ? 'bg-qd-darker border-l-4 border-indigo-500 text-white' : 'text-gray-400 hover:bg-qd-darker hover:text-white border-l-4 border-transparent' }} flex items-center px-4 py-3 rounded-r-lg shadow-sm transition">
                    <i class="fa-solid fa-list-check w-6"></i>
                    <span class="font-medium">Auditoría Detallada</span>
                </a>
            @endif

            @if(in_array($rolActual, ['Operario', 'Lector', 'Administrador', 'Auditor']))
                <a href="{{ route('dashboard') }}" class="{{ request()->routeIs('dashboard') ? 'bg-qd-darker border-l-4 border-indigo-500 text-white' : 'text-gray-400 hover:bg-qd-darker hover:text-white border-l-4 border-transparent' }} flex items-center px-4 py-3 rounded-r-lg shadow-sm transition">
                    <i class="fa-solid fa-folder-open w-6"></i>
                    <span class="font-medium">Directorio Vigente</span>
                </a>

                @if($rolActual !== 'Auditor')
    <a href="{{ route('dashboard.compliances') }}" class="{{ request()->routeIs('dashboard.compliances') ? 'bg-qd-darker border-l-4 border-indigo-500 text-white' : 'text-gray-400 hover:bg-qd-darker hover:text-white border-l-4 border-transparent' }} flex items-center px-4 py-3 rounded-r-lg shadow-sm transition">
        <i class="fa-solid fa-file-signature w-6"></i>
        <span class="font-medium">Mis Cumplimientos</span>
    </a>
@endif
            @endif
        </nav>

        <div class="p-4 bg-qd-darker border-t border-gray-800">
            <div class="flex items-center mb-4">
                <div class="w-10 h-10 rounded-full bg-indigo-600 border border-indigo-400 flex items-center justify-center font-bold shadow-lg">
                    {{ substr(session('name', 'U'), 0, 1) }}
                </div>
                
                <div class="ml-3 overflow-hidden">
                    <p class="text-sm font-bold text-white truncate">{{ session('name') }}</p>
                    <p class="text-xs text-indigo-400 font-medium truncate">{{ session('role') }}</p>
                </div>
            </div>
            <a href="{{ route('logout') }}" class="flex items-center justify-center w-full py-2 px-4 bg-red-500/10 hover:bg-red-600 text-red-500 hover:text-white border border-red-500/50 hover:border-red-600 text-sm font-bold rounded transition">
                <i class="fa-solid fa-right-from-bracket mr-2"></i> Salir del Sistema
            </a>
        </div>
    </aside>

    <div class="flex-1 flex flex-col h-screen overflow-hidden bg-gray-50">
        
        <header class="h-16 bg-white shadow-sm border-b border-gray-200 flex items-center justify-between px-8 z-10 shrink-0">
             <h2 class="text-xl font-black text-gray-800">@yield('header_title', 'Portal Operativo')</h2>
            
            <div class="flex items-center space-x-3">
                <span class="hidden sm:inline-flex items-center bg-gray-100 text-gray-600 text-sm font-bold px-4 py-2 rounded-lg border border-gray-200 shadow-sm">
                    <i class="fa-solid fa-building mr-2 text-indigo-500"></i> {{ session('company_name') }}
                 </span>
            </div>
        </header>

        <main class="flex-1 overflow-y-auto p-6 md:p-8">
            @yield('content')
        </main>
        
    </div>

</body>
</html>