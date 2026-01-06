# Inventory Service Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/InventoryService.Api/InventoryService.Api.csproj", "InventoryService.Api/"]
COPY ["src/ServiceDefaults/ServiceDefaults.csproj", "ServiceDefaults/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]

RUN dotnet restore "InventoryService.Api/InventoryService.Api.csproj"

COPY src/InventoryService.Api/. InventoryService.Api/
COPY src/ServiceDefaults/. ServiceDefaults/
COPY src/Shared.Contracts/. Shared.Contracts/

WORKDIR "/src/InventoryService.Api"
RUN dotnet build "InventoryService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InventoryService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventoryService.Api.dll"]
