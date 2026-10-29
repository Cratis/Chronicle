---
description: Configure how many replay-generated read model versions Chronicle Server keeps when projections are replayed.
---
# Read Models

Read model configuration controls how many replay-generated read model versions Chronicle keeps when projections are replayed.

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
| replayedVersionsToKeep | number | 1 | Number of replay-generated read model versions to keep per read model |

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

