<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/event-sources.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Event-source definitions and streams

Verified at `Chronicle.Kotlin v6.13.0`: `eventSources/EventSource.kt`,
`EventStream.kt`, `ConcurrencyDimension.kt`, `EventSources.kt` and
`eventSequences/EventSequenceEventSources.kt`. Requires kernel `19.30.0` or later.

Definitions are optional: an append without one keeps the ordinary routing
contract. A definition is a carrier class, never instantiated, discovered with
other client artifacts and registered after event types by `ArtifactRegistrations`.

```kotlin
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import io.cratis.chronicle.eventSources.ConcurrencyDimension
import io.cratis.chronicle.eventSources.EventSource
import io.cratis.chronicle.eventSources.EventStream

@EventSource(
    name = "Account",
    description = "A customer account",
    concurrency = [ConcurrencyDimension.EventSourceId],
    streams = [EventStream("Transactions", "Money in and out")]
)
class AccountEventSource
```

Excerpt inside a suspending function with an `IEventSequence` named `eventLog`,
a string `accountId` and a registered event `deposited`:

```kotlin
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import io.cratis.chronicle.eventSequences.appendThroughEventSource

eventLog.appendThroughEventSource(
    AccountEventSource::class, accountId, deposited, eventStream = "Transactions"
)
```

`appendThroughEventSource` extends `IEventSequence` and takes the definition
`KClass`, event-source id, event, optional stream name, then `AppendOptions`.
`appendManyThroughEventSource` takes a `List<Any>` in the event position and
commits one atomic single-source batch. Alternatively set
`AppendOptions(eventSource = AccountEventSource::class, eventStream = "Transactions")`.
Java can use `AppendOptionsBuilder.throughEventSource` on its blocking append path.

The definition resolves source type, validates the named stream and records its
name in `EventContext.eventSource` (empty for events without a definition,
including older stored events). Its concurrency dimensions apply unless an
explicit scope is supplied. Explicit routing must agree with the definition;
this is not a way to silently override another route.

- `UnknownEventSource`: definition was not discovered.
- `EventStreamDoesNotBelongToEventSource`: stream name is not declared.
- `EventRoutingContradictsEventSource`: explicit route contradicts the definition.
- `ConflictingEventSourceConcurrency`: incompatible declared concurrency.

Verify discovery and registration against the target kernel, then specify both
successful routing and rejected contradictory options. Do not treat an initial
registration gate as proof every artifact registered.
