---
name: cratis-chronicle-client-typescript
description: Talk to a Chronicle server from a Node.js or TypeScript application with @cratis/chronicle - reflect-metadata and decorator compiler settings, ChronicleClient and connection strings, @eventType classes, eventLog.append, reactors and reducers dispatched by camelCase method name, model-bound and declarative projections, glob-based artifact discovery, and the lazy connect and keepalive lifecycle. Use when a Node application appends to or observes a Chronicle event store. Do not use for the .NET, Kotlin, or Elixir clients, and do not use for Arc React frontends or generated Arc proxies.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-client-typescript/SKILL.md -->

# The Chronicle client for TypeScript

`@cratis/chronicle` is a **standalone Node.js client SDK** that speaks gRPC to a
Chronicle server. It is not a browser library and it is not the Arc TypeScript
proxy layer — an Arc frontend calls generated command and query proxies over
HTTP and never sees this package.

## Verified product sources

This skill is verified against `Cratis/Chronicle.TypeScript` at tag `v4.0.0`
(commit `bed0b86`), which is the version published on npm.

| Package | Version | Where |
| --- | --- | --- |
| `@cratis/chronicle` | `4.0.0` | `Source/package.json:2` |
| `@cratis/chronicle.contracts` | `17.0.0` | `Source/package.json` dependency |
| `@cratis/fundamentals` | `^7.14.0` | **peer dependency** — you install it |

> **Do not read the version out of the repository.** `Source/package.json` says
> `1.0.0`; `publish-version.js` overwrites it at publish time. Take the version
> from npm.

**`v4.0.0` is a major bump and it changed real API.** Guidance written against
`3.x` is wrong in at least three places — event tags did not exist, reducers took
two arguments, and two event-store RPCs were renamed. Do not carry `3.x` examples
forward.

## Project setup

```shell
yarn add @cratis/chronicle @cratis/fundamentals reflect-metadata
```

`@cratis/fundamentals` is a peer dependency, so it does not arrive on its own.
`reflect-metadata` must be imported **once, at the entry point**, before any
decorated class is loaded (`Documentation/getting-started.md:16-27`):

```typescript
import 'reflect-metadata';
```

The package is **ESM-only** (`"type": "module"`, `Source/package.json:34`) and
requires decorator support:

```jsonc
// tsconfig.json
{
    "compilerOptions": {
        "experimentalDecorators": true,
        "emitDecoratorMetadata": true
    }
}
```

Both are set in the shipped sample (`Samples/Console/tsconfig.json:12-13`).

## Connecting

```typescript
import 'reflect-metadata';
import { ChronicleClient, ChronicleOptions } from '@cratis/chronicle';

const client = new ChronicleClient(ChronicleOptions.development());
try {
    const store = await client.getEventStore('<EventStoreName>');
    // ... use store ...
} finally {
    client.dispose();
}
```

`IChronicleClient` is three members — `Source/IChronicleClient.ts:16-39`:

| Member | Line |
| --- | --- |
| `readonly options: ChronicleOptions` | `:18` |
| `getEventStore(name, namespace?): Promise<IEventStore>` | `:27` |
| `getEventStores(): Promise<EventStoreName[]>` | `:33` |
| `dispose(): void` | `:38` |

`ChronicleClient`'s constructor takes exactly one argument —
`constructor(readonly options: ChronicleOptions)`, `Source/ChronicleClient.ts:58`.

**Connecting is implicit and lazy.** There is no `connect()`. The constructor
starts a health-check watchdog and kicks off artifact discovery; the actual
connect happens on the first `getEventStore`/`getEventStores` through an internal
`ensureConnected()`. `getEventStore` also *creates* the store if it does not
exist — it calls `ensureEventStore` (`Source/ChronicleClient.ts:135`).

`dispose()` is **synchronous** (`Source/ChronicleClient.ts:193`), not
`Symbol.dispose` or `Symbol.asyncDispose`. After disposal every call throws.

### Options

`ChronicleOptions` has a **private constructor**
(`Source/ChronicleOptions.ts:67`); use a static factory:

- `ChronicleOptions.fromConnectionString(connectionString, options?)` — `:92`
- `ChronicleOptions.development(options?)` — `:116`

The optional bag accepts `clientArtifactsProvider`, `discoveryPatterns`, and
`defaultSinkTypeId`. `defaultSinkTypeId` defaults to `WellKnownSinks.MongoDB`
(`:84`).

> `programIdentifier`, `softwareVersion`, and `softwareCommit` exist on the type
> (`:69-71`) but **neither public factory forwards them**, so they cannot be set.
> Do not write guidance that configures them.

### The connection string

Schemes `chronicle://` and `chronicle+srv://`, default port `35000`
(`Source/connection/ChronicleConnectionString.ts`). Query parameters: `apiKey`,
`disableTls`, `skipTlsValidation`, `certificatePath`, `certificatePassword`,
`loadBalancer`, `srvNameServer`.

Two facts to carry into production guidance:

- **`skipTlsValidation` defaults to `true`.** TLS is on but the certificate chain
  is not validated, because a development kernel serves a self-signed
  certificate. Set `?skipTlsValidation=false` against a real server.
- **A connection string with no credentials silently falls back to the
  development client credentials** (`Source/connection/ChronicleConnection.ts`).
  An anonymous-looking connection string is not anonymous; it is
  `chronicle-dev-client`.

> **There is no `./connection` subpath export.** `ChronicleConnectionString`,
> `ChronicleConnectionStringBuilder`, `AuthenticationMode`, and `LoadBalancerMode`
> are unreachable from the public entry points — `Source/index.ts` never
> re-exports them and `Source/package.json`'s `exports` map has no
> `"./connection"` key. **Application code passes a connection *string*.** Do not
> write an example that imports a connection-string type.

The subpath exports that do exist: `.`, `./events`, `./eventSequences`,
`./eventStoreSubscriptions`, `./compliance`, `./reactors`, `./reducers`,
`./seeding`, `./readModels`, `./projections`, `./jobs`, `./webhooks`,
`./externalServices`, `./identities`, `./observation`, `./sinks`, `./schemas`,
`./types`, `./artifacts`, `./identity`, `./auditing`, `./correlation`,
`./transactions`.

## Event types

```typescript
import { eventType } from '@cratis/chronicle';

@eventType()
export class <EventName> {
    constructor(readonly <property>: <Type> = <default>) {}
}
```

Seven overloads — `Source/events/eventTypeDecorator.ts:54-60`:

```typescript
export function eventType(): ClassDecorator;
export function eventType(id: string): ClassDecorator;
export function eventType(id: string, generation: number): ClassDecorator;
export function eventType(id: string, generation: number, tombstone: boolean): ClassDecorator;
export function eventType(generation: number): ClassDecorator;
export function eventType(generation: number, tombstone: boolean): ClassDecorator;
export function eventType(tombstone: boolean): ClassDecorator;
```

**The event type id defaults to the class name** — `new EventTypeId(id || constructor.name)`
at `:91`. Two classes with the same name in different modules collide on the wire.

**Give constructor parameters default values.** Member discovery walks explicit
`@field` declarations from `@cratis/fundamentals` first, then tracked properties,
then the properties present on a *default-constructed* instance, then
`design:paramtypes` — `Source/types/TypeIntrospector.ts:52-60`, with the reason
spelled out at `:40-48`: **esbuild and tsx do not emit `design:type` or
`design:paramtypes`**. Under `tsx`, a class whose constructor parameters have no
defaults and no `@field` decorators produces no members and an empty schema. Every
sample event carries defaults for exactly this reason.

Tag an event type at declaration with `@tag(...)` / `@tags(...)` —
`Source/events/tagDecorator.ts:30`, `:42`.

## Appending

`IEventLog` is an empty extension of `IEventSequence`
(`Source/eventSequences/IEventLog.ts:10`); the surface is on `IEventSequence`:

```typescript
append(eventSourceId: string, event: object, options?: AppendOptions): Promise<AppendResult>;               // :44
appendMany(eventSourceId: string, events: object[], options?: AppendOptions): Promise<AppendResult[]>;       // :53
appendMany(events: EventForEventSourceId[], options?: AppendOptions): Promise<AppendResult[]>;               // :61
```

```typescript
const result = await store.eventLog.append('<event-source-id>', new <EventName>('<value>'));
if (!result.isSuccess) {
    // result.constraintViolations, result.concurrencyViolation, result.errors
}
```

- **The event source id is a plain `string`.** There is no `EventSourceId` value
  type at this boundary.
- `AppendResult` — `Source/eventSequences/AppendResult.ts:15-38`: `sequenceNumber`,
  `constraintViolations`, `concurrencyViolation?`, `errors`, `isSuccess`, and
  `waitForCompletion(timeoutMs?)` which defaults to 5000 ms.
- **`sequenceNumber` wraps a `bigint`.** Read it as `result.sequenceNumber.value`.
  `EventContext.sequenceNumber` is a `bigint` too.
- `AppendOptions` — `Source/eventSequences/AppendOptions.ts`: `correlationId?`,
  `eventSourceId?`, `concurrencyScope?`, `concurrencyScopes?`, and **`tags?`**
  (`:22-29`, new in `4.0.0`).

> **Single `append()` cannot target a stream or a subject.** It hardcodes
> `EventSourceType: 'Default'`, `EventStreamType: 'Default'`,
> `EventStreamId: eventSourceId`, and `Subject: eventSourceId`
> (`Source/eventSequences/EventSequence.ts:100-119`). Only `tags` became a real
> option in `4.0.0` (`:117`). To target a different stream or subject, use the
> `EventForEventSourceId[]` overload, whose entries carry `eventStreamType`,
> `eventStreamId`, `eventSourceType`, `subject`, and `tags`.

Correlation id, identity, and the causation chain are picked up from the ambient
module-level managers (`identityProvider`, `causationManager`,
`correlationIdManager`) — you do not pass them.

Several appends as one unit:

```typescript
const unitOfWork = store.unitOfWorkManager.begin();
await store.eventLog.transactional.append('<id>', new <EventName>('<value>'));
await unitOfWork.commit();
```

`ITransactionalEventSequence` returns `Promise<void>`, not an `AppendResult`;
results are on the unit of work.

## Observing

### Handlers are found by camelCase method name

This is the defining convention of this client, and it is **not** the parameter
type. `Source/reactors/Reactors.ts:378-381` computes the method name from the
event class name and looks it up on the prototype:

```typescript
const className = (eventTypeClass as Function).name;
const methodName = className.charAt(0).toLowerCase() + className.slice(1);

if (typeof proto[methodName] === 'function') { /* it is a handler */ }
```

Reducers do the same (`Source/reducers/Reducers.ts:454-460`). A handler whose
method name does not match is simply never called — **there is no error**.

> The docstring at `Source/reactors/reactor.ts:28-29` claims dispatch is by the
> first parameter's type. **It is wrong**, and it is still wrong at `4.0.0`. The
> code is the authority; so are the client snippets under `Documentation/`, which
> state the name rule explicitly.

```typescript
import { reactor } from '@cratis/chronicle/reactors';
import { EventContext, EventForEventSourceId } from '@cratis/chronicle';

@reactor()
export class <ReactorName> {
    async <eventName>(event: <EventName>, context: EventContext): Promise<EventForEventSourceId> {
        return {
            eventSourceId: '<other-event-source-id>',
            event: new <SideEffectEvent>(context.eventSourceId)
        };
    }
}
```

A returned event, array, or `EventForEventSourceId` is appended as a side effect;
a failed side-effect append fails the partition.

> **The handler receives `JSON.parse(event.Content)` — a plain object, not an
> instance of your event class** (`Source/reactors/Reactors.ts:297`, `:320`;
> `Source/reducers/Reducers.ts:377`). `instanceof` checks and methods on the event
> class do not work. Read properties only.

`@reducer(id?, eventSequenceId?, readModel?, isActive?)` —
`Source/reducers/reducer.ts:58`. Passing `readModel` also registers that type as a
read model. **The reducer handler takes three arguments at `4.0.0`** —
`reducerInstance[methodName](content, currentState, context)`
(`Source/reducers/Reducers.ts:400`). The third is optional by arity, and a handler
may be sync or async.

Restrict an observer to tagged events with `@filterEventsByTag(value)` —
`Source/events/filterEventsByTagDecorator.ts:36`.

### Read models and projections

`store.readModels` — `Source/readModels/IReadModels.ts`: `getInstanceById(type, key, sessionId?)`
(`:38`), `getInstances(type, eventCount?)` (`:46`), `getSnapshotsById` (`:54`),
`watch(type): AsyncIterable<ReadModelChangeset<T>>` (`:61`), plus `materialized`
for paged access.

Two projection styles:

- **Model-bound** — decorators on the read model, from
  `@cratis/chronicle/projections`: `fromEvent`, `fromEvery`, `fromAll`, `setFrom`,
  `setFromContext`, `setValue`, `join`, `addFrom`, `subtractFrom`, `increment`,
  `decrement`, `count`, `childrenFrom`, `nested`, `clearWith`, `removedWith`,
  `removedWithJoin`, `noAutoMap`, `notRewindable`, `passive`.
- **Declarative** — `@projection(id?, readModelType?, eventSequenceId?)` on a class
  implementing `IProjectionFor<TReadModel>` with
  `define(builder: IProjectionBuilderFor<TReadModel>): void`.

Constraints are `@constraint()` on a class implementing `IConstraint` with a
`define(builder)`; the builder gives `unique(...)`, `uniqueFor(...)`,
`perEventSourceType`, `perEventStreamType`, `perEventStreamId`. **There are no
model-bound constraint decorators in this client** — the class-plus-builder form
is the only one.

## Discovery is a runtime file glob — this is the biggest difference

`ChronicleOptions.discoveryPatterns` defaults to
`['**/*.ts', '!**/*.d.ts', '!**/node_modules', '!**/dist', '!**/build', '!**/.git', '!**/.vscode', '!**/*.spec.ts', '!**/*.test.ts']`
(`Source/ChronicleOptions.ts:73-83`), and the client `import()`s every matching
file at construction time so that the decorators run. Registration is into a
process-wide static map.

Two consequences that decide whether an application works at all:

1. **The default pattern matches `.ts`, not `.js`.** It works under a
   TypeScript-capable runtime — the sample runs `tsx index.ts` — and a compiled
   `dist/*.js` deployment discovers **nothing** unless `discoveryPatterns` is
   overridden.
2. **Decorators only register when their module is imported.** Side-effect imports
   of event, reactor, reducer, and projection modules are load-bearing; the sample
   does exactly this with a comment saying why.

`EventStore.registerArtifacts()` registers event types first, then everything else
in parallel, and re-runs for every cached event store on reconnect.

## Lifecycle

- A health-check watchdog runs `getVersionInfo` every 5 seconds and reconnects on
  failure; the interval handle is `unref()`'d so it does not hold the process open.
- Reconnect uses exponential backoff with jitter, capped at 30 seconds, rebuilding
  the gRPC channel on every attempt.
- **Keepalive registration is what keeps observers alive.** Losing it triggers a
  full reconnect, because the kernel tears down the client's observers otherwise.
- Failed operations are retried once after reconnecting, keyed on gRPC status
  codes 4/13/14 plus a substring match over the error text.

> **The client version reported to the kernel is a hardcoded `'1.0.0'`** with a
> TODO — `Source/ChronicleClient.ts:429`, alongside `ClientType: 'TypeScript'`
> (`:434`). The Chronicle 17 server-side compatibility check therefore sees a
> placeholder from this client, and grepping `Source/` for `CheckCompatibility`,
> `ProtocolVersion`, or descriptor-set handling finds nothing. **Do not tell
> developers this client negotiates protocol compatibility** — it does not.

## No dependency injection

There is none, and none is planned in the source: no container integration, no
`addChronicle`-style registration. Reactors, reducers, projections, constraints,
and seeders are instantiated by the client itself, so **constructor injection does
not work**. Use module-scope collaborators, as the shipped sample does.

## Common pitfalls

| Pitfall | Why it bites |
| --- | --- |
| Naming a reactor method after the handler's purpose | Dispatch is by the camelCase event class name; a mismatch is silently never called |
| `instanceof` on the event inside a handler | It is a `JSON.parse`d plain object, not your class |
| Event class with no default constructor values under `tsx` | No decorator metadata is emitted, so the schema comes out empty |
| Two `@eventType()` classes sharing a class name | The id defaults to the class name and they collide |
| Deploying compiled `dist/*.js` with default discovery | The default glob matches `**/*.ts`; nothing is discovered |
| Omitting a side-effect import of an artifact module | Its decorator never runs, so it is never registered |
| Forgetting `import 'reflect-metadata'` at the entry point | Decorators do not work at runtime |
| Forgetting to install `@cratis/fundamentals` | It is a peer dependency and is not installed for you |
| Importing `ChronicleConnectionString` | There is no `./connection` export; pass a string |
| Expecting single `append()` to set a stream or subject | Those fields are hardcoded; use the `EventForEventSourceId[]` overload |
| A connection string with no credentials | Silently falls back to the development client credentials |
| Shipping the default TLS behavior | `skipTlsValidation` defaults to `true` |
| Constructor-injecting a dependency into a reactor | There is no DI; the client constructs it |
| Carrying a `3.x` example forward | Tags, the reducer context parameter, and two RPC names changed in `4.0.0` |
| Reading `result.sequenceNumber` as a number | It is a `bigint` behind `.value` |

## Verify

- The installed version is the one you intended, taken from npm, and
  `@cratis/fundamentals` is installed alongside it.
- `reflect-metadata` is imported exactly once, at the entry point.
- `experimentalDecorators` and `emitDecoratorMetadata` are both on.
- Every event class either has `@field` declarations or default constructor
  values, and its schema is non-empty at runtime.
- `discoveryPatterns` matches the files that actually ship in the deployed
  artifact.
- Every reactor and reducer handler is named as the exact camelCase of its event
  class, and an appended event demonstrably reaches it.
- A production connection string carries credentials and `skipTlsValidation=false`.
- `client.dispose()` runs on shutdown.
- Lint, `tsc`, and the test suite are clean against the verified package version.
