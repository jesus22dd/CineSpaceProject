#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

# El frontend solo recibe el origen público de Render. Ninguna clave privada.
node scripts/prepare-netlify.mjs --validate

# Netlify no incluye el SDK .NET. Usar la misma versión de global.json.
sdk_version="$(node -p "JSON.parse(require('fs').readFileSync('global.json','utf8')).sdk.version")"
if ! dotnet --version >/dev/null 2>&1; then
    export DOTNET_ROOT="$PWD/.local/netlify-dotnet"
    export PATH="$DOTNET_ROOT:$PATH"
    mkdir -p "$DOTNET_ROOT"
    installer="$(mktemp)"
    trap 'rm -f "$installer"' EXIT
    curl --fail --silent --show-error --location --retry 3 \
        https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh --output "$installer"
    bash "$installer" --version "$sdk_version" --install-dir "$DOTNET_ROOT" --no-path
fi

dotnet publish CineSpace.Client/CineSpace.Client.csproj --configuration Release \
    --output .publish/netlify -p:UseAppHost=false
node scripts/prepare-netlify.mjs .publish/netlify/wwwroot
