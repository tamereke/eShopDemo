# Payment Service Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/PaymentService.Api/PaymentService.Api.csproj", "PaymentService.Api/"]
COPY ["src/ServiceDefaults/ServiceDefaults.csproj", "ServiceDefaults/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]

RUN dotnet restore "PaymentService.Api/PaymentService.Api.csproj"

COPY src/PaymentService.Api/. PaymentService.Api/
COPY src/ServiceDefaults/. ServiceDefaults/
COPY src/Shared.Contracts/. Shared.Contracts/

WORKDIR "/src/PaymentService.Api"
RUN dotnet build "PaymentService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PaymentService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.Api.dll"]
