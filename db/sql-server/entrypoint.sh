#!/bin/bash

# Iniciar SQL Server en segundo plano
/opt/mssql/bin/sqlservr &

# Esperar a que SQL Server esté listo (60 segundos máximo)
echo "Esperando a que SQL Server inicie..."
for i in {1..60};
do
    # CAMBIO AQUÍ: Usamos MSSQL_SA_PASSWORD
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1
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
# CAMBIOS AQUÍ: Usamos MSSQL_SA_PASSWORD en lugar de SQL_SERVER_PASSWORD
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -i /usr/config/scripts/01_schema.sql
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -i /usr/config/scripts/02_seed.sql

echo "¡Inicialización completa!"

# Mantener el contenedor vivo
wait