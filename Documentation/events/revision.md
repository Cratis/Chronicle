---
title: Cratis Chronicle event revision for correcting recorded data
description: Cratis Chronicle revision corrects stored event content, retains the original, and requests replay for affected observers that permit replay.
---
# Event Revision

Event revision in Cratis Chronicle corrects incorrectly recorded content at an event's existing sequence number. It retains the original payload and stores a revision alongside it, so later reads can use the corrected content. Use it to repair a recorded mistake, not to erase data or record a new business decision.

## Why revision instead of deletion

Revision keeps the event's position in the sequence while changing the content returned by subsequent event reads. Retaining the original lets you distinguish the recorded data from its later correction; it does not make revision a data-erasure operation. Use [redaction](redaction.mdx) when stored event content must be removed.

After applying a revision, Chronicle requests replay for affected observers marked replayable. Their partitioning determines whether it replays one event-source partition or the whole observer. This can reprocess other events too, so review handlers and external effects before submitting a revision. Non-replayable observers are not automatically rebuilt.

## When to use revision

Use revision when a bug or data-entry error made an event's recorded content incorrect and you need subsequent reads to use the corrected value. The revision must keep the original event type identifier; a different but structurally compatible type is not accepted.

If a new business action happened, such as cancelling an order or reversing a posted entry, append a new domain event instead. [Compensation](compensation.md) links a new corrective event to the event it offsets without changing the earlier content.

## Revising events from the Workbench

In the Workbench, locate the event and review the effects of replay before making the correction:

1. Open the **Event Log** view for the event store and namespace you want to manage.
2. Select the event you want to correct.
3. Click the **Revise** button in the event detail panel.
4. In the dialog that appears, change the values you want to correct.
5. Click **OK** to submit the revision.

Submitting records a revision request in the system event sequence. A built-in reactor applies it asynchronously; accepting the request does not mean the content or dependent read models have already changed. Reopen the event to check the revised content, then inspect affected observers for replay progress and failures. [Observers](../concepts/observers.mdx) explains those states.

## What gets stored

The revision request identifies the event sequence, original sequence number, event type and generation, and corrected JSON content. Chronicle applies compliance protection to the corrected content using the original event's subject before recording the request.

The stored revision includes the corrected content and its hash, generation, revision time, correlation, causation, and submitting identity. The original payload remains stored alongside the revisions. Ordinary event reads select the latest stored revision and its generation.
