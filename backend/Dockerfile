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
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "Locatarius.Api.dll"]