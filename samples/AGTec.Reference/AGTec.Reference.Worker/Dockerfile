# Build from the repository root:  docker build -f samples/AGTec.Reference/AGTec.Reference.Worker/Dockerfile -t agtec-reference-worker .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore samples/AGTec.Reference/AGTec.Reference.Worker/AGTec.Reference.Worker.csproj
RUN dotnet publish samples/AGTec.Reference/AGTec.Reference.Worker/AGTec.Reference.Worker.csproj \
    -c Release -o /app --no-restore /p:ContinuousIntegrationBuild=true

# Uses the ASP.NET chiseled runtime because AGTec.ServiceDefaults references the
# ASP.NET shared framework (telemetry/health). Rootless by default.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "AGTec.Reference.Worker.dll"]
