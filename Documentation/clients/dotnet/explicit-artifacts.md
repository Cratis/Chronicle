---
title: Cratis Chronicle .NET explicit artifact registration
description: Register plain .NET events and observers through ChronicleOptions.ExplicitArtifacts, control their identities, and deploy definitions separately.
---

Use `ChronicleOptions.ExplicitArtifacts` to register types whose assemblies cannot reference Cratis Chronicle, or to choose artifacts in your application's startup configuration. You can keep event records and observers plain while supplying their identities where you configure the client. Existing attributes and interfaces keep working alongside explicit declarations.

Plain reactors and classic instance reducers must be nominated: list them explicitly so the client
knows to use them as observers. A concrete, non-generic model with the exact public static
self-reduction shape can instead be discovered by its method signature. See
[self-reducing read models](/chronicle/reducers/self-reducing-read-models/).

If you are choosing a read-model style, start with model-bound attributes, use fluent mappings for
structured definitions, or use reducer code when it expresses transitions more clearly. Prefer a
self-reducing model for a new reducer. `IReducerFor<T>` suits dependency injection during a fold or
separation from the model; a plain reducer suits an assembly that cannot reference Chronicle.
[Choose how to build a read model](../../projections/choosing-a-read-model-style.mdx) compares those
forms. The sections below cover how to register them explicitly.

## Declare artifacts before creating stores

Configure `ChronicleOptions.ExplicitArtifacts` before creating an event store. Use a generic method
when the type is known in code, or its `Type` counterpart when startup configuration supplies it at
runtime. Both declare the same artifact:

| Generic form | Runtime-type form | Optional configuration |
| --- | --- | --- |
| `RegisterEventType<T>(configure)` | `RegisterEventType(type, configure)` | Event identity, generation, visibility, owning store, publication sequence |
| `RegisterReactor<T>(id)` / `RegisterReactor<T>(options, id)` | `RegisterReactor(type, id)` / `RegisterReactor(type, options, id)` | `ReactorId`, source event store |
| `RegisterReducer<T>(id)` | `RegisterReducer(type, id)` | `ReducerId` |
| `RegisterConstraint<T>()` | `RegisterConstraint(type)` | None |
| `RegisterEventSource<T>(name)` | `RegisterEventSource(type, name)` | Source name |
| `RegisterMigration<T>()` | `RegisterMigration(type)` | None |

Every method returns the same `ExplicitArtifacts` object, so you can chain declarations. Configuration
arguments are optional. For a complete setup that keeps event records plain while registering their
observers and constraints, follow the [composition example](/chronicle/configuration/#explicit-artifacts).

The same object also supports projection, model-bound read-model, and delegate-reactor registration.
See [explicit read-model registration](/chronicle/projections/explicit-registration/).

### Defaults and peer declarations

If you omit an argument, the client uses the type's attributes or marker interfaces, then falls back
to its identity defaults:

- Event id: the CLR type's name; generation: `1`.
- Reactor and reducer id: the full CLR type name, with the existing namespace/name fallback.
- Event source name: the CLR type name with an `EventSource` suffix removed.
- Constraint names: the existing constraint-builder or attribute naming rules.

An explicit declaration does not override an attribute found during discovery. Equal declarations
for the same CLR type combine into one. If supplied ids, generations, owning stores, visibility, or
publication destinations conflict, preparation throws `ConflictingArtifactDeclaration` with the
sources of those declarations. To use an explicit alias, remove a conflicting marked id first.
Two CLR types cannot share the same `(id, generation)`; different generations of one id are valid.

Nominating a type as both reactor and reducer, or contradicting its observer marker, throws
`ConflictingArtifactKind`. An invalid nominated method shape fails preparation instead of being
silently skipped. Local identity, schema, or migration failures prevent registration effects. An
independently invalid projection can still leave a partial registration; inspect the
[registration outcome](./registration-outcome.md) for its failure.

### Event facets

| Builder method | Meaning |
| --- | --- |
| `WithId(EventTypeId)` | Stable event identifier |
| `Generation(EventTypeGeneration)` | Numeric generation |
| `Public()` | Public registration visibility and publication eligibility |
| `Private()` | Private registration visibility, without subtracting publication eligibility |
| `ForEventStore(EventStoreName)` | Owning store |
| `PublishTo(EventSequenceId)` | Destination sequence for event-target publication |

Visibility controls how the event type is registered; publication eligibility controls whether it
can be published. Ownership in the current store, `[Public]`, or explicit `Public()` can each grant
eligibility. `Private()` does not remove eligibility granted by ownership. It conflicts with a
supplied public visibility declaration, but not an ownership-derived default.

Declaring an owning store defaults registration visibility to public, even when that store is
foreign. Foreign ownership alone does not make the event eligible for publication.

Absent visibility and sequence attributes supply no competing declaration. You can therefore add `Public()` or `PublishTo(customSequence)` to an otherwise marked event without conflicting with default private/outbox values. `PublishTo` alone does not make an event eligible for publication, and the event log is not a valid publication destination. The default destination is the outbox.

Setting `Generation(n)` alone does not mark a type as historical or exclude it from exact dispatch.
That behavior belongs to `[EventTypeGenerationFor<T>]`, whose referenced identity is resolved after
the client combines the declarations.

## Snapshot and discovery lifetime

Each event store captures the explicit declarations when it is constructed. Changing the options
later affects only stores created afterwards; reconnecting reuses the captured snapshot. An explicit
`DiscoverAll()` can refresh the discovered provider, but it retains that store's original declarations.

With `AutoDiscoverAndRegister = false`, call `DiscoverAll()` to apply the options declarations, then
`RegisterAll()` once connected. Until you do, a manual store does not acquire them. To move
registration into a separate process, use
[Register definitions as a deployment step](#register-definitions-as-a-deployment-step).

A test fixture's `AutoDiscoverArtifacts = false` has a different meaning: it disables assembly
discovery without disabling the client's registration process.

With no explicit declarations, the supplied artifact provider stays untouched. With nominations,
the store builds a local catalog without mutating that provider. It adds attribute-derived Unique,
property-Unique, RemoveConstraint, and ClosesStream entries only for explicitly nominated events,
without duplicates. For events found only by discovery, a custom provider's derived lists remain
authoritative, including intentional omissions.

## Register definitions as a deployment step

If startup should use definitions registered by another process, call `RegisterDefinitions` from a
CI/CD job or init container before starting the application's observers:

```csharp
using Cratis.Chronicle.Registrations;

var result = await eventStore.RegisterDefinitions(new DeploymentGeneration(buildNumber));
Console.WriteLine(result.Fingerprint);
```

Assign `buildNumber` as a monotonic `ulong` in your deployment pipeline. The call registers event
types, event sources, read models, constraints, and projections using the same discovery rules as
`DiscoverAll()`, including explicit declarations and completed preparation.

It does not start reactors, reducers, read-model reactors, external subscriptions, or seeding. Those
belong to the application's normal connection-driven registration flow. Choose the application's
[registration mode](./short-lived-processes.md#choosing-how-to-register) to match which process owns
registration.

Compare the returned fingerprint between environments to check whether their prepared definitions
match. It is a deterministic, lowercase hex SHA-256 value that does not depend on event store name, discovery
order, or projection update timestamps. Definitions from custom, non-native artifact registries can
register but do not contribute to the fingerprint, so matching fingerprints do not compare those
definitions.

Use a kernel that supports the `RegisterDeployment` RPC. Without it, this deployment step cannot run.
The optional generation defaults to zero, which sends no generation. The kernel refuses a generation
older than its stored value with `DeploymentGenerationIsStale`; a value above `long.MaxValue` throws
`DeploymentGenerationOutOfRange` locally.

The client prepares every payload before sending registrations. An unbuildable projection throws
`ArtifactDefinitionsIncomplete` before anything is sent. Unsupported closing-stream constraints
throw `ClosesStreamConstraintsNotSupported` under the same capability check as ordinary constraint
registration, also before any RPC. Handle failures in the deployment process: this call does not
retry or start background registration.

Keep the returned `DefinitionsRegistration` as the result of a successful call. The prepared
fingerprint also appears on `Registration.Fingerprint`, even if sending definitions fails after the
deployment was recorded, but a concurrent `RegisterAll()` can replace that outcome and reset the
fingerprint to `NotSet`. Cancellation applies while waiting for services and between steps, not to
an RPC already in flight.

## Plain observers and constraint definers

An explicitly nominated reactor uses the same instance-method dispatch, dependency injection,
filters, tags, sequence selection, and replay behavior as a marked reactor. A classic reducer must
identify exactly one read model through supported instance `(event, current[, EventContext])`
methods. Declare `current` nullable because the model may not exist yet; declarations made without
nullable annotations remain compatible. Methods can return the read model, `Task<read model>`, or
classic `Task`. A mutation-only classic `Task` keeps its existing null-return semantics.

To register a self-reducing model, use the same `RegisterReducer<T>(id)` or
`RegisterReducer(typeof(T), id)` API. Its public static
`Reduce(event, model? current[, EventContext])` returns the model or `Task<model>`, including nullable
results. The client does not activate a model instance. A nominated type with a near-miss signature
fails with `InvalidSelfReducerSignature`.

Implementing `IReducer` keeps a type on the classic path, even if it has a static helper shaped like
self-reduction. If a self-reducing model has another builder in the same store catalog, preparation
fails with `AmbiguousReadModelBuilder` before registration effects.

A plain constraint definer exposes exactly one public synchronous instance `void Define(IConstraintBuilder)` method. Existing `IConstraint` implementations keep their direct interface path. A nominated event source keeps its declared streams and metadata; an explicit name changes the registered identity, not its declaration-only description helpers.

For operations on plain observers, import `Cratis.Chronicle.Reactors` or `Cratis.Chronicle.Reducers`
to use the registered-artifact extensions, or pass `typeof(YourObserver)` to Type-based operations.
These resolve aliases through the store. You can call plain `Register(Type)` without listing the
observer in options first, but its event types must already be registered. Existing constrained
generic observer APIs remain available for types that satisfy their constraints.

A custom observer registry that implements only the older generic interface remains compatible.
If it does not support a new Type operation, the operation throws
`TypeBasedArtifactRegistrationNotSupported`.

## Typed and plain migrations

[Explicit migration registration](/chronicle/migrations/explicit-registration/) uses the existing migration builders and kernel-side generation engine.

If either endpoint is unmarked or aliased, have your typed migration call the protected constructor
`EventTypeMigration<TUpgrade,TPrevious>(IEventTypes)`. The client supplies the owning store's
finalized registry, even if ordinary dependency injection contains another registry. Register both
endpoints with the same id and adjacent generations.

The parameterless base constructor still validates declarations without consulting the store
registry. On that legacy path, an attributed previous endpoint can remain valid even when a custom
provider intentionally omits it; placeholder schema behavior is unchanged. Explicitly listing a
legacy migration does not change which constructor path it uses.

A nominated plain, concrete, closed class declares these roles:

- `public void Upcast(IEventMigrationBuilder<TUpgrade,TPrevious> builder)`
- `public void Downcast(IEventMigrationBuilder<TPrevious,TUpgrade> builder)`
- Optional `public void MapValues(IEventValueMapBuilder<TUpgrade,TPrevious> builder)`

Each role must be one non-generic, synchronous instance method with one ordinary parameter. Do not
use overloads, ambiguous endpoints, static methods, or async void; validation rejects these shapes.
Register both endpoints before using the migration. The adapter uses the typed migration engine,
including bidirectional value-map inversion, rather than a separate migration language.

Constructor dependencies use ordinary dependency injection, with the owning registry supplied
explicitly when requested. A plain definer must not implement disposal interfaces because the
migration provider does not own a disposal scope.

Preparation validates every explicit migration, even if no discovered upgrade would select it.
Nominating a migration does not register missing event types. Custom migration providers can retain
compatible legacy output, but a result that needs a new registry binding must identify its original
declaration; otherwise it fails with `ExplicitMigrationBindingNotSupported`.

Plain adapter activation adds no trimming or Native AOT guarantee. The migration-id analyzer cannot
prove plain-role endpoints, so runtime validation checks them.

## Registry-aware schemas and concurrency

For compensation metadata that refers to an explicit alias or unmarked target, register the target
with the owning store. Built-in schema generation resolves those references through the store's
registry, without putting store-specific ids in CLR-only caches.

A custom `IJsonSchemaGenerator` can keep its original output for declaration-only compensation,
even when the marked target is absent from the registry. To support new explicit bindings,
implement `Generate(Type, IEventTypes)` and `GenerateLegacyEventType(Type, IEventTypes)`. If the
generator lacks that support, the required binding throws `RegistryAwareEventTypeSchemasNotSupported`
instead of silently rewriting metadata.

For [concurrency scopes](/chronicle/configuration/#registry-resolved-concurrency), pass `store.EventTypes.GetEventTypeFor(typeof(YourEvent))` to existing `WithEventType(EventType)` or `WithEventTypes` methods. Declaration-only Type/generic helpers cannot know an explicit alias. For a source alias, resolve `store.EventSources.GetFor(typeof(YourSource))` and its `FindStream(...)` definition, then use their effective `EventSourceType` and `EventStreamType` with the existing scope builders.

## Testing and analyzer boundaries

Pass `Defaults(artifacts, serviceProvider, options)` to `ReadModelScenario`, `ReactorScenario`, or
`EventScenario` to use these registrations in a test run. Nominate a plain reactor explicitly; the
scenario does not discover it by method shape. Give independent runs their own defaults and options
instead of changing `Defaults.Instance`.

For XUnit integration tests, configure explicit declarations before the fixture host is created.
Its options live as long as the fixture type's host, client, and store, not one test run. Reusing
that host retains its first snapshot. Derived scenarios in the same closed
`ChronicleClientFixture<TChronicleFixture>` family share the host and snapshot.

A separately created fixture host captures a separate snapshot. Overriding
`ChronicleFixture.UsesPerContextMongoDBHost` to return `true` creates a fresh host for each test
context, so each context captures its own declarations. Disabling fixture assembly discovery does
not remove explicit declarations. See [testing configuration](/chronicle/testing/#explicit-artifacts-and-lifetime).

[Analyzer evidence](/chronicle/code-analysis/#explicit-registration-evidence) identifies possible
registrations in the same compilation. It cannot show that the code ran or that a particular client
received those registrations; runtime validation is the final check.
