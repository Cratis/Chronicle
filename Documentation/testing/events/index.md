---
uid: Chronicle.Testing.Events
---
# Testing Events

Chronicle provides the `EventScenario` utility for testing event-appending behavior entirely in-process, with no Chronicle server, database, or network required.

## Topics

| Guide | Description |
|-------|-------------|
| [EventScenario](scenario) | Append events and pre-seed state using `EventScenario` and the `Given` builder |
| [Append Assertions](assertions) | Assert on `IAppendResult` using the built-in `Should*` extension methods |
| [Event Sequence Assertions](event-sequence-assertions) | Assert on the event sequence itself and on `AppendedEventWithResult` entries |
