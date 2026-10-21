# Contexto de compilación: carpeta CineSpaceFS, que contiene los tres proyectos.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
WORKDIR /src
COPY global.json Directory.Build.props NuGet.Config ./
COPY CineSpace.Shared/CineSpace.Shared.csproj CineSpace.Shared/
COPY CineSpace.Server/CineSpace.Server.csproj CineSpace.Server/
COPY CineSpace.Client/CineSpace.Client.csproj CineSpace.Client/
RUN dotnet restore CineSpace.Server/CineSpace.Server.csproj
COPY . .
RUN dotnet publish CineSpace.Server/CineSpace.Server.csproj -c Release --no-restore -o /publish/server /p:UseAppHost=false
RUN test -f /publish/server/wwwroot/manifest.webmanifest \
    && test -f /publish/server/wwwroot/service-worker-assets.js \
    && test ! -f /publish/server/appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /publish/server/ ./
ENV ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "CineSpace.Server.dll"]
