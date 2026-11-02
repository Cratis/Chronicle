---
description: Limit parallel job steps in Chronicle Server with the MaxParallelSteps option to prevent excessive CPU usage.
---
# Job Throttling

Chronicle Server's job system can be configured to limit the number of parallel job steps to prevent excessive CPU usage.

## Configuration

The maximum number of parallel job steps can be configured using the `MaxParallelSteps` option in `chronicle.json`:

```json
{
  "Jobs": {
    "MaxParallelSteps": 4
  }
}
```

> [!IMPORTANT]
> Do not wrap the section in `Cratis` / `Chronicle` inside `chronicle.json`. Chronicle republishes everything it
> reads from that file under the `Cratis:Chronicle:` configuration path already, so a nested copy binds to
> `Cratis:Chronicle:Cratis:Chronicle:Jobs` and is silently ignored. The `Cratis__Chronicle__` prefix belongs on
> environment variables only.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| MaxParallelSteps | number | Environment.ProcessorCount - 1 | Maximum number of parallel job steps (minimum 1) |
| MaxParallelStepsWithoutJobsRole | number | max(1, effective MaxParallelSteps / 4) | Step budget when `Clustering:Roles:Jobs` is false; explicit values must be greater than zero. Integer division rounds down. |

The limit that applies is the smaller of `Jobs:MaxParallelSteps` and `Observers:MaxConcurrentPartitions` (default 32, see [Observers](observers.md)). With the defaults, `Jobs:MaxParallelSteps` only has an effect below 32, so raising it above 32 needs `Observers:MaxConcurrentPartitions` raised as well.

On a silo with `Clustering:Roles:Jobs=false`, Chronicle uses `Jobs:MaxParallelStepsWithoutJobsRole`
instead, still capped by `Observers:MaxConcurrentPartitions`. When unset, this is one quarter of the
effective `Jobs:MaxParallelSteps`, rounded down and never less than one. For example, an effective
maximum of 20 gives a non-Jobs silo a budget of 5. Silos with the Jobs role keep the existing limit,
regardless of the non-Jobs override. All silos have the Jobs role by default, so existing deployments
keep their current throttling. The role does not restrict step placement yet.

Set `Cratis__Chronicle__Jobs__MaxParallelStepsWithoutJobsRole` to override the reduced budget.

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

