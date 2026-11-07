---
title: Rebuild a read model in Cratis Chronicle
description: Rebuild a Chronicle read model after a projection change by replaying its observer, checking the job result, and verifying the derived data.
---

After changing a [projection](../concepts/projection.md) in Cratis Chronicle, rebuild its read model by replaying its observer: the event-processing unit that tracks the projection's progress. Replay applies the updated mapping to retained event history without rewriting the event log.

## Why this works

A projection describes how recorded events become a view that the application can query. If the view is wrong because of its mapping, fix the mapping and replay the [event log](../concepts/event-sequence.md) through it instead of patching each stored instance. The history must contain the data the new view needs; replay cannot invent a value that was never recorded.

## Do it

This procedure is for a namespace-local projection. Projections with `Global` or `Both` scope reject namespace-local replay because their read-model storage is shared across namespaces. Those views need a coordinated rebuild.

1. Update the projection and register the changed definition. Inspect its observer state before requesting another replay: definition changes can trigger replay automatically, depending on the kernel's definition-evolution policy.
2. Choose the rebuild scope. Replay the full observer when the mapping change affects the whole view. If only one partition's state needs repair, use [replay-partition](/cli/chronicle/observers/#replay-partition) instead. If existing state is correct and processing only needs to resume after a failure, use [failed-partition recovery](../operations/recover-a-failed-partition) rather than rebuilding it.
3. Find the observer id and confirm the target event store and namespace. The CLI's [Replay a projection](/cli/scenarios/replay-a-projection/) walks through finding and replaying it. Use the [Cratis CLI](/cli/) to request the full replay:

   ```bash
   cratis chronicle observers replay <observer-id>
   ```

   The command asks for confirmation. See [`cratis chronicle observers replay`](/cli/chronicle/observers/#replay) for event-store and namespace options before accepting it. The [Chronicle Workbench](/chronicle/workbench/) can also replay an observer, and clear an observer's quarantine, from its observers view for a user who is allowed to.

4. Inspect the [replay job](/cli/chronicle/jobs/) until it completes successfully, then check that the observer has caught up with the event log. A successful replay request is not evidence that the rebuild has finished. Check observer state and failed partitions in the Workbench.
5. Read back instances whose histories exercise the changed mapping, including older events. Confirm their values against the expected result, not just their presence in the read model.

## Notes

A replay runs asynchronously. Plan what readers should see while the view is being rebuilt. The work includes processing history, so a small current read model can still have a large rebuild behind it.

If a reducer builds the view instead of a projection, identify its observer rather than a projection observer before choosing replay. The same distinction between replaying history and retrying a failed event still applies.

## See also

- [Read Models](../read-models/) — consistency and how read models are served.
- [Projections](../projections/) — the full projection model.
