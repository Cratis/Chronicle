---
description: Configure how many replay-generated read model versions Chronicle Server keeps and how many documents one fenced sink transaction may hold.
---
# Read Models

Read model configuration controls how many replay-generated read model versions Chronicle keeps when projections are replayed, and the batch size read-model writes are split at.

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
| replayedVersionsToKeep | number | 1 | Number of replay-generated read model versions to keep per read model |
| maxBatchSize | number | 500 | Maximum number of items per read-model batch. Must be greater than zero |

## Replay retention behavior

When a new replay starts, Chronicle applies retention before creating the next replay context:

- Keeps the latest `replayedVersionsToKeep` replay versions for each read model.
- Removes older replay versions.
- Deletes the associated sink container for each removed version (for example, a MongoDB collection or SQL table).

Set `replayedVersionsToKeep` to:

- `0` to remove all previous replay versions on each new replay.
- `1` (default) to keep only the most recent replay version.
- A higher number to keep more superseded versions for inspection and manual recovery.

Kept versions stop receiving events once a newer version replaces them. Chronicle has no command to restore one. Kernel-owned read models keep no previous versions, regardless of this setting.


## Fenced sink writes

With `Features.ProjectionGenerations` enabled (experimental), each MongoDB sink transaction covers at most `maxBatchSize` documents. A bulk flush larger than that is split into several transactions whose writes are idempotent, so a retry of a part is safe.

A single changeset that touches more documents than `maxBatchSize` and contains a non-idempotent operation, for example a child addition (`$push`), cannot be split safely and is refused. Chronicle raises `OversizedChangesetCannotBeSplit`, logs an error naming the read model, key, partition, collection, the blocking operations and `maxBatchSize`, and the partition fails.

To resolve it:

1. Raise `maxBatchSize` (`Cratis__Chronicle__ReadModels__MaxBatchSize`) above the number of documents the changeset touches. Larger transactions run longer, and MongoDB aborts a transaction that exceeds `transactionLifetimeLimitSeconds` (default 60).
2. Retry the failed partition.
