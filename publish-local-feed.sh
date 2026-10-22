#!/usr/bin/env bash
#
# publish-local-feed.sh — pack every AGTec.* package into a local folder feed.
#
# Creates (if needed) a NuGet feed in a local folder and publishes all AGTecFramework packages into it,
# so developers can consume AGTec.* from that folder without a hosted feed. Point a consuming project at
# it with a nuget.config <add key="agtec" value="<feed>" /> source (the script prints a ready snippet).
#
# Usage:
#   ./publish-local-feed.sh [--feed <dir>] [--configuration <Release|Debug>] [--clean]
#   --feed           target feed folder (default: ../agtec-local-feed, a sibling of this repo)
#   --configuration  build configuration to pack (default: Release)
#   --clean          remove existing *.nupkg from the feed before packing

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FEED="$(cd "$REPO_ROOT/.." && pwd)/agtec-local-feed"
CONFIG="Release"
CLEAN="false"

while [ $# -gt 0 ]; do
  case "$1" in
    --feed) FEED="$2"; shift 2;;
    --configuration|-c) CONFIG="$2"; shift 2;;
    --clean) CLEAN="true"; shift;;
    -h|--help) grep '^#' "$0" | sed 's/^# \{0,1\}//'; exit 0;;
    *) echo "Unknown option: $1" >&2; exit 1;;
  esac
done

# Absolute feed path (so the printed nuget.config snippet is unambiguous).
mkdir -p "$FEED"
FEED="$(cd "$FEED" && pwd)"

VERSION="$(grep -oE '<VersionPrefix>[^<]+' "$REPO_ROOT/Directory.Build.props" | head -1 | sed 's/<VersionPrefix>//')"
VERSION="${VERSION:-unknown}"

echo "Publishing AGTec.* v$VERSION ($CONFIG) to local feed: $FEED"

if [ "$CLEAN" = "true" ]; then
  echo "Cleaning existing packages ..."
  rm -f "$FEED"/*.nupkg
fi

count=0
for proj in "$REPO_ROOT"/src/*/*.csproj; do
  # Pack only packable projects; dotnet skips the rest with a notice.
  if dotnet pack "$proj" -c "$CONFIG" -o "$FEED" --nologo >/tmp/agtec-pack.$$.log 2>&1; then
    if grep -q "Successfully created package" /tmp/agtec-pack.$$.log; then
      pkg="$(grep -oE "[^/']+\.nupkg" /tmp/agtec-pack.$$.log | tail -1)"
      echo "  + $pkg"
      count=$((count + 1))
    fi
  else
    echo "  ! failed to pack $(basename "$proj")" >&2
    cat /tmp/agtec-pack.$$.log >&2
    rm -f /tmp/agtec-pack.$$.log
    exit 1
  fi
done
rm -f /tmp/agtec-pack.$$.log

echo
echo "Done: $count package(s) in $FEED"
cat <<EOF

Consume it from a project by adding a nuget.config next to the solution:

  <?xml version="1.0" encoding="utf-8"?>
  <configuration>
    <packageSources>
      <clear />
      <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
      <add key="agtec" value="$FEED" />
    </packageSources>
  </configuration>

Then reference the packages at version $VERSION (e.g. <PackageReference Include="AGTec.ServiceDefaults" />
with <PackageVersion Include="AGTec.ServiceDefaults" Version="$VERSION" /> under Central Package Management).
EOF
