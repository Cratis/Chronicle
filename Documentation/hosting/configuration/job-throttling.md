---
title: Configure job throttling in Cratis Chronicle Server
description: Configure Cratis Chronicle Server job throttling with MaxParallelSteps to limit concurrent job steps during replays and catch-up work.
---
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
| MaxActiveStepsPerJob | number | 4 × the effective maximum parallel steps, at least 32 | Maximum number of steps a single job keeps active at once (minimum 1) |

The limit that applies is the smaller of `Jobs:MaxParallelSteps` and `Observers:MaxConcurrentPartitions` (default 32, see [Observers](observers.md)). With the defaults, `Jobs:MaxParallelSteps` only has an effect below 32, so raising it above 32 needs `Observers:MaxConcurrentPartitions` raised as well.

Alternatively, you can use environment variables:

```bash
Cratis__Chronicle__Jobs__MaxParallelSteps=4
```

## Active steps per job

A job with thousands of steps, such as the catch-up of an observer with thousands of partitions, does not activate a
grain for every step at once. It keeps `Jobs:MaxActiveStepsPerJob` steps active and starts the next one each time a
step reaches an outcome; the steps not yet started stay in storage. Only `MaxParallelSteps` of the active steps run at
a time, so the limit only needs to keep those slots fed. Lower it to hold less memory during a large catch-up, or
raise it if steps are short and the slots sit idle between them.

```bash
Cratis__Chronicle__Jobs__MaxActiveStepsPerJob=64
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

