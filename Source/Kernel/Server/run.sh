#!/usr/bin/env bash
# Run the Chronicle Kernel with the specified storage backend.
# Usage: ./run.sh [database] [--docker]
#
# Arguments:
#   database  Storage backend: mongodb (default), postgresql, mssql, sqlite
#   --docker  Start only infrastructure; run Kernel manually
#
# Examples:
#   ./run.sh              # MongoDB (default) + Kernel
#   ./run.sh postgresql   # PostgreSQL + Kernel
#   ./run.sh mssql --docker  # MSSQL infrastructure only
#   ./run.sh sqlite       # SQLite + Kernel

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Parse arguments
DATABASE="${1:-mongodb}"
DOCKER_ONLY="${2:-}"

# Validate database choice
case "$DATABASE" in
    mongodb|postgresql|mssql|sqlite)
        ;;
    *)
        echo "Error: Unknown database '$DATABASE'"
        echo ""
        echo "Valid options: mongodb (default), postgresql, mssql, sqlite"
        exit 1
        ;;
esac

# Validate second argument
if [[ -n "$DOCKER_ONLY" && "$DOCKER_ONLY" != "--docker" ]]; then
    echo "Error: Invalid argument '$DOCKER_ONLY'"
    echo "Expected: --docker or nothing"
    exit 1
fi

# Start infrastructure with appropriate profile
if [[ "$DATABASE" == "sqlite" ]]; then
    # SQLite doesn't need a profile (no external database container)
    echo "▶ Starting infrastructure with docker compose (SQLite)..."
    docker compose -f "$SCRIPT_DIR/docker-compose.yml" up -d
else
    echo "▶ Starting infrastructure with docker compose ($DATABASE profile)..."
    docker compose --profile "$DATABASE" -f "$SCRIPT_DIR/docker-compose.yml" up -d
fi

# Display connection info
case "$DATABASE" in
    mongodb)
        echo "✓ MongoDB → localhost:27017"
        ;;
    postgresql)
        echo "✓ PostgreSQL → localhost:5432"
        ;;
    mssql)
        echo "✓ SQL Server → localhost:1433"
        ;;
    sqlite)
        echo "✓ SQLite → $SCRIPT_DIR/data/chronicle.db"
        ;;
esac

# Exit early if --docker flag was set
if [[ "$DOCKER_ONLY" == "--docker" ]]; then
    exit 0
fi

echo ""
echo "▶ Starting Kernel server ($DATABASE)..."
echo ""

# Set environment variables based on database choice
# Environment variables must use prefix Cratis__Chronicle__ (with double underscores)
case "$DATABASE" in
    mongodb)
        # MongoDB is the default in chronicle.json, no overrides needed
        export Cratis__Chronicle__Storage__Type=MongoDB
        export Cratis__Chronicle__Storage__ConnectionDetails="mongodb://localhost:27017"
        ;;
    postgresql)
        export Cratis__Chronicle__Storage__Type=PostgreSql
        export Cratis__Chronicle__Storage__ConnectionDetails="Host=localhost;Database=chronicle;Username=chronicle;Password=chronicle"
        ;;
    mssql)
        export Cratis__Chronicle__Storage__Type=MsSql
        export Cratis__Chronicle__Storage__ConnectionDetails="Server=localhost;Database=chronicle;User Id=sa;Password=Chronicle_Str0ng!;TrustServerCertificate=true"
        ;;
    sqlite)
        export Cratis__Chronicle__Storage__Type=Sqlite
        export Cratis__Chronicle__Storage__ConnectionDetails="Data Source=$SCRIPT_DIR/data/chronicle.db"
        ;;
esac

# Run the Kernel
dotnet run --project "$SCRIPT_DIR/Server.csproj"
