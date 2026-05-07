# --- ETAPA 1: Compilación (SDK) ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# 1. Copiamos el archivo de proyecto para restaurar dependencias
# Nota: La ruta es relativa a la raíz del proyecto (donde está el docker-compose.yml)
COPY ["src/dotnet-app/dotnet-app.csproj", "dotnet-app/"]
RUN dotnet restore "dotnet-app/dotnet-app.csproj"

# 2. Copiamos todo el código fuente de la carpeta src
COPY src/ .

# 3. Nos movemos a la carpeta del proyecto y compilamos
WORKDIR "/app/dotnet-app"
RUN dotnet publish -c Release -o /app/out

# --- ETAPA 2: Ejecución (Runtime ligero) ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# 4. Traemos solo los archivos necesarios para ejecutar (sin basura de compilación)
COPY --from=build /app/out .

# 5. Punto de entrada (Debe coincidir con el nombre del .csproj)
ENTRYPOINT ["dotnet", "dotnet-app.dll"]