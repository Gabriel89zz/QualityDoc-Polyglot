<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <title>Reporte de Auditoría</title>
    <style>
        body {
            font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif;
            font-size: 11px;
            color: #333333;
        }
        .header {
            width: 100%;
            margin-bottom: 20px;
            border-bottom: 2px solid #8772FE; /* Tu color Brand */
            padding-bottom: 10px;
        }
        .title {
            font-size: 18px;
            font-weight: bold;
            color: #2a2b2e;
            margin: 0;
        }
        .subtitle {
            font-size: 12px;
            color: #666;
            margin-top: 5px;
        }
        .date {
            float: right;
            font-size: 10px;
            color: #888;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
        }
        th, td {
            border: 1px solid #e5e7eb;
            padding: 8px 10px;
            text-align: left;
        }
        th {
            background-color: #f8fafc;
            color: #475569;
            font-size: 10px;
            text-transform: uppercase;
            font-weight: bold;
        }
        tr:nth-child(even) {
            background-color: #fcfcfc;
        }
        .badge {
            padding: 3px 6px;
            border-radius: 4px;
            font-weight: bold;
            font-size: 9px;
        }
    </style>
</head>
<body>

    <div class="header">
        <div class="date">Fecha de Emisión: {{ \Carbon\Carbon::now()->format('d/m/Y H:i') }}</div>
        <h1 class="title">Registro de Trazabilidad y Auditoría</h1>
        <div class="subtitle">Empresa: {{ $companyName }}</div>
    </div>

    <table>
        <thead>
            <tr>
                <th width="15%">Fecha y Hora</th>
                <th width="20%">Usuario</th>
                <th width="15%">Acción</th>
                <th width="15%">Código Doc.</th>
                <th width="35%">Detalle / Título</th>
            </tr>
        </thead>
        <tbody>
            @forelse($logs as $log)
                @php
                    $tipoAccion = 'Lectura';
                    $tituloLimpio = $log->document_title;

                    if ($log->document_code == 'DASHBOARD_VIEW') {
                        $tipoAccion = 'Ingreso';
                    } elseif (str_contains($log->document_title, '[FIRMA DE ENTERADO]')) {
                        $tipoAccion = 'Acuse Firmado';
                        $tituloLimpio = str_replace('[FIRMA DE ENTERADO] ', '', $log->document_title);
                    } elseif (str_contains($log->document_title, '[REPORTE DE INCIDENCIA]')) {
                        $tipoAccion = 'Reporte Error';
                        $tituloLimpio = str_replace('[REPORTE DE INCIDENCIA] ', '', $log->document_title);
                    }
                @endphp
                <tr>
                    <td>{{ $log->created_at->format('d/m/Y H:i:s') }}</td>
                    <td>
                        <strong>{{ $log->user_name }}</strong><br>
                        <span style="color:#888; font-size:9px;">IP: {{ $log->ip_address }}</span>
                    </td>
                    <td>{{ $tipoAccion }}</td>
                    <td><strong>{{ $log->document_code }}</strong></td>
                    <td>
                        @if($log->document_code != 'DASHBOARD_VIEW')
                            <span style="color:#888; font-size:9px;">v{{ $log->version_num }} - </span>
                        @endif
                        {{ $tituloLimpio }}
                    </td>
                </tr>
            @empty
                <tr>
                    <td colspan="5" style="text-align:center; padding: 20px;">No hay registros de auditoría para mostrar.</td>
                </tr>
            @endforelse
        </tbody>
    </table>

</body>
</html>