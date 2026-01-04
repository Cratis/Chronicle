#!/bin/bash
set -e

echo "Starting Chronicle infrastructure..."
docker-compose up -d

echo "Waiting for Chronicle to be ready..."
timeout=120
counter=0
until curl -f http://localhost:35000/health > /dev/null 2>&1; do
    sleep 2
    counter=$((counter + 2))
    if [ $counter -ge $timeout ]; then
        echo "Timeout waiting for Chronicle to be ready"
        docker-compose logs
        docker-compose down
        exit 1
    fi
    echo "Waiting for Chronicle... ($counter seconds)"
done

echo "Chronicle is ready!"

echo "Building benchmarks..."
cd Chronicle.Benchmarks
dotnet build -c Release /p:TreatWarningsAsErrors=false

echo "Running benchmarks..."
dotnet run -c Release --no-build -- --exporters json --artifacts ../results

echo "Stopping Chronicle infrastructure..."
cd ..
docker-compose down

echo "Benchmarks completed!"
echo "Results are in: results/"
