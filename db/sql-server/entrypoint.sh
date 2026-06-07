#!/bin/bash

# Iniciar SQL Server en segundo plano
/opt/mssql/bin/sqlservr &

# Esperar a que SQL Server esté listo (60 segundos máximo)
echo "Esperando a que SQL Server inicie..."
for i in {1..60};
do
    # 🚀 CORRECCIÓN: Le quitamos el 18 a mssql-tools
    /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1
    if [ $? -eq 0 ]
    then
        echo "SQL Server está listo."
        break
    else
        echo "Aún no listo... esperando..."
        sleep 1
    fi
done

# Ejecutar los scripts en orden
echo "Ejecutando scripts de inicialización..."
# 🚀 CORRECCIÓN: Le quitamos el 18 a mssql-tools en estas dos líneas también
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -i /usr/config/scripts/01_schema.sql
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -i /usr/config/scripts/02_seed.sql

echo "¡Inicialización completa!"

# Mantener el contenedor vivo
wait