<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/discovery.md -->
## Contents

- Discovery and explicit registration
- Plain-class reducer nomination
- Manual discovery and runtime registration
- Observer handlers and reads

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Discovery and explicit registration

Source: Cratis/Chronicle, covering projection/reactor APIs and self and nominated reducer discovery:
`Source/Clients/DotNET/DefaultClientArtifactsProvider.cs`,
`Reducers/{SelfReducerMethods,RegisteredReducerReadModel,ReducerCatalog}.cs`,
`ReadModels/ReadModelBuilderOwnership.cs`,
`Registrations/{ExplicitArtifacts,ExplicitArtifacts.Types}.cs`.

`DefaultClientArtifactsProvider.Default` scans project- and package-referenced
assemblies. Discovered artifacts include:

| Kind | Shape |
| --- | --- |
| Event types | `[EventType]` or `[EventTypeGenerationFor]` |
| Projections | one `IProjectionFor<T>` per class, or model-bound projection annotations |
| Reactors | non-generic `IReactor` |
| Read-model reactors | non-generic `IReadModelReactor` |
| Separate reducers | non-generic class implementing one `IReducerFor<T>` |
| Self-reducing models | concrete non-generic non-`IReducer` class with public static non-generic `Reduce(event, model? current[, EventContext])`, returning model or `Task<model>`, including nullable results |
| Event source definitions | `IEventSource` |
| Constraints | `IConstraint`, plus `[Unique]`, `[RemoveConstraint]`, `[ClosesStream]` event declarations |
| Seeds / migrations | `ICanSeedEvents`, `IEventTypeMigrationFor<T>` |
| Event enrichment / middleware | `ICanProvideAdditionalEventInformation`, reactor middlewares |

Multiple projection or reducer targets in one class fail discovery with
`TypeImplementsMultipleProjectionInterfaces` / `TypeMustImplementOnlyOneReducer`
(analyzer CHR0057). They are not silently flattened into independent artifacts.

Requires Chronicle 19.58.0 for self shapes.

Complete self shapes are marker-free discovery signals (.NET-only).
Existing static helpers can therefore become reducers on upgrade.
No model is activated, and `Reduce` cannot take injected services. Static near
misses whose current/result refers to the model warn (CHR0058) and are skipped;
explicit nomination rejects them. Once a valid shape exists, malformed siblings
and hooks fail. A self model with another builder in the same catalog throws
`AmbiguousReadModelBuilder`. Configuration alone (`[Passive]`, `[Reducer]`,
filters, sequence selection) is not projection ownership. See the
[reducer skill](../../cratis-chronicle-reducer/SKILL.md) for selection and hooks.

Configure `ChronicleOptions.ExplicitArtifacts` **before creating stores**:
`RegisterProjection<TReadModel>(define, id)`, `RegisterReadModel<TReadModel>()`,
`RegisterReactor(id, builder)`. These participate in every normal registration
pass, including reconnects. Each store captures a snapshot at construction;
later options edits affect only subsequently created stores. For an existing store, use
`IProjections.Register<TReadModel>(define, id)` or `Register<TReadModel>()`,
`IReactors.Define/Register(id, builder)`, and the family-specific event type,
constraint, reducer and read-model registration APIs. Fluent reactors use typed
`On<TEvent>` handlers or catch-all `Subscribe`.

Requires Chronicle 19.52.0 for marker-free event types, reactors, reducers,
constraints, event sources and migrations. Check the client package version;
older versions require their attributes/interfaces. Both generic and `Type`
forms are available on `ExplicitArtifacts`: `RegisterEventType`, `RegisterReactor`,
`RegisterReducer`, `RegisterConstraint`, `RegisterEventSource`, `RegisterMigration`.
Use markers by default; nominate plain contracts at the composition root when
an assembly cannot reference Chronicle.

`RegisterEventType<T>(configure)` supports `WithId`, `Generation`, `Public`,
`Private`, `ForEventStore`, `PublishTo`. Omitted facets inherit declarations,
then defaults. Equal peers coalesce; contradictory ids, generations or facets
fail with `ConflictingArtifactDeclaration`, and contradictory artifact kinds
fail with `ConflictingArtifactKind`. Two types sharing an id and generation fail.
A plain constraint needs one public synchronous `void Define(IConstraintBuilder)`.
Plain migrations use typed public synchronous `Upcast` and `Downcast` methods;
read **cratis-chronicle-event-type-migration** before registering them.
Preparation failures prevent registration effects and are reported as registration
failures, not retried as connection failures.

Registration visibility and publication eligibility are separate. Owning-store
membership or `Public()`/`[Public]` can grant publication eligibility;
`Private()` does not subtract it. Foreign ownership alone does not grant it.
`PublishTo` chooses a destination (outbox by default), not eligibility; the event
log is not a publication destination. Type-based observer registration, state
and replay operations resolve store-local aliases; import the reactor/reducer
namespace for plain-class generic extensions. Custom old-only registries throw
`TypeBasedArtifactRegistrationNotSupported` for unsupported new operations.

## Plain-class reducer nomination

Only nominate a plain instance reducer when its assembly cannot reference
Chronicle; new application reducers normally keep static transitions on the
model, or use `IReducerFor<T>` for DI/replay instance behavior/separation.
Nomination infers exactly one model from supported instance
`(event, model? current[, EventContext])` handlers against registered events.
Names do not dispatch; public/non-public instance methods are scanned. Nullable
or nullable-oblivious current is accepted; returns are model, `Task<model>`
(including nullable results) or classic bare `Task` (which leaves absent state).
Nomination does not relax validation or discover arbitrary plain classes.

```csharp
var options = new ChronicleOptions();
options.ExplicitArtifacts.RegisterReducer<PlainBalanceReducer>();
// Runtime-type equivalent; use one, not both:
// options.ExplicitArtifacts.RegisterReducer(typeof(PlainBalanceReducer));
```

Import `Cratis.Chronicle` for options. Optional `ReducerId` configures identity.
The same APIs accept a self model: `RegisterReducer<AccountBalance>()` or
`RegisterReducer(typeof(AccountBalance))`; there is no special self API.
Register events with `[EventType]` or `RegisterEventType<T>()`: assembly
membership alone does not make an event registered. Equal explicit/discovered
declarations coalesce; incompatible identities fail, rather than overriding.

## Manual discovery and runtime registration

Runtime reactors also support `IReactors.Register(id, configure, handle)` and
`Unregister(id)`. Use an explicit stable id for side effects. Variant read models
cannot be registered explicitly (`VariantReadModelsCannotBeRegisteredExplicitly`).

`AutoDiscoverAndRegister = false` disables the automatic pass; owning explicit
registrations then also means calling `DiscoverAll()` to apply recipes,
`RegisterAll()` when connected, and owning registration/subscription waits.
Reconnect reuses the captured recipes; `DiscoverAll()` can refresh discovery
without adopting later options edits.
Inspect `IEventStore.Discovery` and startup logs before guessing why an artifact
is missing.

## Observer handlers and reads

**A reactor handler is found by its first parameter's type**, and further
parameters are resolved as dependencies — `EventContext`, a read model, a service
(`Source/Clients/DotNET/Reactors/EventHandlerMethods.cs`). The
return may be `Task`, `void`, or a side-effect event type.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class <ReactorName>(<IDependency> <dependency>) : IReactor
{
    public async Task <AnyMethodName>(<EventName> @event, EventContext context)
    {
        await <dependency>.<DoSomething>(context.EventSourceId, @event.<Property>);
    }
}
```

**There is no `[Observer]` attribute and no `IObserver` client artifact.**
"Observer" is the kernel's umbrella term for a projection, reducer, or reactor;
it is not something you write. `Source/Clients/DotNET/Observation/` holds
`ObserverId`, `ObserverRunningState`, and failed-partition types only.

Read models are queried through `eventStore.ReadModels`
(`Source/Clients/DotNET/ReadModels/IReadModels.cs`):
`GetInstanceById<TReadModel>(ReadModelKey, ReadModelSessionId?)`,
`GetInstances<TReadModel>(EventCount?)`,
`GetSnapshotsById<TReadModel>`,
`IObservable<ReadModelChangeset<TReadModel>> Watch<TReadModel>()`, and
`ISubject<IEnumerable<TReadModel>> Observe<TReadModel>(filter)`.
Requires Chronicle 19.54.0. Read
[collection observation](../../cratis-chronicle-read-model/references/collection-observation.md)
when exposing a whole-list snapshot; its filter is client-side.
