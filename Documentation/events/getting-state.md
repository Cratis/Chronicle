# Getting state

Event sequence state provides information about how far a sequence has progressed. The most common state value is the tail sequence number, which represents the latest event appended to the sequence. Use the `IEventSequence` APIs, such as `GetTailSequenceNumber` and related state calls, to capture the current position.

Use sequence state for scenarios such as:

- Tracking progress for consumers and observers
- Capturing a point in time for read model time travel
- Avoiding duplicate processing when resuming work

For point-in-time reads of read models, capture the sequence position from the event sequence state and use it alongside the read model APIs described in the read models guides.

Related reading:

- [Getting a Collection of Instances](../read-models/getting-collection-instances.md)

