# Gateway API Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY["src/Gateway.Api/Gateway.Api.csproj", "Gateway.Api/"]
COPY ["src/ServiceDefaults/ServiceDefaults.csproj", "ServiceDefaults/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]

RUN dotnet restore "Gateway.Api/Gateway.Api.csproj"

COPY src/Gateway.Api/. Gateway.Api/
COPY src/ServiceDefaults/. ServiceDefaults/
COPY src/Shared.Contracts/. Shared.Contracts/

WORKDIR "/src/Gateway.Api"
RUN dotnet build "Gateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
