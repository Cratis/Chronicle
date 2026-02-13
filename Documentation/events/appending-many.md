# Appending many

Appending many events in a single call is useful for batch workflows such as imports, migrations, or multi-step business operations.

Batch appends preserve ordering and reduce per-event overhead compared to appending each event individually.

AppendMany is transactional all the way to storage. Either all events in the batch are committed, or none are. Chronicle assigns sequence numbers in order, persists the events and metadata atomically, and updates the sequence state once the batch succeeds.

Use this approach when you already have a set of events that should be appended together.

Related reading:

- [Appending an event to the event log](../recipes/appending-an-event-to-event-log.md)

