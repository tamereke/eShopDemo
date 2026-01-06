# Order Service Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/OrderService.Api/OrderService.Api.csproj", "OrderService.Api/"]
COPY ["src/ServiceDefaults/ServiceDefaults.csproj", "ServiceDefaults/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]

# Restore dependencies
RUN dotnet restore "OrderService.Api/OrderService.Api.csproj"

# Copy source code
COPY src/OrderService.Api/. OrderService.Api/
COPY src/ServiceDefaults/. ServiceDefaults/
COPY src/Shared.Contracts/. Shared.Contracts/

# Build
WORKDIR "/src/OrderService.Api"
RUN dotnet build "OrderService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrderService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderService.Api.dll"]
