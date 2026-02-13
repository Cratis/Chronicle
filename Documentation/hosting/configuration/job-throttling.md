# Job Throttling

Chronicle Server's job system can be configured to limit the number of parallel job steps to prevent excessive CPU usage.

## Configuration

The maximum number of parallel job steps can be configured using the `MaxParallelSteps` option:

```json
{
  "Cratis": {
    "Chronicle": {
      "Jobs": {
        "MaxParallelSteps": 4
      }
    }
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| MaxParallelSteps | number | Environment.ProcessorCount - 1 | Maximum number of parallel job steps (minimum 1) |

Alternatively, you can use environment variables:

```bash
Cratis__Chronicle__Jobs__MaxParallelSteps=4
```

## Default Behavior

If `MaxParallelSteps` is not configured, Chronicle uses a sensible default:

- Default value: `Environment.ProcessorCount - 1`
- Minimum value: `1` (even on single-core systems)

For example, on an 8-core system, the default would be 7 parallel steps.

## How It Works

1. Each job step attempts to acquire a slot before executing.
2. If all slots are in use, the step waits in a queue.
3. When a step completes (successfully or with an error), it releases its slot.
4. The next waiting step can then acquire the released slot.

This ensures that the job system never uses more than the configured number of CPU cores simultaneously.

## Use Cases

**High-throughput systems**: Limit parallel steps to leave CPU resources for other operations:

```json
{
  "Jobs": {
    "MaxParallelSteps": 2
  }
}
```

**Dedicated job processing**: Use more CPU cores for job processing:

```json
{
  "Jobs": {
    "MaxParallelSteps": 16
  }
}
```

**Testing environments**: Reduce resource usage:

```json
{
  "Jobs": {
    "MaxParallelSteps": 1
  }
}
```

