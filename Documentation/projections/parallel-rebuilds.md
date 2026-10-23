---
title: Parallel projection rebuilds
description: Opt in to bucketed rebuilds for projections whose event-source partitions write independent documents.
---

Parallel rebuilds let independent event-source partitions rebuild concurrently without keeping one job step per partition. They are off by default. Ordinary live projection handling is unchanged.

## Enable after upgrading every silo

Upgrade **every silo** before enabling buckets. An older silo does not understand the bucket on a job step and can treat that step as a full ordered replay.

Set these values in the kernel's `chronicle.json` configuration:

```json
{
  "Jobs": {
    "ProjectionRebuildBuckets": 8
  },
  "ReadModels": {
    "GuardSinkWritesOnWatermark": true
  }
}
```

`Jobs.ProjectionRebuildBuckets` accepts 0 through 64. Zero keeps ordered rebuilds. Eight is an initial choice, not a throughput guarantee. At most 16 bucket steps run concurrently, subject to the jobs system's own limit.

## Which projections can use buckets

Chronicle checks the stored definition and the active subscription when preparing a rebuild. Only namespaced projections whose event-source partitions write independent documents qualify. Sink watermark guarding must be enabled, and the engine subscription must not be collapsing.

Explicit keys, joins, children, composite keys, removed-with-join mappings, global projections and unsupported or unknown definition shapes retain the ordered path. Reactors and reducers keep their existing paths. Bucketed rebuilds do not opt materialization-generation builds into parallel execution.

Each step persists its bucket index, bucket count, hash version and durable checkpoint. Resuming an existing job retains those assignments even if you change the configured count. New jobs use the new setting. Each bucket reads the sequence and filters its own event sources before dispatch, so increasing the count also increases read amplification.

## Failures and recovery

When a failure identifies its partition and progress authoritatively, the bucket records that partition as failed and continues other partitions. Later events for the failed partition are not delivered again by that attempt.

A dispatched-page exception has no authoritative failed partition or handled prefix. Whether it is a timeout or another exception, the step flushes and stops with its durable checkpoint and a transient partial result. It never bypasses page dispatch or immediately redispatches that uncertain page.

If every bucket finishes, replay completion uses the maximum handled position. If any bucket remains unfinished, completion uses the minimum durable position among unfinished buckets. An unfinished bucket without a checkpoint keeps that position unavailable. A completed empty bucket does not hold back completion.

Buckets can share the sink's bulk window. One bulk-write failure can therefore affect more than one step; buckets do not isolate sink failures. Eligibility is checked at admission, not continuously against concurrent definition changes. Keep the default ordered mode when those trade-offs do not fit your deployment.
