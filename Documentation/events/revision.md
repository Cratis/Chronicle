# Event Revision

Event revision lets you correct the content of a previously appended event when you discover it contained incorrect data. Rather than modifying the immutable event log, revision records a new version of the event's content alongside the original, preserving the full audit trail while making the corrected data available to all consumers.

## Why revision instead of deletion

Events are immutable by design. Deleting or modifying an event would break the audit trail and could leave projections, reactors and reducers in an inconsistent state. Revision solves this by storing a corrected payload for the event while keeping the original intact. All observers that read or replay the event see the revised content.

## When to use revision

Use revision when:

- A field was recorded with incorrect data due to a bug or data entry error.
- Regulatory or business requirements demand a correction to a specific event's content.
- You need to fix data without triggering a full rewind of observers.

Do not use revision as a substitute for appending new domain events. If something genuinely happened in the domain — for example a correction being made by a user — model that as a new event instead.

## Revising events from the Workbench

The Workbench provides a UI for revising events in the event log:

1. Open the **Event Log** view for the event store and namespace you want to manage.
2. Select the event you want to correct.
3. Click the **Revise** button in the event detail panel.
4. In the dialog that appears, change the values you want to correct.
5. Click **OK** to submit the revision.

The corrected content is stored alongside the original event. All subsequent reads of that event return the revised content. The original payload remains accessible in the full event history.

## What gets stored

When you revise an event, Chronicle stores:

- The sequence number of the original event.
- The event type of the revised content (which must be the same type or a compatible type).
- The full JSON payload of the corrected event.
- The causation and identity of whoever submitted the revision.

Revisions are ordered, so if you revise the same event more than once, the latest revision is the one consumers see.
