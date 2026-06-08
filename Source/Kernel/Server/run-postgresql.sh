#!/usr/bin/env bash
# Run the Chronicle Kernel with PostgreSQL.
# Usage: ./run-postgresql.sh [--docker]
#   --docker  Start only infrastructure; run Kernel manually

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "▶ Starting infrastructure with docker compose (PostgreSQL profile)..."
docker compose --profile postgresql -f "$SCRIPT_DIR/docker-compose.yml" up -d
echo "✓ PostgreSQL → localhost:5432"

if [[ "${1:-}" != "--docker" ]]; then
    echo ""
    echo "▶ Starting Kernel server (PostgreSQL)..."
    export Cratis__Chronicle__Storage__Type=PostgreSql
    export Cratis__Chronicle__Storage__ConnectionDetails="Host=localhost;Database=chronicle;Username=chronicle;Password=chronicle"
    dotnet run --project "$SCRIPT_DIR/Server.csproj"
fi
