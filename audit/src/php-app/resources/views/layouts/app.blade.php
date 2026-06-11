<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>@yield('title', 'QualityDoc') | Portal Operativo</title>
    
    <script>
        (function() {
            const theme = localStorage.getItem('theme');
            if (theme === 'dark' || (!theme && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
                document.documentElement.classList.add('dark');
            } else {
                document.documentElement.classList.remove('dark');
            }
        })();
    </script>

    <script src="https://cdn.tailwindcss.com"></script>
    <link href="https://fonts.googleapis.com/css2?family=Google+Sans+Flex:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
    
    <script>
        tailwind.config = {
            darkMode: 'class', 
            theme: {
                extend: {
                    fontFamily: {
                        sans: ['Google Sans Flex', 'sans-serif'], 
                    },
                    colors: {
                        brand: {
                            DEFAULT: '#8772FE', 
                            hover: '#755DF0', 
                        },
                        darkbg: {
                            base: '#2a2b2e',    
                            card: '#323338',    
                            border: '#4a4b50'   
                        }
                    },
                    boxShadow: {
                        'soft': '0 4px 20px -2px rgba(0, 0, 0, 0.05)',
                        'hover-soft': '0 10px 30px -4px rgba(0, 0, 0, 0.1)',
                    }
                }
            }
        }
    </script>

    <style>
        /* Variables y CSS Base */
        :root {
            --color-primary: #8772FE; 
            --color-primary-hover: #755DF0; 
            --color-bg-light: #FAFAFA; 
            --color-bg-dark: #2a2b2e; 
        }
        html.dark {
            --color-primary: #B8ACFF; 
            --color-primary-hover: #A594FF; 
        }
        body {
            background-color: var(--color-bg-light);
            transition: background-color 0.3s ease, color 0.3s ease;
        }
        html.dark body { background-color: var(--color-bg-dark); }
        
        /* Scrollbar */
        ::-webkit-scrollbar { width: 8px; height: 8px; }
        ::-webkit-scrollbar-track { background: transparent; }
        ::-webkit-scrollbar-thumb { background: #cbd5e1; border-radius: 10px; }
        html.dark ::-webkit-scrollbar-thumb { background: #4a4b50; }
        ::-webkit-scrollbar-thumb:hover { background: #94a3b8; }

        /* Animaciones */
        @keyframes fadeUp {
            from { opacity: 0; transform: translateY(15px); }
            to { opacity: 1; transform: translateY(0); }
        }
        .animate-fade-up { animation: fadeUp 0.6s cubic-bezier(0.16, 1, 0.3, 1) forwards; }
        .delay-100 { animation-delay: 100ms; opacity: 0; }
        .delay-200 { animation-delay: 200ms; opacity: 0; }

        .main-content { margin-left: 16rem; transition: all 0.3s ease-in-out; }
        @media (max-width: 768px) { .main-content { margin-left: 0; } }
    </style>
</head>
<body class="antialiased font-sans leading-normal tracking-normal text-slate-800 dark:text-slate-200 flex">

    <div class="md:hidden fixed w-full z-30 top-0 bg-white dark:bg-darkbg-card border-b border-slate-200 dark:border-darkbg-border py-3 px-4 flex justify-between items-center shadow-sm">
        <div class="flex items-center gap-3">
            <div class="w-8 h-8 rounded-lg bg-brand flex items-center justify-center">
                <i class="fa-solid fa-file-circle-check text-white text-xs"></i>
            </div>
            <span class="text-slate-900 dark:text-white font-bold text-lg tracking-tight">QualityDoc</span>
        </div>
        <button id="mobileMenuBtn" class="p-2 text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-darkbg-base rounded-lg transition-colors">
            <i class="fa-solid fa-bars text-xl"></i>
        </button>
    </div>

    <aside id="sidebar" class="fixed left-0 top-0 h-screen w-64 bg-white dark:bg-darkbg-card text-slate-600 dark:text-slate-300 pt-20 md:pt-0 border-r border-slate-200 dark:border-darkbg-border overflow-y-auto z-40 transition-transform duration-300 transform -translate-x-full md:translate-x-0 shadow-[4px_0_24px_rgba(0,0,0,0.02)] flex flex-col">
        
        <div class="hidden md:flex px-6 pt-6 pb-4 items-center border-b border-slate-100 dark:border-darkbg-border/50 mb-4">
            <div class="w-9 h-9 rounded-xl bg-brand flex items-center justify-center mr-3 shadow-md shadow-brand/20">
                <i class="fa-solid fa-file-circle-check text-white dark:text-darkbg-card text-sm"></i>
            </div>
            <div class="flex flex-col">
                <span class="text-slate-900 dark:text-white font-bold text-xl tracking-tight leading-none">QualityDoc</span>
                <span class="text-slate-500 dark:text-slate-400 text-[10px] font-semibold uppercase tracking-wider mt-0.5">{{ session('company_name', 'Portal Operativo') }}</span>
            </div>
        </div>
        
        <div class="flex-1 pb-4">
        
            <nav class="space-y-1 px-3">
                @php $rolActual = session('role'); @endphp


                @if(in_array($rolActual, ['Operario', 'Lector', 'Administrador', 'Auditor']))
                    
                    <a href="{{ route('dashboard') }}" class="{{ request()->routeIs('dashboard') ? 'flex items-center px-4 py-3 rounded-xl bg-brand text-white font-medium shadow-md shadow-brand/20 transition-all' : 'group flex items-center px-4 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-darkbg-base hover:text-slate-900 dark:hover:text-white transition-all duration-200' }}">
                        <i class="fa-solid fa-chart-line w-6 text-center {{ request()->routeIs('dashboard') ? 'opacity-90' : 'group-hover:text-brand' }}"></i>
                        <span class="ml-3">Dashboard</span>
                    </a>

                    <a href="{{ route('directorio') }}" class="{{ request()->routeIs('directorio') ? 'flex items-center px-4 py-3 rounded-xl bg-brand text-white font-medium shadow-md shadow-brand/20 transition-all' : 'group flex items-center px-4 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-darkbg-base hover:text-slate-900 dark:hover:text-white transition-all duration-200' }}">
                        <i class="fa-solid fa-folder-open w-6 text-center {{ request()->routeIs('directorio') ? 'opacity-90' : 'group-hover:text-brand' }}"></i>
                        <span class="ml-3">Directorio Vigente</span>
                    </a>

                    @if($rolActual !== 'Auditor')
                        <a href="{{ route('dashboard.compliances') }}" class="{{ request()->routeIs('dashboard.compliances') ? 'flex items-center px-4 py-3 rounded-xl bg-brand text-white font-medium shadow-md shadow-brand/20 transition-all' : 'group flex items-center px-4 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-darkbg-base hover:text-slate-900 dark:hover:text-white transition-all duration-200' }}">
                            <i class="fa-solid fa-file-signature w-6 text-center {{ request()->routeIs('dashboard.compliances') ? 'opacity-90' : 'group-hover:text-brand' }}"></i>
                            <span class="ml-3">Mis Cumplimientos</span>
                        </a>
                    @endif
                @endif

                @if(in_array($rolActual, ['Administrador', 'Auditor']))
                    <a href="{{ route('reports.history') }}" class="{{ request()->routeIs('reports.history') ? 'flex items-center px-4 py-3 rounded-xl bg-brand text-white font-medium shadow-md shadow-brand/20 transition-all' : 'group flex items-center px-4 py-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-darkbg-base hover:text-slate-900 dark:hover:text-white transition-all duration-200' }}">
                        <i class="fa-solid fa-list-check w-6 text-center {{ request()->routeIs('reports.history') ? 'opacity-90' : 'group-hover:text-brand' }}"></i>
                        <span class="ml-3">Auditoría Detallada</span>
                    </a>
                @endif
            </nav>
        </div>
        
        <div class="mt-auto p-4 border-t border-slate-100 dark:border-darkbg-border bg-slate-50/50 dark:bg-darkbg-card">
            <div class="flex items-center justify-between">
                <button id="themeToggleGlobal" class="w-9 h-9 rounded-xl flex items-center justify-center text-slate-500 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-darkbg-base hover:text-brand transition-all duration-300">
                    <i class="fa-solid fa-moon" id="moonIconGlobal"></i>
                    <i class="fa-solid fa-sun hidden" id="sunIconGlobal"></i>
                </button>
                <a href="{{ route('logout') }}" class="w-9 h-9 rounded-xl flex items-center justify-center border border-slate-200 dark:border-[#4a4b50] text-slate-500 dark:text-slate-400 hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-500/10 transition-all duration-300">
                    <i class="fa-solid fa-right-from-bracket"></i>
                </a>
            </div>
        </div>
    </aside>

    <div id="sidebarOverlay" class="fixed inset-0 bg-slate-900/50 backdrop-blur-sm z-30 hidden md:hidden"></div>

    <main class="main-content pt-20 md:pt-8 px-4 md:px-8 pb-10 min-h-screen flex flex-col w-full">
        <div class="w-full flex-1 flex flex-col">
            @yield('content')
        </div>
    </main>

    <script>
        document.addEventListener("DOMContentLoaded", function() {
            const htmlNode = document.documentElement;
            const themeToggleBtn = document.getElementById('themeToggleGlobal');
            const sunIcon = document.getElementById('sunIconGlobal');
            const moonIcon = document.getElementById('moonIconGlobal');
            
            const updateIcons = () => {
                if (htmlNode.classList.contains('dark')) {
                    sunIcon.classList.remove('hidden');
                    moonIcon.classList.add('hidden');
                } else {
                    sunIcon.classList.add('hidden');
                    moonIcon.classList.remove('hidden');
                }
            };
            
            updateIcons();
            
            themeToggleBtn.addEventListener('click', () => {
                htmlNode.classList.toggle('dark');
                localStorage.setItem('theme', htmlNode.classList.contains('dark') ? 'dark' : 'light');
                updateIcons();
            });
            
            const mobileMenuBtn = document.getElementById('mobileMenuBtn');
            const sidebar = document.getElementById('sidebar');
            const sidebarOverlay = document.getElementById('sidebarOverlay');
            
            const toggleMenu = () => {
                sidebar.classList.toggle('-translate-x-full');
                sidebarOverlay.classList.toggle('hidden');
            };

            mobileMenuBtn.addEventListener('click', toggleMenu);
            sidebarOverlay.addEventListener('click', toggleMenu);
        });
    </script>
    
    @yield('scripts')
</body>
</html>