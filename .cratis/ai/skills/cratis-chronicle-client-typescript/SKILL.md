---
name: cratis-chronicle-client-typescript
description: Talk to a Chronicle server from a Node.js or TypeScript application with @cratis/chronicle - reflect-metadata and decorator compiler settings, ChronicleClient and connection strings, @eventType classes, eventLog.append, reactors and reducers dispatched by camelCase method name, model-bound and declarative projections, variants (@variantOf/@entersOn/@globalFor or .variantOf()/.entersOn()) for an entity with mutually exclusive lifecycle shapes, glob-based artifact discovery, and the lazy connect and keepalive lifecycle. Use when a Node application appends to or observes a Chronicle event store. Do not use for the .NET, Kotlin, or Elixir clients, and do not use for Arc React frontends or generated Arc proxies.
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
| Side-effect reactor without `@onceOnly()` | Reactors run again on kernel replay by default since 6.9.0 |
| Reading `result.sequenceNumber` as a number | It is a `bigint` behind `.value` |
| A `@globalFor` mapping targeting a property one variant lacks | `GlobalHandlerPropertyNotOnVariant` when the group is built, not a silently skipped mapping |
| Expecting a `@globalFor`-only class to appear as a projection | It is never registered on its own; it is merged into its variants |

## Verified product sources

Verified against `Cratis/Chronicle.TypeScript` tag **`v6.51.0`**, reading
`Source/**` and `Documentation/getting-started.md`, `testing.md`,
`migrate-append-routing.md` and `reactors.md`. Source paths below refer to that tag.

| Package | Version | Where |
| --- | --- | --- |
| `@cratis/chronicle` | `6.51.0` | npm; `Source/package.json` carries a `1.0.0` placeholder |
| `@cratis/chronicle.contracts` | `19.31.10` | Client's pinned dependency, not the latest contracts package |
| `@cratis/fundamentals` | `^7.22.4` | **peer dependency** — you install it |

> **Do not read the version out of the repository.** `Source/package.json` says
> `1.0.0`; `publish-version.js` overwrites it at publish time. Take the version
> from npm.

**Upgrade the kernel first.** The 5.x→6.0.0 migration changes omitted append
routes and requires compatibility checks (kernel 18.4.1+); registered event
sources introduced in 6.49.0 require kernel 19.30+. Reactors replay by default
since 6.9.0, so review side-effect handlers when upgrading.

## Project setup

```shell
yarn add @cratis/chronicle @cratis/fundamentals reflect-metadata
```

`@cratis/fundamentals` is a peer dependency, so it does not arrive on its own.
`reflect-metadata` is now a regular dependency. Import it **once, at the entry
point**, before any decorated class is loaded:

```typescript
import 'reflect-metadata';
```

The package is **ESM-only** and needs Node.js **22.19+** for its `undici`
dependency (the client itself has no `engines` field). Standard TC39 decorators
are supported (TypeScript 5.2+) and are the documentation default:

```jsonc
// tsconfig.json — standard decorator mode
{
    "compilerOptions": {
        "target": "ES2022",
        "lib": ["ES2022", "ESNext.Decorators"]
    }
}
```

Legacy mode with `experimentalDecorators: true` and
`emitDecoratorMetadata: true` still works. Standard decorators and esbuild/tsx
emit no type metadata: declare every serialized property with `@field(Type)`
from `@cratis/fundamentals`; default values are only an inference fallback.

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

### Options

`ChronicleOptions` has a **private constructor** (`Source/ChronicleOptions.ts`);
use `ChronicleOptions.fromConnectionString(connectionString, options?)` or
`ChronicleOptions.development(options?)`.

The optional bag accepts `telemetry`, `logger`, `clientArtifactsProvider`,
`discoveryPatterns`, `defaultSinkTypeId`, `reactorResultHandler`,
`artifactActivator`, and `readModelNamingPolicy`. `defaultSinkTypeId` defaults
to `WellKnownSinks.MongoDB`.

> `programIdentifier`, `softwareVersion`, and `softwareCommit` exist on the type
> but **neither public factory forwards them**, so they cannot be set.
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
`./transactions`, `./confidentiality`, `./patterns`, `./testing`, and
`./package.json`. Event-source APIs are root exports; there is no
`./eventSources` subpath. `IncompatibleChronicleServer` and
`RejectedChronicleCredentials` are also root exports.

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

**Single `append()` supports routing and subject options.** Since 6.0.0 an
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
results are on the unit of work. Since 6.51.0 transactional appends retain
routing, subject, occurrence, tags and concurrency scopes in enrollment order;
conflicting scopes for one source throw `ConflictingConcurrencyScopesInUnitOfWork`
before enrollment.

## Observing

### Handlers use camelCase names or `@handles(EventType)`

The default is the camelCase event class name, **not** parameter-type dispatch.
An unmatched name is simply never called. Reactors and reducers also support
`@handles(EventType)` on a public instance method: one handler per event;
a reactor handler cannot combine it with `@replay`.

**Reactors replay by default since 6.9.0.** Use class `@onceOnly()` (not inherited)
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

- A health-check watchdog runs `getVersionInfo` every 5 seconds and reconnects on
  failure; the interval handle is `unref()`'d so it does not hold the process open.
- Reconnect uses exponential backoff with jitter, capped at 30 seconds, rebuilding
  the gRPC channel on every attempt.
- **Keepalive registration is what keeps observers alive.** Losing it triggers a
  full reconnect, because the kernel tears down the client's observers otherwise.
- Failed operations are retried once after reconnecting, keyed on gRPC status
  codes 4/13/14 plus a substring match over the error text.

The client reports its **installed package version**, read from
`@cratis/chronicle/package.json`, and performs `CheckCompatibility` with
`ProtocolVersion` and the contracts descriptor set before a connection is usable
and before event-sequence calls. `IncompatibleChronicleServer` is a permanent
verdict, not retried (`Source/connection/CompatibilityPreflight.ts`).
Three consecutive credential rejections cause `RejectedChronicleCredentials`.
A failed token request is logged and the call proceeds without a token (allowing
an auth-less kernel); tokens come from the selected server, not the first host.

Since 6.43.0 built-in spans use `cratis.chronicle.client.*` and scope
`Cratis.Chronicle.Client`; metrics use seconds. Legacy millisecond metrics and
`chronicle.*` attributes are retired except the retained exact-string
`chronicle.sequence_number`. Historical constants, `ChronicleMetrics`, and
`telemetry.spanNames` are deprecated; migrate dashboards.

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
| Subject migration | Subject uses `@subject()` since 6.41.1; earlier appended events used the source id for protection keys |
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
