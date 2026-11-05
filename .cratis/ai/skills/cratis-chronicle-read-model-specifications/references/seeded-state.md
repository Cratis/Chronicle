<!-- cratis-ai-managed: skills/cratis-chronicle-read-model-specifications/references/seeded-state.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Seeded state and appended-event processing

Requires Chronicle 19.83.0 for seeded state; verified against v19.110.1 sources.

Verified in `Source/Clients/Testing/ReadModels/`
(`ReadModelSourceGivenBuilder.cs`, `ReadModelScenario.cs`) and
`Source/Clients/Testing/Events/EventStoreForTesting.cs`.

`await scenario.Given.ForEventSourceId(id).ReadModel(instance)`
sets initial state for both `InstanceForEventSourceId(id)` and
`scenario.ReadModels.GetInstanceById<T>(id)`; it does not create event history.
Use it for handler setup, not as proof that a projection built the instance.

Requires Chronicle 19.82.0 for appended-event processing with real contexts.

`ProcessAppendedEvents(...)` folds `AppendedEvent` values rather
than appending them a second time, preserving their real `EventContext`
(sequence number and occurred time) and sorting across sources by sequence
number. Use distinct sequence numbers. Bare events supplied through `Given`
or `ProcessEventsFor` still use synthetic contexts. With seeded instances,
the fold's results replace the scenario's `ReadModels` instances; this does
not prove kernel storage or replay durability. Seeding the same key twice
throws `ReadModelAlreadySeeded`; models with no projection/reducer keep
lookup-only behavior.

Requires Chronicle 19.106.0 for shared default test-store schema generation.

Default test stores share a JSON schema generator per process,
while each store gets a separate schema copy. Set
`EventStoreForTesting.ShareSchemaGenerator = false` for per-store metadata
providers; custom service providers already bypass sharing. Configure the
static switch at suite setup, not concurrently between specifications.
