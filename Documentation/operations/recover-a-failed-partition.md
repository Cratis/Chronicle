---
title: Recover a failed Chronicle partition
description: Recover a failed Chronicle partition after fixing its cause, choose retry or replay, handle quarantine, and verify processing and output.
---

Recover a failed partition in Cratis Chronicle by resolving the cause, then retrying the failed event or replaying the affected history if its state needs rebuilding. A partition is the unit of event processing held back by the failure; other partitions can continue unless the observer itself is stopped or quarantined.

Use the Cratis CLI runbook [Fix a stuck observer](/cli/scenarios/fix-a-stuck-observer/) for the commands. The decisions below help you choose the right action before running them.

## Before you start

Follow [Diagnostics](/chronicle/operations/diagnostics/) to identify the event store, namespace, observer and partition, read the exception and check for recovery jobs already in progress. Restore a failed dependency or fix and redeploy the handler as the error requires. Retrying while the cause remains can fail at the same event again.

If the handler performs external work, check whether that work completed before the failure was reported. A retry can deliver the event again, and a replay can revisit earlier events too. Make those effects safe to repeat before starting recovery; [Delivery identity](/chronicle/reactors/delivery-identity/) explains how to recognize repeated deliveries.

## Pick the recovery

First decide whether the existing state is correct. Retry resumes processing; it does not undo bad state written before the failure. Replay is a rebuild, so choose only the scope whose history needs processing again.

- If the state is correct and the cause is resolved, [retry the partition](/cli/chronicle/observers/#retry-partition). It resumes at the failed event without resetting the accumulated state.
- If that partition's state is wrong, [replay the partition](/cli/chronicle/observers/#replay-partition) from its first event. Other partitions are outside that rebuild.
- If a projection or handler change requires rebuilding every partition, [replay the observer](/cli/chronicle/observers/#replay). Follow [Rebuild a read model](/chronicle/scenarios/rebuild-a-read-model/) for the full read-model workflow.

Check quarantine before starting the chosen action. If the observer itself is quarantined, [clear the observer quarantine](/cli/chronicle/observers/#clear-quarantine) first. That action does not release individually quarantined partitions.

A quarantined partition refuses retry. [Clear its quarantine](/cli/chronicle/observers/#clear-partition-quarantine) to reset the retry budget; by default this also starts a retry, so do not immediately submit a second one. The CLI offers `--no-retry` when you want to clear without requesting an immediate retry. This command requires Chronicle 19.29.0 or later. Read its result before proceeding: clearing a partition can succeed while its retry is blocked by observer quarantine.

Replay also requires a replayable observer. In the .NET client, `[OnceOnly]` on a reactor prevents its replay; on a method it excludes that handler from replay. A replay cannot force an excluded handler to run. See [Recovering from failures](/chronicle/operations/recovering-from-failures/) for the recovery behavior and side-effect consequences.

## Done when

A command returning successfully does not prove that processing has finished. Confirm the result in the same event store and namespace:

- The partition no longer appears in [`failed-partitions list`](/cli/chronicle/failed-partitions/#list).
- The observer is running and has processed the relevant events. Its position advances if more matching events arrive; an idle observer need not keep moving.
- For a replay, [the job](/cli/chronicle/jobs/) has completed successfully and the rebuilt read model matches the expected state.
- The expected external work is accounted for, including any effect that completed before the original failure. Check that repeated deliveries did not produce unintended duplicates.

If the partition fails again, inspect the new exception and attempt history through [Diagnostics](/chronicle/operations/diagnostics/) before choosing another action.
