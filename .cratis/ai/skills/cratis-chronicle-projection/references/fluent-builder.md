<!-- cratis-ai-managed: skills/cratis-chronicle-projection/references/fluent-builder.md -->
# The fluent projection builder

Verified against `Cratis.Chronicle` `16.45.2`, namespace
`Cratis.Chronicle.Projections`.

## The interfaces

```csharp
public interface IProjection;

public interface IProjectionFor<TReadModel> : IProjection
    where TReadModel : class
{
    void Define(IProjectionBuilderFor<TReadModel> builder);
}
```

`IProjection` is an empty marker. `Define` returns `void`. **The interface has
no `Identifier` or `ProjectionId` member** — declaring one adds a plain class
member that Chronicle does not read. Use
`[Projection(id: "<id>", eventSequence: "<name>")]` when the identity or source
sequence must be explicit.

## `IProjectionBuilderFor<TReadModel>` — its own four members

```csharp
IProjectionBuilderFor<TReadModel> FromEventSequence(EventSequenceId eventSequenceId);
IProjectionBuilderFor<TReadModel> ContainerName(string containerName);
IProjectionBuilderFor<TReadModel> NotRewindable();
IProjectionBuilderFor<TReadModel> Passive();
```

## Inherited from `IProjectionBuilder<TReadModel, TBuilder>`

```csharp
IProjectionBuilder<TReadModel, TBuilder> AutoMap();     // returns the BASE interface
IProjectionBuilder<TReadModel, TBuilder> NoAutoMap();   // returns the BASE interface

TBuilder WithInitialValues(Func<TReadModel> initialValueProviderCallback);
TBuilder From<TEvent>(Action<IFromBuilder<TReadModel, TEvent>>? builderCallback = default);
TBuilder Join<TEvent>(Action<IJoinBuilder<TReadModel, TEvent>>? builderCallback = default);
TBuilder FromEvery(Action<IFromEveryBuilder<TReadModel>> builderCallback);   // callback required
TBuilder FromAll(Action<IFromAllBuilder<TReadModel>> builderCallback);       // callback required
TBuilder RemovedWith<TEvent>(Action<RemovedWithBuilder<TReadModel, TEvent>>? builderCallback = default);
TBuilder RemovedWithJoin<TEvent>(Action<RemovedWithJoinBuilder<TReadModel, TEvent>>? builderCallback = default);
TBuilder Children<TChildModel>(
    Expression<Func<TReadModel, IEnumerable<TChildModel>>> targetProperty,
    Action<IChildrenBuilder<TReadModel, TChildModel>> builderCallback);
TBuilder Nested<TNestedModel>(
    Expression<Func<TReadModel, TNestedModel?>> targetProperty,
    Action<INestedBuilder<TReadModel, TNestedModel>> builderCallback);
```

**`AutoMap()` and `NoAutoMap()` return the base interface, not `TBuilder`.**
Chaining `.NotRewindable()`, `.Passive()`, `.ContainerName()`, or
`.FromEventSequence()` after either of them does not compile. Put them last.

`RemovedWith` and `RemovedWithJoin` hand you the **concrete**
`RemovedWithBuilder<,>` and `RemovedWithJoinBuilder<,>` classes, not interfaces.

## Keys live on the per-event builder

`From<TEvent>` hands the callback an `IFromBuilder<TReadModel, TEvent>`, which
derives from `IReadModelPropertiesBuilder<TReadModel, TEvent, TBuilder>`. That
is where every key method lives:

```csharp
TBuilder UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor);
TBuilder UsingKeyFromContext<TProperty>(Expression<Func<EventContext, TProperty>> keyAccessor);
TBuilder UsingParentKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor);
TBuilder UsingParentKeyFromContext<TProperty>(Expression<Func<EventContext, TProperty>> keyAccessor);
TBuilder UsingCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback);
TBuilder UsingParentCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback);
TBuilder UsingConstantKey(string value);
TBuilder UsingConstantParentKey(string value);
```

The same interface carries the value operations:

```csharp
TBuilder Increment<TProperty>(...);
TBuilder Decrement<TProperty>(...);
TBuilder Add<TProperty>(...);
TBuilder Subtract<TProperty>(...);
TBuilder Count<TProperty>(...);
TBuilder AddChild<TChildModel>(...);
ISetBuilder<...> Set(PropertyPath propertyPath);
ISetBuilder<...> Set<TProperty>(Expression<Func<TReadModel, TProperty>> propertyExpression);
TBuilder SetThisValue();
TBuilder Clear<TProperty>(...);   // default implementation throws unless the builder overrides it
```

## `Set(...).To(...)`

```csharp
// ISetBuilder<TReadModel, TEvent, TParentBuilder>
TParentBuilder To(PropertyPath propertyPath);
TParentBuilder ToEventSourceId();

// ISetBuilder<TReadModel, TEvent, TProperty, TParentBuilder>
TParentBuilder ToValue(TProperty value);
TParentBuilder To(Expression<Func<TEvent, TProperty>> eventPropertyAccessor);
TParentBuilder ToEventContextProperty(Expression<Func<EventContext, object>> eventContextPropertyAccessor);
```

## When the fluent form is the only option

The clearest case is a child collection fed by events that carry **different key
properties**, which the single-key model-bound `[ChildrenFrom<T>]` cannot state:

```csharp
public void Define(IProjectionBuilderFor<<ReadModelName>> builder) =>
    builder
        .From<<AssignedEvent>>(from => from.UsingKey(@event => @event.<KeyA>))
        .From<<UpdatedEvent>>(from => from.UsingKey(@event => @event.<KeyB>));
```

Also reach for it for joins across several events, composite keys, initial
values, and conditional setters the attributes do not express.

## AutoMap default

`ProjectionBuilderFor` is constructed with AutoMap enabled, and the model-bound
builder sets it to enabled unless `[NoAutoMap]` is present on the read model.
Children and nested builders are created with an inherit setting, so they follow
the enclosing scope.
