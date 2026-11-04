---
description: Configure Chronicle constraint-index repair retries, time budgets, cadence, poison threshold, and memory caps in chronicle.json or environment variables.
---
# Constraint index repair

The `constraintIndexRepair` section controls how an event sequence keeps its constraint indexes in step with the event log. When an index write fails or runs out of time, the sequence repairs the index from the log rather than failing the append. Use this section to tune the retries, time budgets, repair cadence, and memory caps of that repair. See [Constraints](../../understanding-constraints.mdx) for the constraints themselves.

The defaults are suitable for most installations. Change them only when your storage latency or memory budget calls for it.

## Example configuration

Put these settings at the root of `chronicle.json`, not inside a `Cratis` or `Chronicle` wrapper. The following example shows the defaults:

```json
{
    "constraintIndexRepair": {
        "updateAttempts": 3,
        "attemptTimeout": "00:00:01",
        "postCommitBudget": "00:00:02",
        "timerTurnBudget": "00:00:02",
        "appendRepairBudget": "00:00:05",
        "appendRepairInterval": "00:00:05",
        "poisonThreshold": 5,
        "foldKeyLimit": 100000,
        "catchUpExclusionLimit": 100000,
        "catchUpSlots": 4
    }
}
```

| Setting | Meaning |
| --- | --- |
| `updateAttempts` | Total in-turn attempts, the first try included, to write an index update. Between 1 and 10. Each retry adds delay to the append. |
| `attemptTimeout` | Longest a single index write may run before it is abandoned. Must be greater than zero. |
| `postCommitBudget` | Time budget for updating the indexes after an append commits. Must be greater than zero. |
| `timerTurnBudget` | Time budget for one timer-driven repair turn. A catch-up turn runs for this budget minus 250 milliseconds. At least 500 milliseconds. |
| `appendRepairBudget` | Time budget for a repair performed in the turn of an append, and for the repair that follows an index rebuild. At least 500 milliseconds. |
| `appendRepairInterval` | Minimum time between repairs started by appends. Zero lets every append turn repair. Must not be negative. |
| `poisonThreshold` | Consecutive identical failures at the same event position after which the failure is settled as poison. At least 2. |
| `foldKeyLimit` | Maximum staged keys and differences a single fold retains. At least 1. |
| `catchUpExclusionLimit` | Maximum excluded hashes a catch-up retains before its boundary is raised. At least 1. |
| `catchUpSlots` | Catch-ups that may run concurrently in a silo, for both executing turns and retained memory. At least 1. |

Chronicle refuses to start when a value is out of range, and the error names the setting.

## Sizing against the response timeout

The budgets are sized against the 30-second `clustering.responseTimeout`. The worst case for one append turn is approximately:

`timerTurnBudget` + 2 × `attemptTimeout` + `appendRepairBudget` + 2 × `postCommitBudget` + 2 × 2 seconds of read-back

With the defaults that is 2 + 2 + 5 + 4 + 4 = 17 of the 30 seconds. When you lower the response timeout or raise a budget, keep that sum well below the response timeout. Chronicle does not check the sum at startup.

## Environment variables

To override a single setting through the environment, use the `Cratis__Chronicle__ConstraintIndexRepair__` prefix, for example `Cratis__Chronicle__ConstraintIndexRepair__AppendRepairBudget=00:00:05`.

## See also

- [Configuration precedence](configuration-precedence.md)
- [Environment variables](environment-variables.md)
- [Events](events.md)
- [Clustering](clustering.md)
