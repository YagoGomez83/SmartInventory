# Usamos el SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiamos los csproj y restauramos dependencias (Capa de optimización de caché)
COPY ["src/SmartInventory.API/SmartInventory.API.csproj", "SmartInventory.API/"]
COPY ["src/SmartInventory.Application/SmartInventory.Application.csproj", "SmartInventory.Application/"]
COPY ["src/SmartInventory.Domain/SmartInventory.Domain.csproj", "SmartInventory.Domain/"]
COPY ["src/SmartInventory.Infrastructure/SmartInventory.Infrastructure.csproj", "SmartInventory.Infrastructure/"]

RUN dotnet restore "SmartInventory.API/SmartInventory.API.csproj"

# Copiamos el resto del código
COPY src/ .

# Compilamos y publicamos
WORKDIR "/src/SmartInventory.API"
RUN dotnet build "SmartInventory.API.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "SmartInventory.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen final ligera para ejecutar
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartInventory.API.dll"]