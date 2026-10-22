FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish AGTecService.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app .

# --- Writable mounted volumes (e.g. a FileSystem object store) -------------------------------------
# This is a rootless, chiseled image: the app runs as $APP_UID and has no shell, so you cannot `RUN
# mkdir`/`chown` here. A freshly-created Docker named volume is owned by root, so a non-root process
# gets "permission denied" writing to it. Give the mount target to $APP_UID by creating it in the build
# stage and copying it in with --chown; the empty volume then inherits that ownership on first mount.
# Uncomment and point at your mount path (must match the service's volume + storage-root config):
#
#   # in the build stage, above:  RUN mkdir -p /seed/<dir>
#   COPY --from=build --chown=$APP_UID:$APP_UID /seed/<dir> /app/<dir>
#
# (Recreate an already-root-owned volume once after adding this: `docker volume rm <name>`.)
# ---------------------------------------------------------------------------------------------------

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "AGTecService.dll"]
