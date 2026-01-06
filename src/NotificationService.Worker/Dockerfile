# Notification Service Worker Dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/NotificationService.Worker/NotificationService.Worker.csproj", "NotificationService.Worker/"]
COPY ["src/ServiceDefaults/ServiceDefaults.csproj", "ServiceDefaults/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]

RUN dotnet restore "NotificationService.Worker/NotificationService.Worker.csproj"

COPY src/NotificationService.Worker/. NotificationService.Worker/
COPY src/ServiceDefaults/. ServiceDefaults/
COPY src/Shared.Contracts/. Shared.Contracts/

WORKDIR "/src/NotificationService.Worker"
RUN dotnet build "NotificationService.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationService.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.Worker.dll"]
