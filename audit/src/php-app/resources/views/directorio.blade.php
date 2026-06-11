@extends('layouts.app')

@section('title', 'Directorio Documental')

@section('content')
<div class="w-full">
    <div class="flex flex-col md:flex-row justify-between items-center gap-4 mb-8 animate-fade-up">
        <div>
            <h1 class="text-2xl font-bold text-slate-800 dark:text-white">Directorio Vigente</h1>
            <p class="text-slate-500 dark:text-slate-400 text-sm mt-1">Manuales, procedimientos y formatos aprobados para planta.</p>
        </div>
        
        <form method="GET" action="{{ route('directorio') }}" class="w-full md:w-96 flex gap-2 relative" id="searchContainer" onsubmit="return false;">
            <div class="relative flex-1">
                <span class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-slate-400 z-20">
                    <i class="fa-solid fa-magnifying-glass text-sm"></i>
                </span>
                
                <input type="text" id="searchInput" value="{{ $searchTerm ?? '' }}" autocomplete="off" placeholder="Buscar por código, título o etiqueta..." class="w-full pl-10 pr-4 py-2.5 bg-white dark:bg-darkbg-card border border-slate-200 dark:border-darkbg-border rounded-xl text-sm focus:outline-none focus:border-brand dark:focus:border-brand shadow-soft transition-all text-slate-700 dark:text-slate-200 relative z-10" />
            </div>
            
            @if(!empty($searchTerm))
                <a href="{{ route('directorio') }}" class="bg-slate-100 hover:bg-slate-200 dark:bg-darkbg-card dark:hover:bg-[#3a3b40] text-slate-600 dark:text-slate-300 px-4 py-2.5 rounded-xl font-bold shadow-sm transition flex items-center border border-slate-200 dark:border-darkbg-border" title="Limpiar búsqueda backend">
                    <i class="fa-solid fa-xmark"></i>
                </a>
            @endif
        </form>
    </div>

   @if(isset($allTags) && count($allTags) > 0)
    <div class="mb-6 animate-fade-up delay-100 relative z-40">
        <div class="relative inline-block text-left" id="tagsDropdownContainer">
            <button type="button" id="btnTagsToggle" class="inline-flex justify-center items-center gap-2 rounded-xl border border-slate-200 dark:border-darkbg-border shadow-sm px-4 py-2 bg-white dark:bg-[#323338] text-sm font-bold text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-[#2a2b2e] transition-colors focus:outline-none focus:ring-2 focus:ring-brand/20">
                <i class="fa-solid fa-tags text-slate-400"></i> Etiquetas <i class="fa-solid fa-chevron-down text-[10px] ml-1"></i>
                <span id="tagCountBadge" class="hidden ml-1 bg-brand text-white text-[10px] px-2 py-0.5 rounded-full shadow-sm">0</span>
            </button>

            <div id="tagsMenu" class="hidden absolute left-0 mt-2 w-80 sm:w-96 rounded-2xl shadow-xl bg-white dark:bg-[#323338] border border-slate-200 dark:border-[#4a4b50] z-50 p-5 transform origin-top-left transition-all">
                
                <div id="selectedTagsContainer" class="hidden mb-4 pb-4 border-b border-slate-100 dark:border-[#4a4b50]">
                    <div class="flex justify-between items-center mb-2">
                        <span class="block text-[10px] font-bold text-slate-400 uppercase tracking-wider">Filtros Activos (Coincidencia Exacta)</span>
                        <button type="button" id="btnClearTags" class="text-[10px] text-red-500 hover:text-red-600 font-bold">Limpiar</button>
                    </div>
                    <div class="flex flex-wrap gap-2" id="active-tags-list">
                        </div>
                </div>

                <div>
                    <span class="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-2">Etiquetas Disponibles</span>
                    <div class="flex flex-wrap gap-2" id="available-tags-list">
                        </div>
                </div>
            </div>
        </div>
    </div>
    @endif

    @if(session('errorApi'))
        <div class="bg-red-50 dark:bg-red-900/20 border-l-4 border-red-500 text-red-700 dark:text-red-400 p-4 mb-6 rounded-xl shadow-sm flex items-center animate-fade-up delay-100">
            <i class="fa-solid fa-triangle-exclamation text-2xl mr-4"></i>
            <div>
                <p class="font-bold">Error de Conexión</p>
                <p class="text-sm">{{ session('errorApi') }}</p>
            </div>
        </div>
    @endif
    @if(session('success'))
        <div class="bg-green-50 dark:bg-green-900/20 border-l-4 border-green-500 text-green-700 dark:text-green-400 p-4 mb-6 rounded-xl shadow-sm flex items-center justify-between animate-fade-up delay-100">
            <div>
                <p class="font-bold"><i class="fa-solid fa-circle-check mr-2"></i>Cumplimiento Registrado</p>
                <p class="text-sm">{{ session('success') }}</p>
            </div>
            <i class="fa-solid fa-file-signature text-3xl opacity-50"></i>
        </div>
    @endif

    <div class="bg-white dark:bg-darkbg-card rounded-2xl shadow-soft dark:shadow-none border border-slate-200 dark:border-darkbg-border overflow-hidden animate-fade-up delay-100">
        <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse min-w-full">
                <thead class="bg-slate-50 dark:bg-[#2a2b2e] text-slate-500 dark:text-slate-400 text-xs uppercase font-semibold">
                    <tr>
                        <th class="p-5">Código</th>
                        <th class="p-5">Título del Documento</th>
                        <th class="p-5">Metadatos (Etiquetas)</th>
                        <th class="p-5 text-center">Acciones Operativas</th>
                    </tr>
                </thead>
                <tbody id="document-table-body" class="divide-y divide-slate-100 dark:divide-darkbg-border text-sm">
                    
                    <tr id="empty-state-row" style="display: none;">
                        <td colspan="4" class="px-6 py-16 text-center">
                            <div class="w-20 h-20 mx-auto bg-slate-50 dark:bg-[#2a2b2e] rounded-full flex items-center justify-center mb-4 border border-slate-100 dark:border-darkbg-border">
                                <i class="fa-solid fa-magnifying-glass text-4xl text-slate-300 dark:text-slate-600"></i>
                            </div>
                            <h3 class="text-lg font-bold text-slate-700 dark:text-slate-300">No hay coincidencias</h3>
                            <p class="text-slate-500 text-sm mt-1">Ningún documento coincide con los filtros aplicados.</p>
                        </td>
                    </tr>

                    @forelse($documentos ?? [] as $doc)
                        <tr class="doc-row hover:bg-slate-50 dark:hover:bg-[#2a2b2e] transition-colors" 
                            data-title="{{ strtolower($doc['titulo']) }}" 
                            data-code="{{ strtolower($doc['codigo']) }}"
                            data-tags="{{ strtolower(implode(',', $doc['etiquetas'])) }}">
                            
                            @php
                                $ext = strtolower(pathinfo($doc['url_archivo'] ?? '', PATHINFO_EXTENSION));
                                $iconClass = 'fa-file-lines text-slate-500';
                                $bgClass = 'bg-slate-100 dark:bg-slate-800';

                                if(in_array($ext, ['pdf'])) {
                                    $iconClass = 'fa-file-pdf text-red-500';
                                    $bgClass = 'bg-red-50 dark:bg-red-500/10';
                                } elseif(in_array($ext, ['doc', 'docx'])) {
                                    $iconClass = 'fa-file-word text-blue-500';
                                    $bgClass = 'bg-blue-50 dark:bg-blue-500/10';
                                } elseif(in_array($ext, ['xls', 'xlsx', 'csv'])) {
                                    $iconClass = 'fa-file-excel text-emerald-500';
                                    $bgClass = 'bg-emerald-50 dark:bg-emerald-500/10';
                                } elseif(in_array($ext, ['ppt', 'pptx'])) {
                                    $iconClass = 'fa-file-powerpoint text-orange-500';
                                    $bgClass = 'bg-orange-50 dark:bg-orange-500/10';
                                } elseif(in_array($ext, ['jpg', 'jpeg', 'png', 'gif', 'webp'])) {
                                    $iconClass = 'fa-image text-purple-500';
                                    $bgClass = 'bg-purple-50 dark:bg-purple-500/10';
                                } elseif(in_array($ext, ['zip', 'rar', '7z'])) {
                                    $iconClass = 'fa-file-zipper text-slate-500';
                                    $bgClass = 'bg-slate-100 dark:bg-[#323338]';
                                }
                            @endphp

                            <td class="p-5 whitespace-nowrap align-middle">
                                <span class="text-sm font-black text-brand bg-brand/10 border border-brand/20 px-2.5 py-1 rounded-lg">{{ $doc['codigo'] }}</span>
                            </td>

                            <td class="p-5 align-middle">
                                <div class="flex items-center gap-3">
                                    <div class="w-10 h-10 rounded-xl {{ $bgClass }} flex items-center justify-center shrink-0 shadow-sm border border-slate-200/50 dark:border-[#4a4b50]">
                                        <i class="fa-solid {{ $iconClass }} text-xl"></i>
                                    </div>
                                    <div class="flex flex-col justify-center">
                                        <div class="text-sm text-slate-800 dark:text-white font-bold">{{ $doc['titulo'] }}</div>
                                        <div class="text-[11px] text-slate-500 mt-0.5 uppercase font-bold tracking-wide">
                                            {{ $ext ? $ext : 'DOC' }} <span class="mx-1.5 text-slate-300 dark:text-slate-600">|</span> 
                                            <i class="fa-solid fa-check-double text-green-500 mr-1"></i> {{ $doc['aprobado_por'] }} <span class="mx-1.5 text-slate-300 dark:text-slate-600">|</span> 
                                            v{{ $doc['version'] }}
                                        </div>
                                    </div>
                                </div>
                            </td>

                            <td class="p-5 align-middle">
                                <div class="flex flex-wrap gap-1.5">
                                    @foreach($doc['etiquetas'] as $etiqueta)
                                        <span class="inline-flex items-center px-2 py-1 rounded border border-slate-200 dark:border-darkbg-border bg-slate-50 dark:bg-[#323338] text-slate-500 dark:text-slate-400 text-[10px] font-bold shadow-sm">
                                            {{ $etiqueta }}
                                        </span>
                                    @endforeach
                                </div>
                            </td>
                            
                            <td class="p-5 align-middle text-right">
                                <div class="flex justify-end space-x-2 items-center">
                                    
                                    <button type="button" onclick="abrirModalReporte('{{ $doc['codigo'] }}', '{{ addslashes($doc['titulo']) }}')"
                                            class="inline-flex items-center text-red-600 hover:text-white bg-red-50 hover:bg-red-600 dark:bg-red-500/10 dark:hover:bg-red-600 border border-transparent px-3 py-1.5 rounded-lg font-bold transition-all text-xs shadow-sm group shrink-0" title="Reportar un problema">
                                        <i class="fa-solid fa-triangle-exclamation mr-1.5 text-red-500 group-hover:text-white transition-colors"></i> Reportar Error
                                    </button>

                                    <a href="{{ route('log.document', ['codigo' => $doc['codigo'], 'titulo' => $doc['titulo'], 'version' => $doc['version'], 'url' => $doc['url_archivo'] ?? '']) }}" 
                                       target="_blank" 
                                       onclick="desbloquearFirma('{{ $doc['codigo'] }}')"
                                        class="inline-flex items-center text-brand hover:text-white bg-brand/10 hover:bg-brand border border-transparent px-3 py-1.5 rounded-lg font-bold transition-all text-xs shadow-sm group shrink-0">
                                        <i class="fa-solid fa-eye mr-1.5 text-brand group-hover:text-white transition-colors"></i> Ver Documento
                                    </a>
                                    
                                    <form action="{{ route('document.acuse') }}" method="POST" onsubmit="return confirm('¿Declaras formalmente haber leído y comprendido este documento?');">
                                        @csrf
                                        <input type="hidden" name="codigo" value="{{ $doc['codigo'] }}">
                                        <input type="hidden" name="titulo" value="{{ $doc['titulo'] }}">
                                        <input type="hidden" name="version" value="{{ $doc['version'] }}">
                                        <button type="submit" id="btn-firma-{{ $doc['codigo'] }}" disabled
                                                class="inline-flex items-center bg-slate-100 dark:bg-[#2a2b2e] text-slate-400 border border-slate-200 dark:border-darkbg-border px-3 py-1.5 rounded-lg font-bold text-xs shadow-sm cursor-not-allowed transition-all shrink-0">
                                            <i class="fa-solid fa-lock mr-1.5" id="icono-firma-{{ $doc['codigo'] }}"></i> Firmar
                                        </button>
                                    </form>
                                </div>
                            </td>
                        </tr>
                    @empty
                    @endforelse
                </tbody>
            </table>
        </div>

        <div class="bg-slate-50 dark:bg-[#2a2b2e] px-5 py-4 border-t border-slate-200 dark:border-darkbg-border flex flex-col sm:flex-row items-center justify-between transition-colors duration-300">
            <div class="text-sm font-medium text-slate-500 dark:text-slate-400 mb-4 sm:mb-0">
                Mostrando <span id="pageInfo" class="font-bold text-slate-800 dark:text-white">Página 1 de 1</span>
                <span id="totalInfo" class="ml-1 text-xs">(0 documentos filtrados)</span>
            </div>
            
            <div class="flex space-x-2">
                <button id="prevPageBtn" class="px-4 py-2 text-xs font-bold rounded-lg border transition-colors shadow-sm text-slate-600 dark:text-slate-300 bg-white dark:bg-[#323338] border-slate-300 dark:border-darkbg-border hover:bg-slate-50 dark:hover:bg-[#3a3b40] hover:text-brand disabled:opacity-50 disabled:cursor-not-allowed">
                    <i class="fa-solid fa-chevron-left mr-1"></i> Anterior
                </button>
                <button id="nextPageBtn" class="px-4 py-2 text-xs font-bold rounded-lg border transition-colors shadow-sm text-slate-600 dark:text-slate-300 bg-white dark:bg-[#323338] border-slate-300 dark:border-darkbg-border hover:bg-slate-50 dark:hover:bg-[#3a3b40] hover:text-brand disabled:opacity-50 disabled:cursor-not-allowed">
                    Siguiente <i class="fa-solid fa-chevron-right ml-1"></i>
                </button>
            </div>
        </div>

    </div>

    <div id="modalReporte" class="fixed inset-0 z-[100] hidden items-center justify-center bg-slate-900/60 backdrop-blur-sm transition-opacity">
    <div class="bg-white dark:bg-[#323338] w-full max-w-lg rounded-2xl shadow-2xl overflow-hidden transform transition-all scale-100 opacity-100 border border-slate-200 dark:border-darkbg-border">
        
        <div class="bg-amber-50 dark:bg-amber-900/20 border-b border-amber-100 dark:border-amber-900/30 px-6 py-4 flex justify-between items-center">
            <div class="flex items-center">
                <div class="w-10 h-10 bg-amber-100 dark:bg-amber-900/40 rounded-full flex items-center justify-center text-amber-500 mr-3 shadow-sm">
                    <i class="fa-solid fa-flag text-lg"></i>
                </div>
                <div>
                    <h3 class="text-lg font-bold text-amber-900 dark:text-amber-400">Reportar Incidencia</h3>
                    <p class="text-xs text-amber-700 dark:text-amber-500">Notificar al administrador del sistema</p>
                </div>
            </div>
            <button type="button" onclick="cerrarModalReporte()" class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-colors">
                <i class="fa-solid fa-xmark text-xl"></i>
            </button>
        </div>

        <form id="formReporte" class="p-6">
            <input type="hidden" id="reporteDocCodigo" name="codigo">
            <input type="hidden" id="reporteDocTitulo" name="titulo">

            <div class="mb-5">
                <label class="block text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider mb-2">Documento Afectado</label>
                <div class="bg-slate-50 dark:bg-[#2a2b2e] border border-slate-200 dark:border-darkbg-border rounded-xl px-4 py-3 flex items-center shadow-inner">
                    <i class="fa-solid fa-file-pdf text-slate-400 mr-3"></i>
                    <span id="reporteDocDisplay" class="text-sm font-bold text-slate-700 dark:text-slate-300 truncate"></span>
                </div>
            </div>

            <div class="mb-5">
                <label for="tipoIncidencia" class="block text-sm font-bold text-slate-700 dark:text-slate-300 mb-2">Tipo de Problema <span class="text-red-500">*</span></label>
                <select id="tipoIncidencia" name="tipo" class="w-full rounded-xl bg-white dark:bg-[#2a2b2e] border border-slate-300 dark:border-darkbg-border px-4 py-2.5 text-sm text-slate-700 dark:text-white focus:border-brand focus:ring-2 focus:ring-brand/20 transition outline-none appearance-none" required>
                    <option value="">-- Selecciona una opción --</option>
                    <option value="Archivo dañado">El archivo PDF no abre o está dañado</option>
                    <option value="Versión obsoleta">La versión publicada está obsoleta</option>
                    <option value="Error ortográfico">Errores ortográficos o de formato</option>
                    <option value="Falta información">Falta información crítica en el documento</option>
                    <option value="Otro">Otro problema...</option>
                </select>
            </div>

            <div class="mb-6">
                <label for="detallesIncidencia" class="block text-sm font-bold text-slate-700 dark:text-slate-300 mb-2">Detalles Adicionales <span class="text-red-500">*</span></label>
                <textarea id="detallesIncidencia" name="detalles" rows="3" class="w-full rounded-xl bg-white dark:bg-[#2a2b2e] border border-slate-300 dark:border-darkbg-border px-4 py-3 text-sm text-slate-700 dark:text-white focus:border-brand focus:ring-2 focus:ring-brand/20 transition outline-none resize-none" placeholder="Describe brevemente en qué página o sección encontraste el error..." required></textarea>
            </div>

            <div class="flex justify-end space-x-3 pt-4 border-t border-slate-100 dark:border-darkbg-border">
                <button type="button" onclick="cerrarModalReporte()" class="px-5 py-2.5 rounded-xl text-sm font-bold text-slate-600 dark:text-slate-300 bg-slate-100 hover:bg-slate-200 dark:bg-[#2a2b2e] dark:hover:bg-[#3a3b40] transition-colors">
                    Cancelar
                </button>
                <button type="submit" class="px-6 py-2.5 rounded-xl text-sm font-bold text-white bg-amber-500 hover:bg-amber-600 shadow-md shadow-amber-500/20 flex items-center transition-colors">
                    <i class="fa-solid fa-paper-plane mr-2"></i> Enviar Reporte
                </button>
            </div>
        </form>
    </div>
</div>
</div>
@endsection

@section('scripts')
<script>
    function desbloquearFirma(codigo) {
        const btnFirma = document.getElementById('btn-firma-' + codigo);
        const iconoFirma = document.getElementById('icono-firma-' + codigo);

        if (btnFirma) {
            btnFirma.removeAttribute('disabled');
            // Le damos el estilo verde llamativo que armamos
            btnFirma.className = "inline-flex items-center text-green-600 dark:text-green-400 hover:text-white bg-green-50 dark:bg-green-900/20 hover:bg-green-600 dark:hover:bg-green-600 border border-green-200 dark:border-transparent px-4 py-2 rounded-xl font-bold transition-all text-xs shadow-sm cursor-pointer shrink-0";
            
            if (iconoFirma) {
                iconoFirma.className = "fa-solid fa-pen-nib mr-1.5";
            }
        }
    }

    // ==========================================
    // CONTROLADOR DEL MODAL DE REPORTES
    // ==========================================
    window.abrirModalReporte = function(codigo, titulo) {
        // Llenar los datos del modal
        document.getElementById('reporteDocCodigo').value = codigo;
        document.getElementById('reporteDocTitulo').value = titulo;
        document.getElementById('reporteDocDisplay').innerText = `[${codigo}] ${titulo}`;
        
        // Mostrar modal (Quitamos hidden y ponemos flex para centrar)
        const modal = document.getElementById('modalReporte');
        modal.classList.remove('hidden');
        modal.classList.add('flex');
    };

    window.cerrarModalReporte = function() {
        const modal = document.getElementById('modalReporte');
        modal.classList.add('hidden');
        modal.classList.remove('flex');
        
        // Limpiar el formulario
        document.getElementById('formReporte').reset();
    };

    // Manejar el envío del formulario (Conexión Real a PHP -> C#)
    document.getElementById('formReporte').addEventListener('submit', async function(e) {
        e.preventDefault(); // Evitamos que la página parpadee o recargue
        
        const codigo = document.getElementById('reporteDocCodigo').value;
        const tipo = document.getElementById('tipoIncidencia').value;
        const botonSubmit = this.querySelector('button[type="submit"]');
        const originalText = botonSubmit.innerHTML;

        // 1. Cambiamos el botón visualmente a estado de carga (Loading)
        botonSubmit.disabled = true;
        botonSubmit.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i> Enviando...';

        try {
            // Recolectamos los datos del formulario
            const formData = new FormData(this);
            formData.append('_token', '{{ csrf_token() }}'); // Agregamos seguridad de Laravel

            // 2. Disparamos la petición a nuestra ruta en Laravel
            const response = await fetch('{{ route("reportar.error") }}', {
                method: 'POST',
                body: formData
            });

            const data = await response.json();

            // 3. Cerramos el Modal
            cerrarModalReporte();
            
            // 4. Evaluamos la respuesta de la base de datos
            if(data.success) {
                alert(`✅ ¡Reporte enviado con éxito!\n\nEl equipo de Calidad revisará la incidencia del folio ${codigo} (${tipo}).`);
            } else {
                alert(`❌ Ocurrió un problema de base de datos:\n${data.message}`);
            }
        } catch (error) {
            cerrarModalReporte();
            alert('❌ Error de red: No se pudo conectar con el servidor.');
        } finally {
            // 5. Restauramos el botón a su estado original
            botonSubmit.disabled = false;
            botonSubmit.innerHTML = originalText;
        }
    });


    // ==========================================
    // MOTOR DE BÚSQUEDA Y PAGINACIÓN EN TIEMPO REAL
    // ==========================================
    document.addEventListener('DOMContentLoaded', function() {
        // Elementos Generales
        const searchInput = document.getElementById('searchInput');
        const rows = document.querySelectorAll('.doc-row');
        const emptyState = document.getElementById('empty-state-row');
        
        // Controles de paginación
        const prevPageBtn = document.getElementById('prevPageBtn');
        const nextPageBtn = document.getElementById('nextPageBtn');
        const pageInfo = document.getElementById('pageInfo');
        const totalInfo = document.getElementById('totalInfo');

        // Variables de Paginación
        let currentPage = 1;
        const itemsPerPage = 7; 
        let filteredRows = [];

        if (searchInput) searchInput.addEventListener('input', ejecutarFiltroVivo);

        // ==========================================
        // NUEVO MOTOR DE FILTROS Y ETIQUETAS (Popover)
        // ==========================================
        let selectedTags = new Set();
        const allAvailableTags = {!! json_encode($allTags ?? []) !!};
        
        // Elementos del Popover
        const btnTagsToggle = document.getElementById('btnTagsToggle');
        const tagsMenu = document.getElementById('tagsMenu');
        const tagCountBadge = document.getElementById('tagCountBadge');
        const availableTagsList = document.getElementById('available-tags-list');
        const activeTagsList = document.getElementById('active-tags-list');
        const selectedTagsContainer = document.getElementById('selectedTagsContainer');
        const btnClearTags = document.getElementById('btnClearTags');

        // Mostrar/Ocultar Menú Popover
        if(btnTagsToggle) {
            btnTagsToggle.addEventListener('click', (e) => {
                e.stopPropagation();
                tagsMenu.classList.toggle('hidden');
            });
        }

        // Cerrar al dar click fuera
        document.addEventListener('click', (e) => {
            if (tagsMenu && !tagsMenu.contains(e.target) && !btnTagsToggle.contains(e.target)) {
                tagsMenu.classList.add('hidden');
            }
        });

        if(tagsMenu) tagsMenu.addEventListener('click', (e) => e.stopPropagation());

        // Botón limpiar todas las etiquetas
        if(btnClearTags) {
            btnClearTags.addEventListener('click', () => {
                selectedTags.clear();
                renderTagsUI();
                ejecutarFiltroVivo();
            });
        }

        // 🚀 Dibujar la interfaz del Popover dinámicamente
        function renderTagsUI() {
            if(!tagsMenu) return;

            // Actualizar Badge y Visibilidad
            if(selectedTags.size > 0) {
                tagCountBadge.innerText = selectedTags.size;
                tagCountBadge.classList.remove('hidden');
                selectedTagsContainer.classList.remove('hidden');
                btnTagsToggle.classList.add('border-brand', 'ring-1', 'ring-brand');
            } else {
                tagCountBadge.classList.add('hidden');
                selectedTagsContainer.classList.add('hidden');
                btnTagsToggle.classList.remove('border-brand', 'ring-1', 'ring-brand');
            }

            // Limpiar contenedores
            availableTagsList.innerHTML = '';
            activeTagsList.innerHTML = '';

            allAvailableTags.forEach(tag => {
                const tagKey = tag.toLowerCase();
                
                if(selectedTags.has(tagKey)) {
                    // Botón Activo (Con la X para quitar)
                    const btn = document.createElement('button');
                    btn.type = 'button';
                    btn.className = 'inline-flex items-center px-3 py-1.5 rounded-lg border border-brand text-brand bg-brand/5 dark:bg-brand/10 text-xs font-bold hover:bg-brand hover:text-white transition-colors group select-none shadow-sm';
                    btn.innerHTML = `${tag} <i class="fa-solid fa-xmark ml-2 text-brand group-hover:text-white transition-colors opacity-70"></i>`;
                    btn.onclick = () => { selectedTags.delete(tagKey); renderTagsUI(); ejecutarFiltroVivo(); };
                    activeTagsList.appendChild(btn);
                } else {
                    // Botón Disponible para seleccionar
                    const btn = document.createElement('button');
                    btn.type = 'button';
                    btn.className = 'inline-flex items-center px-3 py-1.5 rounded-lg border border-slate-200 dark:border-darkbg-border bg-slate-50 dark:bg-[#2a2b2e] text-slate-600 dark:text-slate-400 text-xs font-bold hover:border-brand hover:text-brand transition-colors cursor-pointer select-none shadow-sm';
                    btn.innerHTML = tag;
                    btn.onclick = () => { selectedTags.add(tagKey); renderTagsUI(); ejecutarFiltroVivo(); };
                    availableTagsList.appendChild(btn);
                }
            });
        }
        
        // Inicializar UI de etiquetas
        renderTagsUI();


        // ==========================================
        // FUNCIONES DE DIBUJADO Y LÓGICA 'AND'
        // ==========================================

        // 1. Fase de Filtrado (Calcula quiénes sobreviven al filtro)
        function ejecutarFiltroVivo() {
            const searchTerm = searchInput ? searchInput.value.toLowerCase().trim() : '';
            filteredRows = []; // Vaciamos la lista de resultados

            rows.forEach(row => {
                const title = row.dataset.title;
                const code = row.dataset.code;
                const tagsRaw = row.dataset.tags;
                const rowTags = tagsRaw ? tagsRaw.split(',') : [];

                const matchesSearch = searchTerm === '' ? true : (
                    title.includes(searchTerm) || 
                    code.includes(searchTerm) || 
                    (tagsRaw && tagsRaw.includes(searchTerm))
                );
                
                // 🚀 LÓGICA 'AND' (.every)
                let matchesTags = true;
                if (selectedTags.size > 0) {
                    matchesTags = Array.from(selectedTags).every(selectedTag => rowTags.includes(selectedTag));
                }

                if (matchesSearch && matchesTags) {
                    filteredRows.push(row);
                }
                
                // Ocultamos todas por defecto
                row.style.display = 'none';
            });

            // Al filtrar, regresamos siempre a la página 1
            currentPage = 1;
            renderizarPagina();
        }

        // 2. Fase de Paginación y Dibujado (Solo muestra las de la página actual)
        function renderizarPagina() {
            const totalItems = filteredRows.length;
            const totalPages = Math.ceil(totalItems / itemsPerPage) || 1;

            // Aseguramos que la página actual no se salga de rango
            if (currentPage > totalPages) currentPage = totalPages;
            if (currentPage < 1) currentPage = 1;

            const startIndex = (currentPage - 1) * itemsPerPage;
            const endIndex = startIndex + itemsPerPage;

            // Ocultamos todas primero (limpieza)
            rows.forEach(r => r.style.display = 'none');

            // Dibujamos solo las filas de la página actual
            const rowsToShow = filteredRows.slice(startIndex, endIndex);
            rowsToShow.forEach(row => {
                row.style.display = '';
            });

            // Actualizamos la UI de estado vacío
            if (emptyState) {
                emptyState.style.display = (totalItems === 0) ? '' : 'none';
            }

            pageInfo.innerText = `Página ${currentPage} de ${totalPages}`;
            totalInfo.innerText = `(${totalItems} documentos en total)`;

            // Habilitar/Deshabilitar botones
            prevPageBtn.disabled = currentPage === 1;
            nextPageBtn.disabled = currentPage === totalPages;
        }

        // Eventos de botones Anterior/Siguiente
        prevPageBtn.addEventListener('click', () => {
            if (currentPage > 1) {
                currentPage--;
                renderizarPagina();
            }
        });

        nextPageBtn.addEventListener('click', () => {
            const maxPages = Math.ceil(filteredRows.length / itemsPerPage);
            if (currentPage < maxPages) {
                currentPage++;
                renderizarPagina();
            }
        });

        // Inicializar el filtro vacío al cargar la pantalla por primera vez
        ejecutarFiltroVivo();
    });
</script>
@endsection