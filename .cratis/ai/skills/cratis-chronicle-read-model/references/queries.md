<!-- cratis-ai-managed: skills/cratis-chronicle-read-model/references/queries.md -->
# Read-model queries

Verified against `Cratis.Arc.Core` and `Cratis.Arc.MongoDB` as checked out
alongside `Cratis.Chronicle` `18.3.0`. Confirm the exact Arc package version in
the consuming project before citing one.

## Where the pieces live

| Type | Namespace | Package |
| --- | --- | --- |
| `ReadModelAttribute` (`[ReadModel]`) | `Cratis.Arc.Queries.ModelBound` | `Cratis.Arc.Core` |
| `PathAttribute` (`[Path]`) | `Cratis.Arc.Queries.ModelBound` | `Cratis.Arc.Core` |
| the `Observe` extensions | `MongoDB.Driver` | `Cratis.Arc.MongoDB` |

`[ReadModel]` is a bare marker with **no constructor parameters**. `[Path]` takes
one `string path` and targets a class or a method.

The `Observe` extensions are deliberately declared in the `MongoDB.Driver`
namespace so they resolve from an existing `using MongoDB.Driver;`. They are not
Chronicle extensions — Chronicle declares no `Observe` on `IMongoCollection<T>`.

## Discovery rules

Arc scans the read-model type for **static** methods, public and non-public
alike. Instance methods are never discovered.

The declared return type is validated after unwrapping at most one `Task<>`:

| Accepted |
| --- |
| the read model type |
| an array of it |
| anything assignable to `IEnumerable<T>` of it |
| `IAsyncEnumerable<T>` of it |
| `ISubject<T>` of it |
| `ISubject<T>` of a collection of it |

Rejected: open generic methods, and **any concrete subject type**. The subject
check matches `typeof(ISubject<>)` as an open generic definition, so a method
declared as returning `Subject<T>` or `BehaviorSubject<T>` fails discovery
without an error at the call site. Declare `ISubject<...>`; construct whatever
you like inside.

## Snapshot queries

A snapshot query answers once with the current state.

```csharp
public static async Task<IEnumerable<<ReadModelName>>> All<PluralName>(
    IMongoCollection<<ReadModelName>> collection) =>
    await collection.Find(Builders<<ReadModelName>>.Filter.Empty).ToListAsync();

public static async Task<<ReadModelName>?> <SingularName>ById(
    <IdType> id,
    IMongoCollection<<ReadModelName>> collection) =>
    await collection.Find(model => model.Id == id).FirstOrDefaultAsync();
```

Name the method for what it returns — the name is the query's identity on the
generated client surface.

## Observable queries

An observable query pushes updates as projected state lands.

```csharp
public static ISubject<IEnumerable<<ReadModelName>>> Observe<PluralName>(
    IMongoCollection<<ReadModelName>> collection) =>
    collection.Observe();

public static ISubject<<ReadModelName>> Observe<SingularName>ById(
    <IdType> id,
    IMongoCollection<<ReadModelName>> collection) =>
    collection.ObserveById<<ReadModelName>, <IdType>>(id);
```

Return `ISubject<...>` directly. Do **not** return `Task<ISubject<...>>`.

## The `Observe` family — exact signatures

```csharp
// collection-valued
ISubject<IEnumerable<TDocument>> Observe<TDocument>(
    this IMongoCollection<TDocument> collection,
    Expression<Func<TDocument, bool>>? filter,
    FindOptions? options = null);

ISubject<IEnumerable<TDocument>> Observe<TDocument>(
    this IMongoCollection<TDocument> collection,
    FilterDefinition<TDocument>? filter = null,
    FindOptions? options = null);

// single-valued
ISubject<TDocument> ObserveSingle<TDocument>(
    this IMongoCollection<TDocument> collection,
    Expression<Func<TDocument, bool>>? filter,
    FindOptions? options = null);

ISubject<TDocument> ObserveSingle<TDocument>(
    this IMongoCollection<TDocument> collection,
    FilterDefinition<TDocument>? filter = null,
    FindOptions? options = null);

ISubject<TDocument> ObserveById<TDocument, TId>(
    this IMongoCollection<TDocument> collection,
    TId id);
```

Two details that catch people out:

- **`Observe` always returns a collection**, even with an equality filter. For a
  single document use `ObserveSingle` or `ObserveById`; a filtered `Observe`
  declared as `ISubject<TDocument>` does not compile.
- On the expression overload the `filter` parameter has **no default**. A bare
  `collection.Observe()` therefore binds the `FilterDefinition<TDocument>?`
  overload, which is the intended "everything" call.

Also available in the same place: `FindById<T, TId>` and
`FindByIdAsync<T, TId>`.

## Cost and composition

The behavior below was checked against **Arc v22.49.0**
([collection observation](https://github.com/Cratis/Arc/blob/v22.49.0/Documentation/backend/csharp/mongodb/observing-collections.md),
[implementation](https://github.com/Cratis/Arc/blob/v22.49.0/Source/DotNET/MongoDB/MongoCollectionExtensions.cs)).
Do not generalize it to every provider or custom observable.

- Per-collection `Observe` reads an initial filtered set, retains it in memory,
  and applies change-stream updates. It does **not** re-query the entire
  collection for every event. Updates/replacements perform a keyed membership
  lookup; paged removals can query to refill the page.
- Collection emissions still construct a snapshot of the retained set. In-memory
  joins, sorting, mapping, and serialization downstream can be expensive even
  without a full database re-query. Measure retained rows, payload size, change
  frequency, and concurrent subscriptions separately from database calls.
- A custom change callback that explicitly runs another query pays that query's
  cost on each invocation. Distinguish this recompute path from ordinary retained
  observation; inspect the actual callback before reporting a full scan.
- Filter by the requested entity, parent, tenant, or relevant state at the
  observation source; filtering after an unbounded `Observe()` retains too much.
  This narrows initial state and emissions, not necessarily every watched change:
  updates/deletes must still detect documents leaving the set. Test removal and
  filter-exit behavior as well as additions.
- For counts, do not retain rich documents and entire histories just to count
  them. Consider a slim projected counter or an explicit aggregate query with an
  appropriate refresh policy; preserve the count's actual semantics.
- Prefer a purpose-built projected summary for frequently recomputed joins.
  Query-time composition is supported, not forbidden. Bound each input and define
  count, paging, missing-related-data, and subscription-disposal behavior.
  In v22.49.0 auxiliary observations can use `ignoreQueryContext: true` so client
  paging/counts belong to the primary source. That option reads **all matching
  auxiliary rows**; it is not a performance limit. Confirm version support before
  using it, and keep an explicit narrow filter.

Use the **cratis-arc-query-paging** skill for source-side paging. Paging an
already materialized list does not bound database reads or retained memory.

## Custom routes

```csharp
[ReadModel]
[Path("<resource-path>")]
public record <ReadModelName>(...)
{
    [Path("<sub-path>")]
    public static Task<IEnumerable<<ReadModelName>>> <QueryName>(...) => ...;
}
```

`[Path]` is Arc's attribute and works at class or method level. ASP.NET's
`[Route]` belongs to controller-based endpoints and is not the mechanism here.

## Reading without a query surface

When only backend code reads the model, skip `[ReadModel]` entirely and use
`IEventStore.ReadModels.GetInstanceById<T>(key)`. Its declared return type is
non-nullable even though no instance may exist, so test the result. There is no
predicate or `IQueryable` surface on `IReadModels` — filtered reads belong on the
sink, which is what the query methods above use.
