# Chronicle Benchmarks

This directory contains performance benchmarks for Chronicle.

## Running Benchmarks

### Prerequisites

- Docker and Docker Compose
- .NET 10 SDK

### Execute

```bash
./run.sh
```

This will:
1. Start Chronicle infrastructure (MongoDB + Chronicle Kernel) via Docker Compose
2. Build the benchmark project
3. Run all benchmarks
4. Generate results in `results/` directory
5. Clean up infrastructure

## Benchmarks

### AppendBenchmark

Measures the performance of appending single events to the event log.

### AppendManyBenchmark

Measures the performance of bulk appending events to the event log.

Parameters:
- EventCount: 10, 100, 1000 events per batch

## Results

Benchmark results are exported in JSON format compatible with BenchmarkDotNet and GitHub Actions benchmark visualization.

Results are published to the `Documentation/benchmarks/` directory and visualized at:
https://cratis.github.io/Chronicle/benchmarks/

## CI/CD

Benchmarks are automatically run on push to `main` branch via the `.github/workflows/benchmarks.yml` workflow.
