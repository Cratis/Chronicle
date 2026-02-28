# Chronicle Benchmarks

This directory contains performance benchmarks for Chronicle.

## Running Benchmarks

### Prerequisites

- Docker (for building and running Chronicle container)
- .NET 10 SDK

### Execute

```bash
./run.sh
```

This will:
1. Build the benchmark project (which also builds the Chronicle Docker image)
2. Run all benchmarks (TestContainers will automatically start Chronicle and MongoDB)
3. Generate results in `results/` directory
4. Clean up infrastructure automatically

## Infrastructure

The benchmarks use **TestContainers** to automatically manage the Chronicle infrastructure:
- Chronicle Kernel container is built from the local source using `cratis/chronicle:local-development` image
- MongoDB container is automatically started and linked
- All containers are cleaned up automatically after benchmarks complete

This approach matches the Integration test strategy used in the `Integration/Api` project.

## Benchmarks

### AppendBenchmark

Measures the performance of appending single events to the event log.

### AppendManyBenchmark

Measures the performance of bulk appending events to the event log.

Parameters:
- EventCount: 10, 100, 1000 events per batch

### Future Benchmarks

The following observer benchmarks are planned but require additional infrastructure:

- **ReducerBenchmark**: Measure events processed per second by reducers
- **ReactorBenchmark**: Measure events processed per second by reactors  
- **ProjectionBenchmark**: Measure events processed per second by projections

These will be implemented once we have:
1. A way to deploy observers to the Chronicle server
2. Mechanisms to measure observer processing throughput
3. Integration with the in-process Chronicle client for easier testing

## Results

Benchmark results are exported in JSON format compatible with BenchmarkDotNet and GitHub Actions benchmark visualization.

Results are published to the `Documentation/benchmarks/` directory and visualized at:
https://cratis.github.io/Chronicle/benchmarks/

## CI/CD

Benchmarks are automatically run on push to `main` branch via the `.github/workflows/benchmarks.yml` workflow.
