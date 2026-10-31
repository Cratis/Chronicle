---
description: Configure how many revert containers Chronicle Server keeps after a read model replay, when it deletes them, and what it never cleans up.
---
# Read Models

Read model configuration controls how many revert containers Chronicle keeps after a read model is replayed. This page describes what Chronicle does today, including what it does not do.

## Example configuration

```json
{
  "readModels": {
    "replayedVersionsToKeep": 1
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| replayedVersionsToKeep | number | 1 | Number of replay occurrences to keep per read model. `snapshotsToKeep` is an alias that sets the same value. |

There is no age-based setting. Chronicle counts occurrences; it does not look at how old they are.

## What a revert container is

A replay builds the new read model in a temporary `replay-<container>` container. When the replay completes, Chronicle promotes it to the real container and renames the previous one to a revert container. The container is a MongoDB collection or a SQL table, depending on the sink.

A revert container is named `<container>-<yyyyMMddHHmmss>-<guid8>`, where the timestamp is when the replay started. Chronicle records each replay as an occurrence in the `replayed-read-models` collection of the namespace.

## Replay retention behavior

Retention is counted per event store, namespace and read model. It keeps the newest `replayedVersionsToKeep` occurrences, ordered by when the replay started, and deletes the revert container of every older one, followed by its occurrence record.

Chronicle applies retention at the start of the next replay of that read model, before it creates the new replay context. The occurrence for a replay is recorded when that replay ends. As a result, up to `replayedVersionsToKeep` + 1 revert containers exist between replays:

- `0` removes every previous occurrence when the next replay starts, so one revert container exists after a replay.
- `1` (default) keeps the previous occurrence while the next replay runs. Once that replay ends there are two revert containers, and the older one is deleted when the replay after that starts.
- A higher number keeps more history for diagnostics or rollback workflows, at the cost of storage.

Read models owned by the Chronicle kernel itself apply `0` at the start of a replay and again after promotion, so they keep no revert container once a replay has finished.

Retention runs only at the start of a replay. There is no pass at startup, on a schedule or based on age. A revert container for a read model that is never replayed again stays where it is.

The `rollbackWindow` and `maxFollowingRetiredGenerations` settings belong to projection generations, which are off by default. They are separate and do not change this retention.

## What Chronicle never removes

These are not cleaned up automatically today:

- **Revert containers without an occurrence record.** If a replay finishes with failed partitions, the new container is still promoted and the previous one is renamed, but the replay is not recorded. Nothing then knows about that revert container.
- **A leftover `replay-<container>` container.** Chronicle does not report or delete it. Only the next replay of the same read model clears it, and Chronicle refuses to delete it as a revert container.

An empty replay creates no revert container but still records an occurrence. That occurrence takes up a retention slot, so retention can delete the previous real revert container sooner than the count suggests.

## Checking for leftovers

To see what Chronicle tracks, open the Read Models page in the Workbench. The occurrence selector lists the tracked occurrences with their date and generation, and lets you browse each one. There is no CLI command that lists them.

To find containers Chronicle does not track, compare the container names in your database with the `containerName` and `revertContainerName` values in the `replayed-read-models` collection. A container named like a revert container but missing from that collection is untracked. Do not delete a `replay-<container>` container while its read model is replaying.

Chronicle makes no promise about how long a revert container exists. Do not rely on it for data retention or legal requirements.
