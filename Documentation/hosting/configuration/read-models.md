---
description: Configure replay retention and fenced sink transaction batch sizes in Chronicle Server.
---
# Read Models

Read model configuration controls how many revert containers Chronicle keeps after a read model is replayed, and the batch size read-model writes are split at. This page describes what Chronicle does today, including what it does not do.

## Example configuration

```json
{
  "readModels": {
    "replayedVersionsToKeep": 1,
    "maxBatchSize": 500
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| replayedVersionsToKeep | number | 1 | Number of replay occurrences to keep per read model. `snapshotsToKeep` is an alias that sets the same value. |
| maxBatchSize | number | 500 | Maximum number of items per read-model batch. Must be greater than zero |

There is no age-based setting. Chronicle counts occurrences; it does not look at how old they are.

## What a revert container is

A replay builds the new read model in a temporary `replay-<container>` container. When the replay completes, Chronicle promotes it to the real container and renames the previous one to a revert container. The container is a MongoDB collection or a SQL table, depending on the sink.

A revert container is named `<container>-<yyyyMMddHHmmss>-<guid8>`, where the timestamp is when the replay started. Chronicle records each replay as an occurrence in the namespace: the `replayed-read-models` collection on MongoDB, or the `ReplayedReadModels` table on SQL.

## Replay retention behavior

Retention is counted per event store, namespace and read model. It keeps the newest `replayedVersionsToKeep` occurrences, ordered by when the replay started, and deletes the revert container of every older one, followed by its occurrence record.

Chronicle applies retention at the start of the next replay of that read model, before it creates the new replay context. The occurrence for a replay is recorded when that replay ends. As a result, up to `replayedVersionsToKeep` + 1 tracked revert containers exist between replays:

- `0` removes every previous occurrence when the next replay starts, so one revert container exists after a replay.
- `1` (default) keeps the previous occurrence while the next replay runs. Once that replay ends there are two revert containers, and the older one is deleted when the replay after that starts.
- A higher number keeps more history for diagnostics or rollback workflows, at the cost of storage.

Read models owned by the Chronicle kernel itself apply `0` at the start of a replay and again after promotion, so they keep no revert container once a replay has finished.

Apart from that extra pass for kernel-owned read models, retention runs only at the start of a replay. There is no pass at startup, on a schedule or based on age. A revert container for a read model that is never replayed again stays where it is.

The `rollbackWindow` and `maxFollowingRetiredGenerations` settings belong to projection generations, which are off by default. They are separate and do not change this retention.

## What Chronicle never removes

These are not cleaned up automatically today:

- **Revert containers without an occurrence record.** If a replay finishes with failed partitions, the new container is still promoted and the previous one is renamed, but the replay is not recorded. Nothing then knows about that revert container.
- **A leftover `replay-<container>` container.** Chronicle does not report or delete it. Only the next replay of the same read model clears it, and Chronicle refuses to delete it as a revert container.

An empty replay creates no revert container but still records an occurrence. That occurrence takes up a retention slot, so retention can delete the previous real revert container sooner than the count suggests.

## Checking for leftovers

To see what Chronicle tracks, open the Read Models page in the Workbench. The occurrence selector lists the tracked occurrences with their date and generation, and lets you browse each one. The CLI lists the same occurrences, including the container and revert container names:

```shell
cratis chronicle read-models occurrences <READ_MODEL_TYPE>
```

On MongoDB, you can find containers Chronicle does not track by comparing the collection names in the database with the `containerName` and `revertContainerName` values in the `replayed-read-models` collection. A collection named like a revert container but missing from that collection is untracked. On SQL, the occurrences are in the `ReplayedReadModels` table, but some databases shorten long table names, so a physical table name may not match the recorded name exactly. Check such tables by hand rather than by exact name. Do not delete a `replay-<container>` container while its read model is replaying.

Chronicle makes no promise about how long a revert container exists. Do not rely on it for data retention or legal requirements.

## Fenced sink writes

With `Features.ProjectionGenerations` enabled, each MongoDB sink transaction covers at most `maxBatchSize` documents. A bulk flush larger than that is split into several transactions whose writes are idempotent, so a retry of a part is safe.

A single changeset that touches more documents than `maxBatchSize` and contains a non-idempotent operation, for example a child addition (`$push`), cannot be split safely and is refused. Chronicle raises `OversizedChangesetCannotBeSplit`, logs an error naming the read model, key, partition, collection, the blocking operations and `maxBatchSize`, and the partition fails.

To resolve it:

1. Raise `maxBatchSize` (`Cratis__Chronicle__ReadModels__MaxBatchSize`) above the number of documents the changeset touches. Larger transactions run longer, and MongoDB aborts a transaction that exceeds `transactionLifetimeLimitSeconds` (default 60).
2. Retry the failed partition.
