# Backend AlmacenApi — build de dos etapas.
# Contexto de build: almacenapp/backend/AlmacenApi (ver docker-compose.yml en la raíz).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar solo el csproj primero para aprovechar la caché de Docker en
# `dotnet restore` mientras no cambien las dependencias.
COPY AlmacenApi.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# El puerto lo fija docker-compose.yml (mapea 5097:8080 en el host).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AlmacenApi.dll"]
