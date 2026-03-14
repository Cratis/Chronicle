#!/bin/bash
# Rebuilds, packs, and reinstalls the Cratis CLI from local source.
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

echo "Packing CLI..."
dotnet pack Source/Tools/Cli/Cli.csproj -o nupkg --no-restore -c Release

echo "Reinstalling CLI..."
dotnet tool uninstall -g Cratis.Chronicle.Cli 2>/dev/null || true
dotnet tool install -g Cratis.Chronicle.Cli --version 1.0.0 --add-source nupkg

echo "Done. Installed version:"
cratis --version
