# Build from the repository root:  docker build -f samples/AGTec.Reference/AGTec.Reference.Advisory/Dockerfile -t agtec-reference-advisory .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore samples/AGTec.Reference/AGTec.Reference.Advisory/AGTec.Reference.Advisory.csproj
RUN dotnet publish samples/AGTec.Reference/AGTec.Reference.Advisory/AGTec.Reference.Advisory.csproj \
    -c Release -o /app --no-restore /p:ContinuousIntegrationBuild=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "AGTec.Reference.Advisory.dll"]
