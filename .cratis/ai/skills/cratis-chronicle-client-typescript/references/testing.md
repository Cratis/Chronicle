<!-- cratis-ai-managed: skills/cratis-chronicle-client-typescript/references/testing.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Testing without a kernel

## Contents

- Appends — EventScenario
- Deliveries — ReactorScenario
- Materialization — ReadModelScenario
- Use kernel-backed tests outside the boundary

Source: `Cratis/Chronicle.TypeScript`:
`Source/testing/index.ts`, `EventScenario.ts`, `ReactorScenario.ts`,
`ReadModelScenario.ts`, and `Documentation/testing.md`.
Import the three scenarios from `@cratis/chronicle/testing`. They share
production dispatch/definition code within a **fixture-bounded** evaluator;
a green scenario is not proof of kernel storage, scheduling or compliance.

The examples below share these standard-decorator artifacts:

```typescript
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import { field } from '@cratis/fundamentals';
import { eventType, fromEvent, reactor } from '@cratis/chronicle';
import { EventScenario, ReactorScenario, ReadModelScenario } from '@cratis/chronicle/testing';

@eventType()
class MessageRecorded {
    @field(String) text: string;
    constructor(text: string) { this.text = text; }
}

@fromEvent(MessageRecorded)
class Message {
    @field(String) id = '';
    @field(String) text = '';
}

@eventType()
class MessageCopied {
    @field(String) text: string;
    constructor(text: string) { this.text = text; }
}

@reactor()
class CopyMessage {
    messageRecorded(event: MessageRecorded): MessageCopied {
        return new MessageCopied(event.text);
    }
}
```

## Appends — EventScenario

```typescript
const events = new EventScenario({
    artifacts: { eventTypes: [MessageRecorded] }, constraints: 'disabled'
});
await events.given.forEventSource('message-1').events(new MessageRecorded('seed'));
const result = await events.when.forEventSource('message-2').event(new MessageRecorded('act'));
if (!result.isSuccess) throw new Error('Expected the append to succeed');
```

`given...events` seeds sequential single appends; `when.event` is a single
append and `when.events` an atomic batch. `forEventSource(id, options?)` accepts
fixture-backed routing. Omitted routes resolve to `Default`/`All`/`Default`.
`constraints: 'disabled'` is explicit opt-out, never a silent fallback.
Selected `@unique`/fluent constraints support fixture-proven key domains,
removal cycles and scopes; unproven shapes throw.

## Deliveries — ReactorScenario

```typescript
const deliveries = new ReactorScenario(CopyMessage, {
    artifacts: { eventTypes: [MessageRecorded, MessageCopied] }, constraints: 'disabled'
});
await deliveries.when.forEventSource('message-3').events(new MessageRecorded('copy'));
deliveries.shouldHaveProduced(MessageCopied, event => event.text === 'copy');
```

Returned events are **recorded, not appended or recursively delivered**. A
production reactor returning its own subscribed type may loop; the scenario
does not test termination. `produced`, `sideEffects` and `results`
expose observations. Failed deliveries reject the call and prevent subsequent
deliveries; retries/checkpoints are not simulated. Content is client-serialized
JSON, not necessarily kernel-normalized content. Explicit non-subscribed
service appends enter history but are not delivered to observers.

## Materialization — ReadModelScenario

```typescript
const models = new ReadModelScenario(Message);
models.given.forEventSource('message-4').events(new MessageRecorded('read'));
const message = await models.instanceForEventSourceId('message-4');
if (message?.text !== 'read') throw new Error('Expected the materialized message');
```

`instance` requires a sole model; `instanceForEventSourceId` returns null when
absent, and `wasDeletedForEventSourceId` distinguishes deletion. For composition,
use `new ReadModelScenario(Message).observe(events)` (or a ReactorScenario).
Every read replays the source's accepted history; returned reactor effects do
not reach it. Do not mix observing and seeding or observe twice.

Reducers and fixture-backed projections work: scalar root mappings/AutoMap,
removal/recreation, bounded root arithmetic, one level of typed string-keyed
children, and one string-keyed root join. The **whole** definition is validated
before replay, even for events you never seed.

## Use kernel-backed tests outside the boundary

`UnsupportedEventSequenceOperation`, `UnsupportedReactorOperation`, and
`UnsupportedProjectionOperation` name the operation/artifact and require a
kernel-backed test. Do not catch them and call the scenario green.

Unsupported examples include non-fixture-backed constraints, concurrency,
transactions, compliance, named tags on input appends, tag-filtered reactors,
replay/redelivery, nested children, child/nested joins, `removedWithJoin`,
variants and custom/composite keys. Named tags on returned reactor effects can
be recorded, but are not proof of persisted named-tag behavior. Nondefault
routes cannot be evaluated by ReadModelScenario. Read the tagged
`Documentation/testing.md` capability tables before claiming an advanced shape
is covered; use a live kernel for storage and observer fidelity.
