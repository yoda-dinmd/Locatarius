FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Locatarius.Api/Locatarius.Api.csproj src/Locatarius.Api/
COPY src/Locatarius.Application/Locatarius.Application.csproj src/Locatarius.Application/
COPY src/Locatarius.Domain/Locatarius.Domain.csproj src/Locatarius.Domain/
COPY src/Locatarius.Infrastructure/Locatarius.Infrastructure.csproj src/Locatarius.Infrastructure/
RUN dotnet restore src/Locatarius.Api/Locatarius.Api.csproj
COPY . .

RUN dotnet publish src/Locatarius.Api/Locatarius.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
RUN apk add --no-cache curl icu-libs krb5-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R "$APP_UID" /home/app/.aspnet
USER $APP_UID

# Process liveness only; this does not query PostgreSQL.
HEALTHCHECK --interval=10s --timeout=3s --start-period=20s --retries=3 \
    CMD curl --fail --silent --show-error --max-time 2 http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Locatarius.Api.dll"]
