#!/usr/bin/env bash
# Run the Chronicle Kernel with SQLite.
# Usage: ./run-sqlite.sh [--docker]
#   --docker  Start only infrastructure; run Kernel manually

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "▶ Starting infrastructure with docker compose (SQLite profile)..."
docker compose -f "$SCRIPT_DIR/docker-compose.yml" up -d

if [[ "${1:-}" != "--docker" ]]; then
    echo ""
    echo "▶ Starting Kernel server (SQLite)..."
    export Cratis__Chronicle__Storage__Type=Sqlite
    export Cratis__Chronicle__Storage__ConnectionDetails="Data Source=$SCRIPT_DIR/data/chronicle.db"
    dotnet run --project "$SCRIPT_DIR/Server.csproj"
fi
