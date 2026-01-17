#!/bin/bash
set -e

echo "Building benchmarks..."
cd Chronicle.Benchmarks
dotnet build -c Release /p:TreatWarningsAsErrors=false

echo "Running benchmarks..."
dotnet run -c Release --no-build -- --exporters json --artifacts ../results

echo "Benchmarks completed!"
echo "Results are in: results/"
