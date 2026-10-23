---
name: cratis-chronicle-client-typescript
description: Connect Node.js applications to Chronicle with @cratis/chronicle, including reflect-metadata, compiler settings, camelCase reactor dispatch, projections, reducers, variants and the lazy-connect lifecycle. Use when configuring @cratis/chronicle decorators, glob discovery or eventLog.append in a standalone Node.js process. Do not use for JVM connections (use cratis-chronicle-client-kotlin), OTP supervision (use cratis-chronicle-client-elixir), or React generated proxies (use cratis-arc-react-page).
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-client-typescript/SKILL.md -->

# The Chronicle client for TypeScript

`@cratis/chronicle` is a **standalone Node.js client SDK** that speaks gRPC to a
Chronicle server. It is not a browser library and it is not the Arc TypeScript
proxy layer — an Arc frontend calls generated command and query proxies over
HTTP and never sees this package.

## Common pitfalls

| Pitfall | Why it bites |
| --- | --- |
| Naming a reactor method after the handler's purpose | Use the camelCase event class name or explicit `@handles(EventType)` |
| `instanceof` on the event inside a handler | It is a `JSON.parse`d plain object, not your class |
| Event fields with no `@field` types under `tsx` | No type metadata is emitted; unresolved members make registration fail |
| Two `@eventType()` classes sharing a class name | The id defaults to the class name and they collide |
| Deploying compiled `dist/*.js` with default discovery | Compiled-JS entries do not scan; import artifact modules explicitly |
| Omitting a side-effect import of an artifact module | Its decorator never runs, so it is never registered |
| Forgetting `import 'reflect-metadata'` at the entry point | Decorators do not work at runtime |
| Forgetting to install `@cratis/fundamentals` | It is a peer dependency and is not installed for you |
| Importing `ChronicleConnectionString` | There is no `./connection` export; pass a string |
| Carrying a pre-6.0 stream forward | Pass all three route dimensions explicitly; omitted routes now resolve to `Default`/`All`/`Default` |
| A connection string with no credentials | Silently falls back to the development client credentials |
| Shipping the default TLS behavior | `skipTlsValidation` defaults to `true` |
| Constructor-injecting without an `artifactActivator` | By default the client constructs the artifact itself |
| Side-effect reactor without `@onceOnly()` | Reactors run again on kernel replay by default |
| Reading `result.sequenceNumber` as a number | It is a `bigint` behind `.value` |
| A `@globalFor` mapping targeting a property one variant lacks | `GlobalHandlerPropertyNotOnVariant` when the group is built, not a silently skipped mapping |
| Expecting a `@globalFor`-only class to appear as a projection | It is never registered on its own; it is merged into its variants |

## Verified product sources

Source: `Cratis/Chronicle.TypeScript`, reading
`Source/**` and `Documentation/getting-started.md`, `testing.md`,
`migrate-append-routing.md` and `reactors.md`.

| Package | Where |
| --- | --- |
| `@cratis/chronicle` | npm; `Source/package.json` carries a placeholder version |
| `@cratis/chronicle.contracts` | Client's pinned dependency, not the latest contracts package |
| `@cratis/fundamentals` | **peer dependency** — you install it |

> **Do not read the version out of the repository.** `Source/package.json` carries
> a placeholder; `publish-version.js` overwrites it at publish time. Take the version
> from npm.

**Upgrade the kernel first.** Reactors replay by default, so review side-effect handlers when
upgrading.

Requires Chronicle kernel 18.4.1 for the contract compatibility check and for omitted append routes.

Requires `@cratis/chronicle` 6.49.0, Chronicle kernel 19.30.0 and `@cratis/chronicle.contracts` 19.30.0 for registered event sources.

## Project setup

Read [references/project-setup.md](references/project-setup.md) when installing the package, importing `reflect-metadata`, or choosing decorator settings. Standard TC39 decorators are the default; esbuild/tsx emit no type metadata, so every property needs `@field(Type)`.

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

`IChronicleClient` (`Source/IChronicleClient.ts`) exposes `readonly options`,
`getEventStore(name, namespace?): Promise<IEventStore>`,
`getEventStores(): Promise<EventStoreName[]>`, and `dispose(): void`.

`ChronicleClient`'s constructor takes exactly one argument —
`constructor(readonly options: ChronicleOptions)` (`Source/ChronicleClient.ts`).

**Connecting is implicit and lazy.** There is no `connect()`. The constructor
starts a health-check watchdog and kicks off artifact discovery; the actual
connect happens on the first `getEventStore`/`getEventStores` through an internal
`ensureConnected()`. `getEventStore` also *creates* the store if it does not
exist — it calls `ensureEventStore`.

`dispose()` is **synchronous**, not
`Symbol.dispose` or `Symbol.asyncDispose`. After disposal every call throws.

Read [references/connection-options.md](references/connection-options.md) when building `ChronicleOptions`, writing a connection string, or listing the public subpath exports. Pass a connection string; there is no `./connection` export.

## Event types

```typescript
// Excerpt: replace the domain placeholders with your event and value types.
import { field } from '@cratis/fundamentals';
import { eventType } from '@cratis/chronicle';

@eventType()
export class <EventName> {
    @field(<RuntimeType>) <property>: <Type>;
    constructor(<property>: <Type>) { this.<property> = <property>; }
}
```

`eventType` supports no args, `id`, `id/generation`, `id/generation/tombstone`,
`generation`, `generation/tombstone`, or `tombstone`
(`Source/events/eventTypeDecorator.ts`); both decorator modes work.

**The event type id defaults to the class name** — `new EventTypeId(id || constructor.name)`
in the decorator. Two classes with the same name in different modules collide
on the wire.

**Use explicit `@field` declarations.** `Source/types/TypeIntrospector.ts` checks
field metadata before inference from tracked properties, default-constructed
instances and legacy metadata. Use `@field(Array, { genericArguments: [Item] })`
for typed arrays; concepts need `static readonly valueType`. A property the
client cannot type makes registration fail — do not rely on constructor-only
properties under standard decorators or tsx.

Tag an event type at declaration with `@tag(...)` / `@tags(...)`.

## Appending

`IEventLog` is an empty extension of `IEventSequence`
(`Source/eventSequences/IEventLog.ts`); the surface is on `IEventSequence`:

```typescript
append(eventSourceId: string, event: object, options?: AppendOptions): Promise<AppendResult>;
appendMany(eventSourceId: string, events: object[], options?: AppendOptions): Promise<AppendResult[]>;
appendMany(events: EventForEventSourceId[], options?: AppendOptions): Promise<AppendResult[]>;
```

```typescript
const result = await store.eventLog.append('<event-source-id>', new <EventName>('<value>'));
if (!result.isSuccess) {
    // result.constraintViolations, result.concurrencyViolation, result.errors
}
```

- **The event source id is a plain `string`.** There is no `EventSourceId` value
  type at this boundary.
- `AppendResult` (`Source/eventSequences/AppendResult.ts`): `sequenceNumber`,
  `constraintViolations` (including the constraint name), `concurrencyViolation?`,
  `errors`, `isSuccess`, and `waitForCompletion({ timeoutMs, signal }?)`
  (timeout defaults to 5000 ms).
- **`sequenceNumber` wraps a `bigint`.** Read it as `result.sequenceNumber.value`.
  `EventContext.sequenceNumber` is a `bigint` too.
- `AppendOptions` (`Source/eventSequences/AppendOptions.ts`): `correlationId`,
  `sourceType`, `streamType`, `streamId`, `subject`, `occurred`, `tags`, `namedTags`,
  `concurrencyScope`, `concurrencyScopes`, `eventSource`, and `eventStream`.
  The reserved legacy `eventSourceId` option does not override the method's id.

Requires `@cratis/chronicle` 6.0.0 for omitted append routes.

**Single `append()` supports routing and subject options.** An
unspecified route is omitted on the wire; the kernel resolves source `Default`,
stream type `All`, stream id `Default`. Subject precedence is the explicit option,
then the event's `@subject()` property, then its event source id.

To continue a pre-6.0 stream, pass **all three** dimensions explicitly:
`{ sourceType: 'Default', streamType: 'Default', streamId: id }`.
See `Documentation/migrate-append-routing.md` at the verified tag.

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
results are on the unit of work. Requires `@cratis/chronicle` 6.51.0 for transactional appends to retain
routing, subject, occurrence, tags and concurrency scopes in enrollment order;
conflicting scopes for one source throw `ConflictingConcurrencyScopesInUnitOfWork`
before enrollment.

## Observing

### Handlers use camelCase names or `@handles(EventType)`

The default is the camelCase event class name, **not** parameter-type dispatch.
An unmatched name is simply never called. Reactors and reducers also support
`@handles(EventType)` on a public instance method: one handler per event;
a reactor handler cannot combine it with `@replay`.

**Reactors replay by default.** Use class `@onceOnly()` (not inherited)
or method `@onceOnly()` for effects that must not run on replay; `@replay(EventType)`
provides a separate replay handler. This never prevents failed-partition
redelivery or proves exactly-once effects.

```typescript
import { onceOnly, reactor } from '@cratis/chronicle/reactors';
import { EventContext, EventForEventSourceId } from '@cratis/chronicle';

@reactor()
export class <ReactorName> {
    @onceOnly()
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

Reactor arguments are `(event, context, services, delivery)`
(`Source/reactors/ReactorDispatcher.ts`). `ReactorServices` exposes
`{ eventStore, readModels, signal }` for the observation's owning store/namespace.
`ReactorDelivery` supplies an idempotency identity, not an exactly-once guarantee.

> **Event content is parsed JSON, not an instance of your event class.**
> `instanceof` checks and event methods do not work. Read properties only.

`@reducer(id?, eventSequenceId?, readModel?, isActive?)` also registers the supplied
read model type. Reducers receive `(content, currentState, context)` and may be
sync or async.

Filter with `@filterEventsByTag(value)`, `@eventSourceType(...)`, or
`@eventStreamType(...)`. `reactorResultHandler` handles application-owned returns.

### Read models and projections

Read through `store.readModels`; projections are model-bound (decorators) or
fluent (`@projection`). Constraints use `@unique`/`@removeConstraint` or a builder.

Read [references/read-models-and-projections.md](references/read-models-and-projections.md) when reading a read model instance, choosing a projection style, or declaring a constraint.

### Variants — mutually exclusive read models for one entity's lifecycle

One entity with mutually exclusive lifecycle shapes is modeled as variants, with `@variantOf`/`@entersOn`/`@globalFor` or the fluent `.variantOf()`/`.entersOn()`; every variant needs an entering event.

Read [references/variants.md](references/variants.md) when a read model for one entity changes shape over its lifecycle, or when you meet `VariantMustDeclareEntersOnEvent` or `GlobalHandlerPropertyNotOnVariant`.

## Artifact discovery

`ChronicleOptions.discoveryPatterns` defaults to `**/*.ts` and `**/*.tsx` only
for TypeScript entries (`.ts/.tsx/.mts/.cts`), Vitest, or tsx/ts-node/type-stripping
loader arguments. It excludes declaration files, `node_modules`, `dist`, `build`,
`.git`, `.vscode`, and `*.spec/test.ts(x)` (`Source/ChronicleOptions.ts`).

**A compiled-JS entry defaults to `[]`.** Decorators register when their module is
imported, so side-effect imports of artifacts remain load-bearing in production.
Explicit patterns always win; `[]` disables scanning. The client imports matching
files at construction; failed imports name the file. Registration uses a
process-wide static map.

`EventStore.registerArtifacts()` registers event types first, then everything else
in parallel, and re-runs for every cached event store on reconnect.

## Lifecycle

Read [references/lifecycle.md](references/lifecycle.md) when diagnosing reconnects, keepalive, rejected credentials or telemetry names. The client reports its installed package version, and `IncompatibleChronicleServer` is a permanent verdict, not retried.

## Artifact activation

There is no built-in container. By default the client constructs artifacts.
For reactor/reducer constructor dependencies supply `artifactActivator` in
`ChronicleOptions`: activation is per delivered batch and can implement
`ActivatedArtifact.run/complete/dispose` around handlers and returned effects.

## Testing without a kernel

Read [references/testing.md](references/testing.md) when specifying appends,
reactors or read models with `EventScenario`, `ReactorScenario`, or
`ReadModelScenario`. Unsupported shapes fail explicitly; they need kernel tests.

## More surface

| Need | Public surface and boundary |
| --- | --- |
| Registered sources/streams | Root `@eventSource`, repeatable `@eventStream`, `ConcurrencyDimensions`, `store.eventSources`; append `eventSource/eventStream`. Kernel 19.30+; see `Source/eventSources/` |
| Named tags | `namedTags` on appends; batches cannot combine them with registered event sources |
| Personal data and retained secrets | `@pii`, `@subject`, `store.pii`; `@encrypted` with `EncryptionScope` from `./confidentiality`; release through `readModels.release*`. Never combine PII/encrypted or put either on `eventSourceId` |
| Subject migration | Subject uses `@subject()`; events appended before it used the source id for protection keys |
| Operational catalogs | `store.observers` (list/remove), `store.patterns`; mutations need authority |
| Diagnostics/privacy | `ChronicleOptions.telemetry` and per-client `logger`; source ids are omitted by default, raw/HMAC recording is opt-in |

## Verify

- The installed version is the one you intended, taken from npm, and
  `@cratis/fundamentals` is installed alongside it.
- `reflect-metadata` is imported exactly once, at the entry point.
- Decorator mode is consistent: standard mode uses explicit `@field` types;
  legacy mode uses `experimentalDecorators` with `emitDecoratorMetadata`.
- Every serialized property has a runtime type and a non-empty schema.
- Compiled-JS deployments import artifacts, or configure explicit discovery.
- Every handler uses exact camelCase naming or `@handles(EventType)`, and an
  appended event demonstrably reaches it; side-effect replay policy is explicit.
- A production connection string carries credentials and `skipTlsValidation=false`.
- `client.dispose()` runs on shutdown.
- Every variant group has at least one `@entersOn`/`entersOn(...)` per variant,
  and every `@globalFor` member exists on every variant it targets.
- Lint, `tsc`, and the test suite are clean against the verified package version.
