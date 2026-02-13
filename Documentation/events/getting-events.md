# Getting events

Event sequences support multiple ways of reading events depending on your needs. The `IEventSequence` APIs (and the specialized `IEventLog`) cover common patterns such as:

- Reading events from a sequence in order
- Reading events for a specific event source
- Reading a range of events based on sequence numbers
- Reading a fixed number of events from the tail

Use the client APIs to select the pattern that best matches your scenario, such as replaying state, building read models, or auditing changes.

