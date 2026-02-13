# Appending

Appending adds a single event to an event sequence. The event log is the default sequence, but you can also append to any custom sequence your system defines.

When you append an event, Chronicle:

- Validates the event type and captures metadata
- Applies any [cross-cutting properties](cross-cutting-properties.md)
- Associates the event with its event source
- Assigns the next sequence number
- Persists the event and metadata atomically
- Updates the sequence state

Related reading:

- [Appending an event to the event log](../recipes/appending-an-event-to-event-log.md)

