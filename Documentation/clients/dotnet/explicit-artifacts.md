---
title: Explicit artifact registration
description: Configure marker-free .NET artifacts, their identities, and their store-local registration lifetime.
---

Use `ChronicleOptions.ExplicitArtifacts` when your contract types cannot reference Chronicle or you want to nominate artifacts at the composition root. Existing attributes and interfaces keep working. Plain reactors and classic instance reducers still require nomination. A concrete, non-generic model with the exact public static self-reduction shape is the exception: it is discovered structurally. See [self-reducing read models](/chronicle/reducers/self-reducing-read-models/).

For read models, start with model-bound attributes, then fluent mappings when the definition needs
structure, then reducer code when transitions read better; a self-reducing model is the default for new
reducers. Use `IReducerFor<T>` for fold-time DI or separation, and nominate a plain reducer only when
its assembly cannot reference Chronicle. [Choose how to build a read model](../../projections/choosing-a-read-model-style.mdx)
compares the forms; this page covers their explicit registration.

## Declare artifacts before creating stores

The generic and `Type` forms declare the same artifacts:

| Generic form | Runtime-type form | Optional configuration |
| --- | --- | --- |
| `RegisterEventType<T>(configure)` | `RegisterEventType(type, configure)` | Event identity, generation, visibility, owning store, publication sequence |
| `RegisterReactor<T>(id)` | `RegisterReactor(type, id)` | `ReactorId` |
| `RegisterReducer<T>(id)` | `RegisterReducer(type, id)` | `ReducerId` |
| `RegisterConstraint<T>()` | `RegisterConstraint(type)` | None |
| `RegisterEventSource<T>(name)` | `RegisterEventSource(type, name)` | Source name |
| `RegisterMigration<T>()` | `RegisterMigration(type)` | None |

Every method returns the same `ExplicitArtifacts` object. The configuration arguments are optional. Existing projection, model-bound read-model and delegate-reactor registration remain available on that object; see [explicit read-model registration](/chronicle/projections/explicit-registration/).

The [composition example](/chronicle/configuration/#explicit-artifacts) keeps event records plain while registering their observers and constraints.

### Defaults and peer declarations

Omitted arguments inherit marker declarations, then the existing identity defaults:

- Event id: the CLR type's name; generation: `1`.
- Reactor and reducer id: the full CLR type name, with the existing namespace/name fallback.
- Event source name: the CLR type name with an `EventSource` suffix removed.
- Constraint names: the existing constraint-builder or attribute naming rules.

Discovery and explicit registration are peers, not an override stack. Equal declarations for the same CLR type coalesce. Incompatible supplied ids, generations, owning stores, visibility or publication destinations throw `ConflictingArtifactDeclaration`, including declaration provenance. A marked id cannot be silently replaced by an explicit alias. Two CLR types with the same `(id, generation)` are rejected; distinct generations of one id are valid.

A type explicitly nominated as both reactor and reducer, or against a contradictory observer marker, throws `ConflictingArtifactKind`. Invalid nominated shapes fail preparation rather than being silently omitted. Local identity, schema or migration failures prevent registration effects; an independently invalid projection retains the existing partial-projection outcome behavior.

### Event facets

| Builder method | Meaning |
| --- | --- |
| `WithId(EventTypeId)` | Stable event identifier |
| `Generation(EventTypeGeneration)` | Numeric generation |
| `Public()` | Public registration visibility and publication eligibility |
| `Private()` | Private registration visibility, without subtracting publication eligibility |
| `ForEventStore(EventStoreName)` | Owning store |
| `PublishTo(EventSequenceId)` | Destination sequence for event-target publication |

Registration visibility and publication eligibility are separate. Ownership in the current store, `[Public]`, or explicit `Public()` can grant eligibility; those contributions form a union. `Private()` never removes ownership-derived eligibility. It conflicts with a supplied public visibility declaration, but not with an ownership-derived default. Declared store ownership, including a foreign store, defaults registration visibility to public. Foreign ownership alone does not grant publication eligibility.

Absent visibility and sequence attributes supply no competing declaration. You can therefore add `Public()` or `PublishTo(customSequence)` to an otherwise marked event without conflicting with default private/outbox values. `PublishTo` alone does not make an event eligible for publication, and the event log is not a valid publication destination. The default destination is the outbox.

Numeric `Generation(n)` does not mean “historical type.” Exact-dispatch exclusion remains the behavior of `[EventTypeGenerationFor<T>]`; its referenced identity resolves through the completed peer declarations.

## Snapshot and discovery lifetime

Each store captures all explicit recipes together when it is constructed. Later options changes affect only subsequently created stores. Reconnect reuses that snapshot. Explicit `DiscoverAll()` can refresh the discovered provider while retaining the original recipes.

`AutoDiscoverAndRegister = false` stays manual: call `DiscoverAll()` to apply options recipes, then `RegisterAll()` when connected. For a separate deployment step, use [Register definitions as a deployment step](#register-definitions-as-a-deployment-step). An untouched manual store does not silently acquire those recipes. This differs from a fixture's `AutoDiscoverArtifacts = false`, which disables assembly discovery but does not disable its client's registration process.

An empty explicit snapshot leaves the supplied provider untouched. With nominations, the store merges a local catalog without mutating the provider. Attribute-derived Unique, property-Unique, RemoveConstraint and ClosesStream entries are added only for explicitly nominated events and unioned without duplicates. For discovered-only events, a custom provider's own derived lists remain authoritative, including intentional omissions.

## Register definitions as a deployment step

A CI/CD job or init container can register definitions before the application starts its observers:

```csharp
using Cratis.Chronicle.Registrations;

var result = await eventStore.RegisterDefinitions(new DeploymentGeneration(buildNumber));
Console.WriteLine(result.Fingerprint);
```

`buildNumber` is a monotonic `ulong` assigned by your deployment pipeline. The call registers event types, event sources, read models, constraints and projections. It does not start reactors, reducers, read-model reactors, external subscriptions or seeding; those register when the application connects through its normal registration flow. It reuses discovery with the same rules as `DiscoverAll()`, including explicit recipes and completed preparation.

The fingerprint is a deterministic, lowercase hex SHA-256 value, independent of the event store name, discovery order and projection update timestamps. Compare fingerprints between environments to check that the same definitions were prepared. Custom, non-native artifact registries still register, but their definitions do not contribute to the fingerprint.

The optional generation defaults to zero, meaning no generation is sent. An older generation than the one stored by the kernel is refused with `DeploymentGenerationIsStale`. A value above `long.MaxValue` throws `DeploymentGenerationOutOfRange` locally. Use a kernel that supports `RegisterDeployment`; an older kernel without that RPC cannot perform this step.

Every payload is prepared before registration effects. An unbuildable projection throws `ArtifactDefinitionsIncomplete` before anything is sent. Unsupported closing-stream constraints throw `ClosesStreamConstraintsNotSupported` under the same capability check as ordinary constraint registration, also before any RPC. The call does not retry failures or start background registration.

The prepared fingerprint is reported on `Registration.Fingerprint`, including when sending definitions fails after the deployment was recorded. A concurrent `RegisterAll()` can replace that outcome and reset the fingerprint to `NotSet`, so the returned `DefinitionsRegistration` is the authoritative result of a successful call. Cancellation covers waiting for services and checks between steps, not an RPC already in flight.

## Plain observers and constraint definers

A nominated reactor uses the existing instance-method dispatch, dependency injection, filters, tags, sequence selection and replay behavior. A nominated classic reducer infers exactly one read model from supported instance `(event, current[, EventContext])` methods. Declare current state nullable; nullable-oblivious declarations retain compatibility. Supported returns remain the read model, `Task<read model>` and classic `Task`; classic mutation-only `Task` keeps its existing null-return semantics.

The same `RegisterReducer<T>(id)` and `RegisterReducer(typeof(T), id)` APIs also accept a self-reducing model. Its public static `Reduce(event, model? current[, EventContext])` returns the model or `Task<model>`, including nullable results. No model is activated and no new nomination API is needed. Explicit near misses fail with `InvalidSelfReducerSignature`. Implementing `IReducer` always keeps a type classic, even if it has a self-shaped static helper. A self model with another builder in the same store catalog fails with `AmbiguousReadModelBuilder` before registration effects.

A plain constraint definer exposes exactly one public synchronous instance `void Define(IConstraintBuilder)` method. Existing `IConstraint` implementations keep their direct interface path. A nominated event source keeps its declared streams and metadata; an explicit name changes the registered identity, not its declaration-only description helpers.

The existing constrained generic observer APIs remain. For plain classes, import `Cratis.Chronicle.Reactors` or `Cratis.Chronicle.Reducers` to use the registered-artifact extension siblings, or pass `typeof(YourObserver)` to the Type-based operations. These operations resolve aliases through the store. Plain `Register(Type)` is explicit intent and need not be prelisted in options; its event types must already be registered.

Custom observer registries that implement only the old generic interface surface remain compatible. Unsupported new Type operations throw `TypeBasedArtifactRegistrationNotSupported` rather than pretending registration succeeded.

## Typed and plain migrations

[Explicit migration registration](/chronicle/migrations/explicit-registration/) uses the existing migration builders and kernel-side generation engine.

For unmarked or aliased endpoints, a typed migration calls the additive protected constructor `EventTypeMigration<TUpgrade,TPrevious>(IEventTypes)`. The client supplies the owning store's finalized registry, even if ordinary DI contains another registry. Both endpoints must be registered, share one id and have adjacent generations.

The parameterless base constructor keeps declaration-only validation. An attributed previous endpoint intentionally omitted by a custom provider remains valid on that legacy path; its placeholder schema behavior is unchanged. Merely listing a legacy migration explicitly does not switch its constructor semantics.

A nominated plain, concrete, closed class declares these roles:

- `public void Upcast(IEventMigrationBuilder<TUpgrade,TPrevious> builder)`
- `public void Downcast(IEventMigrationBuilder<TPrevious,TUpgrade> builder)`
- Optional `public void MapValues(IEventValueMapBuilder<TUpgrade,TPrevious> builder)`

Each role is one non-generic synchronous instance method with one ordinary parameter. Overloads, ambiguous endpoints, static methods, async void and malformed roles are rejected. Both endpoints must be registered. The adapter uses the typed migration engine, including bidirectional value-map inversion; there is no separate migration language. Constructor dependencies use ordinary DI, with an owning registry supplied explicitly when requested. Plain definers must not implement disposal interfaces because the migration provider has no disposal ownership contract.

Every explicit recipe is validated during preparation, even if no discovered upgrade could otherwise select it. Migration nomination does not register missing events automatically. Custom migration providers retain compatible legacy output; opaque results that require a new registry binding but cannot identify their original recipe fail with `ExplicitMigrationBindingNotSupported`. Plain adapter activation makes no new trimming or Native AOT guarantee. The migration-id analyzer does not prove plain-role endpoints; runtime validation does.

## Registry-aware schemas and concurrency

Compensation metadata can refer to explicitly registered targets. Built-in schema generation resolves new aliases or unmarked compensation targets through the owning registry without placing store-specific ids in CLR-only caches. Those new targets must be registered.

A custom `IJsonSchemaGenerator` retains its original output for declaration-only compensation, even when the marked target is absent from the registry. For new explicit bindings it must implement the additive `Generate(Type, IEventTypes)` and `GenerateLegacyEventType(Type, IEventTypes)` overloads. Otherwise `RegistryAwareEventTypeSchemasNotSupported` fails narrowly on that requirement; no metadata is silently rewritten.

For [concurrency scopes](/chronicle/configuration/#registry-resolved-concurrency), pass `store.EventTypes.GetEventTypeFor(typeof(YourEvent))` to existing `WithEventType(EventType)` or `WithEventTypes` methods. Declaration-only Type/generic helpers cannot know an explicit alias. For a source alias, resolve `store.EventSources.GetFor(typeof(YourSource))` and its `FindStream(...)` definition, then use their effective `EventSourceType` and `EventStreamType` with the existing scope builders.

## Testing and analyzer boundaries

Per-run `Defaults(artifacts, serviceProvider, options)` honors the same registrations in `ReadModelScenario`, `ReactorScenario` and `EventScenario`. A plain reactor scenario requires explicit nomination; it is not shape-discovered. Give each independent run its own defaults/options rather than changing `Defaults.Instance`.

XUnit integration options have the lifetime of the fixture type's host/client/store, not an individual test run. Configure explicit recipes before that host is created. Reuse keeps its first captured snapshot; a separately created fixture host has a separate snapshot. The sharing boundary is the existing closed `ChronicleClientFixture<TChronicleFixture>` family, so derived scenarios sharing that family also share its host. A fixture that overrides `ChronicleFixture.UsesPerContextMongoDBHost` to return `true` starts a fresh host for each test context, so each context captures its own snapshot. Disabling fixture assembly discovery does not remove explicit recipes. See [testing configuration](/chronicle/testing/#explicit-artifacts-and-lifetime).

[Analyzer evidence](/chronicle/code-analysis/#explicit-registration-evidence) recognizes possible registrations in the same compilation, not execution or per-client membership. Runtime validation is final authority.
