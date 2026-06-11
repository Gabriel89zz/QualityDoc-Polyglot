# --- ETAPA 1: Compilación (SDK) ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# 1. Copiamos el archivo .csproj usando el comodín desde tu nueva estructura física
COPY ["admin/src/dotnet-app/*.csproj", "dotnet-app/"]
RUN dotnet restore "dotnet-app/"

# 2. Copiamos todo el código fuente real de C# que está dentro de tu carpeta src
COPY admin/src/dotnet-app/ ./dotnet-app/

# 3. Nos movemos a la carpeta donde se copió el proyecto y compilamos
WORKDIR "/app/dotnet-app"
RUN dotnet publish -c Release -o /app/out --no-restore

# --- ETAPA 2: Ejecución (Runtime ligero) ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# 4. Traemos los binarios limpios listos para producción
COPY --from=build /app/out .

# 5. Punto de entrada (Debe coincidir con el nombre de tu proyecto compilado)
# ⚠️ NOTA: Si al levantar te da error de ejecución, cambia "dotnet-app.dll" 
# por el nombre real de tu archivo de C# (ej. QualityDoc-Polyglot.dll)
ENTRYPOINT ["dotnet", "dotnet-app.dll"]