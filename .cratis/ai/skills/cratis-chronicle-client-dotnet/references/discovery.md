<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/discovery.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Discovery and explicit registration

Verified at **Cratis/Chronicle v19.50.0**:
`Source/Clients/DotNET/DefaultClientArtifactsProvider.cs`,
`Registrations/ExplicitArtifacts.cs`, `Projections/IProjections.cs`,
`Reactors/IReactors.cs`.

`DefaultClientArtifactsProvider.Default` scans project- and package-referenced
assemblies. Discovered artifacts include:

| Kind | Shape |
| --- | --- |
| Event types | `[EventType]` or `[EventTypeGenerationFor]` |
| Projections | one `IProjectionFor<T>` per class, or model-bound projection annotations |
| Reactors | non-generic `IReactor` |
| Read-model reactors | non-generic `IReadModelReactor` |
| Reducers | non-generic class implementing one `IReducerFor<T>` |
| Event source definitions | `IEventSource` |
| Constraints | `IConstraint`, plus `[Unique]`, `[RemoveConstraint]`, `[ClosesStream]` event declarations |
| Seeds / migrations | `ICanSeedEvents`, `IEventTypeMigrationFor<T>` |
| Event enrichment / middleware | `ICanProvideAdditionalEventInformation`, reactor middlewares |

Multiple projection or reducer targets in one class fail discovery with
`TypeImplementsMultipleProjectionInterfaces` / `TypeMustImplementOnlyOneReducer`
(analyzer CHR0057). They are not silently flattened into independent artifacts.

Configure `ChronicleOptions.ExplicitArtifacts` **before connecting**:
`RegisterProjection<TReadModel>(define, id)`, `RegisterReadModel<TReadModel>()`,
`RegisterReactor(id, builder)`. These participate in every normal registration
pass, including reconnects. For an existing store, use
`IProjections.Register<TReadModel>(define, id)` or `Register<TReadModel>()`,
`IReactors.Define/Register(id, builder)`, and the family-specific event type,
constraint, reducer and read-model registration APIs. Fluent reactors use typed
`On<TEvent>` handlers or catch-all `Subscribe`.

Runtime reactors also support `IReactors.Register(id, configure, handle)` and
`Unregister(id)`. Use an explicit stable id for side effects. Variant read models
cannot be registered explicitly (`VariantReadModelsCannotBeRegisteredExplicitly`).

`AutoDiscoverAndRegister = false` disables the automatic pass; owning explicit
registrations then also means owning their registration and subscription waits.
Inspect `IEventStore.Discovery` and startup logs before guessing why an artifact
is missing.
