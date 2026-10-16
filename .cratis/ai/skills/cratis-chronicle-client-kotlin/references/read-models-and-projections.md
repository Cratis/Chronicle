<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/read-models-and-projections.md -->
# Read models and model-bound projections

This reference holds the read model query surface, projection annotations, the declarative alternative and passive models.

### Read models and model-bound projections

Read models are queried through `store.readModels`
(`readModels/IReadModelsService.kt`): `getInstanceByKey(readModelClass, key): T?`,
`getInstances(readModelClass, eventCount)`,
`getSnapshotsById`, and `watch(readModelClass): Flow<ReadModelChangeset<T>>`
(which is **not** suspending — it hands back a `Flow`).

A model-bound projection puts the projection on the read model with `@FromEvent`
(`projections/FromEvent.kt`) and `@SetFrom`
(`projections/SetFrom.kt`); the full family also includes `SetValue`,
`SetFromContext`, `AddFrom`, `SubtractFrom`, `Increment`, `Decrement`, `Count`,
`Join`, `RemovedWith`, `RemovedWithJoin`, `ChildrenFrom`, `ClearWith`,
`FromAll`, `FromEvery`, `FromEventSourceId`, `CountFromAll`, `IncrementFromAll`,
`DecrementFromAll`, `Nested`, `NoAutoMap`, `NoAutoMapProperties`,
`NotRewindable`. The declarative
alternative is a class implementing `IProjectionFor<TReadModel>`.
Use fluent `set(...).toValue(...)` for a constant, or `toProperty(...)` for an
event property. `set(...).to { }` is deprecated: it never evaluated the lambda.
`noAutoMap()` is honored; composite key parts can use
`toEventContextProperty(targetPropertyName, contextProperty)`.

`@io.cratis.chronicle.readModels.Passive` and `@Reducer(isActive = false)`
compute a passive model from its events on read, so a read immediately after an
append includes that event rather than waiting for a materialized observer.
