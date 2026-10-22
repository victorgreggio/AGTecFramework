# Build from the repository root:  docker build -f samples/AGTec.Reference/AGTec.Reference.Api/Dockerfile -t agtec-reference-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (better layer caching): copy the build config + the full source tree.
COPY . .
RUN dotnet restore samples/AGTec.Reference/AGTec.Reference.Api/AGTec.Reference.Api.csproj
RUN dotnet publish samples/AGTec.Reference/AGTec.Reference.Api/AGTec.Reference.Api.csproj \
    -c Release -o /app --no-restore /p:ContinuousIntegrationBuild=true

# Chiseled, rootless runtime: small image, minimal attack surface.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "AGTec.Reference.Api.dll"]
