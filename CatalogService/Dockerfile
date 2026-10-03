# ── Этап сборки ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["CatalogService/CatalogService.csproj", "CatalogService/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]
RUN dotnet restore "CatalogService/CatalogService.csproj"

COPY . .

WORKDIR "/src/CatalogService"
RUN dotnet publish "CatalogService.csproj" -c Release -o /app/publish

# ── Финальный образ ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

USER app

ENTRYPOINT ["dotnet", "CatalogService.dll"]