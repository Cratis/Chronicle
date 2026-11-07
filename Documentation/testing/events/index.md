---
uid: Chronicle.Testing.Events
title: Cratis Chronicle event append testing
description: Test event appends in Cratis Chronicle without a running server. Use EventScenario to seed history, act, and assert on append results and events.
---
<a id="testing-events"></a>

Cratis Chronicle's `EventScenario` lets you test event-appending code in process without a server, database, or network. Seed the events that already happened, run the operation under test, then check what it appended. The client tabs show each client's supported workflow; their APIs and constraint coverage differ.

Start with the [EventScenario guide](scenario) to set up a spec. Use the assertion guides when you need to check an append's outcome or inspect the events in the sequence.

## Topics

| Guide | Description |
|-------|-------------|
| [EventScenario](scenario) | Set up event history and exercise an append; .NET uses the `Given` and `When` builders |
| [Append Assertions](assertions) | Check success or rejection; .NET provides `Should*` extensions on `IAppendResult` |
| [Event Sequence Assertions](event-sequence-assertions) | Check event content and order, including .NET `AppendedEventWithResult` entries |
