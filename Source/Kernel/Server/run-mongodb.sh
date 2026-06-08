#!/usr/bin/env bash
# Run the Chronicle Kernel with MongoDB (default).
# Usage: ./run-mongodb.sh [--docker]
#   --docker  Start only infrastructure; run Kernel manually

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "▶ Starting infrastructure with docker compose (MongoDB profile)..."
docker compose --profile mongodb -f "$SCRIPT_DIR/docker-compose.yml" up -d
echo "✓ MongoDB → localhost:27017"

if [[ "${1:-}" != "--docker" ]]; then
    echo ""
    echo "▶ Starting Kernel server (MongoDB)..."
    dotnet run --project "$SCRIPT_DIR/Server.csproj"
fi
