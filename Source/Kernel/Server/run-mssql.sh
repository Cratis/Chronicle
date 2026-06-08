#!/usr/bin/env bash
# Run the Chronicle Kernel with Microsoft SQL Server.
# Usage: ./run-mssql.sh [--docker]
#   --docker  Start only infrastructure; run Kernel manually

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "▶ Starting infrastructure with docker compose (MSSQL profile)..."
docker compose --profile mssql -f "$SCRIPT_DIR/docker-compose.yml" up -d
echo "✓ SQL Server → localhost:1433"

if [[ "${1:-}" != "--docker" ]]; then
    echo ""
    echo "▶ Starting Kernel server (SQL Server)..."
    export Cratis__Chronicle__Storage__Type=MsSql
    export Cratis__Chronicle__Storage__ConnectionDetails="Server=localhost;Database=chronicle;User Id=sa;Password=Chronicle_Str0ng!;TrustServerCertificate=true"
    dotnet run --project "$SCRIPT_DIR/Server.csproj"
fi
